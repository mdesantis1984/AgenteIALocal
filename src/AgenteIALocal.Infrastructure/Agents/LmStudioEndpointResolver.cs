using System;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocal.Infrastructure.Agents
{
    public sealed class LmStudioEndpointResolver : IAgentEndpointResolver
    {
        private readonly LmStudioSettings settings;

        public LmStudioEndpointResolver(LmStudioSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Uri GetChatCompletionsEndpoint()
        {
            var baseUrl = settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
            var path = settings.ChatCompletionsPath ?? string.Empty;
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(path)) return null;

            var combined = baseUrl + (path.StartsWith("/") ? string.Empty : "/") + path;
            return new Uri(combined);
        }
    }
}
