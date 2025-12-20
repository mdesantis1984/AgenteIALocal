using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using AgenteIALocalVSIX.Logging;
using Microsoft.VisualStudio.Shell.Interop;

namespace AgenteIALocalVSIX.Commands
{
    internal sealed class OpenAgenteIALocalCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("B1A6E1D0-3F4B-4C2B-9E1A-2C7F9D4F6A2B");

        private readonly AsyncPackage package;

        private OpenAgenteIALocalCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            try { AgentComposition.Logger?.Info("Command: OleMenuCommand registered"); } catch { }
        }

        public static OpenAgenteIALocalCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try { AgentComposition.Logger?.Info("Command: InitializeAsync start"); } catch { }

            if (package == null) throw new ArgumentNullException(nameof(package));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;

            if (commandService == null)
            {
                try { AgentComposition.Logger?.Error("Command: OleMenuCommandService is null"); } catch { }
                return;
            }

            Instance = new OpenAgenteIALocalCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            AgentComposition.Logger?.Info("Command: OpenAgenteIALocal Execute invoked");

            try
            {
                try
                {
                    AgentComposition.Logger?.Info("Command: OpenAgenteIALocal invoked");
                    ActivityLogHelper.TryLog(package, "AgenteIALocal: Command invoked");
                }
                catch { }

                ThreadHelper.ThrowIfNotOnUIThread();

                var sp = package as IServiceProvider;
                var uiShell = sp?.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell == null)
                {
                    try
                    {
                        AgentComposition.Logger?.Error("Command: IVsUIShell service not available");
                        ActivityLogHelper.TryLogError(package, "AgenteIALocal: IVsUIShell service not available");
                    }
                    catch { }

                    return;
                }

                IVsWindowFrame frame = null;
                Guid persistenceGuid = typeof(ToolWindows.AgenteIALocalToolWindow).GUID;

                int hrFind = uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref persistenceGuid, out frame);

                try
                {
                    AgentComposition.Logger?.Info("FindToolWindow HR=0x" + hrFind.ToString("X"));
                    ActivityLogHelper.TryLog(package, "AgenteIALocal: FindToolWindow HR=0x" + hrFind.ToString("X"));
                }
                catch { }

                if (frame == null)
                {
                    try
                    {
                        AgentComposition.Logger?.Error("Command: ToolWindow frame is null after FindToolWindow");
                        ActivityLogHelper.TryLogError(package, "AgenteIALocal: ToolWindow frame is null after FindToolWindow");
                    }
                    catch { }

                    return;
                }

                int hrShow = frame.Show();

                try
                {
                    AgentComposition.Logger?.Info("Show HR=0x" + hrShow.ToString("X"));
                    ActivityLogHelper.TryLog(package, "AgenteIALocal: Show HR=0x" + hrShow.ToString("X"));
                }
                catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Error("Command: exception opening ToolWindow", ex); } catch { }
                try { ActivityLogHelper.TryLogError(package, "AgenteIALocal: command failed", ex); } catch { }
            }
        }
    }
}
