using AgenteIALocal.Core.Models.Agent;
using AgenteIALocalVSIX.Chats;
using AgenteIALocalVSIX.Execution;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

        private IChatService _chatService;

        private IRunExecutor _runExecutor;

        private ExecutionState currentExecutionState = ExecutionState.Idle;

        private CancellationTokenSource logRefreshCts;

        private static bool _mahAppsResolveHooked;
        // Active correlation id for the current Run execution (used by logging in this control)
        private string activeCorrelationId = null;
        private CancellationTokenSource _runCts;
        private int _runVersion;
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

        // Log file size label (bound in XAML, auto-updated)
        private string logFileSizeLabel = "0 K";
        private bool _logScrollInitialized;

        // Chat state
        private List<ChatSession> chats = new List<ChatSession>();
        private ChatSession activeChat = null;

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

        public string LogFileSizeLabel
        {
            get { return logFileSizeLabel; }
            private set { if (logFileSizeLabel == value) return; logFileSizeLabel = value ?? "0 K"; RaisePropertyChanged(nameof(LogFileSizeLabel)); }
        }

        public AgenteIALocalControl()
        {
            EnsureMahAppsIconPacksLoaded();
            InitializeComponent();

            // Set DataContext for XAML bindings
            this.DataContext = this;

            _chatService = new DefaultChatService(this);
            _runExecutor = new DefaultRunExecutor(this);

            // Initial UI state - will be refreshed after loading settings
            UpdateUiState(ExecutionState.Idle);

            // Do not override AgentComposition.LoggerV2 here; package wires the V2 logging pipeline.

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
                // Replace Trace with V2 logger
                try { AgentComposition.Error(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Error ensuring composition: " + ex.Message, ex); } catch { }
            }

            // Load current log file content into the Log tab asynchronously
            StartLogRefreshLoop();

            // Ensure initial size label is correct even before first refresh tick
            try { UpdateLogFileSizeLabelFromBytes(TryGetLogFileSizeBytes()); } catch { }

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
                LoadUiStateSafe();

                chats = ChatStore.LoadAll().ToList();
                if (chats.Count == 0)
                {
                    activeChat = ChatStore.CreateNew();
                    chats.Add(activeChat);
                }
                else
                {
                    // prefer last active chat id from ui state
                    var desiredId = _uiState != null ? _uiState.LastChatId : null;
                    if (!string.IsNullOrEmpty(desiredId))
                    {
                        activeChat = chats.FirstOrDefault(c => c != null && string.Equals(c.Id, desiredId, StringComparison.Ordinal));
                    }

                    if (activeChat == null)
                    {
                        // fallback to first in sorted list
                        activeChat = chats[0];
                    }
                }

                try
                {
                    lock (_uiStateGate)
                    {
                        if (_uiState == null) _uiState = new UiState();
                        _uiState.LastChatId = activeChat != null ? activeChat.Id : null;
                    }
                    SaveUiStateSafe();
                }
                catch { }

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

        // Mock modified files
        public List<string> ModifiedFiles { get; private set; } = new List<string> { "ProjectA/File1.cs", "ProjectB/Helper.cs", "Shared/Utils.cs" };
        private bool isChangesExpanded = false;
        public bool IsChangesExpanded { get { return isChangesExpanded; } set { if (isChangesExpanded == value) return; isChangesExpanded = value; RaisePropertyChanged(nameof(IsChangesExpanded)); } }

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

        private void OpenLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = GetLogFilePath();
                if (string.IsNullOrWhiteSpace(path))
                {
                    AppendLog("Open log file: path not available.");
                    return;
                }

                // Ensure the file exists so Explorer can select it.
                if (!File.Exists(path))
                {
                    try { AppendLogFileLine("(log file created)"); } catch { }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppendLog("Open log file failed: " + ex.Message);
            }
        }

        private void DeleteLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = GetLogFilePath();
                if (string.IsNullOrWhiteSpace(path))
                {
                    AppendLog("Delete log file: path not available.");
                    return;
                }

                if (!File.Exists(path))
                {
                    UpdateLogFileSizeLabelFromBytes(0);
                    AppendLog("Delete log file: file does not exist.");
                    return;
                }

                var res = MessageBox.Show(
                    "This will permanently delete the log file from disk. Are you sure?",
                    "Delete log file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (res != MessageBoxResult.Yes) return;

                File.Delete(path);

                Ui(() =>
                {
                    try
                    {
                        UpdateLogFileSizeLabelFromBytes(0);
                        if (LogText != null)
                        {
                            LogText.Text = "(no logs)";
                            ScrollLogToEnd(force: true);
                        }
                    }
                    catch { }
                });

                AppendLog("Log file deleted from disk.");
            }
            catch (Exception ex)
            {
                AppendLog("Delete log file failed: " + ex.Message);
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
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var content = await Task.Run(() => ReadLogFile());

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

                        var bytes = TryGetLogFileSizeBytes();
                        UpdateLogFileSizeLabelFromBytes(bytes);

                        LogText.Text = string.IsNullOrEmpty(content)
                            ? "(no logs)"
                            : content;

                        ScrollLogToEnd(force: false);
                    }
                    catch
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                        UpdateLogFileSizeLabelFromBytes(0);
                        LogText.Text = "(unable to read logs)";
                        ScrollLogToEnd(force: false);
                    }

                    try { await Task.Delay(2000, ct); } catch { }
                }

            }, ct);
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
                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] RefreshFromSettings invoked.");
                var settings = AgentSettingsStore.Load();
                PopulateSettingsPanel(settings);
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);
                try { AgentComposition.Verbose("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigStatus: RefreshFromSettings completed label={ConfigLabel} isConfigured={IsLlmConfigured}"); } catch { }

                // Refresh models for active server asynchronously (fire-and-forget)
                try { _ = RefreshModelsForActiveServerAsync("RefreshFromSettings"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"[AgenteIALocalControl] RefreshFromSettings error: {ex.Message}", ex); } catch { }
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
                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: GET {url}");
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: non-success status {resp.StatusCode}");
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
                        AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: parse error: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: error: {ex.Message}", ex);
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
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ModelOfLLM.Items.Clear();
                    return;
                }

                var srv = settings.Servers.Find(s => s.Id == activeId);
                if (srv == null)
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ModelOfLLM.Items.Clear();
                    return;
                }

                var baseUrl = srv.BaseUrl ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ModelOfLLM.Items.Clear();
                    return;
                }

                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelOfLLM: RefreshModelsForActiveServer reason={reason} baseUrl={baseUrl}");
                var models = await FetchModelsFromBaseUrlAsync(baseUrl);
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
            }
            catch (Exception ex)
            {
                AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelOfLLM: Refresh error: {ex.Message}", ex);
            }
        }

        // Handler when user changes selection in ModelOfLLM - persist and recompose
        private void ModelOfLLM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var sel = ModelOfLLM.SelectedItem as string;
                if (string.IsNullOrEmpty(sel)) return;
                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelOfLLM: selection changed modelPresent=true modelIdLength={sel.Length}");

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
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelOfLLM: selection handler error: {ex.Message}", ex); } catch { }
            }
        }
        private void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Enter) return;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;
                if (CurrentExecutionState == ExecutionState.Running) return;
                if (!RunButtonEnabled) return;
                e.Handled = true;
                RunButton_Click(sender, new RoutedEventArgs());
            }
            catch
            {
                // never throw from UI
            }
        }

        private bool TryGetActiveLmStudioServer(out AgenteIALocalVSIX.ServerConfig server)
        {
            server = null;
            try
            {
                var settings = AgenteIALocalVSIX.AgentSettingsStore.Load();
                if (settings == null) return false;

                var activeId = settings.ActiveServerId;
                if (string.IsNullOrEmpty(activeId) || settings.Servers == null) return false;

                var srv = settings.Servers.Find(s => string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase));
                if (srv == null) return false;

                if (string.IsNullOrEmpty(srv.Provider) || !srv.Provider.Equals("lmstudio", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (string.IsNullOrWhiteSpace(srv.BaseUrl) || string.IsNullOrWhiteSpace(srv.Model))
                    return false;

                server = srv;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildLmStudioPrompt(AgentHostRequest req)
        {
            if (req == null) return string.Empty;

            var action = req.Action ?? string.Empty;
            var sol = req.SolutionName ?? string.Empty;

            if (string.IsNullOrWhiteSpace(action)) return sol;
            if (string.IsNullOrWhiteSpace(sol)) return action;

            return action + " " + sol;
        }

        private void UpdateStateProperties(ExecutionState state)
        {
            try
            {
                switch (state)
                {
                    case ExecutionState.Idle:
                        StateIconKind = PackIconKind.Play;
                        StateColor = (Brush)FindResource("GreenBrush");
                        StateLabel = "Idle";
                        RunButtonEnabled = true;
                        ClearButtonEnabled = true;
                        IsPromptReadOnly = false;
                        break;

                    case ExecutionState.Running:
                        StateIconKind = PackIconKind.Stop;
                        StateColor = (Brush)FindResource("RedBrush");
                        StateLabel = "Running";
                        RunButtonEnabled = true;
                        ClearButtonEnabled = false;
                        IsPromptReadOnly = true;
                        break;

                    case ExecutionState.Completed:
                        StateIconKind = PackIconKind.Check;
                        StateColor = (Brush)FindResource("BlueBrush");
                        StateLabel = "Completed";
                        RunButtonEnabled = true;
                        ClearButtonEnabled = true;
                        IsPromptReadOnly = false;
                        break;

                    case ExecutionState.Error:
                        StateIconKind = PackIconKind.Error;
                        StateColor = (Brush)FindResource("RedBrush");
                        StateLabel = "Error";
                        RunButtonEnabled = false;
                        ClearButtonEnabled = true;
                        IsPromptReadOnly = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Error updating state properties: " + ex.Message, ex); } catch { }
            }
        }

        private void TryRemoveEmptyAiBubble(ChatSession chat, ChatMessage aiBubble)
        {
            try
            {
                if (chat == null || aiBubble == null) return;
                if (!string.IsNullOrWhiteSpace(aiBubble.Content)) return;

                chat.Messages?.Remove(aiBubble);
                TryPersistChat(chat);
                RenderActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }

        private async System.Threading.Tasks.Task<AgentHostResponse> ExecuteLmStudioStreamingAsync(
            AgentHostRequest req,
            AgenteIALocalVSIX.ServerConfig server,
            ChatSession chat,
            ChatMessage aiBubble,
            CancellationToken ct)
        {
            var respObj = new AgentHostResponse
            {
                RequestId = req?.RequestId,
                Success = false,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            try
            {
                if (server == null) return respObj;

                var baseUrl = (server.BaseUrl ?? string.Empty).TrimEnd('/');
                var url = baseUrl + "/v1/chat/completions";

                var payload = new JObject();
                if (!string.IsNullOrWhiteSpace(server.Model))
                    payload["model"] = server.Model;

                payload["stream"] = true;
                payload["messages"] = new JArray(new JObject
                {
                    ["role"] = "user",
                    ["content"] = BuildLmStudioPrompt(req)
                });

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);

                    using (var httpReq = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        httpReq.Headers.Accept.Clear();
                        try
                        {
                            httpReq.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
                        }
                        catch { }

                        if (!string.IsNullOrWhiteSpace(server.ApiKey))
                        {
                            try
                            {
                                httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", server.ApiKey);
                            }
                            catch { }
                        }

                        httpReq.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                        using (var httpResp = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct))
                        {
                            if (!httpResp.IsSuccessStatusCode)
                            {
                                string body = string.Empty;
                                try { body = await httpResp.Content.ReadAsStringAsync(); } catch { }

                                var sample = body ?? string.Empty;
                                if (sample.Length > 600) sample = sample.Substring(0, 600) + "...";

                                respObj.Error = "LM Studio HTTP " + (int)httpResp.StatusCode + " " + httpResp.ReasonPhrase + (string.IsNullOrEmpty(sample) ? string.Empty : (": " + sample));
                                return respObj;
                            }

                            var sb = new StringBuilder();
                            int? promptTokens = null;
                            int? completionTokens = null;
                            int? totalTokens = null;

                            int renderPending = 0;
                            long lastRenderTick = 0;
                            int lastRenderedLen = 0;

                            async Task QueueRenderAsync(bool force = false)
                            {
                                if (ct.IsCancellationRequested) return;

                                var now = Stopwatch.GetTimestamp();
                                if (!force)
                                {
                                    var last = Interlocked.Read(ref lastRenderTick);
                                    if (last != 0 && (now - last) < 33)
                                        return;
                                }

                                if (Interlocked.Exchange(ref renderPending, 1) != 0) return;
                                try
                                {
                                    Interlocked.Exchange(ref lastRenderTick, now);

                                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

                                    var currentLen = 0;
                                    try { currentLen = aiBubble?.Content?.Length ?? 0; } catch { }

                                    if (force || currentLen != lastRenderedLen)
                                    {
                                        lastRenderedLen = currentLen;
                                        RenderActiveChatToUi();
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    // ignore cancellation
                                }
                                catch
                                {
                                    // ignore render errors
                                }
                                finally
                                {
                                    Interlocked.Exchange(ref renderPending, 0);
                                }
                            }

                            CancellationTokenRegistration cancelReg = default(CancellationTokenRegistration);
                            try
                            {
                                cancelReg = ct.Register(() =>
                                {
                                    try { httpResp.Dispose(); } catch { }
                                });

                                using (var stream = await httpResp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                using (var reader = new StreamReader(stream))
                                {
                                    while (!ct.IsCancellationRequested)
                                    {
                                        string line = null;
                                        try { line = await reader.ReadLineAsync().ConfigureAwait(false); }
                                        catch { break; }

                                        if (line == null) break;

                                        if (string.IsNullOrWhiteSpace(line))
                                            continue;

                                        if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                                            continue;

                                        var data = line.Substring(5).Trim();

                                        if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                                            break;

                                        if (string.IsNullOrEmpty(data))
                                            continue;

                                        try
                                        {
                                            var j = JObject.Parse(data);

                                            var u = j["usage"] as JObject;
                                            if (u != null)
                                            {
                                                promptTokens = promptTokens ?? u.Value<int?>("prompt_tokens");
                                                completionTokens = completionTokens ?? u.Value<int?>("completion_tokens");
                                                totalTokens = totalTokens ?? u.Value<int?>("total_tokens");
                                            }

                                            var chunk =
                                                j.SelectToken("choices[0].delta.content")?.ToString() ??
                                                j.SelectToken("choices[0].message.content")?.ToString() ??
                                                j.SelectToken("choices[0].text")?.ToString();

                                            if (string.IsNullOrEmpty(chunk))
                                                continue;

                                            sb.Append(chunk);

                                            try
                                            {
                                                if (aiBubble != null) aiBubble.Content = sb.ToString();
                                            }
                                            catch { }

                                            await QueueRenderAsync();
                                        }
                                        catch
                                        {
                                            // ignore malformed chunks
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                try { cancelReg.Dispose(); } catch { }
                            }

                            var finalText = sb.ToString();

                            try
                            {
                                if (aiBubble != null) aiBubble.Content = finalText;
                            }
                            catch { }

                            await QueueRenderAsync(force: true);

                            if (ct.IsCancellationRequested)
                            {
                                respObj.Error = "Cancelled";
                                return respObj;
                            }

                            TryPersistChat(chat);

                            RenderActiveChatToUi();

                            respObj.Success = true;
                            respObj.Output = finalText;
                            respObj.PromptTokens = promptTokens;
                            respObj.CompletionTokens = completionTokens;
                            respObj.TotalTokens = totalTokens;
                            return respObj;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                respObj.Error = "Cancelled";
                return respObj;
            }
            catch (Exception ex)
            {
                respObj.Error = ex.Message;
                return respObj;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // If running, treat click as STOP.
            if (CurrentExecutionState == ExecutionState.Running)
            {
                _runExecutor.RequestStop();
                return;
            }

            _ = _runExecutor.RunAsync(sender, e);
        }


    }
}