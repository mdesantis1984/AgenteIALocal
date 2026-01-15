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
using AgenteIALocalVSIX;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;

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
            try { HeaderDragArea.MouseLeftButtonDown += HeaderDragArea_MouseLeftButtonDown; } catch { }
            Loaded += AgenteIALocalConfigWindow_Loaded;
            initialServerId = serverId ?? string.Empty;
            if (!string.IsNullOrEmpty(serverId))
            {
                Title = "Configuración — " + serverId;
            }
        }

        private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void HeaderDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left) return;
                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: DragMove start"); } catch { }
                DragMove();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: DragMove failed: " + ex.Message, ex); } catch { }
            }
        }

        private void AgenteIALocalConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FireAndForget(HandleLoadedAsync(), "ConfigWindow.Loaded");
        }

        // NUEVO METODO LoadAdvancedControls - ID: 20250304_170001
        private void LoadAdvancedControls(AgentSettings settings, ServerConfig activeServer)
        {
            if (settings == null) return;

            var provider = (activeServer != null ? activeServer.Provider : string.Empty).ToLowerInvariant();
            ProviderCombo_Modal.SelectedIndex = provider == "jan" ? 1 : 0;

            var runMode = settings.GlobalSettings != null ? settings.GlobalSettings.Value<string>("runMode") : null;
            if (string.IsNullOrEmpty(runMode)) runMode = "preguntar";
            RunModeCombo_Modal.SelectedIndex = string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            var requestDefaults = settings.GlobalSettings != null ? settings.GlobalSettings["requestDefaults"] as JObject : null;
            if (requestDefaults == null) requestDefaults = new JObject();
            var streamValue = requestDefaults.Value<bool?>("stream") ?? true;
            StreamToggle_Modal.IsChecked = streamValue;

            var streamOptions = requestDefaults["streamOptions"] as JObject;
            var includeUsage = streamOptions != null ? streamOptions.Value<bool?>("includeUsage") ?? false : false;
            IncludeUsageToggle_Modal.IsChecked = includeUsage;
            IncludeUsageToggle_Modal.IsEnabled = provider == "lmstudio";

            var agent = settings.GlobalSettings != null ? settings.GlobalSettings["agent"] as JObject : null;
            if (agent == null) agent = new JObject();
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
                var provider = GetSelectedComboContent(ProviderCombo_Modal).ToLowerInvariant();
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Provider change error: " + ex.Message, ex); } catch { }
            }
        }

        // NUEVO METODO RunModeCombo_Modal_SelectionChanged - ID: 20250304_170004
        private void RunModeCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var selected = GetSelectedComboContent(RunModeCombo_Modal);
                var runMode = string.Equals(selected, "Agente", StringComparison.OrdinalIgnoreCase) ? "agente" : "preguntar";
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
                settings.GlobalSettings["runMode"] = runMode;
                AgentSettingsStore.Save(settings);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: RunMode change error: " + ex.Message, ex); } catch { }
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Stream toggle error: " + ex.Message, ex); } catch { }
            }
        }

        // NUEVO METODO IncludeUsageToggle_Modal_Checked - ID: 20250304_170006
        private void IncludeUsageToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var provider = GetSelectedComboContent(ProviderCombo_Modal).ToLowerInvariant();
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: IncludeUsage toggle error: " + ex.Message, ex); } catch { }
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Agent IDE toggle error: " + ex.Message, ex); } catch { }
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Agent ApplyChanges toggle error: " + ex.Message, ex); } catch { }
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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: MaxSteps change error: " + ex.Message, ex); } catch { }
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
            int value;
            if (int.TryParse(text, out value) && value >= 1)
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
            var cbi = item as ComboBoxItem;
            if (cbi != null) return cbi.Content as string ?? string.Empty;
            var s = item as string;
            if (!string.IsNullOrEmpty(s)) return s;
            return combo.Text ?? string.Empty;
        }

        // NUEVO METODO FireAndForget - ID: 20250310_000001
        private void FireAndForget(Task task, string op)
        {
            if (task == null) return;

            _ = task.ContinueWith(t =>
            {
                try
                {
                    var ex = t.Exception != null ? t.Exception.GetBaseException() : null;
                    if (ex != null)
                    {
                        AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, op + " failed: " + ex.Message, ex);
                    }
                }
                catch
                {
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        // NUEVO METODO HandleLoadedAsync - ID: 20250310_000002
        private async Task HandleLoadedAsync()
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null) return;

                _isInitializingAdvancedUi = true;
                try
                {
                    var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : settings.ActiveServerId;
                    var srv = (settings.Servers ?? new List<ServerConfig>()).Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));

                    ActiveServerIdTextBox_Modal.Text = targetId ?? string.Empty;
                    ServerBaseUrlTextBox_Modal.Text = srv != null ? srv.BaseUrl : string.Empty;
                    _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                    ServerApiKeyTextBox_Modal.Text = srv != null ? srv.ApiKey : string.Empty;

                    LoadAdvancedControls(settings, srv);
                    WireAdvancedHandlersOnce();

                    try
                    {
                        ServerBaseUrlTextBox_Modal.LostFocus -= ServerBaseUrl_LostFocus;
                        ServerBaseUrlTextBox_Modal.LostFocus += ServerBaseUrl_LostFocus;
                    }
                    catch
                    {
                    }

                    var baseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        try
                        {
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModels start for " + baseUrl); } catch { }
                            var models = await FetchModelsAsync(baseUrl).ConfigureAwait(true);
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModels end for " + baseUrl + " (count=" + (models != null ? models.Count : 0) + ")"); } catch { }

                            if (models != null && models.Count > 0)
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);

                                var activeModel = srv != null ? srv.Model : null;
                                if (!string.IsNullOrEmpty(activeModel) && ServerModelCombo_Modal.Items.Contains(activeModel))
                                {
                                    ServerModelCombo_Modal.SelectedItem = activeModel;
                                }
                                else
                                {
                                    ServerModelCombo_Modal.SelectedIndex = 0;
                                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Defaulted model selection to '" + ServerModelCombo_Modal.SelectedItem + "'"); } catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModels error on load: " + ex.Message, ex); } catch { }
                        }
                    }
                }
                finally
                {
                    _isInitializingAdvancedUi = false;
                }
            }
            catch (Exception exOuter)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigWindow Loaded: unexpected error: " + exOuter.Message, exOuter); } catch { }
            }
        }

        // NUEVO METODO HandleServerBaseUrlLostFocusAsync - ID: 20250310_000003
        private Task HandleServerBaseUrlLostFocusAsync()
        {
            return Task.CompletedTask;
        }

        // NUEVO METODO HandleBaseUrlTextChangedAsync - ID: 20250310_000004
        private async Task HandleBaseUrlTextChangedAsync()
        {
            try
            {
                if (_baseUrlPingCts != null)
                {
                    _baseUrlPingCts.Cancel();
                    _baseUrlPingCts.Dispose();
                }
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
                    catch
                    {
                    }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                    return;
                }

                Uri uriResult;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uriResult))
                {
                    ShowBaseUrlError("URL inválida");
                    try
                    {
                        ServerModelCombo_Modal.Items.Clear();
                        ServerModelCombo_Modal.SelectedItem = null;
                    }
                    catch
                    {
                    }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
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
                                models = await FetchModelsAsync(baseUrl).ConfigureAwait(true);
                            }
                            catch
                            {
                            }

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
                            catch
                            {
                            }

                            try { BaseUrlHealthChanged?.Invoke(true, baseUrl, models); } catch { }
                        }
                        else
                        {
                            ShowBaseUrlError("Servidor responde " + resp.StatusCode);
                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                ServerModelCombo_Modal.SelectedItem = null;
                            }
                            catch
                            {
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                        }
                    }
                    catch (OperationCanceledException)
                    {
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
                            catch
                            {
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                        }
                    }
                    finally
                    {
                        try { if (resp != null) resp.Dispose(); } catch { }
                    }
                }
            }
            catch (Exception exOuter)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: BaseUrl ping error: " + exOuter.Message, exOuter); } catch { }
            }
        }

        // NUEVO METODO ApplyServerToUi - ID: 20250304_170015
        private void ApplyServerToUi(string serverId, ServerConfig srv)
        {
            ActiveServerIdTextBox_Modal.Text = serverId ?? string.Empty;
            ServerBaseUrlTextBox_Modal.Text = srv != null ? srv.BaseUrl : string.Empty;
            _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
            ServerApiKeyTextBox_Modal.Text = srv != null ? srv.ApiKey : string.Empty;

            try
            {
                ServerModelCombo_Modal.Items.Clear();
                if (srv != null && !string.IsNullOrWhiteSpace(srv.Model))
                {
                    ServerModelCombo_Modal.Items.Add(srv.Model);
                    ServerModelCombo_Modal.SelectedItem = srv.Model;
                }
            }
            catch { }
        }

        private void ServerBaseUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            FireAndForget(HandleServerBaseUrlLostFocusAsync(), "ConfigWindow.ServerBaseUrl.LostFocus");
        }

        // NUEVO METODO ServerBaseUrlTextBox_Modal_TextChanged - ID: 20260114_000074
        private void ServerBaseUrlTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged");
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
                    var firstServer = settings.Servers.FirstOrDefault();
                    targetId = settings.ActiveServerId ?? (firstServer != null ? firstServer.Id : string.Empty);
                }
                if (string.IsNullOrWhiteSpace(targetId)) return;

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    var firstServer = settings.Servers.FirstOrDefault();
                    var provider = firstServer != null ? firstServer.Provider : "lmstudio";
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = provider, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = baseUrl;
                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);
                _lastPersistedBaseUrl = baseUrl;
                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: BaseUrl ping OK, persisted '" + baseUrl + "' for server '" + targetId + "'"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: PersistBaseUrlIfChanged error: " + ex.Message, ex); } catch { }
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
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync GET " + url); } catch { }

                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync non-success status: " + resp.StatusCode); } catch { }
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

                        var arr = root as Newtonsoft.Json.Linq.JArray;
                        if (arr != null)
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
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync parse error: " + ex.Message, ex); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync error: " + ex.Message, ex); } catch { }
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

                var activeIdInput = ActiveServerIdTextBox_Modal.Text ?? string.Empty;
                var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : activeIdInput;
                if (string.IsNullOrEmpty(targetId))
                {
                    targetId = settings.ActiveServerId ?? (settings.Servers.Count > 0 ? settings.Servers[0].Id : string.Empty);
                    if (string.IsNullOrEmpty(targetId)) targetId = activeIdInput;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    var first = settings.Servers.FirstOrDefault();
                    var provider = first != null ? first.Provider : "lmstudio";
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = provider, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                srv.ApiKey = ServerApiKeyTextBox_Modal.Text ?? string.Empty;

                string selectedModel = null;
                try
                {
                    selectedModel = (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.SelectedItem as string : null)
                                    ?? (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.SelectedValue as string : null)
                                    ?? (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.Text : null);
                }
                catch { }
                selectedModel = (selectedModel ?? string.Empty).Trim();

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save activeId='" + targetId + "', baseUrlPresent=" + (!string.IsNullOrWhiteSpace(srv.BaseUrl)).ToString() + ", modelPresent=" + (string.IsNullOrEmpty(selectedModel) ? "false" : "true")); } catch { }

                if (!string.IsNullOrEmpty(selectedModel))
                {
                    srv.Model = selectedModel;
                }

                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save persisted ActiveServerId=" + settings.ActiveServerId + ", BaseUrl=" + (srv.BaseUrl ?? "(empty)") + ", ModelLength=" + (srv.Model != null ? srv.Model.Length : 0)); } catch { }

                try
                {
                    AgentComposition.RecomposeFromSettings("ConfigModal.Save");
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: RecomposeFromSettings error: " + ex.Message, ex); } catch { }
                }

                try
                {
                    AgenteIALocalControl found = null;
                    var ownerWindow = Owner as Window;
                    if (ownerWindow != null)
                    {
                        var content = ownerWindow.Content as FrameworkElement;
                        if (content is AgenteIALocalControl)
                        {
                            found = content as AgenteIALocalControl;
                        }
                        else if (content != null)
                        {
                            found = FindControlOfType(content, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                        }
                    }

                    if (found == null && System.Windows.Application.Current != null)
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        var mainContent = mainWindow != null ? mainWindow.Content as FrameworkElement : null;
                        if (mainContent is AgenteIALocalControl)
                        {
                            found = mainContent as AgenteIALocalControl;
                        }
                        else if (mainContent != null)
                        {
                            found = FindControlOfType(mainContent, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                        }
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
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Error calling RefreshFromSettings: " + ex.Message, ex); } catch { }
                        }
                    }
                    else
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Owner AgenteIALocalControl not found to refresh UI after save."); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Error refreshing owner UI: " + ex.Message, ex); } catch { }
                }

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save end"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save error: " + ex.Message, ex); } catch { }
            }
            finally
            {
                try { DialogResult = true; } catch { }
                try { Close(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Close error: " + ex.Message, ex); } catch { }
            }
        }

        private static FrameworkElement FindControlOfType(FrameworkElement root, Type t)
        {
            if (root == null) return null;
            if (root.GetType() == t) return root;
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i) as FrameworkElement;
                var res = FindControlOfType(child, t);
                if (res != null) return res;
            }
            return null;
        }
    }
}