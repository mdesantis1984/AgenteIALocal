using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces.AI;
using AgenteIALocal.Core.Models.AI;

namespace AgenteIALocal.Infrastructure.AI
{
    /// <summary>
    /// Minimal OpenAI provider adapter implementing IAIProvider using HttpWebRequest
    /// to avoid adding System.Net.Http or System.Text.Json references in the project.
    /// </summary>
    public class OpenAIProvider : IAIProvider, IDisposable
    {
        private readonly OpenAIOptions options;
        private bool disposed;

        public OpenAIProvider(OpenAIOptions options = null)
        {
            this.options = options ?? new OpenAIOptions();

            Name = "OpenAI";
            AvailableModels = new List<IAIModel>
            {
                new OpenAIModel("gpt-4.1-mini", "GPT-4.1 Mini", Name)
            };
        }

        public string Name { get; }

        public IReadOnlyCollection<IAIModel> AvailableModels { get; }

        public Task<IAIResponse> ExecuteAsync(IAIRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var effectiveApiKey = options.GetEffectiveApiKey();
            if (string.IsNullOrEmpty(effectiveApiKey))
            {
                return Task.FromResult<IAIResponse>(new OpenAIResponse
                {
                    Content = null,
                    IsSuccess = false,
                    ErrorMessage = "API key not configured. Set OPENAI_API_KEY environment variable or provide OpenAIOptions.ApiKey.",
                    Duration = TimeSpan.Zero
                });
            }

            return Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var payload = BuildPayload(request);
                    var bytes = Encoding.UTF8.GetBytes(payload);

                    var url = options.BaseUrl;
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.Headers["Authorization"] = "Bearer " + effectiveApiKey;

                    req.ContentLength = bytes.Length;
                    using (var reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(bytes, 0, bytes.Length);
                    }

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        var responseText = reader.ReadToEnd();

                        if (resp.StatusCode != HttpStatusCode.OK)
                        {
                            return (IAIResponse)new OpenAIResponse
                            {
                                Content = null,
                                IsSuccess = false,
                                ErrorMessage = $"OpenAI error: {resp.StatusCode} - {responseText}",
                                Duration = sw.Elapsed
                            };
                        }

                        var extracted = ExtractFirstChoiceContent(responseText);
                        return (IAIResponse)new OpenAIResponse
                        {
                            Content = extracted,
                            IsSuccess = true,
                            ErrorMessage = null,
                            Duration = sw.Elapsed
                        };
                    }
                }
                catch (WebException wex)
                {
                    string body = null;
                    try
                    {
                        using (var r = new StreamReader(wex.Response?.GetResponseStream() ?? Stream.Null))
                        {
                            body = r.ReadToEnd();
                        }
                    }
                    catch { }

                    return (IAIResponse)new OpenAIResponse
                    {
                        Content = null,
                        IsSuccess = false,
                        ErrorMessage = wex.Message + (string.IsNullOrEmpty(body) ? string.Empty : " - " + body),
                        Duration = sw.Elapsed
                    };
                }
                catch (Exception ex)
                {
                    return (IAIResponse)new OpenAIResponse
                    {
                        Content = null,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        Duration = sw.Elapsed
                    };
                }
                finally
                {
                    sw.Stop();
                }
            }, cancellationToken);
        }

        private string BuildPayload(IAIRequest request)
        {
            // Very small JSON builder with basic escaping
            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendFormat("\"model\":\"{0}\",", JsonEscape(request.Model?.Id ?? "gpt-4.1-mini"));

            sb.Append("\"messages\":[");
            var first = true;
            if (!string.IsNullOrEmpty(request.SystemContext))
            {
                sb.AppendFormat("{0}{{\"role\":\"system\",\"content\":\"{1}\"}}", first ? string.Empty : ",", JsonEscape(request.SystemContext));
                first = false;
            }

            if (request.Parameters != null && request.Parameters.TryGetValue("messages", out var rawMessages) && rawMessages is IEnumerable<AIMessage> aiMessages)
            {
                foreach (var m in aiMessages)
                {
                    sb.AppendFormat("{0}{{\"role\":\"{1}\",\"content\":\"{2}\"}}", first ? string.Empty : ",", MapRole(m.Role), JsonEscape(m.Content));
                    first = false;
                }
            }
            else
            {
                sb.AppendFormat("{0}{{\"role\":\"user\",\"content\":\"{1}\"}}", first ? string.Empty : ",", JsonEscape(request.Prompt ?? string.Empty));
                first = false;
            }

            sb.Append("]");

            // Optional parameters
            if (request.Parameters != null && request.Parameters.TryGetValue("temperature", out var temp) && temp is double dtemp)
            {
                sb.AppendFormat(",\"temperature\":{0}", dtemp.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (request.Parameters != null && request.Parameters.TryGetValue("max_tokens", out var mt) && mt is int imt)
            {
                sb.AppendFormat(",\"max_tokens\":{0}", imt);
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static string JsonEscape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string ExtractFirstChoiceContent(string responseText)
        {
            if (string.IsNullOrEmpty(responseText)) return null;

            // Naive extraction: look for "content": and return the following string value
            var marker = "\"content\"";
            var idx = responseText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var colon = responseText.IndexOf(':', idx);
            if (colon < 0) return null;
            var startQuote = responseText.IndexOf('"', colon);
            if (startQuote < 0) return null;
            var endQuote = responseText.IndexOf('"', startQuote + 1);
            if (endQuote < 0) return null;
            var content = responseText.Substring(startQuote + 1, endQuote - startQuote - 1);
            // Unescape basic sequences
            content = content.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\\", "\\");
            return content;
        }

        private static string MapRole(AIMessageRole role)
        {
            switch (role)
            {
                case AIMessageRole.System:
                    return "system";
                case AIMessageRole.User:
                    return "user";
                case AIMessageRole.Assistant:
                    return "assistant";
                default:
                    return "user";
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
        }

        private class OpenAIModel : IAIModel
        {
            public OpenAIModel(string id, string displayName, string provider)
            {
                Id = id;
                DisplayName = displayName;
                Provider = provider;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Provider { get; }
        }

        private class OpenAIResponse : IAIResponse
        {
            public string Content { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}
