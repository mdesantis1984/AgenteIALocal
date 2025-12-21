namespace AgenteIALocal.Core.Settings
{
    public interface IAgentProviderSettings
    {
        string BaseUrl { get; set; }
        string ApiKey { get; set; }
        string ChatCompletionsPath { get; set; }
    }
}
