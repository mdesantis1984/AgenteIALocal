using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using AgenteIALocalVSIX.Commands;
using System.IO;
using System.Text;
using AgenteIALocal.Core.Logging;
using AgenteIALocal.Infrastructure.LoggingV2;
using AgenteIALocalVSIX.LoggingV2;


namespace AgenteIALocalVSIX
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AgenteIALocalVSIXPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindows.AgenteIALocalToolWindow))]
    public sealed class AgenteIALocalVSIXPackage : AsyncPackage
    {
        /// <summary>
        /// AgenteIALocalVSIXPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "12e93cca-8723-4160-ac43-96fe08854111";

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
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            try
            {
                try
                {
                    // Build V2 logging pipeline
                    var sinks = new CompositeLogSink(new ILogSink[] {
                        new VsActivityLogSink(),
                        new VsixFileLogSink("AgenteIALocal")
                    });

                    var v2 = new AgentLoggerV2(sinks);
                    AgentComposition.LoggerV2 = v2 ?? new AgentLoggerV2(new NullLogSink());
                }
                catch
                {
                    // If any failure, ensure non-null LoggerV2
                    AgentComposition.LoggerV2 = new AgentLoggerV2(new NullLogSink());
                }

                AgentComposition.EnsureComposition();

                AgentComposition.Info("-", new LogEventId(9000, "VSIX.Startup"), "Agent composition scheduled.");
            }
            catch (Exception ex)
            {
                try
                {
                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var logDir = Path.Combine(local ?? string.Empty, "AgenteIALocal", "logs");
                    Directory.CreateDirectory(logDir);
                    var logPath = Path.Combine(logDir, "AgenteIALocal.log");
                    File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " - " + "[AgenteIALocalVSIXPackage] Agent composition threw: " + ex + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }

            // Initialize commands (register menu commands). OpenAgenteIALocalCommand.InitializeAsync will add commands to the OleMenuCommandService.
            await OpenAgenteIALocalCommand.InitializeAsync(this);
        }

        #endregion
    }
}
