using System;
using System.Threading.Tasks;
using AgenteIALocal.Core.Settings;
using AgenteIALocal.Infrastructure.Agents;
using AgenteIALocal.Core.Models.Agent;
// MODIFICADO - ID: 20260122_030204 - Eliminado using AgenteIALocal.Core.Logging (legacy, no usado)
// MODIFICADO - ID: 20260123_215500 - ROLLBACK System.Text.Json → Newtonsoft.Json + agregar Core.Configuration
// MODIFICADO - ID: 20260123_230801 - Eliminado using Newtonsoft.Json.Linq (no usado tras eliminar LogGlobalSettingsPersistence)
using AgenteIALocal.Core.Configuration;

namespace AgenteIALocalVSIX
{
    internal static class AgentComposition
    {
        private static readonly object sync = new object();
        private static bool composed = false;

        public static IAgentService AgentService { get; private set; }

        // ELIMINADO LoggerV2 property - ID: 20260122_195502
        // Reemplazado completamente por AgenteIALocal.Logging.Log (Serilog)

        public static void EnsureComposition()
        {
            if (composed) return;

            lock (sync)
            {
                if (composed) return;

                // NUEVO - ID: 20260123_221500 - Registrar settings provider (Clean Architecture)
                try
                {
                    var settingsProvider = new AgenteIALocal.Application.Settings.FileAgentSettingsProvider();
                    AgentSettingsStore.Initialize(settingsProvider);
                    AgenteIALocal.Logging.Log.Information("-", 9000, "Composition.DI", "FileAgentSettingsProvider registered successfully", null);
                }
                catch (Exception exDI)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9000, "Composition.DI", "Failed to register FileAgentSettingsProvider", exDI);
                }

                // Default: assign mock immediately to avoid blocking UI
                AgentService = new MockAgentService();

                // Attempt to build real backend in background; if it fails, keep mock
                _ = Task.Run(() => TryComposeRealBackend());

                composed = true;
            }
        }

        // NUEVO METODO IsOpenAiCompatibleProvider - ID: 20260116_012700
        private static bool IsOpenAiCompatibleProvider(string provider)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(provider)) return false;
                return provider.Equals("lmstudio", StringComparison.OrdinalIgnoreCase)
                    || provider.Equals("jan", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.IsOpenAi", "IsOpenAiCompatibleProvider failed: " + ex.Message, ex);
                return false;
            }
        }

        private static void TryComposeRealBackend()
        {
            try
            {
                var vsixSettings = AgentSettingsStore.Load();
                if (vsixSettings == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "No VSIX settings found; keeping mock.", null);
                    return;
                }

                var activeId = vsixSettings.ActiveServerId;
                if (string.IsNullOrEmpty(activeId) || vsixSettings.Servers == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "No active server configured; keeping mock.", null);
                    return;
                }

                var srv = vsixSettings.Servers.Find(s => string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase));

                if (srv == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "Active server entry not found; keeping mock.", null);
                    return;
                }

                if (!IsOpenAiCompatibleProvider(srv.Provider))
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "Active provider is not OpenAI-compatible (lmstudio|jan); keeping mock.", null);
                    return;
                }

                if (string.IsNullOrWhiteSpace(srv.BaseUrl))
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "LM Studio BaseUrl empty; keeping mock.", null);
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
                var client = new OpenAiCompatibleClient(lmSettings, resolver); // RENOMBRADO - ID: 20260122_000401
                var appService = new AgenteIALocal.Application.Agents.AgentService(client);
                var adapter = new CoreAgentServiceAdapter(appService);

                AgentService = adapter;

                AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "Real OpenAI-compatible backend composed and active.", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9001, "VSIX.Composition", "Real backend composition failed: " + ex.Message, ex);
                // keep existing mock
            }
        }

        public static void RecomposeFromSettings(string reason)
        {
            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", $"RecomposeFromSettings invoked. Reason: {reason}", null);

                var vsixSettings = AgentSettingsStore.Load();
                if (vsixSettings == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "No settings found during recompose; assigning mock.", null);
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

                AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", $"Recompose settings: ActiveServerId={srv?.Id}, Provider={provider}, BaseUrl={baseUrl}, Model={model}", null);

                try
                {
                    if (!IsOpenAiCompatibleProvider(srv?.Provider))
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "Provider not OpenAI-compatible (lmstudio|jan); assigning mock AgentService.", null);
                        AgentService = new MockAgentService();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(srv.BaseUrl))
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "Active server BaseUrl empty; assigning mock AgentService.", null);
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
                    var client = new OpenAiCompatibleClient(lmSettings, resolver); // RENOMBRADO - ID: 20260122_000402
                    var appService = new AgenteIALocal.Application.Agents.AgentService(client);
                    var adapter = new CoreAgentServiceAdapter(appService);

                    AgentService = adapter;

                    AgenteIALocal.Logging.Log.Information("-", 9001, "VSIX.Composition", "RecomposeFromSettings: Real OpenAI-compatible backend composed and active.", null);
                    return;
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9001, "VSIX.Composition", $"RecomposeFromSettings failed to compose real backend: {ex.Message}", ex);
                    AgentService = new MockAgentService();
                    return;
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9001, "VSIX.Composition", $"RecomposeFromSettings general failure: {ex.Message}", ex);
                try { AgentService = new MockAgentService(); } catch (Exception exMock) { AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Recompose", "Mock assignment failed: " + exMock.Message, exMock); }
            }
        }


        // ELIMINADOS METODOS helpers LoggingV2 - ID: 20260122_195506
        // Info(), Verbose(), Error() eliminados completamente (líneas 196-222 originales)
        // Usar AgenteIALocal.Logging.Log.* directamente en TODO el código

        // ELIMINADO LogGlobalSettingsPersistence - ID: 20260123_230800
        // Método obsoleto (nunca usado) con dependencia JObject - violaba Clean Architecture
        // Si se necesita logging de cambios settings → usar DTOs tipados en lugar de JObject

        // ELIMINADOS METODOS helpers LoggingV2 - ID: 20260122_195505
        // Info(), Verbose(), Error(), Warning() eliminados completamente
        // TODO el código debe usar AgenteIALocal.Logging.Log.* directamente
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
                // Build prompt and include optional AgentConfig instructions based on persisted settings
                var settings = AgentSettingsStore.Load();
                var global = settings?.GlobalSettings;

                // agent config defaults
                bool ideIntegration = true;
                bool applyChanges = false;
                int maxSteps = 5;

                // MODIFICADO - ID: 20260123_225801 - Usar DTOs en lugar de JsonObject
                try
                {
                    if (global != null)
                    {
                        ideIntegration = global.Agent.IdeIntegration;
                        applyChanges = global.Agent.ApplyChanges;
                        maxSteps = global.Agent.MaxSteps;
                    }
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Execute", "Failed to parse agent config defaults: " + ex.Message, ex);
                }

                // Build a short AgentConfig block to prepend to the prompt when runMode=agente
                var agentConfigBlock = "";
                try
                {
                    agentConfigBlock = "[AgentConfig] " + "ideIntegration=" + (ideIntegration ? "true" : "false") + "; applyChanges=" + (applyChanges ? "true" : "false") + "; maxSteps=" + maxSteps + "\n";
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Execute", "Failed to build agent config block: " + ex.Message, ex);
                    agentConfigBlock = string.Empty;
                }

                // Compose prompt
                string promptBody = (req?.Action ?? string.Empty) ?? string.Empty;
                if (ideIntegration)
                {
                    var sol = req?.SolutionName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(sol)) promptBody = string.IsNullOrWhiteSpace(promptBody) ? sol : (promptBody + " " + sol);
                    // ProjectCount is kept out of prompt when ideIntegration=false; when true, append count if present
                    try
                    {
                        var pc = req?.ProjectCount ?? 0;
                        if (pc > 0) promptBody = promptBody + " (ProjectCount=" + pc + ")";
                    }
                    catch (Exception ex)
                    {
                        AgenteIALocal.Logging.Log.Verbose("-", 9001, "Composition.Execute", "Failed to append ProjectCount to prompt: " + ex.Message, ex);
                    }
                }

                var fullPrompt = string.IsNullOrWhiteSpace(agentConfigBlock) ? promptBody : (agentConfigBlock + promptBody);

                // Build Core AgentRequest and propagate correlation id
                var agentReq = new AgenteIALocal.Core.Agents.AgentRequest
                {
                    Prompt = fullPrompt,
                    CorrelationId = req?.CorrelationId,
                    Stream = req != null && req.Stream,
                    OnDelta = req?.OnDelta
                };

                // Populate temperature/maxTokens from settings.requestDefaults when present - MODIFICADO - ID: 20260123_225901
                try
                {
                    if (global != null)
                    {
                        var temp = global.RequestDefaults.Temperature;
                        agentReq.Temperature = temp;
                        var mt = global.RequestDefaults.MaxTokens;
                        if (mt > 0) agentReq.MaxTokens = mt;
                    }
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Execute", "Failed to read requestDefaults: " + ex.Message, ex);
                }
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
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Helpers", "TryGetPropInt failed: " + ex.Message, ex);
            }
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
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Warning("-", 9001, "Composition.Helpers", "TryGetPropString failed: " + ex.Message, ex);
            }
            return null;
        }
    }
}
