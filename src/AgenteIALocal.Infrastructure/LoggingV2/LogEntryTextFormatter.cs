using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    internal static class LogEntryTextFormatter
    {
        public static string Format(LogEntry e) => AgenteIALocal.Core.Logging.LogEntryTextFormatter.Format(e);
    }
}
