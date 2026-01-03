using System;
using System.Collections.Generic;
using System.Text;

namespace AgenteIALocal.Core.Logging
{
    public static class LogEntryTextFormatter
    {
        public static string Format(LogEntry e)
        {
            if (e == null) return string.Empty;
            var sb = new StringBuilder();

            void Append(string label, string val)
            {
                sb.Append(label);
                sb.Append('=');
                sb.Append(val ?? string.Empty);
                sb.Append(' ');
            }

            Append("ts", e.TimestampUtc.ToString("o"));
            Append("lvl", e.Level.ToString());
            Append("corr", e.CorrelationId ?? "-");
            Append("eid", (e.EventId.Id.ToString() + ":" + (string.IsNullOrEmpty(e.EventId.Name) ? "-" : e.EventId.Name)));

            if (e.Source != null)
            {
                Append("src", e.Source.ToString());
                Append("asm", e.Source.AssemblyName);
                Append("ns", e.Source.Namespace);
                Append("type", e.Source.TypeName);
                Append("member", e.Source.MemberName);
                Append("file", e.Source.FilePath);
                Append("line", e.Source.LineNumber?.ToString() ?? "-");
            }

            Append("thr", "T" + e.ThreadId.ToString() + ":" + (System.Threading.Thread.CurrentThread.Name ?? "-"));

            Append("msg", Sanitize(e.Message));

            if (e.Exception != null)
            {
                Append("ex", Sanitize(e.Exception.GetType().FullName + ": " + e.Exception.Message));
                try { sb.Append(" ex.stack="); sb.Append(Sanitize(e.Exception.ToString())); } catch { }
            }

            if (e.Context != null)
            {
                foreach (var kv in e.Context)
                {
                    sb.Append(kv.Key);
                    sb.Append('=');
                    sb.Append(Sanitize(kv.Value));
                    sb.Append(' ');
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string Sanitize(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\r", " ").Replace("\n", " ");
        }
    }
}
