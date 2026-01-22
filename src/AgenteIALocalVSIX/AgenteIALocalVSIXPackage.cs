using AgenteIALocal.Core.Logging;
using AgenteIALocal.Infrastructure.LoggingV2;
using AgenteIALocalVSIX.Commands;
using AgenteIALocalVSIX.LoggingV2;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AgenteIALocalVSIX
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AgenteIALocalVSIXPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindows.AgenteIALocalToolWindow))]
    public sealed class AgenteIALocalVSIXPackage : AsyncPackage
    {
        public const string PackageGuidString = "12e93cca-8723-4160-ac43-96fe08854111";

        private static int _loggerConfigured;

        // Startup logging MUST never break package load.
        private static void ConfigureLoggerV2Once()
        {
            if (Interlocked.Exchange(ref _loggerConfigured, 1) == 1) return;

            try
            {
                var sinks = new CompositeLogSink(new ILogSink[]
                {
                    new VsActivityLogSink(),
                    new VsixFileLogSink("AgenteIALocal")
                });

                AgentComposition.LoggerV2 = new AgentLoggerV2(sinks);
                VsixSafeLog.Info("VSIX.Startup", "LoggerV2 pipeline configured.", null, 9000);
            }
            catch (Exception ex)
            {
                try { AgentComposition.LoggerV2 = new AgentLoggerV2(new NullLogSink()); } catch { }
                VsixSafeLog.Error("VSIX.Startup", "Failed to configure LoggerV2 pipeline.", ex, 9000);
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        // This event is raised in a variety of teardown scenarios.
                        // Logging MUST be best-effort and MUST NEVER throw.
                        var ex = e.ExceptionObject as Exception;
                        VsixSafeLog.Error("VSIX.Unhandled", "Unhandled exception", ex, 9000);
                    }
                    catch { }
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        // Raised on the finalizer thread; VS services may already be torn down.
                        // Always mark as observed and log via the always-safe path.
                        try { e.SetObserved(); } catch { }
                        VsixSafeLog.Error("VSIX.UnobservedTask", "Unobserved task exception", e.Exception, 9000);
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("VSIX.Startup", "Failed to hook global exception handlers.", ex, 9000);
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            ConfigureLoggerV2Once();

            try
            {
                AgentComposition.EnsureComposition();
                AgentComposition.Info("-", new LogEventId(9000, "VSIX.Startup"), "VSIX initialized.");
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("VSIX.Startup", "InitializeAsync failed.", ex, 9000);
            }

            await OpenAgenteIALocalCommand.InitializeAsync(this);
        }
    }
}
