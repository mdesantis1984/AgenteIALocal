using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

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

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandID);
            commandService?.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new OpenAgenteIALocalCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            // Fire-and-forget: show tool window asynchronously on UI thread
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                await package.ShowToolWindowAsync(typeof(ToolWindows.AgenteIALocalToolWindow), 0, true, package.DisposalToken);
            });
        }
    }
}
