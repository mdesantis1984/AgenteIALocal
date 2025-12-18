using System;
using System.Linq;

namespace AgenteIALocal.Infrastructure.Workspace
{
    internal static class VsSdkAvailability
    {
        /// <summary>
        /// Heuristically detect whether Visual Studio SDK types are present in the current AppDomain.
        /// This does not require compile-time references.
        /// </summary>
        public static bool IsVsSdkPresent()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    var names = new[]
                    {
                        "Microsoft.VisualStudio.Shell.Interop.IVsSolution",
                        "EnvDTE.DTE",
                        "Microsoft.VisualStudio.Shell.AsyncPackage"
                    };

                    foreach (var name in names)
                    {
                        var t = asm.GetType(name, throwOnError: false, ignoreCase: false);
                        if (t != null) return true;
                    }
                }
            }
            catch
            {
                // swallow: if reflection fails, treat as not present
            }

            return false;
        }
    }
}
