using System;
using System.Diagnostics;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;
using System.Threading.Tasks;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Core.Settings;
using AgenteIALocal.Infrastructure.Agents;

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
                    // Default: assign mock immediately to avoid blocking UI
                    AgentService = new MockAgentService();

                    // Attempt to build real backend in background; if it fails, keep mock
                    Task.Run(() => TryComposeRealBackend());

                    composed = true;

                    LogInfo("[AgentComposition] Composition scheduled.");
                }
                catch (Exception ex)
                {
                    AgentService = new MockAgentService();
                    composed = true; // mark as composed to avoid retrying repeatedly
                    LogError($"[AgentComposition] Composition failure: {ex}");
                }
            }
        }

        private static void TryComposeRealBackend()
        {
            try
            {
                // Read settings from VSIX local settings store (file-based). This may perform minimal IO but runs off-UI-thread.
                var vsixSettings = AgentSettingsStore.Load();
                if (vsixSettings == null)
                {
                    LogInfo("[AgentComposition] No VSIX settings found; keeping mock.");
                    return;
                }

                // Determine active server
                var activeId = vsixSettings.ActiveServerId;
                if (string.IsNullOrEmpty(activeId) || vsixSettings.Servers == null)
                {
                    LogInfo("[AgentComposition] No active server configured; keeping mock.");
                    return;
                }

                var srv = vsixSettings.Servers.Find(s => string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    LogInfo("[AgentComposition] Active server entry not found; keeping mock.");
                    return;
                }

                // Provider selection: expect provider string 'lmstudio' for LM Studio
                if (string.IsNullOrEmpty(srv.Provider) || !srv.Provider.Equals("lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    LogInfo("[AgentComposition] Active provider is not lmstudio; keeping mock.");
                    return;
                }

                // Validate base URL
                if (string.IsNullOrWhiteSpace(srv.BaseUrl))
                {
                    LogInfo("[AgentComposition] LM Studio BaseUrl empty; keeping mock.");
                    return;
                }

                // Map VSIX server config to Core LmStudioSettings
                var lmSettings = new LmStudioSettings
                {
                    BaseUrl = srv.BaseUrl ?? string.Empty,
                    ApiKey = srv.ApiKey ?? string.Empty,
                    Model = srv.Model ?? string.Empty,
                    ChatCompletionsPath = "/v1/chat/completions"
                };

                // Create endpoint resolver and client (no IO in constructors)
                var resolver = new LmStudioEndpointResolver(lmSettings);
                var client = new LmStudioClient(lmSettings, resolver);

                // Create application-level AgentService
                var appService = new AgenteIALocal.Application.Agents.AgentService(client);

                // Wrap with adapter that implements the VSIX AgentService contract
                var adapter = new CoreAgentServiceAdapter(appService);

                // Replace the AgentService atomically
                AgentService = adapter;

                LogInfo("[AgentComposition] Real LM Studio backend composed and active.");
            }
            catch (Exception ex)
            {
                try { LogInfo("[AgentComposition] Real backend composition failed: " + ex.Message); } catch { }
                // keep existing mock
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

        // Adapter that exposes the Core/Application async AgentService through the VSIX sync IAgentService
        private sealed class CoreAgentServiceAdapter : IAgentService
        {
            private readonly AgenteIALocal.Application.Agents.IAgentService appService;

            public CoreAgentServiceAdapter(AgenteIALocal.Application.Agents.IAgentService appService)
            {
                this.appService = appService ?? throw new ArgumentNullException(nameof(appService));
            }

            public CopilotResponse Execute(CopilotRequest req)
            {
                try
                {
                    // Build a prompt string from CopilotRequest fields (minimal mapping)
                    var prompt = (req?.Action ?? string.Empty) + " " + (req?.SolutionName ?? string.Empty);

                    // Execute asynchronously on the app service and block here (caller runs in background task)
                    var agentResp = appService.RunAsync(prompt, System.Threading.CancellationToken.None).GetAwaiter().GetResult();

                    if (agentResp == null)
                    {
                        return new CopilotResponse { RequestId = req?.RequestId, Success = false, Output = null, Error = "Empty response from agent", Timestamp = DateTime.UtcNow.ToString("o") };
                    }

                    return new CopilotResponse
                    {
                        RequestId = req?.RequestId,
                        Success = agentResp.IsSuccess,
                        Output = agentResp.Content,
                        Error = agentResp.Error,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    };
                }
                catch (Exception ex)
                {
                    return new CopilotResponse { RequestId = req?.RequestId, Success = false, Output = null, Error = ex.Message, Timestamp = DateTime.UtcNow.ToString("o") };
                }
            }
        }
    }
}
