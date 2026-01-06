using System;

namespace AgenteIALocal.Core.Models.Agent
{
    // Neutral DTO moved from VSIX.Contracts
    public class AgentHostRequest
    {
        public string RequestId { get; set; }
        public string Action { get; set; }
        public string Timestamp { get; set; }
        public string SolutionName { get; set; }
        public int ProjectCount { get; set; }
        // Correlation id propagated from VSIX execution
        public string CorrelationId { get; set; }

        // Optional streaming controls used by VSIX chat UI.
        public bool Stream { get; set; }

        // Called with delta text chunks when Stream=true and provider supports SSE.
        public Action<string> OnDelta { get; set; }
    }
}
