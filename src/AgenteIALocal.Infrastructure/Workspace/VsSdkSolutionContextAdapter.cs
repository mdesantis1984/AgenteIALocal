using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces;

// Note: this file references VS SDK types only at runtime via reflection to avoid
// compile-time dependency on the SDK for the Infrastructure project. If the SDK
// types are available at runtime, the adapter will use them via dynamic invocation.

namespace AgenteIALocal.Infrastructure.Workspace
{
    internal class VsSdkSolutionContextAdapter : ISolutionContext
    {
        private readonly object vsSolution; // IVsSolution at runtime

        public VsSdkSolutionContextAdapter(object vsSolution)
        {
            this.vsSolution = vsSolution ?? throw new ArgumentNullException(nameof(vsSolution));
        }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public IReadOnlyList<IProjectInfo> Projects { get; private set; } = Array.Empty<IProjectInfo>();

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Use reflection to call GetSolutionInfo and enumerate projects if possible.
            try
            {
                // Attempt to call GetSolutionInfo via reflection
                var getSolutionInfo = vsSolution.GetType().GetMethod("GetSolutionInfo");
                if (getSolutionInfo != null)
                {
                    // Signature: void GetSolutionInfo(out string, out string, out string)
                    var parameters = new object[] { null, null, null };
                    getSolutionInfo.Invoke(vsSolution, parameters);
                    var solutionFile = parameters[1] as string;
                    Path = string.IsNullOrEmpty(solutionFile) ? null : solutionFile;
                    Name = string.IsNullOrEmpty(solutionFile) ? null : System.IO.Path.GetFileNameWithoutExtension(solutionFile);
                }

                // Try to enumerate projects using GetProjectEnum if available
                var getProjectEnum = vsSolution.GetType().GetMethod("GetProjectEnum");
                if (getProjectEnum != null)
                {
                    // We'll attempt to invoke and then parse IVsHierarchy instances if returned.
                    // As this is a best-effort adapter, we will fall back gracefully on any failures.
                }
            }
            catch
            {
                Name = null;
                Path = null;
                Projects = Array.Empty<IProjectInfo>();
            }

            await Task.CompletedTask;
        }
    }
}
