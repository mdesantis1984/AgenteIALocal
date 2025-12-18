using System;
using System.Text;
using AgenteIALocal.Core.Interfaces.Agent;

namespace AgenteIALocal.Infrastructure.Agent
{
    /// <summary>
    /// Builds deterministic, structured prompts from an agent context and a single action.
    /// Pure function: no side effects, no AI calls, no IO.
    /// </summary>
    public static class AgentPromptBuilder
    {
        /// <summary>
        /// Build a prompt string containing system header, goal, action and a safe stringified context snapshot.
        /// </summary>
        public static string BuildPrompt(IAgentContext context, IAgentAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var sb = new StringBuilder();

            // System header with simple constraints
            sb.AppendLine("SYSTEM: You are an offline deterministic agent planner helper. Follow instructions exactly.");
            sb.AppendLine("CONSTRAINTS: Provide a concise plain-text response. Do not call external services.");
            sb.AppendLine();

            // Goal
            var goal = context?.Goal;
            if (!string.IsNullOrWhiteSpace(goal))
            {
                sb.AppendLine("GOAL:");
                sb.AppendLine(goal.Trim());
                sb.AppendLine();
            }

            // Action
            sb.AppendLine("ACTION:");
            sb.AppendLine($"Id: {action.Id}");
            sb.AppendLine($"Type: {action.Type}");
            sb.AppendLine($"Description: {action.Description}");
            sb.AppendLine();

            // Parameters
            sb.AppendLine("PARAMETERS:");
            if (action.Parameters != null && action.Parameters.Count > 0)
            {
                foreach (var kv in action.Parameters)
                {
                    var value = kv.Value ?? string.Empty;
                    sb.AppendLine($"- {kv.Key}: {SafeToString(value)}");
                }
            }
            else
            {
                sb.AppendLine("(none)");
            }

            sb.AppendLine();

            // Context snapshot (opaque). Print type name and ToString() for safety.
            sb.AppendLine("CONTEXT SNAPSHOT:");
            if (context != null && context.WorkspaceSnapshot != null)
            {
                var ws = context.WorkspaceSnapshot;
                sb.AppendLine($"Type: {ws.GetType().FullName}");
                sb.AppendLine("Value: ");
                sb.AppendLine(SafeToString(ws));
            }
            else
            {
                sb.AppendLine("(no context snapshot)");
            }

            sb.AppendLine();
            sb.AppendLine("INSTRUCTION: Return a single plain-text plan or result. Do not include metadata.");

            var prompt = sb.ToString();
            return string.IsNullOrWhiteSpace(prompt) ? "" : prompt.Trim();
        }

        private static string SafeToString(object obj)
        {
            try
            {
                if (obj == null) return string.Empty;
                var s = obj.ToString();
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                // Limit length to keep prompt size reasonable
                const int max = 1024;
                if (s.Length <= max) return s;
                return s.Substring(0, max) + "...";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
