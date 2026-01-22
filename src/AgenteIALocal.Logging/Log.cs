using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic; // NUEVO - ID: 20260122_010901 - Para GetRecentLogs
using System.IO;
using AgenteIALocal.Logging.Sinks; // NUEVO - ID: 20260122_010901 - UiLogSink

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
        private static UiLogSink _uiSink; // NUEVO - ID: 20260122_010901 - UI buffer sink

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

                    // MODIFICADO - ID: 20260122_010901 - UI sink + filtrado por niveles (B3 + B4)
                    // B3: UI sink buffer 250 + formato usuario final
                    // B4: MinimumLevel según settings + Async wrapper restaurado
                    _uiSink = new UiLogSink(250);

                    var cfg = new LoggerConfiguration();

                    // B4: Configurar MinimumLevel según settings
                    if (settings.All)
                    {
                        cfg.MinimumLevel.Verbose();
                    }
                    else
                    {
                        // Default: Information como base
                        cfg.MinimumLevel.Information();

                        // Override por nivel específico
                        if (settings.Verbose) cfg.MinimumLevel.Verbose();
                        else if (settings.Debug) cfg.MinimumLevel.Debug();
                        else if (settings.Information) cfg.MinimumLevel.Information();
                        else if (settings.Warning) cfg.MinimumLevel.Warning();
                        else if (settings.Error) cfg.MinimumLevel.Error();
                        else if (settings.Critical) cfg.MinimumLevel.Fatal();
                    }

                    cfg.Enrich.WithProperty("app", settings.AppName ?? "AgenteIALocal")
                        .Enrich.WithProperty("pid", System.Diagnostics.Process.GetCurrentProcess().Id)
                        .Enrich.WithProperty("proc", System.Diagnostics.Process.GetCurrentProcess().ProcessName);

                    // B4: Restaurar Async wrapper con configuración adecuada
                    cfg.WriteTo.Async(a =>
                    {
                        a.File(
                            path: _currentPath,
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: settings.RollingFileSizeBytes > 0 ? settings.RollingFileSizeBytes : 3L * 1024 * 1024,
                            retainedFileCountLimit: settings.RetainedFileCount > 0 ? settings.RetainedFileCount : 10,
                            shared: true,
                            outputTemplate: "ts={Timestamp:O} lvl={Level:u3} corr={corr} eid={eid} src={src} msg={Message:lj} ex={Exception}{NewLine}"
                        );
                    }, bufferSize: 1000, blockWhenFull: false);

                    // B3: Agregar UI sink (siempre síncrono para visibilidad inmediata en panel)
                    cfg.WriteTo.Sink(_uiSink);

                    _logger = cfg.CreateLogger();
                    Serilog.Log.Logger = _logger;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("Log.Configure failed: " + ex.Message);
                    // Last resort: keep previous logger.
                }
            }
        }

        public static void CloseAndFlush()
        {
            try
            {
                Serilog.Log.CloseAndFlush();
            }
            catch (Exception closeEx)
            {
                try { System.Diagnostics.Trace.TraceWarning("Log.CloseAndFlush failed: " + closeEx.Message); } catch { }
            }
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
            catch (Exception logEx)
            {
                try { System.Diagnostics.Trace.TraceWarning("Log.WriteIfEnabled failed: " + logEx.Message); } catch { }
                // never throw
            }
        }

        // NUEVO METODO - ID: 20260122_010902 - B3: API para UI consumir buffer
        /// <summary>
        /// Obtiene las últimas N entradas del buffer UI (formato usuario final).
        /// Thread-safe. Default 250.
        /// </summary>
        public static List<string> GetRecentLogs(int count = 250)
        {
            try
            {
                return _uiSink?.GetRecentLogs(count) ?? new List<string>();
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Trace.TraceWarning("Log.GetRecentLogs failed: " + ex.Message); } catch { }
                return new List<string>();
            }
        }

        // NUEVO METODO - ID: 20260122_010902 - B3: Limpiar buffer UI
        /// <summary>
        /// Limpia el buffer UI. Thread-safe.
        /// </summary>
        public static void ClearUiBuffer()
        {
            try
            {
                _uiSink?.Clear();
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Trace.TraceWarning("Log.ClearUiBuffer failed: " + ex.Message); } catch { }
                // Never throw
            }
        }
    }
}
