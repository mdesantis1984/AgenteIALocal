using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    public class VsixLegacyFileLogSink : ILogSink
    {
        private static readonly object gate = new object();
        private readonly string origin;
        private readonly string logFilePath;

        public VsixLegacyFileLogSink(string origin = "AgenteIALocal")
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

                // Map level
                string levelToken;
                switch (entry.Level)
                {
                    case LogLevel.Verbose:
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        levelToken = "INFO";
                        break;
                    case LogLevel.Warning:
                        levelToken = "WARN";
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        levelToken = "ERROR";
                        break;
                    default:
                        levelToken = "INFO";
                        break;
                }

                // Timestamp with +00:00
                var dto = new DateTimeOffset(entry.TimestampUtc, TimeSpan.Zero);
                var ts = dto.ToString("o");

                // Thread info
                var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                var threadName = System.Threading.Thread.CurrentThread.Name;

                // Message and exception sanitized
                var safeMsg = entry.Message ?? string.Empty;
                safeMsg = safeMsg.Replace("\r", " ").Replace("\n", " ");

                var sb = new StringBuilder();
                sb.Append(ts);
                sb.Append(" ["); sb.Append(levelToken); sb.Append("] ");
                sb.Append('['); sb.Append(origin); sb.Append("] ");
                sb.Append("[T"); sb.Append(threadId);
                if (!string.IsNullOrEmpty(threadName)) { sb.Append(":"); sb.Append(threadName); }
                sb.Append("] ");
                sb.Append(safeMsg);

                if (entry.Exception != null)
                {
                    var exMsg = (entry.Exception.Message ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                    sb.Append(" | ex="); sb.Append(entry.Exception.GetType().FullName); sb.Append(": "); sb.Append(exMsg);
                }

                var line = sb.ToString();

                lock (gate)
                {
                    File.AppendAllText(logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Never throw from logging sink
            }
        }
    }
}
