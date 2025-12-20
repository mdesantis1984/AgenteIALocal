namespace AgenteIALocal.Application.Logging
{
    public interface IAgentLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, System.Exception ex = null);
    }
}
