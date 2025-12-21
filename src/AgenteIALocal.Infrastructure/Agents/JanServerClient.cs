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
            return Task.FromResult(new AgentResponse
            {
                IsSuccess = true,
                Content = "[JanServer stub response] " + (request?.Prompt ?? string.Empty)
            });
        }
    }
}
