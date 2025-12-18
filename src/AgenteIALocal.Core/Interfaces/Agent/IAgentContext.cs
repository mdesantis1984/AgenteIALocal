using System;

namespace AgenteIALocal.Core.Interfaces.Agent
{
    /// <summary>
    /// Immutable snapshot of agent-relevant context provided to planners.
    /// Implementations should treat WorkspaceSnapshot and Memory as opaque objects.
    /// </summary>
    public interface IAgentContext
    {
        DateTimeOffset Timestamp { get; }
        string Goal { get; }
        object WorkspaceSnapshot { get; }
        object Memory { get; }
    }
}
