using System;

namespace AgenteIALocal.Core.Models.Agent
{
    // Neutral DTO moved from VSIX.Contracts
    public class AgentHostResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public string Timestamp { get; set; }
    }
}
