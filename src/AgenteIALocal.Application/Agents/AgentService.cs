using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;

namespace AgenteIALocal.Application.Agents
{
    public sealed class AgentService : IAgentService
    {
        private readonly IAgentClient client;

        public AgentService(IAgentClient client)
        {
            this.client = client;
        }

        public Task<AgentResponse> RunAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new System.ArgumentNullException(nameof(request));
            return client.ExecuteAsync(request, cancellationToken);
        }

        // Keep original convenience method for compatibility
        public Task<AgentResponse> RunAsync(string prompt, CancellationToken cancellationToken)
        {
            var req = new AgentRequest { Prompt = prompt };
            return RunAsync(req, cancellationToken);
        }
    }
}
