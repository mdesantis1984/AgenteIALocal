using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;

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

                var uri = endpointResolver.GetChatCompletionsEndpoint();
                if (uri == null)
                {
                    return new AgentResponse { IsSuccess = false, Error = "Endpoint not configured" };
                }

                try
                {
                    // Build minimal JSON payload without external JSON libs
                    var sb = new StringBuilder();
                    sb.Append('{');
                    if (!string.IsNullOrEmpty(model)) sb.AppendFormat("\"model\":\"{0}\",", EscapeJson(model));
                    sb.Append("\"messages\":[{\"role\":\"user\",\"content\":\"");
                    sb.Append(EscapeJson(request.Prompt ?? string.Empty));
                    sb.Append("\"}]");
                    sb.Append('}');

                    var payloadJson = sb.ToString();
                    var bytes = Encoding.UTF8.GetBytes(payloadJson);

                    var req = (HttpWebRequest)WebRequest.Create(uri);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    if (!string.IsNullOrEmpty(apiKey)) req.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey;
                    req.ContentLength = bytes.Length;

                    using (var s = req.GetRequestStream())
                    {
                        s.Write(bytes, 0, bytes.Length);
                    }

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        var text = sr.ReadToEnd();
                        if (resp.StatusCode != HttpStatusCode.OK)
                        {
                            return new AgentResponse { IsSuccess = false, Error = $"HTTP {resp.StatusCode}: {text}" };
                        }

                        try
                        {
                            var extracted = ExtractFirstChoiceContent(text);
                            return new AgentResponse { IsSuccess = true, Content = extracted };
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
                            return new AgentResponse { IsSuccess = false, Error = wex.Message + " - " + body };
                        }
                    }
                    catch
                    {
                        return new AgentResponse { IsSuccess = false, Error = wex.Message };
                    }
                }
                catch (Exception ex)
                {
                    return new AgentResponse { IsSuccess = false, Error = ex.Message };
                }
            }, cancellationToken);
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
            var end = responseText.IndexOf('"', start + 1);
            if (end < 0) return null;

            var content = responseText.Substring(start + 1, end - start - 1);
            // unescape basic sequences
            content = content.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\\", "\\");
            return content;
        }
    }
}
