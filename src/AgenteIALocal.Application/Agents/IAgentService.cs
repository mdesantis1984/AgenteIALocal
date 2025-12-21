using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Agents;

namespace AgenteIALocal.Application.Agents
{
    public interface IAgentService
    {
        Task<AgentResponse> RunAsync(string prompt, CancellationToken cancellationToken);
    }
}
