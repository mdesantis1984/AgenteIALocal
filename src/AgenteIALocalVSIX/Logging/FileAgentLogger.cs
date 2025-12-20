using System;
using System.IO;
using System.Threading;
using AgenteIALocal.Application.Logging;

namespace AgenteIALocalVSIX.Logging
{
    public sealed class FileAgentLogger : IAgentLogger
    {
        private readonly string origin;
        private readonly string logFilePath;
        private static readonly object gate = new object();

        public FileAgentLogger(string origin)
        {
            this.origin = string.IsNullOrWhiteSpace(origin) ? "AgenteIALocal" : origin.Trim();

            try
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? string.Empty;
                var dir = Path.Combine(baseDir, "AgenteIALocal", "logs");

                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    // ignore
                }

                logFilePath = Path.Combine(dir, "AgenteIALocal.log");
            }
            catch
            {
                logFilePath = null;
            }
        }

        public void Info(string message) => WriteLine("INFO", message, null);
        public void Warn(string message) => WriteLine("WARN", message, null);
        public void Error(string message, Exception ex = null) => WriteLine("ERROR", message, ex);

        private void WriteLine(string level, string message, Exception ex)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath)) return;

                var ts = DateTimeOffset.UtcNow.ToString("o");
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var threadName = Thread.CurrentThread.Name;

                var safeMsg = message ?? string.Empty;
                safeMsg = safeMsg.Replace("\r", " ").Replace("\n", " ");

                var line = $"{ts} [{level}] [{origin}] [T{threadId}{(string.IsNullOrEmpty(threadName) ? "" : ":" + threadName)}] {safeMsg}";
                if (ex != null)
                {
                    var exMsg = (ex.Message ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                    line += $" | ex={ex.GetType().FullName}: {exMsg}";
                }

                lock (gate)
                {
                    File.AppendAllText(logFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // fail-safe: never throw
            }
        }
    }
}
