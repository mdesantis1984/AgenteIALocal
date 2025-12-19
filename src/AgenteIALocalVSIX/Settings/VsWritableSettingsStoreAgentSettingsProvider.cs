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
        private const string BaseUrlKey = "BaseUrl";
        private const string ModelKey = "Model";
        private const string ApiKeyKey = "ApiKey";

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

        public AgentSettings Load()
        {
            return new AgentSettings
            {
                BaseUrl = store.PropertyExists(CollectionPath, BaseUrlKey) ? store.GetString(CollectionPath, BaseUrlKey) : string.Empty,
                Model = store.PropertyExists(CollectionPath, ModelKey) ? store.GetString(CollectionPath, ModelKey) : string.Empty,
                ApiKey = store.PropertyExists(CollectionPath, ApiKeyKey) ? store.GetString(CollectionPath, ApiKeyKey) : string.Empty
            };
        }

        public void Save(AgentSettings settings)
        {
            store.SetString(CollectionPath, BaseUrlKey, settings.BaseUrl ?? string.Empty);
            store.SetString(CollectionPath, ModelKey, settings.Model ?? string.Empty);
            store.SetString(CollectionPath, ApiKeyKey, settings.ApiKey ?? string.Empty);
        }
    }
}
