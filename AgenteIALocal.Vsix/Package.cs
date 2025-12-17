using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AgenteIALocal.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("AgenteIALocal", "Extensión para AgenteIA Local", "1.0")]
    [Guid("a4f0f588-9e6c-4b4a-8d6c-72f8b491e8a2")]
    public sealed class AgenteIALocalPackage : AsyncPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            return Task.CompletedTask;
        }
    }
}
