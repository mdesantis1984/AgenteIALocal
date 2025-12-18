using System;

namespace AgenteIALocalVSIX.Application
{
    internal static class VisualStudioWorkspaceProvider
    {
        // Best-effort provider: if Infrastructure.VisualStudioSolutionContext is available, return an instance (as object to avoid compile-time dependency).
        public static object TryGetWorkspaceContext()
        {
            try
            {
                var vsContextType = Type.GetType("AgenteIALocal.Infrastructure.Workspace.VisualStudioSolutionContext, AgenteIALocal.Infrastructure");
                if (vsContextType != null)
                {
                    var solutionContext = Activator.CreateInstance(vsContextType);
                    return solutionContext;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
    }
}
