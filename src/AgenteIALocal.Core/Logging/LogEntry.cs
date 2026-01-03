using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AgenteIALocal.Core.Logging
{
    public class LogEntry
    {
        public LogEntry(DateTime timestampUtc, LogLevel level, string correlationId, LogEventId eventId, string message, Exception exception, LogSource source, IReadOnlyDictionary<string, string> context)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            CorrelationId = string.IsNullOrEmpty(correlationId) ? "-" : correlationId;
            EventId = eventId;
            Message = message ?? string.Empty;
            Exception = exception;
            Source = source ?? new LogSource();
            Context = context ?? new Dictionary<string, string>(StringComparer.Ordinal);

            try { ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId; } catch { ThreadId = -1; }
            try { ProcessId = Process.GetCurrentProcess().Id; } catch { ProcessId = -1; }
        }

        public DateTime TimestampUtc { get; }
        public LogLevel Level { get; }
        public string CorrelationId { get; }
        public LogEventId EventId { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public LogSource Source { get; }
        public IReadOnlyDictionary<string, string> Context { get; }
        public int ThreadId { get; }
        public int ProcessId { get; }
    }
}
