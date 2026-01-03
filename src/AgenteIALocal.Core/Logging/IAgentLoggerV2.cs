using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AgenteIALocal.Core.Logging
{
    public interface IAgentLoggerV2
    {
        void Log(LogEntry entry);

        void Verbose(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);

        void Debug(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);

        void Info(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);

        void Warning(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);

        void Error(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);

        void Critical(string correlationId, LogEventId eventId, string message, Exception ex = null, IReadOnlyDictionary<string,string> ctx = null, [CallerMemberName] string member = null, [CallerFilePath] string file = null, [CallerLineNumber] int? line = null, string ns = null, string type = null, string assembly = null);
    }
}
