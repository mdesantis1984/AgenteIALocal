using System;

namespace AgenteIALocal.Core.Agents
{
    public sealed class AgentRequest
    {
        public string Prompt { get; set; }
        public string CorrelationId { get; set; }

        // Streaming (optional). When Stream=true, providers may emit partial deltas.
        // OnDelta is invoked with the text delta (may be small chunks).
        public bool Stream { get; set; }

        public Action<string> OnDelta { get; set; }

        // Optional OpenAI-style tuning parameters
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
    }
}
