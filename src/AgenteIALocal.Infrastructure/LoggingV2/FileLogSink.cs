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

        public void Write(LogEntry entry)
        {
            try
            {
                if (entry == null) return;

                // Persist only warnings/errors (and Critical as error)
                if (entry.Level != LogLevel.Warning && entry.Level != LogLevel.Error && entry.Level != LogLevel.Critical) return;

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
