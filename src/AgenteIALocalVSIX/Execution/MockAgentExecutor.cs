using System;
using AgenteIALocal.Core.Models.Agent;

namespace AgenteIALocalVSIX.Execution
{
    internal static class MockAgentExecutor
    {
        public static AgentHostResponse Execute(AgentHostRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            // Deterministic mock response based on request data
            var resp = new AgentHostResponse
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
