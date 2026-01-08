using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Core.StreamingV2
{
    public static class StreamingV2LogEventIds
    {
        public static readonly LogEventId StreamingV2_Start = new LogEventId(9150, "StreamingV2.Start");
        public static readonly LogEventId StreamingV2_Delta = new LogEventId(9151, "StreamingV2.Delta");
        public static readonly LogEventId StreamingV2_Done = new LogEventId(9152, "StreamingV2.Done");
        public static readonly LogEventId StreamingV2_Canceled = new LogEventId(9153, "StreamingV2.Canceled");
        public static readonly LogEventId StreamingV2_Error = new LogEventId(9154, "StreamingV2.Error");
    }
}
