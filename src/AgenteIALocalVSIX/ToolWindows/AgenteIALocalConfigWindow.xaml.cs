using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Net.Http;
using System.Windows.Controls;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalConfigWindow : Window
    {
        private readonly string initialServerId;
        // NUEVO CAMPO BaseUrlPingCts - ID: 20260114_000072
        private CancellationTokenSource _baseUrlPingCts;
        // NUEVO CAMPO LastPersistedBaseUrl - ID: 20260114_000073
        private string _lastPersistedBaseUrl;
        private bool _isInitializingAdvancedUi;
        // NUEVO EVENTO BaseUrlHealthChanged - ID: 20260114_000079
        public event Action<bool, string, IReadOnlyList<string>> BaseUrlHealthChanged;

        public AgenteIALocalConfigWindow(string serverId = null)
        {
            InitializeComponent();
            // Attach drag handler for custom header
            try
            {
                HeaderDragArea.MouseLeftButtonDown += HeaderDragArea_MouseLeftButtonDown;
            }
            catch { }

            try
            {
                //if (CloseButton != null)
                //{
                //    CloseButton.PreviewMouseLeftButtonDown += CloseButton_PreviewMouseLeftButtonDown;
                //}
            }
            catch { }

            this.Loaded += AgenteIALocalConfigWindow_Loaded;
            this.initialServerId = serverId ?? string.Empty;
            if (!string.IsNullOrEmpty(serverId))
            {
                this.Title = $"Configuración — {serverId}";
            }
        }

        private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // prevent header drag from stealing the click
            e.Handled = true;
        }

        private void HeaderDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left) return;

                // If click originated within CloseButton visual tree, ignore
                try
                {
                    //if (CloseButton != null)
                    //{
                    //    var src = e.OriginalSource as DependencyObject;
                    //    while (src != null)
                    //    {
                    //        if (ReferenceEquals(src, CloseButton)) return;
                    //        src = VisualTreeHelper.GetParent(src);
                    //    }
                    //}
                }
                catch { }

                // Log attempt
                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: DragMove start"); } catch { }
                this.DragMove();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: DragMove failed: {ex.Message}", ex); } catch { }
            }
        }

        private void AgenteIALocalConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    try
                    {
                        var settings = AgentSettingsStore.Load();
                        if (settings != null)
                        {
                            _isInitializingAdvancedUi = true;
                            try
                            {
                                // choose server to display: prefer initialServerId, else settings.ActiveServerId
                                var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : settings.ActiveServerId;

                                var srv = (settings.Servers ?? new List<ServerConfig>())
                                    .Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));

                                ActiveServerIdTextBox_Modal.Text = targetId ?? string.Empty;
                                ServerBaseUrlTextBox_Modal.Text = srv?.BaseUrl ?? string.Empty;
                                _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                                ServerApiKeyTextBox_Modal.Text = srv?.ApiKey ?? string.Empty;

                                LoadAdvancedControls(settings, srv);

                                WireAdvancedHandlersOnce();

                                // Wire BaseUrl change to re-fetch models
                                try
                                {
                                    ServerBaseUrlTextBox_Modal.LostFocus -= ServerBaseUrl_LostFocus;
                                    ServerBaseUrlTextBox_Modal.LostFocus += ServerBaseUrl_LostFocus;
                                }
                                catch { }

                                var baseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(baseUrl))
                                {
                                    try
                                    {
                                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels start for {baseUrl}"); } catch { }
                                        var models = await FetchModelsAsync(baseUrl);
                                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels end for {baseUrl} (count={models?.Count ?? 0})"); } catch { }

                                        if (models != null && models.Count > 0)
                                        {
                                            ServerModelCombo_Modal.Items.Clear();
                                            foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);

                                            var activeModel = srv?.Model;
                                            if (!string.IsNullOrEmpty(activeModel) && ServerModelCombo_Modal.Items.Contains(activeModel))
                                            {
                                                ServerModelCombo_Modal.SelectedItem = activeModel;
                                            }
                                            else
                                            {
                                                ServerModelCombo_Modal.SelectedIndex = 0;
                                                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Defaulted model selection to '{ServerModelCombo_Modal.SelectedItem}'"); } catch { }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels error on load: {ex.Message}", ex); } catch { }
                                    }
                                }
                            }
                            finally
                            {
                                _isInitializingAdvancedUi = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Load error: {ex.Message}", ex); } catch { }
                    }
                }

        // NUEVO METODO LoadAdvancedControls - ID: 20250304_170001
        private void LoadAdvancedControls(AgentSettings settings, ServerConfig activeServer)
        {
            if (settings == null)
            {
                return;
            }

            var provider = (activeServer?.Provider ?? string.Empty).ToLowerInvariant();
            ProviderCombo_Modal.SelectedIndex = provider == "jan" ? 1 : 0;

            var runMode = settings.GlobalSettings?.Value<string>("runMode") ?? "preguntar";
            RunModeCombo_Modal.SelectedIndex = string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            var requestDefaults = settings.GlobalSettings?["requestDefaults"] as JObject ?? new JObject();
            var streamValue = requestDefaults.Value<bool?>("stream") ?? true;
            StreamToggle_Modal.IsChecked = streamValue;

            var streamOptions = requestDefaults["streamOptions"] as JObject;
            var includeUsage = streamOptions?.Value<bool?>("includeUsage") ?? false;
            IncludeUsageToggle_Modal.IsChecked = includeUsage;
            IncludeUsageToggle_Modal.IsEnabled = provider == "lmstudio";

            var agent = settings.GlobalSettings?["agent"] as JObject ?? new JObject();
            AgentIdeIntegrationToggle_Modal.IsChecked = agent.Value<bool?>("ideIntegration") ?? true;
            AgentApplyChangesToggle_Modal.IsChecked = agent.Value<bool?>("applyChanges") ?? false;
            var maxSteps = agent.Value<int?>("maxSteps") ?? 5;
            AgentMaxStepsTextBox_Modal.Text = maxSteps.ToString();
        }

        // NUEVO METODO WireAdvancedHandlersOnce - ID: 20250304_170002
        private void WireAdvancedHandlersOnce()
        {
            try
            {
                ProviderCombo_Modal.SelectionChanged -= ProviderCombo_Modal_SelectionChanged;
                ProviderCombo_Modal.SelectionChanged += ProviderCombo_Modal_SelectionChanged;

                RunModeCombo_Modal.SelectionChanged -= RunModeCombo_Modal_SelectionChanged;
                RunModeCombo_Modal.SelectionChanged += RunModeCombo_Modal_SelectionChanged;

                StreamToggle_Modal.Checked -= StreamToggle_Modal_Checked;
                StreamToggle_Modal.Unchecked -= StreamToggle_Modal_Checked;
                StreamToggle_Modal.Checked += StreamToggle_Modal_Checked;
                StreamToggle_Modal.Unchecked += StreamToggle_Modal_Checked;

                IncludeUsageToggle_Modal.Checked -= IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Unchecked -= IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Checked += IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Unchecked += IncludeUsageToggle_Modal_Checked;

                AgentIdeIntegrationToggle_Modal.Checked -= AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Unchecked -= AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Checked += AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Unchecked += AgentIdeIntegrationToggle_Modal_Checked;

                AgentApplyChangesToggle_Modal.Checked -= AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Unchecked -= AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Checked += AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Unchecked += AgentApplyChangesToggle_Modal_Checked;

                AgentMaxStepsTextBox_Modal.TextChanged -= AgentMaxStepsTextBox_Modal_TextChanged;
                AgentMaxStepsTextBox_Modal.LostFocus -= AgentMaxStepsTextBox_Modal_LostFocus;
                AgentMaxStepsTextBox_Modal.TextChanged += AgentMaxStepsTextBox_Modal_TextChanged;
                AgentMaxStepsTextBox_Modal.LostFocus += AgentMaxStepsTextBox_Modal_LostFocus;
            }
            catch { }
        }

        // NUEVO METODO ProviderCombo_Modal_SelectionChanged - ID: 20250304_170003
        private void ProviderCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;

            try
            {
                var provider = GetSelectedComboContent(ProviderCombo_Modal)?.ToLowerInvariant() ?? "lmstudio";
                var targetServerId = string.Equals(provider, "jan", StringComparison.OrdinalIgnoreCase) ? "jan-local" : "lmstudio-local";

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                settings.ActiveServerId = targetServerId;
                AgentSettingsStore.Save(settings);

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetServerId, StringComparison.OrdinalIgnoreCase));
                ApplyServerToUi(targetServerId, srv);

                IncludeUsageToggle_Modal.IsEnabled = string.Equals(provider, "lmstudio", StringComparison.OrdinalIgnoreCase);

                try
                {
                    ServerBaseUrlTextBox_Modal_TextChanged(ServerBaseUrlTextBox_Modal, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
                }
                catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Provider change error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO RunModeCombo_Modal_SelectionChanged - ID: 20250304_170004
        private void RunModeCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var runMode = string.Equals(GetSelectedComboContent(RunModeCombo_Modal), "Agente", StringComparison.OrdinalIgnoreCase) ? "agente" : "preguntar";
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
                settings.GlobalSettings["runMode"] = runMode;
                AgentSettingsStore.Save(settings);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: RunMode change error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO StreamToggle_Modal_Checked - ID: 20250304_170005
        private void StreamToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
                var requestDefaults = settings.GlobalSettings["requestDefaults"] as JObject ?? new JObject();
                settings.GlobalSettings["requestDefaults"] = requestDefaults;
                requestDefaults["stream"] = StreamToggle_Modal.IsChecked == true;
                AgentSettingsStore.Save(settings);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Stream toggle error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO IncludeUsageToggle_Modal_Checked - ID: 20250304_170006
        private void IncludeUsageToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var provider = GetSelectedComboContent(ProviderCombo_Modal)?.ToLowerInvariant() ?? string.Empty;
                if (!string.Equals(provider, "lmstudio", StringComparison.OrdinalIgnoreCase)) return;

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
                var requestDefaults = settings.GlobalSettings["requestDefaults"] as JObject ?? new JObject();
                settings.GlobalSettings["requestDefaults"] = requestDefaults;
                var streamOptions = requestDefaults["streamOptions"] as JObject ?? new JObject();
                requestDefaults["streamOptions"] = streamOptions;
                streamOptions["includeUsage"] = IncludeUsageToggle_Modal.IsChecked == true;
                AgentSettingsStore.Save(settings);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: IncludeUsage toggle error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO AgentIdeIntegrationToggle_Modal_Checked - ID: 20250304_170007
        private void AgentIdeIntegrationToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistAgentFlag("ideIntegration", AgentIdeIntegrationToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Agent IDE toggle error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO AgentApplyChangesToggle_Modal_Checked - ID: 20250304_170008
        private void AgentApplyChangesToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistAgentFlag("applyChanges", AgentApplyChangesToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Agent ApplyChanges toggle error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO PersistAgentFlag - ID: 20250304_170009
        private void PersistAgentFlag(string key, bool value)
        {
            var settings = AgentSettingsStore.Load() ?? new AgentSettings();
            if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
            var agent = settings.GlobalSettings["agent"] as JObject ?? new JObject();
            settings.GlobalSettings["agent"] = agent;
            agent[key] = value;
            AgentSettingsStore.Save(settings);
        }

        // NUEVO METODO AgentMaxStepsTextBox_Modal_TextChanged - ID: 20250304_170010
        private void AgentMaxStepsTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var value = ParseMaxSteps(AgentMaxStepsTextBox_Modal.Text);
                PersistAgentMaxSteps(value);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: MaxSteps change error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO AgentMaxStepsTextBox_Modal_LostFocus - ID: 20250304_170011
        private void AgentMaxStepsTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var value = ParseMaxSteps(AgentMaxStepsTextBox_Modal.Text);
                AgentMaxStepsTextBox_Modal.Text = value.ToString();
            }
            catch { }
        }

        // NUEVO METODO PersistAgentMaxSteps - ID: 20250304_170012
        private void PersistAgentMaxSteps(int value)
        {
            var settings = AgentSettingsStore.Load() ?? new AgentSettings();
            if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
            var agent = settings.GlobalSettings["agent"] as JObject ?? new JObject();
            settings.GlobalSettings["agent"] = agent;
            agent["maxSteps"] = value;
            AgentSettingsStore.Save(settings);
        }

        // NUEVO METODO ParseMaxSteps - ID: 20250304_170013
        private static int ParseMaxSteps(string text)
        {
            if (int.TryParse(text, out var value) && value >= 1)
            {
                return value;
            }

            return 5;
        }

        // NUEVO METODO GetSelectedComboContent - ID: 20250304_170014
        private static string GetSelectedComboContent(ComboBox combo)
        {
            if (combo == null) return string.Empty;
            var item = combo.SelectedItem;
            if (item is ComboBoxItem cbi) return cbi.Content as string ?? string.Empty;
            return item as string ?? combo.Text ?? string.Empty;
        }

        // NUEVO METODO ApplyServerToUi - ID: 20250304_170015
        private void ApplyServerToUi(string serverId, ServerConfig srv)
        {
            ActiveServerIdTextBox_Modal.Text = serverId ?? string.Empty;
            ServerBaseUrlTextBox_Modal.Text = srv?.BaseUrl ?? string.Empty;
            _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
            ServerApiKeyTextBox_Modal.Text = srv?.ApiKey ?? string.Empty;

            try
            {
                ServerModelCombo_Modal.Items.Clear();
                if (!string.IsNullOrWhiteSpace(srv?.Model))
                {
                    ServerModelCombo_Modal.Items.Add(srv.Model);
                    ServerModelCombo_Modal.SelectedItem = srv.Model;
                }
            }
            catch { }
        }
                catch (Exception exOuter)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigWindow Loaded: unexpected error: {exOuter.Message}", exOuter); } catch { }
                }
            });
        }

        private void ServerBaseUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                }
                catch (Exception exOuter)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigWindow ServerBaseUrl LostFocus: unexpected error: {exOuter.Message}", exOuter); } catch { }
                }
            });
        }

        // NUEVO METODO ServerBaseUrlTextBox_Modal_TextChanged - ID: 20260114_000074
        private async void ServerBaseUrlTextBox_Modal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                _baseUrlPingCts?.Cancel();
                _baseUrlPingCts?.Dispose();
                _baseUrlPingCts = new CancellationTokenSource();
                var ct = _baseUrlPingCts.Token;

                var baseUrl = (ServerBaseUrlTextBox_Modal.Text ?? string.Empty).Trim();
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Collapsed;
                ServerBaseUrlTextBox_Modal.ToolTip = null;

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    try
                    {
                        ServerModelCombo_Modal.Items.Clear();
                        ServerModelCombo_Modal.SelectedItem = null;
                    }
                    catch { }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, Array.Empty<string>()); } catch { }
                    return;
                }

                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _))
                {
                    ShowBaseUrlError("URL inválida");
                    try
                    {
                        ServerModelCombo_Modal.Items.Clear();
                        ServerModelCombo_Modal.SelectedItem = null;
                    }
                    catch { }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, Array.Empty<string>()); } catch { }
                    return;
                }

                var endpoint = baseUrl.TrimEnd('/') + "/v1/models";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    HttpResponseMessage resp = null;
                    try
                    {
                        resp = await client.GetAsync(endpoint, ct).ConfigureAwait(true);
                        if (ct.IsCancellationRequested) return;

                        if (resp.IsSuccessStatusCode)
                        {
                            HideBaseUrlError();
                            PersistBaseUrlIfChanged(baseUrl);

                            List<string> models = new List<string>();
                            try
                            {
                                models = await FetchModelsAsync(baseUrl);
                            }
                            catch { }

                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                if (models != null && models.Count > 0)
                                {
                                    foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);
                                    ServerModelCombo_Modal.SelectedIndex = 0;
                                }
                                else
                                {
                                    ServerModelCombo_Modal.SelectedItem = null;
                                }
                            }
                            catch { }

                            try { BaseUrlHealthChanged?.Invoke(true, baseUrl, models); } catch { }
                        }
                        else
                        {
                            ShowBaseUrlError($"Servidor responde {resp.StatusCode}");
                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                ServerModelCombo_Modal.SelectedItem = null;
                            }
                            catch { }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, Array.Empty<string>()); } catch { }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // cancelled; ignore
                    }
                    catch (Exception ex)
                    {
                        if (!ct.IsCancellationRequested)
                        {
                            ShowBaseUrlError(ex.Message);
                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                ServerModelCombo_Modal.SelectedItem = null;
                            }
                            catch { }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, Array.Empty<string>()); } catch { }
                        }
                    }
                    finally
                    {
                        try { resp?.Dispose(); } catch { }
                    }
                }
            }
            catch (Exception exOuter)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: BaseUrl ping error: {exOuter.Message}", exOuter); } catch { }
            }
        }

        // NUEVO METODO PersistBaseUrlIfChanged - ID: 20260114_000075
        private void PersistBaseUrlIfChanged(string baseUrl)
        {
            try
            {
                if (string.Equals(baseUrl, _lastPersistedBaseUrl, StringComparison.Ordinal)) return;

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                var targetId = ActiveServerIdTextBox_Modal.Text;
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    targetId = settings.ActiveServerId ?? settings.Servers.FirstOrDefault()?.Id ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(targetId)) return;

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = settings.Servers.FirstOrDefault()?.Provider ?? "lmstudio", CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = baseUrl;
                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);
                _lastPersistedBaseUrl = baseUrl;
                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: BaseUrl ping OK, persisted '{baseUrl}' for server '{targetId}'"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: PersistBaseUrlIfChanged error: {ex.Message}", ex); } catch { }
            }
        }

        // NUEVO METODO ShowBaseUrlError - ID: 20260114_000076
        private void ShowBaseUrlError(string message)
        {
            try
            {
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(message)) ServerBaseUrlTextBox_Modal.ToolTip = message;
            }
            catch { }
        }

        // NUEVO METODO HideBaseUrlError - ID: 20260114_000077
        private void HideBaseUrlError()
        {
            try
            {
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Collapsed;
                ServerBaseUrlTextBox_Modal.ToolTip = null;
            }
            catch { }
        }

        private async Task<List<string>> FetchModelsAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;

                var url = baseUrl.TrimEnd('/') + "/v1/models";
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync GET {url}"); } catch { }

                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync non-success status: {resp.StatusCode}"); } catch { }
                        return result;
                    }

                    var txt = await resp.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(txt)) return result;

                    try
                    {
                        var root = Newtonsoft.Json.Linq.JToken.Parse(txt);
                        var data = root["data"] as Newtonsoft.Json.Linq.JArray;
                        if (data != null)
                        {
                            foreach (var item in data)
                            {
                                try
                                {
                                    var id = item.Value<string>("id");
                                    if (!string.IsNullOrEmpty(id)) result.Add(id);
                                }
                                catch { }
                            }

                            return result;
                        }

                        var models = root["models"] as Newtonsoft.Json.Linq.JArray;
                        if (models != null)
                        {
                            foreach (var item in models)
                            {
                                try
                                {
                                    var id = item.Value<string>("id") ?? item.ToString();
                                    if (!string.IsNullOrEmpty(id)) result.Add(id);
                                }
                                catch { }
                            }

                            return result;
                        }

                        if (root is Newtonsoft.Json.Linq.JArray arr)
                        {
                            foreach (var item in arr)
                            {
                                var s = item.ToString();
                                if (!string.IsNullOrEmpty(s)) result.Add(s);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync parse error: {ex.Message}", ex); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync error: {ex.Message}", ex); } catch { }
            }

            return result;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save start"); } catch { }
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                // Determine target server id: prefer initialServerId (derived from ToolWindow selection), else textbox value
                var activeIdInput = ActiveServerIdTextBox_Modal.Text ?? string.Empty;
                var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : activeIdInput;
                if (string.IsNullOrEmpty(targetId))
                {
                    // fallback to existing active or first server
                    targetId = settings.ActiveServerId ?? (settings.Servers.Count > 0 ? settings.Servers[0].Id : string.Empty);
                    if (string.IsNullOrEmpty(targetId)) targetId = activeIdInput;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = settings.Servers.FirstOrDefault()?.Provider ?? "lmstudio", CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                // do not log apiKey
                srv.ApiKey = ServerApiKeyTextBox_Modal.Text ?? string.Empty;

                // Robust extraction of selected model value
                string selectedModel = null;
                try
                {
                    selectedModel = (ServerModelCombo_Modal?.SelectedItem as string) 
                                    ?? (ServerModelCombo_Modal?.SelectedValue as string) 
                                    ?? ServerModelCombo_Modal?.Text;
                }
                catch { }
                selectedModel = (selectedModel ?? string.Empty).Trim();

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save activeId='{targetId}', baseUrlPresent={(!string.IsNullOrWhiteSpace(srv.BaseUrl)).ToString()}, modelPresent={(string.IsNullOrEmpty(selectedModel) ? "false" : "true")} "); } catch { }

                try { if (!string.IsNullOrEmpty(selectedModel)) srv.Model = selectedModel; } catch { }

                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save persisted ActiveServerId={settings.ActiveServerId}, BaseUrl={(srv.BaseUrl ?? "(empty)" )}, ModelLength={(srv.Model != null ? srv.Model.Length : 0)}"); } catch { }

                // Recompose and refresh
                try
                {
                    AgentComposition.RecomposeFromSettings("ConfigModal.Save");
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: RecomposeFromSettings error: {ex.Message}", ex); } catch { }
                }

                // Refresh owner control UI state using public API (no reflection)
                try
                {
                    AgenteIALocalControl found = null;
                    var ownerWindow = this.Owner as Window;
                    if (ownerWindow != null)
                    {
                        var content = ownerWindow.Content as FrameworkElement;
                        if (content is AgenteIALocalControl ac) found = ac;
                        else if (content != null) found = FindControlOfType(content, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                    }

                    if (found == null && System.Windows.Application.Current != null)
                    {
                        var mainContent = System.Windows.Application.Current.MainWindow?.Content as FrameworkElement;
                        if (mainContent is AgenteIALocalControl ac2) found = ac2;
                        else if (mainContent != null) found = FindControlOfType(mainContent, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                    }

                    if (found != null)
                    {
                        try
                        {
                            found.RefreshFromSettings();
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: RefreshFromSettings called on owner control."); } catch { }
                        }
                        catch (Exception ex)
                        {
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Error calling RefreshFromSettings: {ex.Message}", ex); } catch { }
                        }
                    }
                    else
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Owner AgenteIALocalControl not found to refresh UI after save."); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Error refreshing owner UI: {ex.Message}", ex); } catch { }
                }

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save end"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save error: {ex.Message}", ex); } catch { }
            }
            finally
            {
                try { this.DialogResult = true; } catch { }
                try { this.Close(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Close error: {ex.Message}", ex); } catch { }
            }
        }

        private static FrameworkElement FindControlOfType(FrameworkElement root, Type t)
        {
            if (root == null) return null;
            if (root.GetType() == t) return root;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i) as FrameworkElement;
                var res = FindControlOfType(child, t);
                if (res != null) return res;
            }
            return null;
        }
    }
}