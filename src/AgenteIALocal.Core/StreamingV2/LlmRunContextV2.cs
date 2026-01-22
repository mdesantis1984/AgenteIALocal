using System.Collections.Generic;

namespace AgenteIALocal.Core.StreamingV2
{
    public class LlmRunContextV2
    {
        public string CorrelationId { get; set; }

        public IReadOnlyDictionary<string, string> Ctx { get; set; }
    }
}
