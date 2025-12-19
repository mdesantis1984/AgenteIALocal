using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocalVSIX.Options
{
    public sealed class AgenteIALocalOptionsPage : DialogPage
    {
        internal IAgentSettingsProvider SettingsProvider { get; set; }

        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            var settings = SettingsProvider?.Load();
            if (settings != null)
            {
                BaseUrl = settings.BaseUrl;
                Model = settings.Model;
                ApiKey = settings.ApiKey;
            }
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            SettingsProvider?.Save(new AgentSettings
            {
                BaseUrl = BaseUrl ?? string.Empty,
                Model = Model ?? string.Empty,
                ApiKey = ApiKey ?? string.Empty
            });
        }
    }
}
