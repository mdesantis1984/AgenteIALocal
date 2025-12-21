using System;
using System.Diagnostics;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;

namespace AgenteIALocalVSIX
{
    /// <summary>
    /// Static composition root for the VSIX. Responsible for providing a lightweight
    /// AgentService instance at runtime. Composition is idempotent and fail-safe.
    /// </summary>
    internal static class AgentComposition
    {
        private static readonly object sync = new object();
        private static bool composed = false;

        // Publicly accessible service used by ToolWindow and other VSIX components.
        public static IAgentService AgentService { get; private set; }

        // Optional logger hook that consumers can set. When null, Trace is used as fallback.
        public static Action<string> Logger { get; set; }

        public static void EnsureComposition()
        {
            if (composed) return;

            lock (sync)
            {
                if (composed) return;

                LogInfo("[AgentComposition] Composition start.");

                try
                {
                    // Manual composition: provide a mock agent that delegates to MockCopilotExecutor.
                    AgentService = new MockAgentService();

                    composed = true;

                    LogInfo("[AgentComposition] Composition success.");
                }
                catch (Exception ex)
                {
                    AgentService = null;
                    composed = true; // mark as composed to avoid retrying repeatedly
                    LogError($"[AgentComposition] Composition failure: {ex}");
                }
            }
        }

        private static void LogInfo(string message)
        {
            if (Logger != null)
            {
                try { Logger.Invoke(message); } catch { /* swallow logging errors */ }
                return;
            }

            Trace.TraceInformation(message);
        }

        private static void LogError(string message)
        {
            if (Logger != null)
            {
                try { Logger.Invoke("ERROR: " + message); } catch { }
                return;
            }

            Trace.TraceError(message);
        }

        // Minimal agent service interface local to VSIX project.
        internal interface IAgentService
        {
            CopilotResponse Execute(CopilotRequest req);
        }

        // Mock implementation that delegates to the existing MockCopilotExecutor.
        private class MockAgentService : IAgentService
        {
            public CopilotResponse Execute(CopilotRequest req)
            {
                return MockCopilotExecutor.Execute(req);
            }
        }
    }
}
