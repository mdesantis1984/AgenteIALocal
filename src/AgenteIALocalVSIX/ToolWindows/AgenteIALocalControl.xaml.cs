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
        public bool RunButtonEnabled { get { return runButtonEnabled; } private set { if (runButtonEnabled == value) return; runButtonEnabled = value; OnPropertyChanged(nameof(RunButtonEnabled)); } }

        private bool clearButtonEnabled;
        public bool ClearButtonEnabled { get { return clearButtonEnabled; } private set { if (clearButtonEnabled == value) return; clearButtonEnabled = value; OnPropertyChanged(nameof(ClearButtonEnabled)); } }

        private bool isPromptReadOnly;
        public bool IsPromptReadOnly { get { return isPromptReadOnly; } private set { if (isPromptReadOnly == value) return; isPromptReadOnly = value; OnPropertyChanged(nameof(IsPromptReadOnly)); } }

        private bool isLlmConfigured;
        private bool IsLlmConfigured { get { return isLlmConfigured; } set { if (isLlmConfigured == value) return; isLlmConfigured = value; OnPropertyChanged(nameof(IsLlmConfigured)); } }

        // Chat state
        private List<ChatSession> chats = new List<ChatSession>();
        private ChatSession activeChat = null;

        // Mock modified files
        public List<string> ModifiedFiles { get; private set; } = new List<string> { "ProjectA/File1.cs", "ProjectB/Helper.cs", "Shared/Utils.cs" };
        private bool isChangesExpanded = false;
        public bool IsChangesExpanded { get { return isChangesExpanded; } set { if (isChangesExpanded == value) return; isChangesExpanded = value; OnPropertyChanged(nameof(IsChangesExpanded)); } }

        public ExecutionState CurrentExecutionState
        {
            get => currentExecutionState;
            private set
            {
                if (currentExecutionState == value) return;
                currentExecutionState = value;
                OnPropertyChanged(nameof(CurrentExecutionState));
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
                    Log("AgentService available at control construction.");
                }
                else
                {
                    Log("AgentService is null at control construction.");
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
            OnPropertyChanged(nameof(ModifiedFiles));
            OnPropertyChanged(nameof(ModifiedFilesCount));
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
                OnPropertyChanged(nameof(ModifiedFiles));
                OnPropertyChanged(nameof(ModifiedFilesCount));
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
                if (settings != null && !string.IsNullOrEmpty(settings.ActiveServerId) && settings.Servers != null)
                {
                    var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                    if (srv != null)
                    {
                        if (!string.IsNullOrEmpty(srv.BaseUrl) && !string.IsNullOrEmpty(srv.Model)) configured = true;
                    }
                }

                IsLlmConfigured = configured;
            }
            catch
            {
                // never throw from UI
                IsLlmConfigured = false;
            }
        }

        private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var panel = GetElement<FrameworkElement>("SettingsPanel");
            if (panel != null)
            {
                panel.Visibility = panel.Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
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
                Log("Settings saved.");
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
            SolutionNameText.Text = solutionName;
            ProjectCountText.Text = projectCount.ToString();

            // Prepare initial request but do not execute
            var req = BuildRequest(solutionName, projectCount);
            PromptTextBox.Text = SerializeToJson(req);
            ResponseJsonText.Text = string.Empty;
            // Do not clear LogText here; keep file-backed content
        }

        private CopilotRequest BuildRequest(string solutionName, int projectCount)
        {
            return new CopilotRequest
            {
                RequestId = System.Guid.NewGuid().ToString(),
                Action = "mock-execute",
                Timestamp = System.DateTime.UtcNow.ToString("o"),
                SolutionName = solutionName,
                ProjectCount = projectCount
            };
        }

        private string SerializeToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        private void UpdateStateProperties(ExecutionState newState)
        {
            // Map states to icon kind, color and label according to UX spec
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

            // Notify bindings for related properties
            OnPropertyChanged(nameof(StateIconKind));
            OnPropertyChanged(nameof(StateColor));
            OnPropertyChanged(nameof(StateLabel));
        }

        private void UpdateUiState(ExecutionState newState)
        {
            CurrentExecutionState = newState;

            // Apply enable/disable rules based on LLM configuration and state
            if (!IsLlmConfigured)
            {
                // No LLM configured: disable interactive controls except settings/help, allow prompt read-only
                RunButtonEnabled = false;
                ClearButtonEnabled = false;
                IsPromptReadOnly = true;
                return;
            }

            // LLM configured: apply state-specific rules
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

        private async void RunButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CurrentExecutionState == ExecutionState.Running) return;

            Log("Run clicked.");
            UpdateUiState(ExecutionState.Running);
            Log("Execution started.");

            try
            {
                // Ensure composition is available before attempting execution
                AgentComposition.EnsureComposition();

                // NEW: treat user input as plain text prompt; build CopilotRequest internally
                var userInput = PromptTextBox.Text ?? string.Empty;

                var req = new CopilotRequest
                {
                    RequestId = System.Guid.NewGuid().ToString(),
                    Action = userInput, // use user text as main prompt fragment
                    Timestamp = System.DateTime.UtcNow.ToString("o"),
                    SolutionName = SolutionNameText.Text ?? string.Empty,
                    ProjectCount = int.TryParse(ProjectCountText.Text, out var pc) ? pc : 0
                };

                // Execute using composed AgentService if available; otherwise fall back to direct MockCopilotExecutor
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
                            Log("AgentService not composed; using MockCopilotExecutor fallback.");
                            return MockCopilotExecutor.Execute(req);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Execution exception in background task: " + ex.Message);
                        throw;
                    }
                });

                // Display plain text output (or error) instead of serializing full DTO
                var display = string.Empty;
                try
                {
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
                }
                catch
                {
                    display = "(unable to render response)";
                }

                // Update UI on UI thread
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResponseJsonText.Text = ChatRenderPreprocessor.Preprocess(display);
                    UpdateUiState(ExecutionState.Completed);
                    Log("Execution completed successfully.");

                    // Refresh log tab to show newly appended entries (immediately)
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
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                UpdateUiState(ExecutionState.Error);
                Log("Execution failed: " + ex.Message);
                Trace.TraceError("[AgenteIALocalControl] Execution failed: " + ex);
                ResponseJsonText.Text = ChatRenderPreprocessor.Preprocess("{ \"error\": \"Execution failed\" }");

                // Attempt to refresh log view even on error
                try { RefreshLogFromFile(); } catch { }
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PromptTextBox.Text = string.Empty;
            ResponseJsonText.Text = string.Empty;

            // Clear the persistent log file and refresh view
            try
            {
                ClearLogFile();
            }
            catch
            {
                // ensure UI does not throw
            }

            RefreshLogFromFile();

            UpdateUiState(ExecutionState.Idle);
        }

        private void RefreshLogFromFile()
        {
            try
            {
                var content = ReadLogFile();
                // If file is empty, show placeholder
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
                // never throw from UI refresh; show minimal info
                LogText.Text = "(unable to read logs)";
            }
        }

        private void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            // Prepend to UI log view for immediate feedback
            LogText.Text = ts + " - " + message + "\n" + LogText.Text;

            // Write to persistent log via composition logger or direct file writer
            try
            {
                if (AgentComposition.Logger != null)
                {
                    try { AgentComposition.Logger.Invoke("[AgenteIALocalControl] " + message); } catch { }
                }
                else
                {
                    // fallback
                    AppendLogFileLine("[AgenteIALocalControl] " + message);
                }
            }
            catch
            {
                // never throw
            }
        }

        // Helper methods to access the persistent log file without depending on LogFile.cs being in project
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
            catch
            {
                // never throw
            }
        }

        private static void ClearLogFile()
        {
            try
            {
                var path = GetLogFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Truncate, FileAccess.Write)) { }
                }
                else
                {
                    using (var fs = new FileStream(path, FileMode.CreateNew)) { }
                }
            }
            catch
            {
                // never throw
            }
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Helper to build a FlowDocument from raw chat text (prepares future rich rendering)
        private static FlowDocument BuildChatDocument(string raw)
        {
            var doc = new FlowDocument();
            if (string.IsNullOrEmpty(raw)) return doc;

            string[] lines = raw.Replace("\r\n", "\n").Split('\n');
            bool inFence = false;
            var codeLines = new List<string>();
            foreach (var line in lines)
            {
                if (line.StartsWith("```"))
                {
                    if (!inFence)
                    {
                        inFence = true;
                        codeLines.Clear();
                    }
                    else
                    {
                        // close fence
                        inFence = false;
                        var codeText = string.Join("\n", codeLines);
                        var section = new Section();
                        var para = new Paragraph(new Run(codeText)) { FontFamily = new FontFamily("Consolas") };
                        section.Blocks.Add(para);
                        doc.Blocks.Add(section);
                        codeLines.Clear();
                    }
                    continue;
                }

                if (inFence)
                {
                    codeLines.Add(line);
                }
                else
                {
                    var para = new Paragraph(new Run(line));
                    doc.Blocks.Add(para);
                }
            }

            // If file ends while inside fence, emit collected as code block
            if (inFence && codeLines.Count > 0)
            {
                var codeText = string.Join("\n", codeLines);
                var section = new Section();
                var para = new Paragraph(new Run(codeText)) { FontFamily = new FontFamily("Consolas") };
                section.Blocks.Add(para);
                doc.Blocks.Add(section);
            }

            return doc;
        }

        private void VerboseLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: connect to verbose log view if available.
                RefreshLogFromFile();
            }
            catch
            {
                // never throw from UI
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
    }
}
