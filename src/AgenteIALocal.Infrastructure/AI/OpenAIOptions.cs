using System;

namespace AgenteIALocal.Infrastructure.AI
{
    /// <summary>
    /// Configuration options for the OpenAI provider.
    /// API key must not be hardcoded; prefer environment variables.
    /// </summary>
    public sealed class OpenAIOptions
    {
        /// <summary>
        /// API key for OpenAI. If null or empty, the provider will attempt to read from environment variable OPENAI_API_KEY.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Base URL for the OpenAI API. Defaults to the Chat Completions endpoint.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// Request timeout in seconds. Null means provider default (100 seconds for HttpClient).
        /// </summary>
        public int? TimeoutSeconds { get; set; }

        /// <summary>
        /// Allow reading the API key from environment if ApiKey is not provided.
        /// </summary>
        public bool AllowEnvironmentApiKey { get; set; } = true;

        public string GetEffectiveApiKey()
        {
            if (!string.IsNullOrEmpty(ApiKey)) return ApiKey;
            if (AllowEnvironmentApiKey)
            {
                try
                {
                    var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                    if (!string.IsNullOrEmpty(env)) return env;
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }
    }
}
