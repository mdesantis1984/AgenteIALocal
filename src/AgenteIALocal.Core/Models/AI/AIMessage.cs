using System;

namespace AgenteIALocal.Core.Models.AI
{
    /// <summary>
    /// Represents a role-based message exchanged with an AI provider.
    /// Immutable by design.
    /// </summary>
    public sealed class AIMessage
    {
        public AIMessage(AIMessageRole role, string content)
        {
            Role = role;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Timestamp = DateTimeOffset.UtcNow;
        }

        public AIMessageRole Role { get; }
        public string Content { get; }
        public DateTimeOffset Timestamp { get; }
    }
}
