using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace AgenteIALocal.Commands
{
    /// <summary>
    /// Command handler for the "Abrir Agente IA Local" menu command.
    /// Registers the menu command and opens the associated ToolWindow when invoked.
    /// </summary>
    internal sealed class OpenAgenteIALocalCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("D2A1C9B4-5C3E-4C88-9E6C-3F2A8A7B1E21");

        private readonly AsyncPackage package;

        private OpenAgenteIALocalCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService?.AddCommand(menuItem);
        }

        /// <summary>
        /// Instance of the command.
        /// </summary>
        public static OpenAgenteIALocalCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// This method is safe to call from any thread; it will switch to the UI thread when registering the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            // Ensure we are on the UI thread when interacting with Visual Studio services
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenAgenteIALocalCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Open the ToolWindow asynchronously
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();

                var window = await package.FindToolWindowAsync(typeof(AgenteIALocal.ToolWindows.AgenteIALocalToolWindow), 0, true, CancellationToken.None);
                if ((window == null) || (window.Frame == null))
                {
                    Debug.WriteLine("Failed to create or find AgenteIALocal tool window.");
                    return;
                }

                var windowFrame = (IVsWindowFrame)window.Frame;
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            });
        }
    }
}
