using System;
using AgenteIALocalVSIX.Contracts;

namespace AgenteIALocalVSIX.Execution
{
    internal static class MockCopilotExecutor
    {
        public static CopilotResponse Execute(CopilotRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            // Deterministic mock response based on request data
            var resp = new CopilotResponse
            {
                RequestId = req.RequestId,
                Success = true,
                Output = $"Mock executed for action '{req.Action}'. Solution '{req.SolutionName}' with {req.ProjectCount} projects.",
                Error = null,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            return resp;
        }
    }
}
