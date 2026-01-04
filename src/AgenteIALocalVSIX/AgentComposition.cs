using System;
using System.Threading.Tasks;
using AgenteIALocal.Core.Settings;
using AgenteIALocal.Infrastructure.Agents;
using AgenteIALocal.Core.Models.Agent;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocalVSIX
{
    internal static class AgentComposition
    {
        private static readonly object sync = new object();
        private static bool composed = false;

        public static IAgentService AgentService { get; private set; }

        // V2 logger instance exposed as the single logging entry point for all code.
        private static IAgentLoggerV2 loggerV2;
        public static IAgentLoggerV2 LoggerV2
        {
            get
            {
                if (loggerV2 == null)
                {
                    lock (sync)
                    {
                        if (loggerV2 == null)
                        {
                            loggerV2 = new AgenteIALocal.Infrastructure.LoggingV2.AgentLoggerV2(new AgenteIALocal.Infrastructure.LoggingV2.NullLogSink());
                        }
                    }
                }
                return loggerV2;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                loggerV2 = value;
            }
        }

        public static void EnsureComposition()
        {
            if (composed) return;

            lock (sync)
            {
                if (composed) return;

                // Default: assign mock immediately to avoid blocking UI
                AgentService = new MockAgentService();

                // Attempt to build real backend in background; if it fails, keep mock
                _ = Task.Run(() => TryComposeRealBackend());

                composed = true;
            }
        }

        private static void TryComposeRealBackend()
        {
            try
            {
                var vsixSettings = AgentSettingsStore.Load();
                if (vsixSettings == null)
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "No VSIX settings found; keeping mock.");
                    return;
                }

                var activeId = vsixSettings.ActiveServerId;
                if (string.IsNullOrEmpty(activeId) || vsixSettings.Servers == null)
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "No active server configured; keeping mock.");
                    return;
                }

                var srv = vsixSettings.Servers.Find(s => string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "Active server entry not found; keeping mock.");
                    return;
                }

                if (string.IsNullOrEmpty(srv.Provider) || !srv.Provider.Equals("lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "Active provider is not lmstudio; keeping mock.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(srv.BaseUrl))
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "LM Studio BaseUrl empty; keeping mock.");
                    return;
                }

                var lmSettings = new LmStudioSettings
                {
                    BaseUrl = srv.BaseUrl ?? string.Empty,
                    ApiKey = srv.ApiKey ?? string.Empty,
                    Model = srv.Model ?? string.Empty,
                    ChatCompletionsPath = "/v1/chat/completions"
                };

                var resolver = new LmStudioEndpointResolver(lmSettings);
                var client = new LmStudioClient(lmSettings, resolver);
                var appService = new AgenteIALocal.Application.Agents.AgentService(client);
                var adapter = new CoreAgentServiceAdapter(appService);

                AgentService = adapter;

                LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "Real LM Studio backend composed and active.");
            }
            catch (Exception ex)
            {
                try { LoggerV2.Error("-", new LogEventId(9001, "VSIX.Composition"), "Real backend composition failed: " + ex.Message, ex); } catch { }
                // keep existing mock
            }
        }

        public static void RecomposeFromSettings(string reason)
        {
            try
            {
                LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), $"RecomposeFromSettings invoked. Reason: {reason}");

                var vsixSettings = AgentSettingsStore.Load();
                if (vsixSettings == null)
                {
                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "No settings found during recompose; assigning mock.");
                    AgentService = new MockAgentService();
                    return;
                }

                var activeId = vsixSettings.ActiveServerId;
                ServerConfig srv = null;
                if (!string.IsNullOrEmpty(activeId) && vsixSettings.Servers != null)
                {
                    srv = vsixSettings.Servers.Find(s => string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase));
                }

                if (srv == null && vsixSettings.Servers != null && vsixSettings.Servers.Count > 0)
                {
                    srv = vsixSettings.Servers[0];
                }

                var provider = srv?.Provider ?? string.Empty;
                var baseUrl = srv?.BaseUrl ?? string.Empty;
                var model = srv?.Model ?? string.Empty;

                LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), $"Recompose settings: ActiveServerId={srv?.Id}, Provider={provider}, BaseUrl={baseUrl}, Model={model}");

                try
                {
                    if (string.IsNullOrEmpty(srv?.Provider) || !srv.Provider.Equals("lmstudio", StringComparison.OrdinalIgnoreCase))
                    {
                        LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "Provider not lmstudio; assigning mock AgentService.");
                        AgentService = new MockAgentService();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(srv.BaseUrl))
                    {
                        LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "LM Studio BaseUrl empty; assigning mock AgentService.");
                        AgentService = new MockAgentService();
                        return;
                    }

                    var lmSettings = new LmStudioSettings
                    {
                        BaseUrl = srv.BaseUrl ?? string.Empty,
                        ApiKey = srv.ApiKey ?? string.Empty,
                        Model = srv.Model ?? string.Empty,
                        ChatCompletionsPath = "/v1/chat/completions"
                    };

                    var resolver = new LmStudioEndpointResolver(lmSettings);
                    var client = new LmStudioClient(lmSettings, resolver);
                    var appService = new AgenteIALocal.Application.Agents.AgentService(client);
                    var adapter = new CoreAgentServiceAdapter(appService);

                    AgentService = adapter;

                    LoggerV2.Info("-", new LogEventId(9001, "VSIX.Composition"), "RecomposeFromSettings: Real LM Studio backend composed and active.");
                    return;
                }
                catch (Exception ex)
                {
                    LoggerV2.Error("-", new LogEventId(9001, "VSIX.Composition"), $"RecomposeFromSettings failed to compose real backend: {ex.Message}", ex);
                    AgentService = new MockAgentService();
                    return;
                }
            }
            catch (Exception ex)
            {
                try { LoggerV2.Error("-", new LogEventId(9001, "VSIX.Composition"), $"RecomposeFromSettings general failure: {ex.Message}", ex); } catch { }
                try { AgentService = new MockAgentService(); } catch { }
            }
        }

        // Minimal V2 helpers that capture caller info via CallerInfo helpers
        public static void Info(string correlationId, LogEventId eventId, string message, Exception ex = null, System.Collections.Generic.IReadOnlyDictionary<string, string> ctx = null, string ns = null, string type = null, string assembly = null, [System.Runtime.CompilerServices.CallerMemberName] string member = "", [System.Runtime.CompilerServices.CallerFilePath] string file = "", [System.Runtime.CompilerServices.CallerLineNumber] int? line = null)
        {
            try
            {
                LoggerV2.Info(string.IsNullOrEmpty(correlationId) ? "-" : correlationId, eventId, message, ex, ctx, member, file, line, ns, type, assembly);
            }
            catch { }
        }

        public static void Verbose(string correlationId, LogEventId eventId, string message, Exception ex = null, System.Collections.Generic.IReadOnlyDictionary<string, string> ctx = null, string ns = null, string type = null, string assembly = null, [System.Runtime.CompilerServices.CallerMemberName] string member = "", [System.Runtime.CompilerServices.CallerFilePath] string file = "", [System.Runtime.CompilerServices.CallerLineNumber] int? line = null)
        {
            try
            {
                LoggerV2.Verbose(string.IsNullOrEmpty(correlationId) ? "-" : correlationId, eventId, message, ex, ctx, member, file, line, ns, type, assembly);
            }
            catch { }
        }

        public static void Error(string correlationId, LogEventId eventId, string message, Exception ex = null, System.Collections.Generic.IReadOnlyDictionary<string, string> ctx = null, string ns = null, string type = null, string assembly = null, [System.Runtime.CompilerServices.CallerMemberName] string member = "", [System.Runtime.CompilerServices.CallerFilePath] string file = "", [System.Runtime.CompilerServices.CallerLineNumber] int? line = null)
        {
            try
            {
                LoggerV2.Error(string.IsNullOrEmpty(correlationId) ? "-" : correlationId, eventId, message, ex, ctx, member, file, line, ns, type, assembly);
            }
            catch { }
        }

        // ... other levels as needed (Debug, Warning, Critical)
    }

    // Minimal agent service interface local to VSIX project.
    internal interface IAgentService
    {
        AgentHostResponse Execute(AgentHostRequest req);
    }

    // Mock implementation that delegates to the existing MockAgentExecutor.
    internal class MockAgentService : IAgentService
    {
        public AgentHostResponse Execute(AgentHostRequest req)
        {
            return AgenteIALocalVSIX.Execution.MockAgentExecutor.Execute(req);
        }
    }

    // Adapter that exposes the Core/Application async AgentService through the VSIX sync IAgentService
    internal sealed class CoreAgentServiceAdapter : IAgentService
    {
        private readonly AgenteIALocal.Application.Agents.IAgentService appService;

        public CoreAgentServiceAdapter(AgenteIALocal.Application.Agents.IAgentService appService)
        {
            this.appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        public AgentHostResponse Execute(AgentHostRequest req)
        {
            try
            {
                var prompt = (req?.Action ?? string.Empty) + " " + (req?.SolutionName ?? string.Empty);
                // Build Core AgentRequest and propagate correlation id
                var agentReq = new AgenteIALocal.Core.Agents.AgentRequest { Prompt = prompt, CorrelationId = req?.CorrelationId };
                var agentResp = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(
                    () => appService.RunAsync(agentReq, System.Threading.CancellationToken.None)
                    );


                if (agentResp == null)
                {
                    return new AgentHostResponse { RequestId = req?.RequestId, Success = false, Output = null, Error = "Empty response from agent", Timestamp = DateTime.UtcNow.ToString("o") };
                }

                return new AgentHostResponse
                {
                    RequestId = req?.RequestId,
                    Success = agentResp.IsSuccess,
                    Output = agentResp.Content,
                    Error = agentResp.Error,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    // propagate token usage/raw payload when available (direct copy)
                    PromptTokens = agentResp.PromptTokens,
                    CompletionTokens = agentResp.CompletionTokens,
                    TotalTokens = agentResp.TotalTokens,
                    RawResponse = agentResp.RawResponse
                };
            }
            catch (Exception ex)
            {
                return new AgentHostResponse { RequestId = req?.RequestId, Success = false, Output = null, Error = ex.Message, Timestamp = DateTime.UtcNow.ToString("o") };
            }
        }
    }

    internal static class AgentCompositionHelpers
    {
        // Small helpers to avoid adding direct compile-time dependency on AgentResponse shape
        internal static int? TryGetPropInt(object obj, string propName)
        {
            try
            {
                if (obj == null) return null;
                var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p == null) return null;
                var v = p.GetValue(obj, null);
                if (v is int i) return i;
                if (v != null && int.TryParse(v.ToString(), out var pi)) return pi;
            }
            catch { }
            return null;
        }

        internal static string TryGetPropString(object obj, string propName)
        {
            try
            {
                if (obj == null) return null;
                var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p == null) return null;
                var v = p.GetValue(obj, null);
                return v?.ToString();
            }
            catch { }
            return null;
        }
    }
}
