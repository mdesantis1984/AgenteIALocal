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
    }
}
