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
using System.Windows.Controls.Primitives;
using AgenteIALocalVSIX;
using AgenteIALocalVSIX.Commons;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalConfigWindow : Window
    {
        // NUEVO - suppress BaseUrl TextChanged handler while programmatically setting text - ID: 20260115_223100
        private bool _suppressBaseUrlTextChanged_20260116;
        private readonly string initialServerId;
        // NUEVO CAMPO BaseUrlPingCts - ID: 20260114_000072
        private CancellationTokenSource _baseUrlPingCts;
        // NUEVO CAMPO LastPersistedBaseUrl - ID: 20260114_000073
        private string _lastPersistedBaseUrl;
        private bool _isInitializingAdvancedUi;
        // NUEVO EVENTO BaseUrlHealthChanged - ID: 20260114_000079
        public event Action<bool, string, IReadOnlyList<string>> BaseUrlHealthChanged;

        // NUEVO: in-memory models cache per serverId - ID: 20260115_220500
        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>> _modelsCacheByServerId = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>>();

        // NUEVO: versioning to avoid stale UI updates for models fetch - ID: 20260115_220500
        private int _modelsFetchVersion = 0;
        // NUEVO: guard to avoid recursion when toggling nav buttons - ID: 20260116_112500
        private bool _suppressNavToggleChecked_20260116;

        public AgenteIALocalConfigWindow(string serverId = null)
        {
            InitializeComponent();
            try { HeaderDragArea.MouseLeftButtonDown += HeaderDragArea_MouseLeftButtonDown; } catch { }
            Loaded += AgenteIALocalConfigWindow_Loaded;
            // Subscribe to settings saved notifications to refresh modal when settings change elsewhere
            try { AgentSettingsStore.SettingsSaved += OnSettingsSaved; } catch { }
            initialServerId = serverId ?? string.Empty;
            if (!string.IsNullOrEmpty(serverId))
            {
                Title = "Configuración — " + serverId;
            }

            // MODIFICADO - ID: 20260116_110300
            // Set window and header caption to match ToolWindowPane caption including VSIX version
            try
            {
                var version = typeof(AgenteIALocalVSIXPackage).GetVsixVersionString();
                var caption = $"Chat de Agente IA Local {version} - Configuracion";
                try { this.Title = caption; } catch { }
                try { HeaderTitleText.Text = caption; } catch { }
            }
            catch { }

            // NUEVO: view switching is handled by XAML DataTriggers; no code-behind wiring required - ID: 20260116_094500
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

        private void OnSettingsSaved(string reason)
        {
            try
            {
                if (!IsLoaded) return;
                // Rehydrate advanced controls only, avoid triggering save
                try
                {
                    var jt = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        try
                        {
                            _isInitializingAdvancedUi = true;
                            var settings = AgentSettingsStore.Load();
                            var targetId = settings != null ? settings.ActiveServerId : null;
                            var srv = (settings != null && settings.Servers != null) ? settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase)) : null;
                            ApplyServerToUi(targetId ?? string.Empty, srv);
                            LoadAdvancedControls(settings, srv);
                        }
                        catch { }
                        finally { _isInitializingAdvancedUi = false; }
                    });

                    _ = jt.Task.ContinueWith(t => { var _e = t.Exception; }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, System.Threading.Tasks.TaskScheduler.Default);
                }
                catch { }
            }
            catch { }
        }

        // NUEVO METODO NavToggle_Checked - ID: 20260116_112500
        // Make sidebar ToggleButtons act mutually exclusive without changing their x:Name
        private void NavToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressNavToggleChecked_20260116) return;
            try
            {
                _suppressNavToggleChecked_20260116 = true;
                var tb = sender as ToggleButton;
                if (tb == null) return;

                // If Idioma was checked, uncheck LLM
                if (tb == NavIdiomaToggle)
                {
                    try { NavLlmToggle.IsChecked = false; } catch { }
                }
                else if (tb == NavLlmToggle)
                {
                    try { NavIdiomaToggle.IsChecked = false; } catch { }
                }
            }
            catch { }
            finally { _suppressNavToggleChecked_20260116 = false; }
        }

        // NUEVO METODO LoadAdvancedControls - ID: 20250304_170001
        private void LoadAdvancedControls(AgentSettings settings, ServerConfig activeServer)
        {
            if (settings == null) return;

            var provider = (activeServer != null ? activeServer.Provider : string.Empty).ToLowerInvariant();
            // Use TrySelectComboByText to avoid creating new items and to select the existing item
            try { TrySelectComboByText(ProviderCombo_Modal, provider == "jan" ? "JAN" : "LM Studio"); } catch { }

            var runMode = settings.GlobalSettings != null ? settings.GlobalSettings.Value<string>("runMode") : null;
            if (string.IsNullOrEmpty(runMode)) runMode = "preguntar";
            try { TrySelectComboByText(RunModeCombo_Modal, string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase) ? "Agente" : "Preguntar"); } catch { }

            var requestDefaults = settings.GlobalSettings != null ? settings.GlobalSettings["requestDefaults"] as JObject : null;
            if (requestDefaults == null) requestDefaults = new JObject();
            // MODIFICADO METODO LoadAdvancedControls - ID: 20260117_124200
            // Force Stream as the only option in UI: checked + disabled
            var streamValue = requestDefaults.Value<bool?>("stream") ?? true;
            try { StreamToggle_Modal.IsChecked = true; StreamToggle_Modal.IsEnabled = false; } catch { }

            var streamOptions = requestDefaults["streamOptions"] as JObject;
            var includeUsage = streamOptions != null ? streamOptions.Value<bool?>("includeUsage") ?? false : false;
            IncludeUsageToggle_Modal.IsChecked = includeUsage;
            IncludeUsageToggle_Modal.IsEnabled = provider == "lmstudio";

            // hydrate temperature and maxTokens
            try
            {
                var temp = requestDefaults.Value<double?>("temperature") ?? 0.2;
                TemperatureTextBox_Modal.Text = temp.ToString("G");
            }
            catch { TemperatureTextBox_Modal.Text = "0.2"; }

            try
            {
                var mt = requestDefaults.Value<int?>("maxTokens") ?? 0;
                MaxTokensTextBox_Modal.Text = mt.ToString();
            }
            catch { MaxTokensTextBox_Modal.Text = "0"; }

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

                // Temperature and MaxTokens handlers
                TemperatureTextBox_Modal.TextChanged -= TemperatureTextBox_Modal_TextChanged;
                TemperatureTextBox_Modal.LostFocus -= TemperatureTextBox_Modal_LostFocus;
                TemperatureTextBox_Modal.TextChanged += TemperatureTextBox_Modal_TextChanged;
                TemperatureTextBox_Modal.LostFocus += TemperatureTextBox_Modal_LostFocus;

                MaxTokensTextBox_Modal.TextChanged -= MaxTokensTextBox_Modal_TextChanged;
                MaxTokensTextBox_Modal.LostFocus -= MaxTokensTextBox_Modal_LostFocus;
                MaxTokensTextBox_Modal.TextChanged += MaxTokensTextBox_Modal_TextChanged;
                MaxTokensTextBox_Modal.LostFocus += MaxTokensTextBox_Modal_LostFocus;

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

        // NUEVO METODO TrySelectComboByText - ID: 20260116_180500
        // Selects an existing ComboBox item by comparing display text case-insensitively.
        private bool TrySelectComboByText(ComboBox cb, string text)
        {
            if (cb == null || string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                foreach (var item in cb.Items)
                {
                    try
                    {
                        string s = null;
                        var cbi = item as ComboBoxItem;
                        if (cbi != null)
                        {
                            try { s = cbi.Content?.ToString(); } catch { s = null; }
                        }
                        if (string.IsNullOrEmpty(s))
                        {
                            try { s = item?.ToString(); } catch { s = null; }
                        }
                        if (string.IsNullOrEmpty(s)) continue;
                        if (string.Equals(s, text, StringComparison.OrdinalIgnoreCase))
                        {
                            cb.SelectedItem = item;
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        // NUEVO METODO NormalizeProviderId - ID: 20260117_234000
        // Normaliza el texto visible del combo Provider a un identificador corto de proveedor.
        // Entrada: texto (ej. "LM Studio", "JAN") -> salida: "lmstudio" | "jan" | ""
        private static string NormalizeProviderId(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;
                var s = text.Trim().ToLowerInvariant();
                // remove spaces and hyphens for tolerant matching
                s = s.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (s == "lmstudio" || s == "lmstudio") return "lmstudio";
                if (s == "lmstudio") return "lmstudio"; // defensive
                if (s.StartsWith("lmstudio")) return "lmstudio";
                if (s.StartsWith("lmstudio")) return "lmstudio";
                if (s == "jan" || s.StartsWith("jan")) return "jan";
                return string.Empty;
            }
            catch { return string.Empty; }
        }

        // MODIFICADO METODO ProviderCombo_Modal_SelectionChanged - ID: 20260117_234000
        private void ProviderCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;

            try
            {
                var providerId = NormalizeProviderId(GetSelectedComboContent(ProviderCombo_Modal));
                var targetServerId = string.Equals(providerId, "jan", StringComparison.OrdinalIgnoreCase) ? "jan-local" : "lmstudio-local";

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                var prev = settings.ActiveServerId ?? string.Empty;
                if (string.Equals(prev, targetServerId, StringComparison.OrdinalIgnoreCase))
                {
                    // no change
                }
                else
                {
                    settings.ActiveServerId = targetServerId;
                    AgentSettingsStore.Save(settings);
                }
                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetServerId, StringComparison.OrdinalIgnoreCase));
                ApplyServerToUi(targetServerId, srv);

                // Enable include-usage toggle only for LM Studio; disable and clear otherwise
                try
                {
                    IncludeUsageToggle_Modal.IsEnabled = string.Equals(providerId, "lmstudio", StringComparison.OrdinalIgnoreCase);
                    if (!string.Equals(providerId, "lmstudio", StringComparison.OrdinalIgnoreCase))
                    {
                        IncludeUsageToggle_Modal.IsChecked = false;
                    }
                }
                catch { }

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
                var before = (settings.GlobalSettings as JObject)?.DeepClone() as JObject ?? new JObject();
                settings.GlobalSettings["runMode"] = runMode;
                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch { }
                AgentSettingsStore.Save(settings);
                try { AgentComposition.LogGlobalSettingsPersistence("RunModeCombo_Modal_SelectionChanged", before, settings.GlobalSettings, provider); } catch { }
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
                // Ensure requestDefaults.stream is always true on save
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();
                var before = (settings.GlobalSettings as JObject)?.DeepClone() as JObject ?? new JObject();
                var requestDefaults = settings.GlobalSettings["requestDefaults"] as JObject ?? new JObject();
                settings.GlobalSettings["requestDefaults"] = requestDefaults;
                requestDefaults["stream"] = true;
                // provider for tracing
                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch { }
                AgentSettingsStore.Save(settings);
                try { AgentComposition.LogGlobalSettingsPersistence("StreamToggle_Modal_Checked", before, settings.GlobalSettings, provider); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Stream toggle error: " + ex.Message, ex); } catch { }
            }
        }

        // MODIFICADO METODO IncludeUsageToggle_Modal_Checked - ID: 20260117_234000
        private void IncludeUsageToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var providerId = NormalizeProviderId(GetSelectedComboContent(ProviderCombo_Modal));
                // Delegate to unified persister which sets includeUsage according to provider
                PersistRequestDefaultsFromUi();
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
            var before = (settings.GlobalSettings as JObject)?.DeepClone() as JObject ?? new JObject();
            var agent = settings.GlobalSettings["agent"] as JObject ?? new JObject();
            settings.GlobalSettings["agent"] = agent;
            agent[key] = value;
            string provider = null;
            try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch { }
            AgentSettingsStore.Save(settings);
            try { AgentComposition.LogGlobalSettingsPersistence($"PersistAgentFlag:{key}", before, settings.GlobalSettings, provider); } catch { }
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
            var before = (settings.GlobalSettings as JObject)?.DeepClone() as JObject ?? new JObject();
            var agent = settings.GlobalSettings["agent"] as JObject ?? new JObject();
            settings.GlobalSettings["agent"] = agent;
            agent["maxSteps"] = value;
            string provider = null;
            try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch { }
            AgentSettingsStore.Save(settings);
            try { AgentComposition.LogGlobalSettingsPersistence("PersistAgentMaxSteps", before, settings.GlobalSettings, provider); } catch { }
        }

        // NUEVO METODO PersistRequestDefaultsFromUi - ID: 20260118_000001
        private void PersistRequestDefaultsFromUi()
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();

                var before = (settings.GlobalSettings as JObject)?.DeepClone() as JObject ?? new JObject();

                var requestDefaults = settings.GlobalSettings["requestDefaults"] as JObject ?? new JObject();
                settings.GlobalSettings["requestDefaults"] = requestDefaults;

                // Force stream true
                requestDefaults["stream"] = true;

                // streamOptions.includeUsage depends on provider
                var providerId = NormalizeProviderId(GetSelectedComboContent(ProviderCombo_Modal));
                var streamOptions = requestDefaults["streamOptions"] as JObject ?? new JObject();
                requestDefaults["streamOptions"] = streamOptions;
                if (string.Equals(providerId, "lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    streamOptions["includeUsage"] = IncludeUsageToggle_Modal.IsChecked == true;
                }
                else
                {
                    streamOptions["includeUsage"] = false;
                }

                // temperature
                double temp = 0.2;
                try
                {
                    double parsed;
                    if (double.TryParse(TemperatureTextBox_Modal.Text, out parsed)) temp = parsed;
                }
                catch { }
                requestDefaults["temperature"] = temp;

                // maxTokens
                int maxTokens = 0;
                try
                {
                    int parsed;
                    if (int.TryParse(MaxTokensTextBox_Modal.Text, out parsed) && parsed >= 0) maxTokens = parsed;
                }
                catch { }
                requestDefaults["maxTokens"] = maxTokens;

                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch { }
                AgentSettingsStore.Save(settings);
                try { AgentComposition.LogGlobalSettingsPersistence("PersistRequestDefaultsFromUi", before, settings.GlobalSettings, provider); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: PersistRequestDefaultsFromUi error: " + ex.Message, ex); } catch { }
            }
        }

        private void TemperatureTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            // no-op; persist on LostFocus
        }

        private void TemperatureTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            PersistRequestDefaultsFromUi();
        }

        private void MaxTokensTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            // no-op; persist on LostFocus
        }

        private void MaxTokensTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            PersistRequestDefaultsFromUi();
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

                // Suppress baseUrl change handler while we hydrate controls to avoid races
                _isInitializingAdvancedUi = true;
                _suppressBaseUrlTextChanged_20260116 = true;
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
                    catch { }
                }
                finally
                {
                    _suppressBaseUrlTextChanged_20260116 = false;
                    _isInitializingAdvancedUi = false;
                }

                // After init, if there's a baseUrl, trigger a single evaluation (do not run parallel fetches here)
                var baseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    try { FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged.OnLoaded"); } catch { }
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
        // MODIFICADO METODO HandleBaseUrlTextChangedAsync - ID: 20260117_120000
        private async Task HandleBaseUrlTextChangedAsync()
        {
            try
            {
                // Respect suppression: do not run when suppress flag set
                if (_suppressBaseUrlTextChanged_20260116 || _isInitializingAdvancedUi) return;

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

                // MODIFICADO HandleBaseUrlTextChangedAsync: use unified endpoint builder + auth header + fallback host - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
                var baseUri = NormalizeBaseUri(baseUrl);
                if (baseUri == null)
                {
                    ShowBaseUrlError("URL inválida");
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                    return;
                }

                var primary = BuildModelsUri(baseUri);
                var fallbackHost = TryGetFallbackHost(baseUri);

                // If models endpoint cannot be built (baseUrl not yet complete/valid), do not perform HTTP or log error.
                if (string.IsNullOrWhiteSpace(primary))
                {
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                    return;
                }

                var myFetchVersion = System.Threading.Interlocked.Increment(ref _modelsFetchVersion); // NUEVO: version tag for this fetch - ID: 20260115_220500
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    HttpResponseMessage resp = null;
                    Exception firstEx = null;

                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Get, primary))
                        {
                            req.Headers.Accept.Clear();
                            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            try
                            {
                                var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                apiKey = apiKey.Trim();
                                if (!string.IsNullOrWhiteSpace(apiKey))
                                {
                                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                }
                            }
                            catch { }

                            resp = await client.SendAsync(req, ct).ConfigureAwait(true);
                        }
                    }
                    catch (Exception exPrimary)
                    {
                        firstEx = exPrimary;
                    }

                    // If primary failed with connection refused and a fallback host is available, retry
                    if (firstEx != null && IsConnectionRefused(firstEx) && !string.IsNullOrEmpty(fallbackHost))
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: primary GET failed, retrying with fallback host"); } catch { }
                        var altBuilder = new UriBuilder(baseUri) { Host = fallbackHost };
                        var alt = BuildModelsUri(altBuilder.Uri);
                        try
                        {
                            using (var req = new HttpRequestMessage(HttpMethod.Get, alt))
                            {
                                req.Headers.Accept.Clear();
                                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                try
                                {
                                    var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                    apiKey = apiKey.Trim();
                                    if (!string.IsNullOrWhiteSpace(apiKey))
                                    {
                                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                    }
                                }
                                catch { }

                                resp = await client.SendAsync(req, ct).ConfigureAwait(true);
                                firstEx = null;

                                // Update textbox to effective host so user sees working host (do not persist)
                                try { _suppressBaseUrlTextChanged_20260116 = true; ServerBaseUrlTextBox_Modal.Text = altBuilder.Uri.ToString().TrimEnd('/'); } catch { } finally { _suppressBaseUrlTextChanged_20260116 = false; }
                            }
                        }
                        catch (Exception)
                        {
                            // both attempts failed -> try offline fallback using persisted model
                            try { ShowBaseUrlError("Servidor no responde (/v1/models)"); } catch { }
                            try
                            {
                                var offlineModels = new System.Collections.Generic.List<string>();
                                try
                                {
                                    var settings = AgentSettingsStore.Load();
                                    if (settings != null && settings.Servers != null)
                                    {
                                        var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                        if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                    }
                                }
                                catch { }

                                if (offlineModels.Count > 0)
                                {
                                    try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch { }
                                }

                                try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch { }
                            }
                            catch { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch { } }
                            return;
                        }
                    }

                    if (firstEx != null)
                    {
                        // If cancelled, do not touch UI
                        if (ct.IsCancellationRequested) return;
                        try { ShowBaseUrlError(firstEx.Message); } catch { }
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch { }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch { }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch { }
                        }
                        catch { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch { } }
                        return;
                    }

                    if (resp == null)
                    {
                        // stale check
                        if (myFetchVersion != _modelsFetchVersion) return; // discard
                        try { ShowBaseUrlError("Servidor no responde (/v1/models)"); } catch { }
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch { }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch { }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch { }
                        }
                        catch { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch { } }
                        return;
                    }

                    // Handle auth vs other failures vs success
                    if (resp.IsSuccessStatusCode)
                    {
                        // stale check
                        if (myFetchVersion != _modelsFetchVersion) return; // discard if newer fetch started

                        HideBaseUrlError();

                        List<string> models = new List<string>();
                        try
                        {
                            models = await FetchModelsAsync(baseUrl).ConfigureAwait(true);
                        }
                        catch { }

                        // If fetch returned models, update cache and UI; if not, try using cache or persisted model
                        if (models != null && models.Count > 0)
                        {
                            try { _modelsCacheByServerId.AddOrUpdate(ActiveServerIdTextBox_Modal.Text ?? string.Empty, (k) => new System.Collections.Generic.List<string>(models), (k, v) => new System.Collections.Generic.List<string>(models)); } catch { }
                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);
                                ServerModelCombo_Modal.SelectedIndex = 0;
                            }
                            catch { }
                        }
                        else
                        {
                            // no models returned: fallback to cache or persisted model
                            var sid = (ActiveServerIdTextBox_Modal.Text ?? string.Empty);
                            System.Collections.Generic.List<string> cached = null;
                            _modelsCacheByServerId.TryGetValue(sid, out cached);
                            if (cached != null && cached.Count > 0)
                            {
                                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Using cached models for server " + sid); } catch { }
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in cached) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch { }
                            }
                            else
                            {
                                // fallback to persisted single model if exists
                                try
                                {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model))
                                    {
                                        try
                                        {
                                            if (AgenteIALocalControl.IsChatModelId(srv.Model))
                                            {
                                                ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.Items.Add(srv.Model); ServerModelCombo_Modal.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                // persisted model is not chat-capable -> do not inject, show error
                                                try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch { }
                                                try { ShowBaseUrlError("Modelo no compatible (embedding)"); } catch { }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                }
                                catch { }
                            }
                        }

                        try { BaseUrlHealthChanged?.Invoke(true, baseUrl, models); } catch { }
                    }
                    else if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized || resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // Auth issue: clear combo and show error
                        try { ShowBaseUrlError("No autorizado: revisa API Key"); } catch { }
                        try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch { }
                        try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch { }
                    }
                    else
                    {
                        try { ShowBaseUrlError("Servidor responde " + resp.StatusCode); } catch { }
                        // non-success (other than auth): try offline fallback before clearing
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch { }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch { }
                            }
                            else
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch { }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch { }
                        }
                        catch { try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch { } try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch { } }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        // NUEVO METODO ApplyServerToUi - ID: 20250304_170015
        private void ApplyServerToUi(string serverId, ServerConfig srv)
        {
            // NUEVO - suppress BaseUrl change handler while hydrating UI - ID: 20260115_223100
            _suppressBaseUrlTextChanged_20260116 = true;
            try
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
            finally
            {
                _suppressBaseUrlTextChanged_20260116 = false;
            }

            // Fire a single evaluation of the base URL after hydration
            try
            {
                FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged.ApplyServerToUi");
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
            if (_suppressBaseUrlTextChanged_20260116 || _isInitializingAdvancedUi) return;
            FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged");
        }

        // NUEVO METODO PersistBaseUrlIfChanged - ID: 20260114_000075
        private void PersistBaseUrlIfChanged(string baseUrl)
        {
            if (_isInitializingAdvancedUi) return;
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
                // Persist only BaseUrl/server related fields here. requestDefaults persistence handled elsewhere
                // Apply modal globals (provider/runMode/requestDefaults/agent) before final save
                ApplyModalGlobalsToSettings(settings);

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

        // NUEVO METODO NormalizeBaseUri - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static Uri NormalizeBaseUri(string baseUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return null;
                Uri uri;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri))
                {
                    if (Uri.TryCreate("http://" + baseUrl, UriKind.Absolute, out uri)) return uri;
                    return null;
                }
                return uri;
            }
            catch { return null; }
        }

        // NUEVO METODO BuildModelsUri - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static string BuildModelsUri(Uri baseUri)
        {
            try
            {
                if (baseUri == null) return null;
                var s = baseUri.ToString().TrimEnd('/');
                var lower = s.ToLowerInvariant();
                if (lower.EndsWith("/v1/models")) return s; // already complete
                if (lower.EndsWith("/v1")) return s + "/models";
                return s + "/v1/models";
            }
            catch { return null; }
        }

        // NUEVO METODO TryGetFallbackHost - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static string TryGetFallbackHost(Uri baseUri)
        {
            try
            {
                if (baseUri == null) return null;
                var host = baseUri.Host ?? string.Empty;
                if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) return "localhost";
                if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return "127.0.0.1";
                return null;
            }
            catch { return null; }
        }

        // NUEVO METODO IsConnectionRefused - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static bool IsConnectionRefused(Exception ex)
        {
            try
            {
                if (ex == null) return false;
                var ae = ex as AggregateException;
                if (ae != null) ex = ae.GetBaseException();

                var httpEx = ex as HttpRequestException;
                if (httpEx != null)
                {
                    var inner = httpEx.InnerException;
                    if (inner != null) ex = inner;
                }

                var socketEx = ex as System.Net.Sockets.SocketException;
                if (socketEx != null)
                {
                    if (socketEx.ErrorCode == 10061) return true;
                }

                if (ex.InnerException != null) return IsConnectionRefused(ex.InnerException);

                var msg = ex.Message ?? string.Empty;
                if (msg.IndexOf("Connection refused", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (msg.IndexOf("ECONNREFUSED", StringComparison.OrdinalIgnoreCase) >= 0) return true;

                return false;
            }
            catch { return false; }
        }

        private async Task<List<string>> FetchModelsAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;
                // MODIFICADO FetchModelsAsync - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
                var baseUri = NormalizeBaseUri(baseUrl);
                if (baseUri == null) return result;

                var primary = BuildModelsUri(baseUri);
                var fallbackHost = TryGetFallbackHost(baseUri);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync GET " + primary); } catch { }

                    HttpResponseMessage resp = null;
                    Exception firstEx = null;
                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Get, primary))
                        {
                            req.Headers.Accept.Clear();
                            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            try
                            {
                                var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                apiKey = apiKey.Trim();
                                if (!string.IsNullOrWhiteSpace(apiKey))
                                {
                                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                }
                            }
                            catch { }

                            resp = await client.SendAsync(req).ConfigureAwait(true);
                        }
                    }
                    catch (Exception exPrimary)
                    {
                        firstEx = exPrimary;
                    }

                    // If primary attempt threw connection refused and we have a fallback host, try alternate host
                    if (firstEx != null && IsConnectionRefused(firstEx) && !string.IsNullOrEmpty(fallbackHost))
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: primary GET failed with connection refused, will retry with fallback host"); } catch { }
                        var altBuilder = new UriBuilder(baseUri) { Host = fallbackHost };
                        var alt = BuildModelsUri(altBuilder.Uri);
                        try
                        {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync fallback GET " + alt); } catch { }
                        try
                        {
                            using (var req = new HttpRequestMessage(HttpMethod.Get, alt))
                            {
                                req.Headers.Accept.Clear();
                                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                try
                                {
                                    var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                    apiKey = apiKey.Trim();
                                    if (!string.IsNullOrWhiteSpace(apiKey))
                                    {
                                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                    }
                                }
                                catch { }

                                resp = await client.SendAsync(req).ConfigureAwait(true);
                                firstEx = null; // mark fallback attempted
                            }
                        }
                        catch (Exception exAlt)
                        {
                            // fallback also failed
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync both primary and fallback failed: " + exAlt.Message, exAlt); } catch { }
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch { }
                            return result;
                        }
                        }
                        catch (Exception exAlt)
                        {
                            // fallback also failed
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync both primary and fallback failed: " + exAlt.Message, exAlt); } catch { }
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch { }
                            return result;
                        }
                    }

                    if (firstEx != null)
                    {
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync error: " + firstEx.Message, firstEx); } catch { }
                        return result;
                    }

                    if (resp == null)
                    {
                        return result;
                    }

                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: FetchModelsAsync non-success status: " + resp.StatusCode); } catch { }
                        return result;
                    }

                    var txt = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);
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
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch { }
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
                    // MODIFICADO METODO SaveButton_Click - ID: 20260117_132400
                    // Do not persist embedding/non-chat models
                    if (AgenteIALocalControl.IsChatModelId(selectedModel))
                    {
                        srv.Model = selectedModel;
                    }
                    else
                    {
                        // do not persist; clear selection and inform user via UI error
                        srv.Model = string.Empty;
                        try { ShowBaseUrlError("Modelo no compatible (embedding) - no guardado"); } catch { }
                    }
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

        // NUEVO METODO ApplyModalGlobalsToSettings - ID: 20260118_000002
        private void ApplyModalGlobalsToSettings(AgentSettings settings)
        {
            if (settings == null) return;
            try
            {
                // provider -> active server selection already applied via servers list
                if (settings.GlobalSettings == null) settings.GlobalSettings = new JObject();

                // runMode
                try
                {
                    var selected = GetSelectedComboContent(RunModeCombo_Modal);
                    var runMode = string.Equals(selected, "Agente", StringComparison.OrdinalIgnoreCase) ? "agente" : "preguntar";
                    settings.GlobalSettings["runMode"] = runMode;
                }
                catch { }

                // agent settings
                try
                {
                    var agent = settings.GlobalSettings["agent"] as JObject ?? new JObject();
                    settings.GlobalSettings["agent"] = agent;
                    agent["ideIntegration"] = AgentIdeIntegrationToggle_Modal.IsChecked == true;
                    agent["applyChanges"] = AgentApplyChangesToggle_Modal.IsChecked == true;
                    int ms = ParseMaxSteps(AgentMaxStepsTextBox_Modal.Text);
                    agent["maxSteps"] = ms;
                }
                catch { }

                // requestDefaults via unified persister
                PersistRequestDefaultsFromUi();
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                try { AgentSettingsStore.SettingsSaved -= OnSettingsSaved; } catch { }
            }
            catch { }
            base.OnClosed(e);
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