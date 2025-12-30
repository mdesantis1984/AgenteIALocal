using System;
using System.Diagnostics;
using AgenteIALocal.Application.Logging;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Application.LoggingV2
{
    /// <summary>
    /// Adapter that implements the legacy IAgentLogger contract and delegates
    /// to the newer IAgentLoggerV2 interface. Designed to be added in the
    /// Application layer so it can be used to replace implementations without
    /// changing call-sites.
    /// </summary>
    public sealed class LegacyAgentLoggerAdapter : IAgentLogger
    {
        private readonly IAgentLoggerV2 v2;
        private readonly string defaultCorrelationId;

        public LegacyAgentLoggerAdapter(IAgentLoggerV2 v2, string correlationId = "-")
        {
            this.v2 = v2 ?? throw new ArgumentNullException(nameof(v2));
            this.defaultCorrelationId = string.IsNullOrEmpty(correlationId) ? "-" : correlationId;
        }

        public void Info(string message)
        {
            try
            {
                var ci = GetCallerInfo();
                v2.Info(defaultCorrelationId, new LogEventId(1000, "Legacy.Info"), message, null, null, ci.member, ci.file, ci.line, ci.ns, ci.type, ci.assembly);
            }
            catch
            {
                // fail-safe
            }
        }

        public void Warn(string message)
        {
            try
            {
                var ci = GetCallerInfo();
                v2.Warning(defaultCorrelationId, new LogEventId(1001, "Legacy.Warn"), message, null, null, ci.member, ci.file, ci.line, ci.ns, ci.type, ci.assembly);
            }
            catch
            {
                // fail-safe
            }
        }

        public void Error(string message, Exception ex = null)
        {
            try
            {
                var ci = GetCallerInfo();
                v2.Error(defaultCorrelationId, new LogEventId(1002, "Legacy.Error"), message, ex, null, ci.member, ci.file, ci.line, ci.ns, ci.type, ci.assembly);
            }
            catch
            {
                // fail-safe
            }
        }

        private (string member, string file, int? line, string ns, string type, string assembly) GetCallerInfo()
        {
            try
            {
                // Skip frames until we find an external caller (skip this adapter frame)
                var stack = new StackTrace(true);
                for (int i = 0; i < stack.FrameCount; i++)
                {
                    var frame = stack.GetFrame(i);
                    var method = frame?.GetMethod();
                    if (method == null) continue;

                    var declaringType = method.DeclaringType;
                    if (declaringType == null) continue;

                    // Skip frames that are inside this adapter type
                    if (declaringType == typeof(LegacyAgentLoggerAdapter)) continue;

                    var member = method.Name ?? string.Empty;
                    var file = frame.GetFileName();
                    int line = 0;
                    try { line = frame.GetFileLineNumber(); } catch { line = 0; }
                    var ns = declaringType.Namespace ?? string.Empty;
                    var typeName = declaringType.FullName ?? declaringType.Name;
                    var asm = declaringType.Assembly?.GetName()?.Name ?? string.Empty;

                    return (member, file, line == 0 ? (int?)null : line, ns, typeName, asm);
                }
            }
            catch
            {
                // ignore
            }

            return (member: null, file: null, line: null, ns: null, type: null, assembly: null);
        }
    }
}
