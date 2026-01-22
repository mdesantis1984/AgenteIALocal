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
                // Use assembly reference obtained via a compile-time-known type instead of Type.GetType with assembly-qualified string
                var vsContextType = (Type)null;
                try
                {
                    var infraAsm = typeof(AgenteIALocal.Infrastructure.Agents.OpenAiCompatibleClient).Assembly; // RENOMBRADO - ID: 20260122_000405
                    vsContextType = infraAsm.GetType("AgenteIALocal.Infrastructure.Workspace.VisualStudioSolutionContext", throwOnError: false, ignoreCase: false);
                }
                catch
                {
                    vsContextType = null;
                }

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
