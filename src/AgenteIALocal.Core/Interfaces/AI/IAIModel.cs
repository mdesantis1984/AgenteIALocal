namespace AgenteIALocal.Core.Interfaces.AI
{
    /// <summary>
    /// Descriptor for an AI model supported by a provider.
    /// </summary>
    public interface IAIModel
    {
        string Id { get; }
        string DisplayName { get; }
        string Provider { get; }
    }
}
