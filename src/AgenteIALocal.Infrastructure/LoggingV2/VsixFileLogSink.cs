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

        public void Write(LogEntry entry)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath) || entry == null) return;

                // Persist only warnings/errors (and Critical as error)
                if (entry.Level != LogLevel.Warning && entry.Level != LogLevel.Error && entry.Level != LogLevel.Critical) return;

                var line = AgenteIALocal.Core.Logging.LogEntryTextFormatter.Format(entry);

                lock (gate)
                {
                    try
                    {
                        LogFileRolling.EnsureRolled(logFilePath, LogFileRolling.MaxBytesDefault);
                        File.AppendAllText(logFilePath, line + Environment.NewLine, Encoding.UTF8);
                    }
                    catch { }
                }
            }
            catch
            {
                // Never throw from logging sink
            }
        }
    }
}
