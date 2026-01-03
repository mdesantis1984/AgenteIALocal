namespace AgenteIALocal.Core.Logging
{
    public interface ILogSink
    {
        void Write(LogEntry entry);
    }
}
