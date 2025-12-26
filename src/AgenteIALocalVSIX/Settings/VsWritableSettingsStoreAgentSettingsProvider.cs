using System;
using AgenteIALocal.Core.Settings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace AgenteIALocalVSIX.Settings
{
    public sealed class VsWritableSettingsStoreAgentSettingsProvider : IAgentSettingsProvider
    {
        private const string CollectionPath = "AgenteIALocal";

        // LmStudio keys
        private const string LmBaseUrlKey = "LmStudio.BaseUrl";
        private const string LmModelKey = "LmStudio.Model";
        private const string LmApiKeyKey = "LmStudio.ApiKey";
        private const string LmPathKey = "LmStudio.ChatCompletionsPath";

        // JanServer keys
        private const string JanBaseUrlKey = "JanServer.BaseUrl";
        private const string JanApiKeyKey = "JanServer.ApiKey";
        private const string JanPathKey = "JanServer.ChatCompletionsPath";

        // Root provider
        private const string ProviderKey = "Provider";

        private readonly WritableSettingsStore store;

        public VsWritableSettingsStoreAgentSettingsProvider(IServiceProvider serviceProvider)
        {
            var settingsManager = new ShellSettingsManager(serviceProvider);
            store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!store.CollectionExists(CollectionPath))
            {
                store.CreateCollection(CollectionPath);
            }
        }

        // Implement interface with fully-qualified types to avoid ambiguous AgentSettings symbol collisions
        public AgenteIALocal.Core.Settings.AgentSettings Load()
        {
            var s = new AgenteIALocal.Core.Settings.AgentSettings();

            // Provider
            if (store.PropertyExists(CollectionPath, ProviderKey))
            {
                var p = store.GetString(CollectionPath, ProviderKey);
                if (int.TryParse(p, out var pi) && Enum.IsDefined(typeof(AgentProviderType), pi))
                {
                    s.Provider = (AgentProviderType)pi;
                }
            }

            // LmStudio
            s.LmStudio.BaseUrl = store.PropertyExists(CollectionPath, LmBaseUrlKey) ? store.GetString(CollectionPath, LmBaseUrlKey) : string.Empty;
            s.LmStudio.Model = store.PropertyExists(CollectionPath, LmModelKey) ? store.GetString(CollectionPath, LmModelKey) : string.Empty;
            s.LmStudio.ApiKey = store.PropertyExists(CollectionPath, LmApiKeyKey) ? store.GetString(CollectionPath, LmApiKeyKey) : string.Empty;
            s.LmStudio.ChatCompletionsPath = store.PropertyExists(CollectionPath, LmPathKey) ? store.GetString(CollectionPath, LmPathKey) : "/v1/chat/completions";

            // JanServer
            s.JanServer.BaseUrl = store.PropertyExists(CollectionPath, JanBaseUrlKey) ? store.GetString(CollectionPath, JanBaseUrlKey) : string.Empty;
            s.JanServer.ApiKey = store.PropertyExists(CollectionPath, JanApiKeyKey) ? store.GetString(CollectionPath, JanApiKeyKey) : string.Empty;
            s.JanServer.ChatCompletionsPath = store.PropertyExists(CollectionPath, JanPathKey) ? store.GetString(CollectionPath, JanPathKey) : "/v1/chat/completions";

            return s;
        }

        public void Save(AgenteIALocal.Core.Settings.AgentSettings settings)
        {
            if (settings == null) return;

            store.SetString(CollectionPath, ProviderKey, ((int)settings.Provider).ToString());

            store.SetString(CollectionPath, LmBaseUrlKey, settings.LmStudio?.BaseUrl ?? string.Empty);
            store.SetString(CollectionPath, LmModelKey, settings.LmStudio?.Model ?? string.Empty);
            store.SetString(CollectionPath, LmApiKeyKey, settings.LmStudio?.ApiKey ?? string.Empty);
            store.SetString(CollectionPath, LmPathKey, settings.LmStudio?.ChatCompletionsPath ?? "/v1/chat/completions");

            store.SetString(CollectionPath, JanBaseUrlKey, settings.JanServer?.BaseUrl ?? string.Empty);
            store.SetString(CollectionPath, JanApiKeyKey, settings.JanServer?.ApiKey ?? string.Empty);
            store.SetString(CollectionPath, JanPathKey, settings.JanServer?.ChatCompletionsPath ?? "/v1/chat/completions");
        }
    }
}
