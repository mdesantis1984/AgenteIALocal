using System;

namespace AgenteIALocal.Core.Logging
{
    public struct LogEventId : IEquatable<LogEventId>
    {
        public LogEventId(int id, string name = null)
        {
            Id = id;
            Name = name ?? string.Empty;
        }

        public int Id { get; }
        public string Name { get; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name)) return Id.ToString();
            return $"{Id} ({Name})";
        }

        public bool Equals(LogEventId other) => Id == other.Id && string.Equals(Name, other.Name);
        public override bool Equals(object obj) => obj is LogEventId other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode() ^ (Name?.GetHashCode() ?? 0);
    }
}
