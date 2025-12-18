using System;

namespace AgenteIALocalVSIX.Contracts
{
    internal class CopilotResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public string Timestamp { get; set; }
    }
}
