namespace AgenteIALocal.Core.Interfaces.Agent
{
    /// <summary>
    /// Outcome of executing an agent action.
    /// </summary>
    public interface IAgentResult
    {
        string ActionId { get; }
        bool Success { get; }
        string Output { get; }
        string Error { get; }
    }
}
