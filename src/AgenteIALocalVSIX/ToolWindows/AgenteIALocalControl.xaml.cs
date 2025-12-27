using AgenteIALocalVSIX.Chats;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum ExecutionState { Idle, Running, Completed, Error }

        private ExecutionState currentExecutionState = ExecutionState.Idle;

        // Bindable properties for UI (icon, color, text)
        public PackIconKind StateIconKind { get; private set; }
        public Brush StateColor { get; private set; }
        public string StateLabel { get; private set; }

        // New bindable properties for control enablement
        private bool runButtonEnabled;
        public bool RunButtonEnabled { get { return runButtonEnabled; } private set { if (runButtonEnabled == value) return; runButtonEnabled = value; RaisePropertyChanged(nameof(RunButtonEnabled)); } }

        private bool clearButtonEnabled;
        public bool ClearButtonEnabled { get { return clearButtonEnabled; } private set { if (clearButtonEnabled == value) return; clearButtonEnabled = value; RaisePropertyChanged(nameof(ClearButtonEnabled)); } }

        private bool isPromptReadOnly;
        public bool IsPromptReadOnly { get { return isPromptReadOnly; } private set { if (isPromptReadOnly == value) return; isPromptReadOnly = value; RaisePropertyChanged(nameof(IsPromptReadOnly)); } }

        private bool isLlmConfigured;
        private bool IsLlmConfigured { get { return isLlmConfigured; } set { if (isLlmConfigured == value) return; isLlmConfigured = value; RaisePropertyChanged(nameof(IsLlmConfigured)); } }

        // Config status label (bound in XAML)
        private string configLabel = "Not Config";
        public string ConfigLabel { get { return configLabel; } private set { if (configLabel == value) return; configLabel = value ?? "Not Config"; RaisePropertyChanged(nameof(ConfigLabel)); } }

        // Chat state
        private List<ChatSession> chats = new List<ChatSession>();
        private ChatSession activeChat = null;

        // Mock modified files
        public List<string> ModifiedFiles { get; private set; } = new List<string> { "ProjectA/File1.cs", "ProjectB/Helper.cs", "Shared/Utils.cs" };
        private bool isChangesExpanded = false;
        public bool IsChangesExpanded { get { return isChangesExpanded; } set { if (isChangesExpanded == value) return; isChangesExpanded = value; RaisePropertyChanged(nameof(IsChangesExpanded)); } }

        public ExecutionState CurrentExecutionState
        {
            get => currentExecutionState;
            private set
            {
                if (currentExecutionState == value) return;
                currentExecutionState = value;
                RaisePropertyChanged(nameof(CurrentExecutionState));
                UpdateStateProperties(value);
            }
        }

        private CancellationTokenSource logRefreshCts;

        private static bool _mahAppsResolveHooked;

        private static void EnsureMahAppsIconPacksLoaded()
        {
            try
            {
                // Already loaded?
                if (AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => string.Equals(a.GetName().Name, "MahApps.Metro.IconPacks.Material", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var dir = Path.GetDirectoryName(typeof(AgenteIALocalControl).Assembly.Location);
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    return;

                 void LoadIfExists(string path)
                {
                    if (File.Exists(path))
                        Assembly.LoadFrom(path);
                }

                // Load core first, then material
                LoadIfExists(Path.Combine(dir, "MahApps.Metro.IconPacks.Core.dll"));
                LoadIfExists(Path.Combine(dir, "MahApps.Metro.IconPacks.Material.dll"));

                if (!_mahAppsResolveHooked)
                {
                    _mahAppsResolveHooked = true;
                    AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
                    {
                        try
                        {
                            var name = new AssemblyName(e.Name).Name + ".dll";
                            var candidate = Path.Combine(dir, name);
                            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
                        }
                        catch
                        {
                            return null;
                        }
                    };
                }
            }
            catch
            {
                // Must never break the toolwindow; avoid throwing here.
            }
        }


        public AgenteIALocalControl()
        {
            EnsureMahAppsIconPacksLoaded();
            InitializeComponent();

            // Set DataContext for XAML bindings
            this.DataContext = this;

            // Initial UI state - will be refreshed after loading settings
            UpdateUiState(ExecutionState.Idle);

            // Do not override AgentComposition.Logger here; package provides a file-based logger.

            // Attempt to set initial solution info using composition if available
            try
            {
                AgentComposition.EnsureComposition();
                if (AgentComposition.AgentService != null)
                {
                    AppendLog("AgentService available at control construction.");
                }
                else
                {
                    AppendLog("AgentService is null at control construction.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[AgenteIALocalControl] Error ensuring composition: {ex}");
            }

            // Load current log file content into the Log tab asynchronously
            StartLogRefreshLoop();

            // Load current settings into settings panel (but keep panel hidden)
            try
            {
                var settings = AgentSettingsStore.Load();
                PopulateSettingsPanel(settings);

                // compute LLM configured state and refresh UI
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);
            }
            catch { }

            // Load chats
            try
            {
                chats = ChatStore.LoadAll().ToList();
                if (chats.Count == 0)
                {
                    activeChat = ChatStore.CreateNew();
                    chats.Add(activeChat);
                }
                else
                {
                    // select last active (first in sorted list)
                    activeChat = chats[0];
                }

                RefreshChatCombo();
                LoadActiveChatToUi();
            }
            catch
            {
                // ignore chat errors
            }

            // Ensure mock modified files are available for binding
            RaisePropertyChanged(nameof(ModifiedFiles));
            RaisePropertyChanged(nameof(ModifiedFilesCount));

            // Ensure header reflects current settings immediately
            try
            {
                RefreshFromSettings();
            }
            catch { }
        }

        public int ModifiedFilesCount
        {
            get
            {
                if (ModifiedFiles == null)
                {
                    return 0;
                }

                return ModifiedFiles.Count;
            }
        }

        private void ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show("You are applying the changes. Are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                // Mock: show message and keep list unchanged
                MessageBox.Show("Apply changes (mock) executed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // ignore
            }
        }

        private void RevertChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show("You are reverting the changes. Are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;

                // Mock: show message
                MessageBox.Show("Revert changes (mock) executed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                // ignore
            }
        }

        private void ClearChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show("You are clearing the list of changes. Are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;

                ModifiedFiles.Clear();
                RaisePropertyChanged(nameof(ModifiedFiles));
                RaisePropertyChanged(nameof(ModifiedFilesCount));
            }
            catch
            {
                // ignore
            }
        }

        private void RefreshChatCombo()
        {
            try
            {
                ChatComboBox.Items.Clear();
                foreach (var c in chats)
                {
                    var tb = new System.Windows.Controls.TextBlock { Text = c.Title };
                    ChatComboBox.Items.Add(new ComboBoxItem { Content = c.Title, Tag = c.Id });
                }

                // select active
                if (activeChat != null)
                {
                    for (int i = 0; i < ChatComboBox.Items.Count; i++)
                    {
                        var item = (ComboBoxItem)ChatComboBox.Items[i];
                        if ((string)item.Tag == activeChat.Id)
                        {
                            ChatComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void LoadActiveChatToUi()
        {
            try
            {
                if (activeChat == null)
                {
                    PromptTextBox.Text = string.Empty;
                    ResponseJsonText.Text = string.Empty;
                    return;
                }

                // For simplicity in this sprint, show messages concatenated in Response area and keep request empty
                var sb = new StringBuilder();
                foreach (var m in activeChat.Messages)
                {
                    sb.AppendLine($"[{m.Timestamp}] {m.Sender}: {m.Content}");
                }

                ResponseJsonText.Text = ChatRenderPreprocessor.Preprocess(sb.ToString());
                PromptTextBox.Text = string.Empty;
            }
            catch
            {
                // ignore
            }
        }

        private void ChatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                if (cb == null) return;
                var item = cb.SelectedItem as ComboBoxItem;
                if (item == null) return;
                var id = item.Tag as string;
                if (string.IsNullOrEmpty(id)) return;

                var s = ChatStore.Load(id);
                if (s != null)
                {
                    activeChat = s;
                    LoadActiveChatToUi();
                }
            }
            catch
            {
                // ignore
            }
        }

        private void NewChatButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var res = System.Windows.MessageBox.Show("You are creating a new chat. Are you sure? Yes / No", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (res != System.Windows.MessageBoxResult.Yes) return;

                var s = ChatStore.CreateNew();
                chats.Insert(0, s);
                activeChat = s;
                RefreshChatCombo();
                LoadActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }

        private void DeleteChatButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (activeChat == null) return;
                var res = System.Windows.MessageBox.Show("Are you sure you want to delete this chat? Yes / No", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res != System.Windows.MessageBoxResult.Yes) return;

                ChatStore.Delete(activeChat.Id);
                chats.RemoveAll(c => c.Id == activeChat.Id);

                if (chats.Count > 0)
                {
                    activeChat = chats[0];
                }
                else
                {
                    activeChat = ChatStore.CreateNew();
                    chats.Add(activeChat);
                }

                RefreshChatCombo();
                LoadActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }

        private T GetElement<T>(string name) where T : class
        {
            return this.FindName(name) as T;
        }

        private void PopulateSettingsPanel(AgentSettings settings)
        {
            if (settings == null) return;

            var activeIdTb = GetElement<TextBox>("ActiveServerIdTextBox");
            var baseUrlTb = GetElement<TextBox>("ServerBaseUrlTextBox");
            var modelTb = GetElement<TextBox>("ServerModelTextBox");
            var apiKeyTb = GetElement<TextBox>("ServerApiKeyTextBox");

            if (activeIdTb != null)
                activeIdTb.Text = settings.ActiveServerId ?? string.Empty;

            // find active server details
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

                try { AgentComposition.Logger?.Invoke($"ConfigStatus: computed configured={configured} activeServerId={activeId ?? "(none)"} baseUrlPresent={baseUrlPresent} modelPresent={modelPresent}"); } catch { }
            }
            catch
            {
                // never throw from UI
                IsLlmConfigured = false;
                ConfigLabel = "Not Config";
                try { AgentComposition.Logger?.Invoke("ConfigStatus: compute error, defaulted to Not Config"); } catch { }
            }
        }

        private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Logger?.Invoke("[AgenteIALocalControl] Settings button clicked (open modal).");

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                var title = settings.ActiveServerId ?? string.Empty;

                var owner = Window.GetWindow(this);
                var win = new AgenteIALocalConfigWindow(title);
                if (owner != null) win.Owner = owner;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                try
                {
                    AgentComposition.Logger?.Invoke($"[AgenteIALocalControl] Opening config modal with title '{win.Title}'");
                    win.ShowDialog();
                    AgentComposition.Logger?.Invoke("[AgenteIALocalControl] Config modal closed.");
                }
                catch (Exception ex)
                {
                    AgentComposition.Logger?.Invoke($"[AgenteIALocalControl] Error showing config modal: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                AgentComposition.Logger?.Invoke($"[AgenteIALocalControl] SettingsButton_Click failure: {ex.Message}");
            }
        }

        private void CloseSettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var panel = GetElement<FrameworkElement>("SettingsPanel");
            if (panel != null)
            {
                panel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void SaveSettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null) settings = new AgentSettings();

                var activeIdTb = GetElement<TextBox>("ActiveServerIdTextBox");
                var baseUrlTb = GetElement<TextBox>("ServerBaseUrlTextBox");
                var modelTb = GetElement<TextBox>("ServerModelTextBox");
                var apiKeyTb = GetElement<TextBox>("ServerApiKeyTextBox");

                // update active server id
                settings.ActiveServerId = activeIdTb != null ? activeIdTb.Text : string.Empty;

                // ensure server exists or update existing
                if (settings.Servers == null) settings.Servers = new System.Collections.Generic.List<ServerConfig>();

                var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                if (srv == null)
                {
                    srv = new ServerConfig { Id = settings.ActiveServerId, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = baseUrlTb != null ? baseUrlTb.Text : string.Empty;
                srv.Model = modelTb != null ? modelTb.Text : string.Empty;
                srv.ApiKey = apiKeyTb != null ? apiKeyTb.Text : string.Empty;

                AgentSettingsStore.Save(settings);

                // recompute configuration and update UI
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);

                // feedback
                AppendLog("Settings saved.");
            }
            catch
            {
                // never throw from UI
            }
        }

        private void StartLogRefreshLoop()
        {
            // Cancel any previous
            if (logRefreshCts != null)
            {
                logRefreshCts.Cancel();
            }
            logRefreshCts = new CancellationTokenSource();
            var ct = logRefreshCts.Token;

            // Start a background task that refreshes the log every 2 seconds without blocking the UI
            Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var content = await Task.Run(() => ReadLogFile());
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (string.IsNullOrEmpty(content))
                            {
                                LogText.Text = "(no logs)";
                            }
                            else
                            {
                                LogText.Text = content;
                            }
                        }));
                    }
                    catch
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => { LogText.Text = "(unable to read logs)"; }));
                    }

                    try { await Task.Delay(2000, ct); } catch { }
                }
            }, ct);
        }

        private void StopLogRefreshLoop()
        {
            try
            {
                if (logRefreshCts != null)
                {
                    logRefreshCts.Cancel();
                }
                logRefreshCts = null;
            }
            catch { }
        }

        public void SetSolutionInfo(string solutionName, int projectCount)
        {
            try
            {
                // Minimal behavior: only update visible solution/project labels.
                // Do not prepare or serialize any request, and do not modify prompt/response fields.
                SolutionNameText.Text = solutionName ?? string.Empty;
                ProjectCountText.Text = projectCount.ToString();
            }
            catch
            {
                // never throw from UI
            }
        }

        // Public helper to refresh UI from persisted settings (used by modal after save)
        public void RefreshFromSettings()
        {
            try
            {
                AgentComposition.Logger?.Invoke("[AgenteIALocalControl] RefreshFromSettings invoked.");
                var settings = AgentSettingsStore.Load();
                PopulateSettingsPanel(settings);
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);
                try { AgentComposition.Logger?.Invoke($"ConfigStatus: RefreshFromSettings completed label={ConfigLabel} isConfigured={IsLlmConfigured}"); } catch { }

                // Refresh models for active server asynchronously (fire-and-forget)
                try { _ = RefreshModelsForActiveServerAsync("RefreshFromSettings"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke($"[AgenteIALocalControl] RefreshFromSettings error: {ex.Message}"); } catch { }
            }
        }

        // Fetch models from baseUrl (same parsing logic as modal) and return list of ids
        private async Task<List<string>> FetchModelsFromBaseUrlAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;
                var url = baseUrl.TrimEnd('/') + "/v1/models";
                AgentComposition.Logger?.Invoke($"ModelsFetch: GET {url}");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        AgentComposition.Logger?.Invoke($"ModelsFetch: non-success status {resp.StatusCode}");
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
                                try { var id = item.Value<string>("id"); if (!string.IsNullOrEmpty(id)) result.Add(id); } catch { }
                            }
                            return result;
                        }
                        var models = root["models"] as Newtonsoft.Json.Linq.JArray;
                        if (models != null)
                        {
                            foreach (var item in models)
                            {
                                try { var id = item.Value<string>("id") ?? item.ToString(); if (!string.IsNullOrEmpty(id)) result.Add(id); } catch { }
                            }
                            return result;
                        }
                        if (root is Newtonsoft.Json.Linq.JArray arr)
                        {
                            foreach (var item in arr) { var s = item.ToString(); if (!string.IsNullOrEmpty(s)) result.Add(s); }
                        }
                    }
                    catch (Exception ex)
                    {
                        AgentComposition.Logger?.Invoke($"ModelsFetch: parse error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AgentComposition.Logger?.Invoke($"ModelsFetch: error: {ex.Message}");
            }
            return result;
        }

        // Refresh the ModelOfLLM ComboBox based on Active Server settings and remote model list
        public async Task RefreshModelsForActiveServerAsync(string reason)
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null) return;
                var activeId = settings.ActiveServerId;
                if (string.IsNullOrWhiteSpace(activeId) || settings.Servers == null)
                {
                    // clear UI
                    this.Dispatcher.BeginInvoke(new Action(() => { ModelOfLLM.Items.Clear(); }));
                    return;
                }

                var srv = settings.Servers.Find(s => s.Id == activeId);
                if (srv == null)
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { ModelOfLLM.Items.Clear(); }));
                    return;
                }

                var baseUrl = srv.BaseUrl ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { ModelOfLLM.Items.Clear(); }));
                    return;
                }

                AgentComposition.Logger?.Invoke($"ModelOfLLM: RefreshModelsForActiveServer reason={reason} baseUrl={baseUrl}");
                var models = await FetchModelsFromBaseUrlAsync(baseUrl);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ModelOfLLM.Items.Clear();
                        if (models != null && models.Count > 0)
                        {
                            foreach (var m in models) ModelOfLLM.Items.Add(m);
                            // try select saved model
                            var saved = srv.Model ?? string.Empty;
                            if (!string.IsNullOrEmpty(saved) && ModelOfLLM.Items.Contains(saved))
                            {
                                ModelOfLLM.SelectedItem = saved;
                            }
                            else
                            {
                                ModelOfLLM.SelectedIndex = 0;
                                // if saved model existed but not found, persist first as fallback
                                if (!string.IsNullOrEmpty(saved))
                                {
                                    try
                                    {
                                        srv.Model = ModelOfLLM.SelectedItem as string ?? string.Empty;
                                        AgentSettingsStore.Save(settings);
                                        AgentComposition.RecomposeFromSettings("ModelOfLLM.AutoFallback");
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                AgentComposition.Logger?.Invoke($"ModelOfLLM: Refresh error: {ex.Message}");
            }
        }

        // Handler when user changes selection in ModelOfLLM - persist and recompose
        private void ModelOfLLM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var sel = ModelOfLLM.SelectedItem as string;
                if (string.IsNullOrEmpty(sel)) return;
                AgentComposition.Logger?.Invoke($"ModelOfLLM: selection changed modelPresent=true modelIdLength={sel.Length}");

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();
                var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                if (srv == null)
                {
                    // nothing to persist against
                    return;
                }

                srv.Model = sel;
                AgentSettingsStore.Save(settings);
                try
                {
                    AgentComposition.RecomposeFromSettings("ModelOfLLM.SelectionChanged");
                }
                catch { }

                // Refresh UI state
                try
                {
                    ComputeIsLlmConfigured(settings);
                    UpdateUiState(CurrentExecutionState);
                }
                catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke($"ModelOfLLM: selection handler error: {ex.Message}"); } catch { }
            }
        }
        private void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Enter) return;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;
                if (!RunButtonEnabled) return;
                e.Handled = true;
                RunButton_Click(sender, new RoutedEventArgs());
            }
            catch
            {
                // never throw from UI
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentExecutionState == ExecutionState.Running) return;

            AppendLog("Run clicked.");
            UpdateUiState(ExecutionState.Running);
            AppendLog("Execution started.");

            try
            {
                AgentComposition.EnsureComposition();

                var userInput = PromptTextBox.Text ?? string.Empty;

                var req = new CopilotRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Action = userInput,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    SolutionName = SolutionNameText.Text ?? string.Empty,
                    ProjectCount = int.TryParse(ProjectCountText.Text, out var pc) ? pc : 0
                };

                var response = await Task.Run(() =>
                {
                    try
                    {
                        if (AgentComposition.AgentService != null)
                        {
                            return AgentComposition.AgentService.Execute(req);
                        }
                        else
                        {
                            AppendLog("AgentService not composed; using MockCopilotExecutor fallback.");
                            return MockCopilotExecutor.Execute(req);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog("Execution exception in background task: " + ex.Message);
                        throw;
                    }
                });

                var display = string.Empty;
                if (response == null)
                {
                    display = "(no response)";
                }
                else if (!string.IsNullOrEmpty(response.Output))
                {
                    display = response.Output;
                }
                else if (!string.IsNullOrEmpty(response.Error))
                {
                    display = "Error: " + response.Error;
                }
                else
                {
                    display = "(empty response)";
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResponseJsonText.Text = ChatRenderPreprocessor.Preprocess(display);
                    UpdateUiState(ExecutionState.Completed);
                    AppendLog("Execution completed successfully.");

                    try
                    {
                        var content = ReadLogFile();
                        if (string.IsNullOrEmpty(content)) LogText.Text = "(no logs)"; else LogText.Text = content;
                    }
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                UpdateUiState(ExecutionState.Error);
                AppendLog("Execution failed: " + ex.Message);
                Trace.TraceError("[AgenteIALocalControl] Execution failed: " + ex);
                ResponseJsonText.Text = ChatRenderPreprocessor.Preprocess("{ \"error\": \"Execution failed\" }");

                try { RefreshLogFromFile(); } catch { }
            }
        }

        // Raise property changed helper to avoid name collisions
        private void RaisePropertyChanged(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch { }
        }

        // Map ExecutionState to UI properties (icon/color/label)
        private void UpdateStateProperties(ExecutionState newState)
        {
            try
            {
                switch (newState)
                {
                    case ExecutionState.Idle:
                        StateIconKind = PackIconKind.PauseCircleOutline;
                        StateColor = Brushes.Gray;
                        StateLabel = "Idle";
                        break;
                    case ExecutionState.Running:
                        StateIconKind = PackIconKind.ProgressClock;
                        StateColor = Brushes.DodgerBlue;
                        StateLabel = "Running";
                        break;
                    case ExecutionState.Completed:
                        StateIconKind = PackIconKind.CheckCircleOutline;
                        StateColor = Brushes.LimeGreen;
                        StateLabel = "Completed";
                        break;
                    case ExecutionState.Error:
                        StateIconKind = PackIconKind.AlertCircleOutline;
                        StateColor = Brushes.IndianRed;
                        StateLabel = "Error";
                        break;
                    default:
                        StateIconKind = PackIconKind.PauseCircleOutline;
                        StateColor = Brushes.Gray;
                        StateLabel = newState.ToString();
                        break;
                }

                RaisePropertyChanged(nameof(StateIconKind));
                RaisePropertyChanged(nameof(StateColor));
                RaisePropertyChanged(nameof(StateLabel));
            }
            catch { }
        }

        // Update UI enablement based on current state and configuration
        private void UpdateUiState(ExecutionState newState)
        {
            CurrentExecutionState = newState;

            if (!IsLlmConfigured)
            {
                RunButtonEnabled = false;
                ClearButtonEnabled = false;
                IsPromptReadOnly = true;
                return;
            }

            switch (CurrentExecutionState)
            {
                case ExecutionState.Running:
                    RunButtonEnabled = false;
                    ClearButtonEnabled = false;
                    IsPromptReadOnly = false;
                    break;
                case ExecutionState.Idle:
                case ExecutionState.Completed:
                case ExecutionState.Error:
                default:
                    RunButtonEnabled = true;
                    ClearButtonEnabled = true;
                    IsPromptReadOnly = false;
                    break;
            }
        }

        // Logging file helpers
        private static string GetLogFilePath()
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var logDir = Path.Combine(local ?? string.Empty, "AgenteIALocal", "logs");
                var logPath = Path.Combine(logDir, "AgenteIALocal.log");
                return logPath;
            }
            catch
            {
                return Path.Combine(".", "logs", "AgenteIALocal.log");
            }
        }

        private static string ReadLogFile()
        {
            try
            {
                var path = GetLogFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) return string.Empty;
                if (!File.Exists(path)) return string.Empty;
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AppendLogFileLine(string message)
        {
            try
            {
                var path = GetLogFilePath();
                var dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);
                var line = DateTime.UtcNow.ToString("o") + " - " + (message ?? string.Empty) + Environment.NewLine;
                File.AppendAllText(path, line, Encoding.UTF8);
            }
            catch { }
        }

        private static void ClearLogFile()
        {
            try
            {
                var path = GetLogFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Truncate, FileAccess.Write)) { }
                }
                else
                {
                    using (var fs = new FileStream(path, FileMode.CreateNew)) { }
                }
            }
            catch { }
        }

        // Append to UI log and persistent storage
        private void AppendLog(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            try
            {
                LogText.Text = ts + " - " + message + "\n" + LogText.Text;
            }
            catch { }

            try
            {
                if (AgentComposition.Logger != null)
                {
                    try { AgentComposition.Logger.Invoke("[AgenteIALocalControl] " + message); } catch { }
                }
                else
                {
                    AppendLogFileLine("[AgenteIALocalControl] " + message);
                }
            }
            catch { }
        }

        private void RefreshLogFromFile()
        {
            try
            {
                var content = ReadLogFile();
                if (string.IsNullOrEmpty(content))
                {
                    LogText.Text = "(no logs)";
                }
                else
                {
                    LogText.Text = content;
                }
            }
            catch
            {
                try { LogText.Text = "(unable to read logs)"; } catch { }
            }
        }
    }

    namespace AgenteIALocalVSIX.ToolWindows
    {
        public sealed class Truncate11Converter : System.Windows.Data.IValueConverter
        {
            public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                var s = value as string;
                if (string.IsNullOrEmpty(s)) return string.Empty;
                if (s.Length <= 11) return s;
                return s.Substring(0, 11) + "...";
            }

            public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return value;
            }
        }
    }
}
