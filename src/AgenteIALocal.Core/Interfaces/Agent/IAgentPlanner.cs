using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgenteIALocal.Core.Interfaces.Agent
{
    /// <summary>
    /// Decides next action(s) based on the provided agent context.
    /// Pure contract with no side effects.
    /// </summary>
    public interface IAgentPlanner
    {
        /// <summary>
        /// Planner display name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Produce a list of actions to execute for the given context.
        /// Implementations should be read-only and side-effect free.
        /// </summary>
        Task<IReadOnlyList<IAgentAction>> PlanAsync(IAgentContext context, CancellationToken cancellationToken);
    }
}
