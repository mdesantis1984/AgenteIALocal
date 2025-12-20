using System;

namespace AgenteIALocal.Core.Settings
{
    public sealed class AgentSettings
    {
        public AgentProviderType Provider { get; set; } = AgentProviderType.LmStudio;

        public LmStudioSettings LmStudio { get; set; } = new LmStudioSettings();
        public JanServerSettings JanServer { get; set; } = new JanServerSettings();

        // Helpers to get/set active provider settings polymorphically
        public IAgentProviderSettings GetActiveProviderSettings()
        {
            switch (Provider)
            {
                case AgentProviderType.JanServer:
                    return JanServer;
                case AgentProviderType.LmStudio:
                default:
                    return LmStudio;
            }
        }

        public void SetActiveProviderSettings(IAgentProviderSettings settings)
        {
            if (settings == null) return;

            switch (Provider)
            {
                case AgentProviderType.JanServer:
                    if (settings is JanServerSettings js) JanServer = js;
                    else
                    {
                        // map common fields
                        JanServer.BaseUrl = settings.BaseUrl ?? string.Empty;
                        JanServer.ApiKey = settings.ApiKey ?? string.Empty;
                        JanServer.ChatCompletionsPath = settings.ChatCompletionsPath ?? string.Empty;
                    }
                    break;
                case AgentProviderType.LmStudio:
                default:
                    if (settings is LmStudioSettings ls) LmStudio = ls;
                    else
                    {
                        LmStudio.BaseUrl = settings.BaseUrl ?? string.Empty;
                        LmStudio.ApiKey = settings.ApiKey ?? string.Empty;
                        LmStudio.ChatCompletionsPath = settings.ChatCompletionsPath ?? string.Empty;
                    }
                    break;
            }
        }
    }
}
