using System.Collections.Generic;

namespace AgenteIALocal.Core.Interfaces.AI
{
    /// <summary>
    /// Neutral request for AI inference.
    /// </summary>
    public interface IAIRequest
    {
        string Prompt { get; }
        string SystemContext { get; }
        IAIModel Model { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
