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

                // Use the canonical formatter from Core to produce the exact string to send to ActivityLog
                var message = AgenteIALocal.Core.Logging.LogEntryTextFormatter.Format(entry) ?? string.Empty;

                uint kind = (entry.Level == LogLevel.Error || entry.Level == LogLevel.Critical) ?
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR : (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;

                try
                {
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
