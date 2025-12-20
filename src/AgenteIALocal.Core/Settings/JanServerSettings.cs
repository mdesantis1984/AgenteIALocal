namespace AgenteIALocal.Core.Settings
{
    public class JanServerSettings : IAgentProviderSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChatCompletionsPath { get; set; } = "/v1/chat/completions";
    }
}
