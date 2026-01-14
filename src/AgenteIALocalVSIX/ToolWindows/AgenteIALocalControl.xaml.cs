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
using System.Windows.Threading;
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

        // Sticky auto-scroll support
        private System.Windows.Controls.ScrollViewer _responseScrollViewer = null;
        private double _stickyThresholdPx = 48.0; // distance from bottom to consider 'at bottom'
        private bool _userScrolledAway = false;
        private int _autoScrollMinDeltaChars = 128;
        private int _lastAutoScrollTotalChars = 0;
        private bool _responseScrollViewerWired = false;
        // Streaming incremental append state
        private int _streamingFlushedLength = 0; // NUEVO CAMPO - ID: 20260114_000031
        private Paragraph _streamingAiParagraph = null; // NUEVO CAMPO - ID: 20260114_000032
        private Run _streamingAiLastRun = null; // NUEVO CAMPO - ID: 20260114_000033

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

        // NUEVO METODO ApplyStreamingDeltaFrom - ID: 20260114_000034
        private void ApplyStreamingDeltaFrom(StringBuilder sb)
        {
            try
            {
                if (_streamingAiRun == null) return;

                var para = _streamingAiParagraph;
                var lastRun = _streamingAiLastRun ?? _streamingAiRun;

                var prev = _streamingFlushedLength;
                var curr = sb.Length;
                var deltaLen = curr - prev;
                if (deltaLen <= 0) return;

                string delta = null;
                try { delta = sb.ToString(prev, deltaLen); } catch { delta = sb.ToString(); }

                if (para == null)
                {
                    try { _streamingAiRun.Text = sb.ToString(); } catch { }
                    _streamingFlushedLength = curr;
                    try { AutoScrollIfSticky(deltaLen); } catch { }
                    return;
                }

                if (lastRun.Text != null && lastRun.Text.Length < 4096)
                {
                    try { lastRun.Text += delta; }
                    catch
                    {
                        var nr = new Run(delta);
                        try { para.Inlines.Add(nr); } catch { }
                        lastRun = nr;
                    }
                }
                else
                {
                    var nr = new Run(delta);
                    try { para.Inlines.Add(nr); } catch { }
                    lastRun = nr;
                }

                _streamingAiLastRun = lastRun;
                _streamingFlushedLength = curr;
                try { AutoScrollIfSticky(deltaLen); } catch { }
            }
            catch { }
        }


        // NUEVO METODO AutoScrollIfSticky - ID: 20260114_000021
        private void AutoScrollIfSticky(int recentDeltaChars)
        {
            try
            {
                EnsureResponseScrollViewer();
                if (ResponseJsonText == null) return;

                // Determine total chars in document roughly
                var totalText = string.Empty;
                try
                {
                    var doc = ResponseJsonText.Document;
                    if (doc != null)
                    {
                        var range = new TextRange(doc.ContentStart, doc.ContentEnd);
                        totalText = range.Text ?? string.Empty;
                    }
                }
                catch { }

                int totalChars = totalText.Length;

                // Delta guard to reduce ScrollToEnd frequency
                if (Math.Abs(totalChars - _lastAutoScrollTotalChars) < _autoScrollMinDeltaChars) return;

                // If user scrolled away, don't force scroll
                if (_userScrolledAway) return;

                // Only scroll if we have a viewer
                if (_responseScrollViewer == null)
                {
                    EnsureResponseScrollViewer();
                    if (_responseScrollViewer == null) return;
                }

                // Check distance from bottom in pixels; if already near bottom, perform autoscroll
                var extentHeight = _responseScrollViewer.ExtentHeight;
                var viewportHeight = _responseScrollViewer.ViewportHeight;
                var verticalOffset = _responseScrollViewer.VerticalOffset;
                var distanceFromBottom = extentHeight - (verticalOffset + viewportHeight);

                if (distanceFromBottom <= _stickyThresholdPx)
                {
                    // Post-layout scroll to end to avoid forcing layout during chunk update
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            ResponseJsonText.ScrollToEnd();
                            _lastAutoScrollTotalChars = totalChars;
                        }
                        catch { }
                    }), DispatcherPriority.Background);
                }
            }
            catch { }
        }

        // NUEVO METODO EnsureResponseScrollViewer - ID: 20260114_000020
        private void EnsureResponseScrollViewer()
        {
            try
            {
                if (_responseScrollViewer != null || ResponseJsonText == null) return;

                // Try to find internal ScrollViewer of the RichTextBox
                _responseScrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(ResponseJsonText);
                if (_responseScrollViewer != null && !_responseScrollViewerWired)
                {
                    _responseScrollViewerWired = true;
                    _responseScrollViewer.ScrollChanged += (s, e) =>
                    {
                        try
                        {
                            // If the user scrolls away from bottom by more than threshold, mark userScrolledAway
                            var extentHeight = _responseScrollViewer.ExtentHeight;
                            var viewportHeight = _responseScrollViewer.ViewportHeight;
                            var verticalOffset = _responseScrollViewer.VerticalOffset;
                            var distanceFromBottom = extentHeight - (verticalOffset + viewportHeight);
                            _userScrolledAway = distanceFromBottom > _stickyThresholdPx;
                        }
                        catch { }
                    };
                    ResponseJsonText.PreviewMouseWheel += (s, e) =>
                    {
                        try { EnsureResponseScrollViewer(); } catch { }
                    };
                }
            }
            catch { }
        }

        // Visual tree helper generic finder
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var res = FindVisualChild<T>(child);
                if (res != null) return res;
            }
            return null;
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

            // Wire up response scroll viewer detection after control is loaded
            try
            {
                this.Loaded += (s, e) =>
                {
                    try { EnsureResponseScrollViewer(); } catch { }
                };
            }
            catch { }

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
                Ui(() =>
                {
                    var green = ResolveBrushOrFallback("GreenBrush", Brushes.LimeGreen);
                    var red = ResolveBrushOrFallback("RedBrush", Brushes.IndianRed);
                    var blue = ResolveBrushOrFallback("BlueBrush", Brushes.DodgerBlue);

                    switch (state)
                    {
                        case ExecutionState.Idle:
                            StateIconKind = PackIconKind.Play;
                            StateColor = green;
                            StateLabel = "Idle";
                            RunButtonEnabled = true;
                            ClearButtonEnabled = true;
                            IsPromptReadOnly = false;
                            break;

                        case ExecutionState.Running:
                            StateIconKind = PackIconKind.Stop;
                            StateColor = red;
                            StateLabel = "Running";
                            RunButtonEnabled = true;
                            ClearButtonEnabled = false;
                            IsPromptReadOnly = true;
                            break;

                        case ExecutionState.Completed:
                            StateIconKind = PackIconKind.Check;
                            StateColor = blue;
                            StateLabel = "Completed";
                            RunButtonEnabled = true;
                            ClearButtonEnabled = true;
                            IsPromptReadOnly = false;
                            break;

                        case ExecutionState.Error:
                            StateIconKind = PackIconKind.Error;
                            StateColor = red;
                            StateLabel = "Error";
                            RunButtonEnabled = false;
                            ClearButtonEnabled = true;
                            IsPromptReadOnly = false;
                            break;
                    }

                    RaisePropertyChanged(nameof(StateIconKind));
                    RaisePropertyChanged(nameof(StateColor));
                    RaisePropertyChanged(nameof(StateLabel));
                });
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Error updating state properties: " + ex.Message, ex); } catch { }
            }
        }

        private readonly System.Collections.Generic.HashSet<string> _warnedMissingBrushKeys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        // NUEVO METODO ResolveBrushOrFallback - ID: 20260114_000010
        private Brush ResolveBrushOrFallback(string key, Brush fallback)
        {
            try
            {
                var resolved = TryFindResource(key) as Brush;
                if (resolved != null) return resolved;

                // Log a single warning per missing key to avoid spam
                try
                {
                    if (_warnedMissingBrushKeys.Add(key))
                    {
                        AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"Brush resource missing: {key}. Using fallback.");
                    }
                }
                catch { }

                return fallback;
            }
            catch
            {
                return fallback;
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

            if (server == null) return respObj;

            var baseUrl = (server.BaseUrl ?? string.Empty).TrimEnd('/');
            var url = baseUrl + "/v1/chat/completions";

            var payload = new JObject();
            if (!string.IsNullOrWhiteSpace(server.Model)) payload["model"] = server.Model;
            payload["stream"] = true;
            try { payload["stream_options"] = new JObject(new JProperty("include_usage", true)); } catch { }
            payload["messages"] = new JArray(new JObject { ["role"] = "user", ["content"] = BuildLmStudioPrompt(req) });

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    using (var httpReq = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        httpReq.Headers.Accept.Clear();
                        try { httpReq.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")); } catch { }
                        if (!string.IsNullOrWhiteSpace(server.ApiKey))
                        {
                            try { httpReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", server.ApiKey); } catch { }
                        }
                        httpReq.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                        HttpResponseMessage httpResp = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
                        if (!httpResp.IsSuccessStatusCode && httpResp.StatusCode == System.Net.HttpStatusCode.BadRequest && payload["stream_options"] != null)
                        {
                            try { httpResp.Dispose(); } catch { }
                            payload.Remove("stream_options");
                            using (var retryReq = new HttpRequestMessage(HttpMethod.Post, url))
                            {
                                retryReq.Headers.Accept.Clear();
                                try { retryReq.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")); } catch { }
                                if (!string.IsNullOrWhiteSpace(server.ApiKey))
                                {
                                    try { retryReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", server.ApiKey); } catch { }
                                }
                                retryReq.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                                try { httpResp = await client.SendAsync(retryReq, HttpCompletionOption.ResponseHeadersRead, ct); try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "LM Studio: stream_options.include_usage rejected by server; retrying without it."); } catch { } }
                                catch (Exception exRetry) { respObj.Error = "LM Studio retry failed: " + exRetry.Message; return respObj; }
                            }
                        }

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

                        long lastIncrementalTick = 0;
                        CancellationTokenRegistration cancelReg = default(CancellationTokenRegistration);
                        try
                        {
                            cancelReg = ct.Register(() => { try { httpResp.Dispose(); } catch { } });
                            using (var stream = await httpResp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var reader = new StreamReader(stream))
                            {
                                while (!ct.IsCancellationRequested)
                                {
                                    string line = null;
                                    try { line = await reader.ReadLineAsync().ConfigureAwait(false); } catch { break; }
                                    if (line == null) break;
                                    if (string.IsNullOrWhiteSpace(line)) continue;
                                    if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
                                    var data = line.Substring(5).Trim();
                                    if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase)) break;
                                    if (string.IsNullOrEmpty(data)) continue;

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

                                        var chunk = j.SelectToken("choices[0].delta.content")?.ToString() ?? j.SelectToken("choices[0].message.content")?.ToString() ?? j.SelectToken("choices[0].text")?.ToString();
                                        if (string.IsNullOrEmpty(chunk)) continue;
                                        sb.Append(chunk);
                                        try { if (aiBubble != null) aiBubble.Content = sb.ToString(); } catch { }

                                        var nowTick = Stopwatch.GetTimestamp();
                                        if (lastIncrementalTick == 0 || (nowTick - lastIncrementalTick) >= (Stopwatch.Frequency / 10))
                                        {
                                            lastIncrementalTick = nowTick;
                                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                                            try
                                            {
                                                if (ReferenceEquals(aiBubble, _streamingAiMessage) && _streamingAiRun != null)
                                                {
                                                    ApplyStreamingDeltaFrom(sb);
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    catch { /* ignore malformed chunks */ }
                                }
                            }
                        }
                        finally { try { cancelReg.Dispose(); } catch { } }

                        var finalText = sb.ToString();
                        try { if (aiBubble != null) aiBubble.Content = finalText; } catch { }
                        try { await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct); if (ReferenceEquals(aiBubble, _streamingAiMessage) && _streamingAiRun != null) _streamingAiRun.Text = finalText; if (ReferenceEquals(aiBubble, _streamingAiMessage) && _streamingAiViewer != null) { try { _streamingAiViewer.BringIntoView(); } catch { } } } catch { }

                        if (ct.IsCancellationRequested) { ClearStreamingPlaceholder(); respObj.Error = "Cancelled"; return respObj; }

                        try { int finalTokens = (completionTokens ?? totalTokens) ?? 0; try { TrySetProp(aiBubble, "Tokens", finalTokens); } catch { } try { TrySetProp(aiBubble, "TokenCount", finalTokens); } catch { } try { TrySetProp(aiBubble, "TotalTokens", finalTokens); } catch { } try { TrySetTokensForMessage(chat.Id, TryGetDateTimeProp(aiBubble, "Timestamp"), "IA", finalText, finalTokens); } catch { } } catch { }

                        TryPersistChat(chat);
                        RenderActiveChatToUi();
                        ClearStreamingPlaceholder();

                        respObj.Success = true; respObj.Output = finalText; respObj.PromptTokens = promptTokens; respObj.CompletionTokens = completionTokens; respObj.TotalTokens = totalTokens; return respObj;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                ClearStreamingPlaceholder(); respObj.Error = "Cancelled"; return respObj;
            }
            catch (Exception ex)
            {
                ClearStreamingPlaceholder(); respObj.Error = ex.Message; return respObj;
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