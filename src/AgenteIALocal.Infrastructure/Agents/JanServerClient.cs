using System;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocal.Infrastructure.Agents
{
    // Stub client: does not perform real HTTP calls, returns canned response
    public class JanServerClient : IAgentClient
    {
        private readonly JanServerSettings settings;

        public JanServerClient(JanServerSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var final = "[JanServer stub response] " + (request?.Prompt ?? string.Empty);

                // Optional streaming simulation for UI wiring validation
                if (request != null && request.Stream && request.OnDelta != null)
                {
                    try
                    {
                        const int chunkSize = 16;
                        for (var i = 0; i < final.Length; i += chunkSize)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            var len = Math.Min(chunkSize, final.Length - i);
                            var chunk = final.Substring(i, len);
                            try { request.OnDelta(chunk); } catch { }
                        }
                    }
                    catch { }
                }

                return new AgentResponse
                {
                    IsSuccess = true,
                    Content = final
                };
            }, cancellationToken);
        }
    }
}
