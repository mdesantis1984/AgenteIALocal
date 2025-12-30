using System;
using AgenteIALocal.Core.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AgenteIALocalVSIX.LoggingV2
{
    public class VsActivityLogSink : ILogSink
    {
        public void Write(LogEntry entry)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (entry == null) return;

                IServiceProvider serviceProvider = ServiceProvider.GlobalProvider;
                if (serviceProvider == null) return;

                var log = serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                if (log == null) return;

                var message = entry.Message ?? string.Empty;
                if (entry.Exception != null)
                {
                    message += " | " + entry.Exception.GetType().FullName + ": " + (entry.Exception.Message ?? string.Empty);
                }

                uint kind = (entry.Level == LogLevel.Error || entry.Level == LogLevel.Critical) ?
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR : (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;

                try
                {
                    // For errors include exception.ToString() if possible
                    if (kind == (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR && entry.Exception != null)
                    {
                        try { message += "\n" + entry.Exception.ToString(); } catch { }
                    }

                    log.LogEntry(kind, "AgenteIALocal", message);
                }
                catch
                {
                    // swallow
                }
            }
            catch
            {
                // fail-safe
            }
        }
    }
}
