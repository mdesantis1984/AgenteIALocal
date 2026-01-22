using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
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

            AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Init", "OleMenuCommand registered", null);
        }

        public static OpenAgenteIALocalCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Init", "InitializeAsync start", null);

            if (package == null) throw new ArgumentNullException(nameof(package));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;

            if (commandService == null)
            {
                AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Init", "OleMenuCommandService is null", null);
                return;
            }

            Instance = new OpenAgenteIALocalCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "OpenAgenteIALocal Execute invoked", null);

            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "OpenAgenteIALocal invoked", null);

                ThreadHelper.ThrowIfNotOnUIThread();

                var sp = package as IServiceProvider;
                var uiShell = sp?.GetService(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "IVsUIShell service not available", null);
                    return;
                }

                IVsWindowFrame frame = null;
                Guid persistenceGuid = typeof(ToolWindows.AgenteIALocalToolWindow).GUID;

                int hrFind = uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref persistenceGuid, out frame);

                AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "FindToolWindow HR=0x" + hrFind.ToString("X"), null);

                if (frame == null)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "ToolWindow frame is null after FindToolWindow", null);
                    return;
                }

                int hrShow = frame.Show();

                try
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.Execute", "Show HR=0x" + hrShow.ToString("X"), null);
                }
                catch { }
            }
            catch (Exception ex)
            {
                try { AgenteIALocal.Logging.Log.Error("-", 9200, "Command.Execute", "exception opening ToolWindow: " + ex.Message, ex); } catch { }
            }
        }
    }
}
