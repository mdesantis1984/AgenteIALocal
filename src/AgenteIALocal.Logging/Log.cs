using Serilog;
using Serilog.Core; // NUEVO - ID: 20260123_015000 - LoggingLevelSwitch
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
        private static LoggingLevelSwitch _levelSwitch; // NUEVO - ID: 20260123_015000 - Para reconfiguration dinámica

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

                    // NUEVO - ID: 20260123_015000 - Crear LoggingLevelSwitch para reconfiguration dinámica
                    _levelSwitch = new LoggingLevelSwitch();

                    var cfg = new LoggerConfiguration();

                    // MODIFICADO - ID: 20260123_015000 - Usar LoggingLevelSwitch en lugar de MinimumLevel directo
                    // Esto permite reconfiguración dinámica sin recrear el logger
                    if (settings.All)
                    {
                        _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    }
                    else
                    {
                        // Calcular nivel mínimo según settings
                        if (settings.Verbose) _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                        else if (settings.Debug) _levelSwitch.MinimumLevel = LogEventLevel.Debug;
                        else if (settings.Information) _levelSwitch.MinimumLevel = LogEventLevel.Information;
                        else if (settings.Warning) _levelSwitch.MinimumLevel = LogEventLevel.Warning;
                        else if (settings.Error) _levelSwitch.MinimumLevel = LogEventLevel.Error;
                        else if (settings.Critical) _levelSwitch.MinimumLevel = LogEventLevel.Fatal;
                        else _levelSwitch.MinimumLevel = LogEventLevel.Information; // default fallback
                    }

                    // Configurar logger con LevelSwitch
                    cfg.MinimumLevel.ControlledBy(_levelSwitch);

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

        // MODIFICADO METODO Reconfigure - ID: 20260123_020100
        // FIX 3: Fallback correcto cuando todos los niveles están desmarcados
        // Si ningún nivel habilitado → usar nivel imposible para bloquear todo
        /// <summary>
        /// Reconfigura los niveles de logging en runtime sin recrear el pipeline.
        /// Usa LoggingLevelSwitch para cambios dinámicos inmediatos.
        /// </summary>
        /// <param name="settings">Nueva configuración de logging. Si null, usa defaults.</param>
        public static void Reconfigure(LogSettings settings)
        {
            if (settings == null) settings = new LogSettings();

            lock (_gate)
            {
                try
                {
                    // Actualizar settings en memoria
                    _settings = settings;

                    // CRÍTICO: NO recrear logger - solo cambiar el LoggingLevelSwitch
                    if (_levelSwitch == null)
                    {
                        System.Diagnostics.Trace.TraceWarning("Log.Reconfigure: _levelSwitch is null - cannot reconfigure. Call Configure first.");
                        return;
                    }

                    // Calcular nuevo nivel mínimo según settings
                    LogEventLevel newLevel;
                    if (settings.All)
                    {
                        newLevel = LogEventLevel.Verbose;
                    }
                    else
                    {
                        // Calcular nivel mínimo más bajo habilitado
                        if (settings.Verbose) newLevel = LogEventLevel.Verbose;
                        else if (settings.Debug) newLevel = LogEventLevel.Debug;
                        else if (settings.Information) newLevel = LogEventLevel.Information;
                        else if (settings.Warning) newLevel = LogEventLevel.Warning;
                        else if (settings.Error) newLevel = LogEventLevel.Error;
                        else if (settings.Critical) newLevel = LogEventLevel.Fatal;
                        else
                        {
                            // FIX 3: Si ningún nivel habilitado, usar nivel imposible para bloquear todo
                            // Serilog no tiene "OFF", pero Fatal+1 efectivamente deshabilita logging
                            newLevel = (LogEventLevel)((int)LogEventLevel.Fatal + 1);
                        }
                    }

                    // Cambiar nivel dinámicamente (SIN recrear logger)
                    _levelSwitch.MinimumLevel = newLevel;

                    // Log la reconfiguración inmediatamente (se verá con el nuevo nivel)
                    Information("-", 9300, "Log.Reconfigure", $"Logging reconfigured: enabled={settings.Enabled}, newLevel={newLevel}, v={settings.Verbose}, d={settings.Debug}, i={settings.Information}, w={settings.Warning}, e={settings.Error}, c={settings.Critical}", null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("Log.Reconfigure failed: " + ex.Message);
                    // Mantener nivel anterior si falla reconfiguración
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

        // MODIFICADO METODO WriteIfEnabled - ID: 20260123_020000
        // FIX CRÍTICO: Eliminar gates manuales - LoggingLevelSwitch ya filtra automáticamente
        // El filtrado por nivel lo hace Serilog con _levelSwitch.MinimumLevel
        private static void WriteIfEnabled(LogEventLevel level, string correlationId, int eventId, string source, string message, Exception ex)
        {
            try
            {
                // ELIMINADO: gate manual por _settings.Enabled - causaba que enabled=false bloqueara TODO
                // ELIMINADO: gates manuales por nivel (_settings.Verbose, etc.) - LoggingLevelSwitch ya filtra

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
