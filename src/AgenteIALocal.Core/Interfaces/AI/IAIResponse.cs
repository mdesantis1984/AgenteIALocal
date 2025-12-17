using System;

namespace AgenteIALocal.Core.Interfaces.AI
{
    /// <summary>
    /// Neutral response representation from an AI provider.
    /// </summary>
    public interface IAIResponse
    {
        string Content { get; }
        bool IsSuccess { get; }
        string ErrorMessage { get; }
        TimeSpan Duration { get; }
    }
}
