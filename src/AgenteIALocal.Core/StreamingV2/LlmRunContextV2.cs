using System.Collections.Generic;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Core.StreamingV2
{
    public class LlmRunContextV2
    {
        public string CorrelationId { get; set; }

        public IAgentLoggerV2 Logger { get; set; }

        public IReadOnlyDictionary<string, string> Ctx { get; set; }
    }
}
