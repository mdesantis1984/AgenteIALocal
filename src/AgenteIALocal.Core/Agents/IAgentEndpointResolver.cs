using System;

namespace AgenteIALocal.Core.Agents
{
    public interface IAgentEndpointResolver
    {
        Uri GetChatCompletionsEndpoint();
    }
}
