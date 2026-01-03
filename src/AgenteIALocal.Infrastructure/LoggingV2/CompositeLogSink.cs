using System;
using System.Collections.Generic;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    public class CompositeLogSink : ILogSink
    {
        private readonly IReadOnlyList<ILogSink> sinks;

        public CompositeLogSink(IEnumerable<ILogSink> sinks)
        {
            this.sinks = sinks == null ? new List<ILogSink>() : new List<ILogSink>(sinks);
        }

        public void Write(LogEntry entry)
        {
            foreach (var s in sinks)
            {
                try
                {
                    s.Write(entry);
                }
                catch
                {
                    // swallow to ensure no sink breaks pipeline
                }
            }
        }
    }
}
