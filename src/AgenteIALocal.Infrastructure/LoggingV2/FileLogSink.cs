using System;
using System.IO;
using System.Text;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    internal class FileLogSink : ILogSink, IDisposable
    {
        private readonly string path;
        private readonly object fileLock = new object();

        public FileLogSink(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            path = filePath;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            catch { }
        }

        private static bool IsStreamingV2(LogEventId id)
        {
            var v = id.Id;
            return v >= 9150 && v <= 9153;
        }

        public void Write(LogEntry entry)
        {
            try
            {
                if (entry == null) return;

                var isStreaming = IsStreamingV2(entry.EventId);
                var isPersistable = entry.Level == LogLevel.Warning || entry.Level == LogLevel.Error || entry.Level == LogLevel.Critical || (isStreaming && entry.Level == LogLevel.Info);
                if (!isPersistable) return;

                var line = LogEntryTextFormatter.Format(entry);

                lock (fileLock)
                {
                    try
                    {
                        LogFileRolling.EnsureRolled(path, LogFileRolling.MaxBytesDefault);
                        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                    }
                    catch { }
                }
            }
            catch
            {
                // swallow all
            }
        }

        public void Dispose() { }
    }
}
