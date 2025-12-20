using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                    var payload = new
                    {
                        model = model,
                        messages = new[] { new { role = "user", content = request.Prompt ?? string.Empty } }
                    };

                    var payloadJson = JsonConvert.SerializeObject(payload);
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

                        // Parse OpenAI-compatible response: choices[0].message.content or choices[0].text
                        try
                        {
                            var j = JObject.Parse(text);
                            var content = (string)j.SelectToken("$.choices[0].message.content") ?? (string)j.SelectToken("$.choices[0].text");
                            return new AgentResponse { IsSuccess = true, Content = content };
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
    }
}
