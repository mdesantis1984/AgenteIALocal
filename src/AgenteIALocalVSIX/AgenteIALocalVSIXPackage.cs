using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using AgenteIALocalVSIX.Commands;
using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Settings;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Application.Logging;
using AgenteIALocalVSIX.Logging;


namespace AgenteIALocalVSIX
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AgenteIALocalVSIXPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindows.AgenteIALocalToolWindow))]
    [ProvideOptionPage(typeof(AgenteIALocalVSIX.Options.AgenteIALocalOptionsPage), "Agente IA Local", "General", 0, 0, true)]
    public sealed class AgenteIALocalVSIXPackage : AsyncPackage
    {
        /// <summary>
        /// AgenteIALocalVSIXPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "12e93cca-8723-4160-ac43-96fe08854111";

        internal IAgentSettingsProvider AgentSettingsProvider { get; private set; }

        // Composition: client + service
        internal IAgentClient AgentClient { get; private set; }
        internal IAgentService AgentService { get; private set; }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            IAgentLogger logger = null;
            try
            {
                logger = new FileAgentLogger("VSIXPackage");
                AgentComposition.Logger = logger;
                logger.Info("Package: InitializeAsync start");
            }
            catch
            {
                logger = null;
            }

            try
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                try { logger?.Info("Package: switched to UI thread"); } catch { }

                try
                {
                    logger?.Info("InitializeAsync: package initialized");
                    ActivityLogHelper.TryLog(this, "AgenteIALocal: Package initialized");
                }
                catch { }

                try { ActivityLogHelper.TryLog(this, "AgenteIALocal: InitializeAsync start"); } catch { }

                try
                {
                    logger?.Info("InitializeAsync: ToolWindow registration (ProvideToolWindow attribute) = AgenteIALocalToolWindow");
                    ActivityLogHelper.TryLog(this, "AgenteIALocal: ToolWindow registered (ProvideToolWindow attribute) = AgenteIALocalToolWindow");
                }
                catch { }

                // Initialize IAgentSettingsProvider and expose it for Options Page
                AgentSettingsProvider = new Settings.VsWritableSettingsStoreAgentSettingsProvider(this);
                logger?.Info("InitializeAsync: SettingsProvider initialized");

                // Assign provider to options page instance so it can load/save settings
                var options = (AgenteIALocalVSIX.Options.AgenteIALocalOptionsPage)GetDialogPage(typeof(AgenteIALocalVSIX.Options.AgenteIALocalOptionsPage));
                if (options != null)
                {
                    options.SettingsProvider = AgentSettingsProvider;
                    logger?.Info("InitializeAsync: OptionsPage.SettingsProvider assigned");
                }

                // Composition Root: load AgentSettings ONLY here
                try
                {
                    var root = AgentSettingsProvider.Load() ?? new AgentSettings();
                    logger?.Info("InitializeAsync: loaded AgentSettings Provider=" + root.Provider.ToString());
                    logger?.Info("InitializeAsync: active BaseUrl=" + MaskUrl(GetActiveBaseUrl(root)));
                    logger?.Info("InitializeAsync: active ChatPath=" + (GetActiveChatPath(root) ?? string.Empty));

                    string endpointResolverTypeName;
                    string clientTypeName;

                    if (root.Provider == AgentProviderType.JanServer)
                    {
                        var janSettings = root.JanServer ?? new JanServerSettings();
                        endpointResolverTypeName = "AgenteIALocal.Infrastructure.Agents.JanServerEndpointResolver, AgenteIALocal.Infrastructure";
                        clientTypeName = "AgenteIALocal.Infrastructure.Agents.JanServerClient, AgenteIALocal.Infrastructure";

                        var endpointResolverType = Type.GetType(endpointResolverTypeName, false);
                        var clientType = Type.GetType(clientTypeName, false);

                        logger?.Info("InitializeAsync: reflection endpointResolverType=" + endpointResolverTypeName + " resolved=" + (endpointResolverType != null));
                        logger?.Info("InitializeAsync: reflection clientType=" + clientTypeName + " resolved=" + (clientType != null));

                        object endpointResolver = endpointResolverType != null ? Activator.CreateInstance(endpointResolverType, new object[] { janSettings }) : null;
                        object clientObj = clientType != null ? Activator.CreateInstance(clientType, new object[] { janSettings }) : null;

                        AgentClient = clientObj as IAgentClient;
                    }
                    else
                    {
                        var lmSettings = root.LmStudio ?? new LmStudioSettings();
                        endpointResolverTypeName = "AgenteIALocal.Infrastructure.Agents.LmStudioEndpointResolver, AgenteIALocal.Infrastructure";
                        clientTypeName = "AgenteIALocal.Infrastructure.Agents.LmStudioClient, AgenteIALocal.Infrastructure";

                        var endpointResolverType = Type.GetType(endpointResolverTypeName, false);
                        var clientType = Type.GetType(clientTypeName, false);

                        logger?.Info("InitializeAsync: reflection endpointResolverType=" + endpointResolverTypeName + " resolved=" + (endpointResolverType != null));
                        logger?.Info("InitializeAsync: reflection clientType=" + clientTypeName + " resolved=" + (clientType != null));

                        object endpointResolver = endpointResolverType != null ? Activator.CreateInstance(endpointResolverType, new object[] { lmSettings }) : null;
                        object clientObj = clientType != null ? Activator.CreateInstance(clientType, new object[] { lmSettings, endpointResolver }) : null;

                        AgentClient = clientObj as IAgentClient;
                    }

                    if (AgentClient != null)
                    {
                        AgentService = new AgentService(AgentClient);
                        AgentComposition.AgentClient = AgentClient;
                        AgentComposition.AgentService = AgentService;
                        logger?.Info("InitializeAsync: AgentService composed");
                    }
                    else
                    {
                        logger?.Warn("InitializeAsync: AgentClient is null; AgentService not composed");
                        try { ActivityLogHelper.TryLogError(this, "AgenteIALocal: AgentClient is null; AgentService not composed"); } catch { }

                        try
                        {
                            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                            var names = assemblies.Select(a =>
                            {
                                try { return a.GetName().Name; } catch { return "?"; }
                            }).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n).ToArray();

                            logger?.Info("InitializeAsync: loaded assemblies=" + string.Join(",", names));

                            var hasInfra = names.Any(n => string.Equals(n, "AgenteIALocal.Infrastructure", StringComparison.OrdinalIgnoreCase));
                            if (!hasInfra)
                            {
                                logger?.Warn("InitializeAsync: Infrastructure assembly not loaded/packaged");
                                try { ActivityLogHelper.TryLogError(this, "AgenteIALocal: Infrastructure assembly not loaded/packaged"); } catch { }
                            }
                        }
                        catch (Exception ex2)
                        {
                            logger?.Error("InitializeAsync: failed listing assemblies", ex2);
                        }
                    }
                }
                catch (Exception ex)
                {
                    AgentClient = null;
                    AgentService = null;
                    logger?.Error("InitializeAsync: composition failed", ex);
                    try { ActivityLogHelper.TryLogError(this, "AgenteIALocal: composition failed", ex); } catch { }
                }

                // Initialize commands
                await OpenAgenteIALocalCommand.InitializeAsync(this);
                logger?.Info("Package: OpenAgenteIALocalCommand.InitializeAsync called");

                try { AgenteIALocalVSIX.Commands.VsctConsistencyValidator.LogConsistency(); } catch { }

                try { ActivityLogHelper.TryLog(this, "AgenteIALocal: InitializeAsync end"); } catch { }
            }
            catch (Exception ex)
            {
                logger?.Error("InitializeAsync: fatal", ex);
                try { ActivityLogHelper.TryLogError(this, "AgenteIALocal: InitializeAsync fatal", ex); } catch { }
                // NO relanzar
            }
            finally
            {
                logger?.Info("InitializeAsync: end");
            }
        }

        private static string GetActiveBaseUrl(AgentSettings settings)
        {
            try
            {
                if (settings == null) return null;
                if (settings.Provider == AgentProviderType.JanServer) return settings.JanServer?.BaseUrl;
                return settings.LmStudio?.BaseUrl;
            }
            catch
            {
                return null;
            }
        }

        private static string GetActiveChatPath(AgentSettings settings)
        {
            try
            {
                if (settings == null) return null;
                if (settings.Provider == AgentProviderType.JanServer) return settings.JanServer?.ChatCompletionsPath;
                return settings.LmStudio?.ChatCompletionsPath;
            }
            catch
            {
                return null;
            }
        }

        private static string MaskUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            return url.Trim();
        }

        #endregion
    }
}
