using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgenteIALocal.Logging.Sinks
{
    /// <summary>
    /// NUEVA CLASE - ID: 20260122_010900
    /// Custom Serilog sink que mantiene buffer circular de 250 entradas para UI.
    /// Thread-safe. Formato usuario final (sin metadatos técnicos).
    /// </summary>
    public sealed class UiLogSink : ILogEventSink
    {
        private readonly object _gate = new object();
        private readonly int _capacity;
        private readonly Queue<string> _buffer;

        public UiLogSink(int capacity = 250)
        {
            _capacity = capacity > 0 ? capacity : 250;
            _buffer = new Queue<string>(_capacity);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            try
            {
                // Formato usuario final: HH:mm:ss [NIVEL] mensaje
                var timestamp = logEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss");
                var level = FormatLevel(logEvent.Level);
                var message = logEvent.RenderMessage();

                // Si hay excepción, agregar al mensaje
                if (logEvent.Exception != null)
                {
                    message = $"{message} | Exception: {logEvent.Exception.Message}";
                }

                var line = $"{timestamp} [{level}] {message}";

                lock (_gate)
                {
                    // Buffer circular: si excede capacidad, eliminar el más antiguo
                    if (_buffer.Count >= _capacity)
                    {
                        _buffer.Dequeue();
                    }

                    _buffer.Enqueue(line);
                }
            }
            catch
            {
                // Sink MUST NOT throw - swallow all exceptions
            }
        }

        /// <summary>
        /// Obtiene las últimas N entradas del buffer (thread-safe).
        /// </summary>
        public List<string> GetRecentLogs(int count = 250)
        {
            lock (_gate)
            {
                // Queue<T> no tiene TakeLast en .NET Standard 2.0
                // Convertir a lista y tomar últimos N
                var all = _buffer.ToList();
                var take = Math.Min(count, all.Count);
                
                if (take <= 0) return new List<string>();
                if (take >= all.Count) return all;
                
                return all.Skip(all.Count - take).ToList();
            }
        }

        /// <summary>
        /// Limpia el buffer (thread-safe).
        /// </summary>
        public void Clear()
        {
            lock (_gate)
            {
                _buffer.Clear();
            }
        }

        private static string FormatLevel(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose: return "VRB";
                case LogEventLevel.Debug: return "DBG";
                case LogEventLevel.Information: return "INF";
                case LogEventLevel.Warning: return "WRN";
                case LogEventLevel.Error: return "ERR";
                case LogEventLevel.Fatal: return "CRT";
                default: return "???";
            }
        }
    }
}
