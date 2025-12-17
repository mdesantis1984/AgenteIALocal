using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces.Agent;
using AgenteIALocal.Core.Interfaces;

namespace AgenteIALocal.Infrastructure.Agent
{
    /// <summary>
    /// Executes read-only agent actions using workspace context and the prompt builder.
    /// Returns IAgentResult describing the outcome. No side effects, no file writes, no AI calls.
    /// </summary>
    public class AgentActionExecutor
    {
        private readonly IWorkspaceContext workspaceContext;

        public AgentActionExecutor(IWorkspaceContext workspaceContext)
        {
            this.workspaceContext = workspaceContext;
        }

        public Task<IAgentResult> ExecuteAsync(IAgentAction action, CancellationToken cancellationToken)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            // Supported action types: idle, analyze-workspace
            switch (action.Type)
            {
                case "idle":
                    return Task.FromResult<IAgentResult>(new ExecutorResult(action.Id, true, "Idle: no operation performed.", null));

                case "analyze-workspace":
                    return Task.FromResult<IAgentResult>(AnalyzeWorkspace(action));

                default:
                    return Task.FromResult<IAgentResult>(new ExecutorResult(action.Id, false, null, "Unsupported action type."));
            }
        }

        private IAgentResult AnalyzeWorkspace(IAgentAction action)
        {
            try
            {
                var sol = workspaceContext?.Solution;
                var sb = new System.Text.StringBuilder();

                if (sol == null)
                {
                    sb.AppendLine("No solution loaded.");
                }
                else
                {
                    sb.AppendLine("Solution: " + (sol.Name ?? "(unknown)"));
                    sb.AppendLine("Path: " + (sol.Path ?? "(unknown)"));
                    sb.AppendLine("Projects: " + (sol.Projects == null ? "0" : sol.Projects.Count.ToString()));

                    if (sol.Projects != null && sol.Projects.Count > 0)
                    {
                        int take = Math.Min(10, sol.Projects.Count);
                        sb.AppendLine("Sample projects:");
                        for (int i = 0; i < take; i++)
                        {
                            var p = sol.Projects[i];
                            sb.AppendLine($"- {p.Name} ({p.Path}) [{p.Language}]");
                        }
                    }
                }

                var openDocs = workspaceContext?.OpenDocuments;
                if (openDocs == null || openDocs.Count == 0)
                {
                    sb.AppendLine("Open documents: none");
                }
                else
                {
                    sb.AppendLine("Open documents:");
                    foreach (var d in openDocs.Take(10))
                    {
                        sb.AppendLine($"- {d.FileName} ({d.Path}) [{d.Language}] Dirty={d.IsDirty}");
                    }
                }

                var active = workspaceContext?.ActiveDocument;
                if (active != null)
                {
                    sb.AppendLine("Active document: " + active.FileName + " (" + active.Path + ")");
                }

                // Build a prompt using the prompt builder for informational purposes (not sent to any AI)
                var prompt = AgentPromptBuilder.BuildPrompt(new AgentContextSnapshot(), action);

                sb.AppendLine();
                sb.AppendLine("Generated prompt preview:");
                sb.AppendLine(prompt);

                return new ExecutorResult(action.Id, true, sb.ToString().Trim(), null);
            }
            catch (Exception ex)
            {
                return new ExecutorResult(action.Id, false, null, ex.Message);
            }
        }

        /// <summary>
        /// A minimal snapshot implementation to feed the prompt builder when building previews.
        /// Does not attempt to provide a full workspace snapshot; keeps data opaque as required.
        /// </summary>
        private class AgentContextSnapshot : IAgentContext
        {
            public DateTimeOffset Timestamp { get { return DateTimeOffset.UtcNow; } }
            public string Goal { get { return null; } }
            public object WorkspaceSnapshot { get { return null; } }
            public object Memory { get { return null; } }
        }

        private class ExecutorResult : IAgentResult
        {
            public ExecutorResult(string actionId, bool success, string output, string error)
            {
                ActionId = actionId;
                Success = success;
                Output = output;
                Error = error;
            }

            public string ActionId { get; private set; }
            public bool Success { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }
        }
    }
}
