using System;
using System.Collections.Generic;
using System.Text;

namespace AgenteIALocal.Logging
{
    public enum AgentLogLevel { Info, Error }

    public sealed class AgentLogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public AgentLogLevel Level { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            var level = Level == AgentLogLevel.Info ? "INFO" : "ERROR";
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {level}: {Message}";
        }
    }

    public class InMemoryAgentLogger
    {
        private readonly List<AgentLogEntry> entries = new List<AgentLogEntry>();
        private readonly object sync = new object();

        public void Info(string message)
        {
            Add(AgentLogLevel.Info, message);
        }

        public void Error(string message)
        {
            Add(AgentLogLevel.Error, message);
        }

        private void Add(AgentLogLevel level, string message)
        {
            var e = new AgentLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = level,
                Message = message ?? string.Empty
            };
            lock (sync)
            {
                entries.Add(e);
            }
        }

        public IReadOnlyList<AgentLogEntry> GetEntries()
        {
            lock (sync)
            {
                return entries.ToArray();
            }
        }

        public string GetFormattedLog()
        {
            var sb = new StringBuilder();
            var list = GetEntries();
            foreach (var e in list)
            {
                sb.AppendLine(e.ToString());
            }
            return sb.ToString();
        }
    }
}
