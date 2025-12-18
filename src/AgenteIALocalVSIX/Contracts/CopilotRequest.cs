using System;

namespace AgenteIALocalVSIX.Contracts
{
    internal class CopilotRequest
    {
        public string RequestId { get; set; }
        public string Action { get; set; }
        public string Timestamp { get; set; }
        public string SolutionName { get; set; }
        public int ProjectCount { get; set; }
    }
}
