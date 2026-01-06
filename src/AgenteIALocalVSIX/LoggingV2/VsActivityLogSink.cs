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

                // Persist only warnings/errors (and Critical as error)
                if (entry.Level != LogLevel.Warning && entry.Level != LogLevel.Error && entry.Level != LogLevel.Critical) return;

                IServiceProvider serviceProvider = ServiceProvider.GlobalProvider;
                if (serviceProvider == null) return;

                var log = serviceProvider.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
                if (log == null) return;

                var message = AgenteIALocal.Core.Logging.LogEntryTextFormatter.Format(entry) ?? string.Empty;

                uint kind;
                if (entry.Level == LogLevel.Warning)
                {
                    kind = (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;
                }
                else
                {
                    kind = (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;
                }

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
