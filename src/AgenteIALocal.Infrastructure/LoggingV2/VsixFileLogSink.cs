using System;
using System.IO;
using System.Text;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    public class VsixFileLogSink : ILogSink
    {
        private static readonly object gate = new object();
        private readonly string origin;
        private readonly string logFilePath;

        public VsixFileLogSink(string origin = "AgenteIALocal")
        {
            this.origin = string.IsNullOrWhiteSpace(origin) ? "AgenteIALocal" : origin.Trim();
            try
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? string.Empty;
                var dir = Path.Combine(baseDir, "AgenteIALocal", "logs");
                try { Directory.CreateDirectory(dir); } catch { }
                logFilePath = Path.Combine(dir, "AgenteIALocal.log");
            }
            catch
            {
                logFilePath = null;
            }
        }

        private static bool IsStreamingV2(LogEventId id)
        {
            var v = id.Id;
            return v >= 9150 && v <= 9153;
        }

        private static readonly long RollingMaxBytes = 3L * 1024 * 1024; // 3MB
        private static RollingFileWriter rollingWriter;

        public void Write(LogEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath) || entry == null) return;

                var isStreaming = IsStreamingV2(entry.EventId);
                var isPersistable = entry.Level == LogLevel.Warning || entry.Level == LogLevel.Error || entry.Level == LogLevel.Critical || (isStreaming && entry.Level == LogLevel.Info);
                if (!isPersistable) return;

                var line = AgenteIALocal.Core.Logging.LogEntryTextFormatter.Format(entry);

                // Publish to hub for UI sinks
                try { LogEventHub.Publish(line, entry); } catch { }

                try
                {
                    lock (gate)
                    {
                        if (rollingWriter == null)
                        {
                            rollingWriter = new RollingFileWriter(logFilePath, RollingMaxBytes);
                        }
                        rollingWriter.WriteLine(line);
                    }
                }
                catch { }
            }
            catch
            {
                // Never throw from logging sink
            }
        }
    }
}
