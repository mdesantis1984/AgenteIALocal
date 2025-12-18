using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces.Agent;

namespace AgenteIALocal.Infrastructure.Agent
{
    /// <summary>
    /// Deterministic basic planner: returns a single action based on the presence of a Goal.
    /// - If Goal is null or empty -> Idle action
    /// - If Goal is provided -> AnalyzeWorkspace action
    /// This planner is pure and has no side effects.
    /// </summary>
    public class BasicAgentPlanner : IAgentPlanner
    {
        public string Name => "BasicAgentPlanner";

        public Task<IReadOnlyList<IAgentAction>> PlanAsync(IAgentContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IAgentAction action;
            if (string.IsNullOrWhiteSpace(context.Goal))
            {
                action = new PlannerAction("idle", "Idle", "Idle when no goal is provided", null);
            }
            else
            {
                var parameters = new Dictionary<string, object>
                {
                    { "goal", context.Goal }
                };

                action = new PlannerAction("analyze-workspace", "AnalyzeWorkspace", "Analyze the workspace for the provided goal", parameters);
            }

            IReadOnlyList<IAgentAction> result = new List<IAgentAction> { action };
            return Task.FromResult(result);
        }

        private class PlannerAction : IAgentAction
        {
            public PlannerAction(string id, string type, string description, IDictionary<string, object> parameters)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Type = type ?? throw new ArgumentNullException(nameof(type));
                Description = description ?? string.Empty;
                Parameters = parameters == null ? new Dictionary<string, object>() : new Dictionary<string, object>(parameters);
            }

            public string Id { get; }
            public string Description { get; }
            public string Type { get; }
            public IReadOnlyDictionary<string, object> Parameters { get; }
        }
    }
}
