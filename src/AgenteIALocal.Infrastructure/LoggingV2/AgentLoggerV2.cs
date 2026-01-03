using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    public class AgentLoggerV2 : IAgentLoggerV2
    {
        private readonly ILogSink sink;
        private readonly string assemblyName;

        public AgentLoggerV2(ILogSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            try { assemblyName = typeof(AgentLoggerV2).Assembly.GetName().Name; } catch { assemblyName = string.Empty; }
        }

        public void Log(LogEntry entry)
        {
            try { sink.Write(entry); } catch { }
        }

        private void Write(LogLevel level, string correlationId, LogEventId eventId, string message, Exception ex, IReadOnlyDictionary<string,string> ctx, string ns, string type, string assembly, string member, string file, int? line)
        {
            var corr = string.IsNullOrEmpty(correlationId) ? "-" : correlationId;
            var asm = string.IsNullOrEmpty(assembly) ? assemblyName : assembly;
            var src = CallerInfo.Create(asm, ns, type, member, file, line);
            var entry = new LogEntry(DateTime.UtcNow, level, corr, eventId, message, ex, src, ctx);
            Log(entry);
        }

        public void Verbose(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Verbose, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }

        public void Debug(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Debug, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }

        public void Info(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Info, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }

        public void Warning(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Warning, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }

        public void Error(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Error, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }

        public void Critical(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null)
        {
            Write(LogLevel.Critical, correlationId, eventId, message, ex, ctx, ns, type, assembly, member, file, line);
        }
    }
}
