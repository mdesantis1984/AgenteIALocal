using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocal.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        "Agente IA Local",
        "Extensión base para IA local",
        "1.0")]
    [Guid(PackageGuidString)]
    public sealed class AgenteIALocalPackage : AsyncPackage
    {
        public const string PackageGuidString = "6D5B8A7E-1C8F-4E7A-9F5C-123456789ABC";

        protected override async System.Threading.Tasks.Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
        }
    }
}
