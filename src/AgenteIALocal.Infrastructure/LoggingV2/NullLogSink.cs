using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    public class NullLogSink : ILogSink
    {
        public void Write(LogEntry entry)
        {
            // no-op
        }
    }
}
