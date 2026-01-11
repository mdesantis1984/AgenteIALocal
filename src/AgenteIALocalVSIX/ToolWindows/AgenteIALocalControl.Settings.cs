// NUEVO ARCHIVO AgenteIALocalControl.Settings.cs - Configuración y estado LLM
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        private void PopulateSettingsPanel(AgentSettings settings)
        {
            if (settings == null) return;

            var activeIdTb = GetElement<TextBox>("ActiveServerIdTextBox");
            var baseUrlTb = GetElement<TextBox>("ServerBaseUrlTextBox");
            var modelTb = GetElement<TextBox>("ServerModelTextBox");
            var apiKeyTb = GetElement<TextBox>("ServerApiKeyTextBox");

            if (activeIdTb != null)
                activeIdTb.Text = settings.ActiveServerId ?? string.Empty;

            if (!string.IsNullOrEmpty(settings.ActiveServerId) && settings.Servers != null)
            {
                var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                if (srv != null)
                {
                    if (baseUrlTb != null) baseUrlTb.Text = srv.BaseUrl ?? string.Empty;
                    if (modelTb != null) modelTb.Text = srv.Model ?? string.Empty;
                    if (apiKeyTb != null) apiKeyTb.Text = srv.ApiKey ?? string.Empty;
                }
            }
            else if (settings.Servers != null && settings.Servers.Count > 0)
            {
                var srv = settings.Servers[0];
                if (baseUrlTb != null) baseUrlTb.Text = srv.BaseUrl ?? string.Empty;
                if (modelTb != null) modelTb.Text = srv.Model ?? string.Empty;
                if (apiKeyTb != null) apiKeyTb.Text = srv.ApiKey ?? string.Empty;
                if (activeIdTb != null) activeIdTb.Text = srv.Id ?? string.Empty;
            }
        }

        private void ComputeIsLlmConfigured(AgentSettings settings)
        {
            try
            {
                bool configured = false;
                string activeId = null;
                bool baseUrlPresent = false;
                bool modelPresent = false;

                if (settings != null)
                {
                    activeId = settings.ActiveServerId;
                    if (!string.IsNullOrEmpty(activeId) && settings.Servers != null)
                    {
                        var srv = settings.Servers.Find(s => s.Id == activeId);
                        if (srv != null)
                        {
                            baseUrlPresent = !string.IsNullOrWhiteSpace(srv.BaseUrl);
                            modelPresent = !string.IsNullOrWhiteSpace(srv.Model);
                            if (baseUrlPresent && modelPresent) configured = true;
                        }
                    }
                }

                IsLlmConfigured = configured;
                ConfigLabel = configured ? "OK Config" : "Not Config";

                try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigStatus: computed configured={configured} activeServerId={activeId ?? "(none)"} baseUrlPresent={baseUrlPresent} modelPresent={modelPresent}"); } catch { }
            }
            catch
            {
                IsLlmConfigured = false;
                ConfigLabel = "Not Config";
                try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigStatus: compute error, defaulted to Not Config"); } catch { }
            }
        }
    }
}
