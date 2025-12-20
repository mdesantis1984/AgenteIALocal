using System.Threading;
using System.Threading.Tasks;

namespace AgenteIALocal.Core.Agents
{
    public interface IAgentClient
    {
        Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken);
    }
}
