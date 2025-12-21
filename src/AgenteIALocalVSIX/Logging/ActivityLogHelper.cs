using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AgenteIALocalVSIX.Logging
{
    internal static class ActivityLogHelper
    {
        public static void TryLog(IServiceProvider serviceProvider, string message, Exception ex = null)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (serviceProvider == null) return;

                var log = serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                if (log == null) return;

                var full = message ?? string.Empty;
                if (ex != null)
                {
                    full += " | " + ex.GetType().FullName + ": " + ex.Message;
                }

                log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, "AgenteIALocal", full);
            }
            catch
            {
                // fail-safe
            }
        }

        public static void TryLogError(IServiceProvider serviceProvider, string message, Exception ex = null)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (serviceProvider == null) return;

                var log = serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                if (log == null) return;

                var full = message ?? string.Empty;
                if (ex != null)
                {
                    full += " | " + ex.GetType().FullName + ": " + ex.Message;
                    try { full += "\n" + ex; } catch { }
                }

                log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "AgenteIALocal", full);
            }
            catch
            {
                // fail-safe
            }
        }
    }
}
