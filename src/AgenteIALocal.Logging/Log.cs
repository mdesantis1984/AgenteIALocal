using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace AgenteIALocal.Logging
{
    /// <summary>
    /// Global logging facade (Serilog). Safe-by-default: never throws.
    /// </summary>
    public static class Log
    {
        private static readonly object _gate = new object();
        private static LogSettings _settings = new LogSettings();
        private static ILogger _logger = Serilog.Log.Logger;
        private static string _currentPath;

        public static LogSettings CurrentSettings
        {
            get { return _settings; }
        }

        public static string CurrentLogFilePath
        {
            get { return _currentPath ?? string.Empty; }
        }

        public static void Configure(LogSettings settings)
        {
            if (settings == null) settings = new LogSettings();

            lock (_gate)
            {
                try
                {
                    _settings = settings;

                    var dir = settings.ResolveLogDirectory();
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    // MODIFICADO - ID: 20260122_010700 - Path con separador para RollingInterval.Day
                    // Genera: AgenteIALocal_yyyyMMdd.log (ej: AgenteIALocal_20260122.log)
                    _currentPath = Path.Combine(dir, "AgenteIALocal_.log");

                    // Always build the pipeline even if disabled so errors during config are visible.
                    // MODIFICADO - ID: 20260122_010400 - Async wrapper TEMPORALMENTE removido para diagnóstico B1
                    // TODO: Restaurar WriteTo.Async en B4 cuando implementemos UI sink + buffer config
                    // MODIFICADO - ID: 20260122_010700 - RollingInterval.Day para naming con fecha ISO (yyyyMMdd)
                    var cfg = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .Enrich.WithProperty("app", settings.AppName ?? "AgenteIALocal")
                        .Enrich.WithProperty("pid", System.Diagnostics.Process.GetCurrentProcess().Id)
                        .Enrich.WithProperty("proc", System.Diagnostics.Process.GetCurrentProcess().ProcessName)
                        .WriteTo.File(
                            path: _currentPath,
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: settings.RollingFileSizeBytes > 0 ? settings.RollingFileSizeBytes : 3L * 1024 * 1024,
                            retainedFileCountLimit: settings.RetainedFileCount > 0 ? settings.RetainedFileCount : 10,
                            shared: true,
                            outputTemplate: "ts={Timestamp:O} lvl={Level:u3} corr={corr} eid={eid} src={src} msg={Message:lj} ex={Exception}{NewLine}"
                        );

                    _logger = cfg.CreateLogger();
                    Serilog.Log.Logger = _logger;
                }
                catch
                {
                    // Last resort: keep previous logger.
                }
            }
        }

        public static void CloseAndFlush()
        {
            try { Serilog.Log.CloseAndFlush(); } catch { }
        }

        public static void Verbose(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Verbose, correlationId, eventId, source, message, ex);

        public static void Debug(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Debug, correlationId, eventId, source, message, ex);

        public static void Information(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Information, correlationId, eventId, source, message, ex);

        public static void Warning(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Warning, correlationId, eventId, source, message, ex);

        public static void Error(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Error, correlationId, eventId, source, message, ex);

        public static void Critical(string correlationId, int eventId, string source, string message, Exception ex = null)
            => WriteIfEnabled(LogEventLevel.Fatal, correlationId, eventId, source, message, ex);

        private static void WriteIfEnabled(LogEventLevel level, string correlationId, int eventId, string source, string message, Exception ex)
        {
            try
            {
                if (_settings == null) _settings = new LogSettings();
                if (!_settings.Enabled) return;

                if (!_settings.All)
                {
                    if (level == LogEventLevel.Verbose && !_settings.Verbose) return;
                    if (level == LogEventLevel.Debug && !_settings.Debug) return;
                    if (level == LogEventLevel.Information && !_settings.Information) return;
                    if (level == LogEventLevel.Warning && !_settings.Warning) return;
                    if (level == LogEventLevel.Error && !_settings.Error) return;
                    if (level == LogEventLevel.Fatal && !_settings.Critical) return;
                }

                var corr = string.IsNullOrEmpty(correlationId) ? "-" : correlationId;
                var src = string.IsNullOrEmpty(source) ? "-" : source;

                var l = _logger ?? Serilog.Log.Logger;

                // Bind properties used by output template.
                if (ex != null)
                {
                    l.ForContext("corr", corr)
                     .ForContext("eid", eventId)
                     .ForContext("src", src)
                     .Write(level, ex, message ?? string.Empty);
                }
                else
                {
                    l.ForContext("corr", corr)
                     .ForContext("eid", eventId)
                     .ForContext("src", src)
                     .Write(level, message ?? string.Empty);
                }
            }
            catch
            {
                // never throw
            }
        }
    }
}
