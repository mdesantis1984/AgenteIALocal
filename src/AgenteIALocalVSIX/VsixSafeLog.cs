using System;
using AgenteIALocal.Core.Logging;
using AgenteIALocal.Infrastructure.LoggingV2;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocalVSIX
{
    /// <summary>
    /// Minimal, always-safe logger for early startup paths.
    /// - Never throws
    /// - Writes to VS ActivityLog when available
    /// - Writes to %LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log
    /// </summary>
    internal static class VsixSafeLog
    {
        private static readonly object Gate = new object();
        private static VsixFileLogSink _fileSink;

        private static void EnsureFileSink()
        {
            if (_fileSink != null) return;

            lock (Gate)
            {
                if (_fileSink != null) return;
                try { _fileSink = new VsixFileLogSink("AgenteIALocal"); }
                catch { _fileSink = null; }
            }
        }

        internal static void Info(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Info, source, message, ex, eventId);

        internal static void Warning(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Warning, source, message, ex, eventId);

        internal static void Error(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Error, source, message, ex, eventId);

        internal static void Critical(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Critical, source, message, ex, eventId);

        private static void Write(LogLevel level, string source, string message, Exception ex, int eventId)
        {
            // Always safe: never throw.
            try
            {
                var src = string.IsNullOrWhiteSpace(source) ? "AgenteIALocal" : source.Trim();
                var msg = message ?? string.Empty;

                try
                {
                    var detail = ex == null ? msg : msg + Environment.NewLine + ex;

                    if (level == LogLevel.Error || level == LogLevel.Critical)
                        ActivityLog.LogError(src, detail);
                    else if (level == LogLevel.Warning)
                        ActivityLog.LogWarning(src, detail);
                    else
                        ActivityLog.LogInformation(src, detail);
                }
                catch { }

                try
                {
                    EnsureFileSink();
                    if (_fileSink == null) return;

                    var entry = new LogEntry(
                        timestampUtc: DateTime.UtcNow,
                        level: level,
                        correlationId: "-",
                        eventId: new LogEventId(eventId, src),
                        message: msg,
                        exception: ex,
                        source: new LogSource(assemblyName: nameof(AgenteIALocalVSIX), @namespace: nameof(AgenteIALocalVSIX), typeName: nameof(VsixSafeLog), memberName: "Write"),
                        context: null);

                    _fileSink.Write(entry);
                }
                catch { }
            }
            catch { }
        }
    }
}
