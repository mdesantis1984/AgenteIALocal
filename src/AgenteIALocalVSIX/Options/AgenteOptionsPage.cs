using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace AgenteIALocalVSIX.Options
{
    [Guid("A1B2C3D4-1234-5678-90AB-CDEF12345678")]
    public class AgenteOptionsPage : DialogPage
    {
        // Keys
        private const string CollectionPath = "AgenteIALocal";
        private const string BaseUrlKey = "BaseUrl";
        private const string ModelKey = "Model";
        private const string ApiKeyKey = "ApiKey";

        [Category("Agente IA Local")]
        [DisplayName("Base URL")]
        [Description("Base URL for the AI provider (e.g. http://localhost:####)")]
        public string BaseUrl { get; set; } = string.Empty;

        [Category("Agente IA Local")]
        [DisplayName("Model")]
        [Description("Model identifier to use for the provider")]
        public string Model { get; set; } = string.Empty;

        [Category("Agente IA Local")]
        [DisplayName("API Key")]
        [Description("API Key or token for the provider (optional for local providers)")]
        public string ApiKeyValue { get; set; } = string.Empty;

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();
            try
            {
                var store = new ShellSettingsManager(ServiceProvider.GlobalProvider).GetReadOnlySettingsStore(SettingsScope.UserSettings);
                if (store.CollectionExists(CollectionPath))
                {
                    BaseUrl = store.GetString(CollectionPath, BaseUrlKey, string.Empty);
                    Model = store.GetString(CollectionPath, ModelKey, string.Empty);
                    ApiKeyValue = store.GetString(CollectionPath, ApiKeyKey, string.Empty);
                }
            }
            catch { }
        }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            try
            {
                var store = new ShellSettingsManager(ServiceProvider.GlobalProvider).GetWritableSettingsStore(SettingsScope.UserSettings);
                if (!store.CollectionExists(CollectionPath)) store.CreateCollection(CollectionPath);
                store.SetString(CollectionPath, BaseUrlKey, BaseUrl ?? string.Empty);
                store.SetString(CollectionPath, ModelKey, Model ?? string.Empty);
                store.SetString(CollectionPath, ApiKeyKey, ApiKeyValue ?? string.Empty);
            }
            catch { }
        }
    }
}
