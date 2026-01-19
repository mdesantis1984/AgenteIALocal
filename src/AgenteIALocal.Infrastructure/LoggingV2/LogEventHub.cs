using System;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    // NUEVO COMPONENTE LogEventHub - ID: 20260118_190100
    // Central hub where the formatted log line is published for UI sinks.
    public static class LogEventHub
    {
        // Subscribers receive the exact formatted line produced by LogEntryTextFormatter.Format
        public static event Action<string, LogEntry> OnLog;

        public static void Publish(string formattedLine, LogEntry entry)
        {
            try
            {
                var h = OnLog;
                if (h == null) return;
                try { h.Invoke(formattedLine ?? string.Empty, entry); } catch { }
            }
            catch { }
        }
    }
}
