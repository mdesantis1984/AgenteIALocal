using AgenteIALocal.Application.Agents;
using AgenteIALocal.Application.Logging;
using AgenteIALocal.Core.Agents;

namespace AgenteIALocalVSIX
{
    public static class AgentComposition
    {
        public static IAgentClient AgentClient { get; set; }
        public static IAgentService AgentService { get; set; }
        public static IAgentLogger Logger { get; set; }
    }
}
