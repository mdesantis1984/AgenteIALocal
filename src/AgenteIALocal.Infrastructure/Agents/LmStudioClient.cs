using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;
using AgenteIALocal.Core.Networking;

namespace AgenteIALocal.Infrastructure.Agents
{
    public class LmStudioClient : IAgentClient
    {
        private readonly LmStudioSettings settings;
        private readonly IAgentEndpointResolver endpointResolver;

        public LmStudioClient(LmStudioSettings settings, IAgentEndpointResolver endpointResolver)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.endpointResolver = endpointResolver ?? throw new ArgumentNullException(nameof(endpointResolver));
        }

        public Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return Task.Run(() =>
            {
                var model = settings.Model ?? string.Empty;
                var apiKey = settings.ApiKey ?? string.Empty;

                // H1: ensure authorization header present; default to 'lm-studio' if empty
                if (string.IsNullOrEmpty(apiKey)) apiKey = "lm-studio";

                var useStream = request.Stream;

                var uri = endpointResolver.GetChatCompletionsEndpoint();
                if (uri == null)
                {
                    return new AgentResponse { IsSuccess = false, Error = "Endpoint not configured" };
                }

                try
                {
                    // Build stable JSON payload using StringBuilder to avoid external deps
                    var sb = new StringBuilder();
                    sb.Append('{');
                    var wroteField = false;

                    if (!string.IsNullOrEmpty(model))
                    {
                        sb.AppendFormat("\"model\":\"{0}\"", EscapeJson(model));
                        wroteField = true;
                    }

                    // H3: stable payload additions
                    if (wroteField) sb.Append(',');
                    sb.Append("\"stream\":"); sb.Append(useStream ? "true" : "false"); sb.Append(',');
                    sb.Append("\"max_tokens\":512,");

                    sb.Append("\"messages\":[{");
                    sb.Append("\"role\":\"user\",\"content\":\"");
                    sb.Append(EscapeJson(request.Prompt ?? string.Empty));
                    // Close the content string and the message object/array: "}
                    sb.Append("\"}]");

                    sb.Append('}');

                    var payloadJson = sb.ToString();
                    var bytes = Encoding.UTF8.GetBytes(payloadJson);

                    var req = (HttpWebRequest)WebRequest.Create(uri);
                    req.Method = "POST";
                    req.ContentType = "application/json"; // Content-Type header
                    // H2: Accept header
                    req.Accept = useStream ? "text/event-stream" : "application/json";
                    // H1: Authorization: Bearer <apiKey>
                    req.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey;

                    // Increase the default timeout (default ~100s). Apply both main timeout and read/write timeout.
                    req.Timeout = (int)HttpTimeouts.LmStudioChatRequestTimeout.TotalMilliseconds;
                    req.ReadWriteTimeout = (int)HttpTimeouts.LmStudioChatRequestTimeout.TotalMilliseconds;

                    // Do NOT add any new headers that change network behavior (correlation used only for logging)

                    req.ContentLength = bytes.Length;

                    using (var s = req.GetRequestStream())
                    {
                        s.Write(bytes, 0, bytes.Length);
                    }

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        if (useStream)
                        {
                            if (resp.StatusCode != HttpStatusCode.OK)
                            {
                                var errText = sr.ReadToEnd();
                                return new AgentResponse { IsSuccess = false, Error = $"HTTP {resp.StatusCode}: {errText}" };
                            }

                            var contentSb = new StringBuilder();
                            var rawSb = new StringBuilder();

                            // PERF: batch delta callbacks to avoid UI overload
                            var deltaBatch = new StringBuilder();
                            var deltaFlushSw = Stopwatch.StartNew();

                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return new AgentResponse { IsSuccess = false, Error = "Canceled" };
                                }

                                if (line.Length == 0) continue;
                                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

                                var data = line.Substring(5).Trim();
                                if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                                {
                                    break;
                                }

                                rawSb.AppendLine(data);

                                var delta = ExtractStreamDeltaContent(data);
                                if (!string.IsNullOrEmpty(delta))
                                {
                                    contentSb.Append(delta);

                                    deltaBatch.Append(delta);
                                    var ms = deltaFlushSw.ElapsedMilliseconds;
                                    if (deltaBatch.Length >= 128 || ms >= 30)
                                    {
                                        SafeInvokeDelta(request, deltaBatch.ToString());
                                        deltaBatch.Clear();
                                        deltaFlushSw.Restart();
                                    }
                                }
                            }

                            if (deltaBatch.Length > 0)
                            {
                                SafeInvokeDelta(request, deltaBatch.ToString());
                                deltaBatch.Clear();
                            }

                            return new AgentResponse
                            {
                                IsSuccess = true,
                                Content = contentSb.ToString(),
                                RawResponse = rawSb.ToString()
                            };
                        }

                        var text = sr.ReadToEnd();

                        if (resp.StatusCode != HttpStatusCode.OK)
                        {
                            return new AgentResponse { IsSuccess = false, Error = $"HTTP {resp.StatusCode}: {text}" };
                        }

                        // H4: defensive parsing - ensure JSON object
                        var trimmed = (text ?? string.Empty).TrimStart();
                        if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith("{"))
                        {
                            // return clear error without throwing
                            var sample = trimmed.Length > 300 ? trimmed.Substring(0, 300) + "..." : trimmed;
                            return new AgentResponse { IsSuccess = false, Error = "Non-JSON response from LM Studio: " + sample };
                        }

                        try
                        {
                            var extracted = ExtractFirstChoiceContent(text);

                            // Token usage (best-effort): usage.prompt_tokens / usage.completion_tokens / usage.total_tokens
                            var totalTokens = ExtractIntField(text, "total_tokens");
                            var promptTokens = ExtractIntField(text, "prompt_tokens");
                            var completionTokens = ExtractIntField(text, "completion_tokens");

                            // Prefer direct properties (these exist in sprint-012-commit-08). Keep reflection fallbacks for robustness.
                            var ar = new AgentResponse
                            {
                                IsSuccess = true,
                                Content = extracted,
                                PromptTokens = promptTokens,
                                CompletionTokens = completionTokens,
                                TotalTokens = totalTokens,
                                RawResponse = text
                            };

                            // Reflection fallbacks (no harm if properties already set)
                            TrySetIntAny(ar, totalTokens, "TotalTokens", "Tokens", "TokensUsed", "TotalTokenCount", "TokenCount");
                            TrySetIntAny(ar, promptTokens, "PromptTokens", "PromptTokenCount");
                            TrySetIntAny(ar, completionTokens, "CompletionTokens", "CompletionTokenCount");
                            TrySetStringAny(ar, text, "RawResponse", "RawJson", "RawResponseJson", "ResponseJson", "ResponseText", "Body", "Json");

                            return ar;
                        }
                        catch (Exception ex)
                        {
                            return new AgentResponse { IsSuccess = false, Error = "Invalid JSON response: " + ex.Message };
                        }
                    }
                }
                catch (WebException wex)
                {
                    try
                    {
                        using (var sr = new StreamReader(wex.Response?.GetResponseStream() ?? Stream.Null))
                        {
                            var body = sr.ReadToEnd();
                            // Distinguish timeout vs other web exceptions
                            if (wex.Status == WebExceptionStatus.Timeout)
                            {
                                return new AgentResponse { IsSuccess = false, Error = $"Se excedió el tiempo de espera ({(int)HttpTimeouts.LmStudioChatRequestTimeout.TotalSeconds}s): {wex.Message} - {body}" };
                            }

                            return new AgentResponse { IsSuccess = false, Error = wex.Message + " - " + body };
                        }
                    }
                    catch
                    {
                        if (wex.Status == WebExceptionStatus.Timeout)
                        {
                            return new AgentResponse { IsSuccess = false, Error = $"Se excedió el tiempo de espera ({(int)HttpTimeouts.LmStudioChatRequestTimeout.TotalSeconds}s): {wex.Message}" };
                        }
                        return new AgentResponse { IsSuccess = false, Error = wex.Message };
                    }
                }
                catch (OperationCanceledException)
                {
                    return new AgentResponse { IsSuccess = false, Error = "Operación cancelada" };
                }
                catch (Exception ex)
                {
                    return new AgentResponse { IsSuccess = false, Error = ex.Message };
                }
            }, cancellationToken);
        }

        private static int? ExtractIntField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return null;

            // Best-effort scan for: "fieldName" : 123
            var key = "\"" + fieldName + "\"";
            var i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;

            i = json.IndexOf(':', i);
            if (i < 0) return null;
            i++;

            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

            var start = i;
            while (i < json.Length && char.IsDigit(json[i])) i++;

            if (i <= start) return null;

            if (int.TryParse(json.Substring(start, i - start), out var val)) return val;
            return null;
        }

        private static void TrySetIntAny(object target, int? value, params string[] propNames)
        {
            if (target == null) return;
            if (!value.HasValue) return;
            if (propNames == null || propNames.Length == 0) return;

            foreach (var name in propNames)
            {
                try
                {
                    var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (p == null || !p.CanWrite) continue;

                    var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                    if (t == typeof(int))
                    {
                        p.SetValue(target, value.Value, null);
                        return;
                    }

                    if (t == typeof(long))
                    {
                        p.SetValue(target, (long)value.Value, null);
                        return;
                    }

                    if (t == typeof(string))
                    {
                        p.SetValue(target, value.Value.ToString(), null);
                        return;
                    }
                }
                catch { }
            }
        }

        private static void TrySetStringAny(object target, string value, params string[] propNames)
        {
            if (target == null) return;
            if (string.IsNullOrEmpty(value)) return;
            if (propNames == null || propNames.Length == 0) return;

            foreach (var name in propNames)
            {
                try
                {
                    var p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (p == null || !p.CanWrite) continue;

                    if (p.PropertyType == typeof(string))
                    {
                        p.SetValue(target, value, null);
                        return;
                    }
                }
                catch { }
            }
        }

        
        private static void SafeInvokeDelta(AgentRequest request, string delta)
        {
            try
            {
                var cb = request?.OnDelta;
                if (cb == null) return;
                cb(delta);
            }
            catch
            {
                // ignore callback errors
            }
        }

        private static string ExtractStreamDeltaContent(string jsonChunk)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonChunk)) return null;

                // OpenAI-compatible SSE chunk commonly contains: choices[0].delta.content
                // We best-effort extract the first "content":"...".
                var contentIdx = jsonChunk.IndexOf("content", StringComparison.OrdinalIgnoreCase);
                if (contentIdx < 0) return null;

                var colon = jsonChunk.IndexOf(':', contentIdx);
                if (colon < 0) return null;

                // Skip whitespace
                var i = colon + 1;
                while (i < jsonChunk.Length && char.IsWhiteSpace(jsonChunk[i])) i++;

                if (i >= jsonChunk.Length) return null;

                if (jsonChunk[i] == 'n') return null; // null

                if (jsonChunk[i] != '"') return null;
                var start = i + 1;
                var end = FindJsonStringEnd(jsonChunk, start);
                if (end < 0) return null;

                var raw = jsonChunk.Substring(start, end - start);
                return UnescapeJsonString(raw);
            }
            catch
            {
                return null;
            }
        }

        private static int FindJsonStringEnd(string s, int startIndex)
        {
            var escaped = false;
            for (var i = startIndex; i < s.Length; i++)
            {
                var c = s[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"') return i;
            }

            return -1;
        }

        private static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder(s.Length);
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (i + 1 >= s.Length)
                {
                    sb.Append('\\');
                    break;
                }

                var n = s[++i];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < s.Length)
                        {
                            var hex = s.Substring(i + 1, 4);
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                        }
                        break;
                    default:
                        sb.Append(n);
                        break;
                }
            }

            return sb.ToString();
        }

private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string ExtractFirstChoiceContent(string responseText)
        {
            if (string.IsNullOrEmpty(responseText)) return null;

            // Best-effort extraction of choices[0].message.content or choices[0].text
            var choicesIdx = responseText.IndexOf("\"choices\"", StringComparison.OrdinalIgnoreCase);
            if (choicesIdx < 0) return null;

            var contentIdx = responseText.IndexOf("\"content\"", choicesIdx, StringComparison.OrdinalIgnoreCase);
            if (contentIdx < 0)
            {
                contentIdx = responseText.IndexOf("\"text\"", choicesIdx, StringComparison.OrdinalIgnoreCase);
                if (contentIdx < 0) return null;
            }

            var colon = responseText.IndexOf(':', contentIdx);
            if (colon < 0) return null;
            var start = responseText.IndexOf('"', colon);
            if (start < 0) return null;

            // find the closing quote, but handle escaped quotes
            var i = start + 1;
            var sb = new StringBuilder();
            bool escaped = false;
            for (; i < responseText.Length; i++)
            {
                var ch = responseText[i];
                if (escaped)
                {
                    sb.Append(ch);
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"') break;

                sb.Append(ch);
            }

            if (i >= responseText.Length) return null;

            var content = sb.ToString();
            // unescape basic sequences
            content = content.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\\", "\\");
            return content;
        }
    }
}
