using System.Collections.Generic;

namespace AgenteIALocal.Core.Interfaces.Agent
{
    /// <summary>
    /// Represents a single agent action. Read-only DTO consumed by executors.
    /// </summary>
    public interface IAgentAction
    {
        string Id { get; }
        string Description { get; }
        string Type { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
