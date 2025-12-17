using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using AgenteIALocal.Commands;

namespace AgenteIALocal
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        productName: "Agente IA Local",
        productDetails: "Agente IA local para Visual Studio",
        productId: "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    public sealed class AgenteIALocalPackage : AsyncPackage
    {
        public const string PackageGuidString = "9F6C3B2E-7A3D-4A2B-9E7A-8E5B6A4D2F10";

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Inicialización mínima.
            // Comandos y ToolWindow se registrarán en pasos posteriores.

            // Register commands (Phase 1: command shell)
            await OpenAgenteIALocalCommand.InitializeAsync(this);
        }
    }
}
