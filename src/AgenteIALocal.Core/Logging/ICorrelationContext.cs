namespace AgenteIALocal.Core.Logging
{
    public interface ICorrelationContext
    {
        string CorrelationId { get; }
    }
}
