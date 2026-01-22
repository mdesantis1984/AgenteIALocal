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
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace AgenteIALocalVSIX.ToolWindows
{
    // NUEVO ENUM ExecutionState - ID: 20260116_103000
    public enum ExecutionState { Idle, Running, Completed, Error }

    public partial class AgenteIALocalControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
        // NUEVO CAMPO UiLogBuffer - ID: 20260118_190200
        private AgenteIALocal.Infrastructure.LoggingV2.UiLogBuffer _uiLogBuffer;
        // NUEVO CAMPO _isRefreshingUiProvider - ID: 20260115_123000
        private bool _isRefreshingUiProvider = false;
        // NUEVO CAMPO guard para evitar reentrancia en RefreshFromSettings via SettingsSaved - ID: 20260116_173500
        private bool _isRefreshingFromSettings = false;
        // NUEVO CAMPO para recordar el ultimo activeServerId aplicado en UI - ID: 20260116_181200
        private string _lastActiveServerIdUi = null;
        // NUEVO CAMPO last logged provider info to avoid repeated info logs - ID: 20260116_103000
        private string _lastLoggedProviderInfo = null;
        // Guard to avoid spamming error logs when opening settings fails
        private bool _settingsOpenErrorLogged = false; // NUEVO CAMPO - ID: 20260114_000051
        // NUEVO CAMPO ConfigStatusLabel - ID: 20260114_000061
        private string _configStatusLabel = "CONFIG UNKNOWN";
        // NUEVO CAMPO ConfigStatusBrush - ID: 20260114_000062
        private Brush _configStatusBrush = Brushes.Gray;
        // NUEVA PROPIEDAD ConfigStatusLabel - ID: 20260114_000070
        public string ConfigStatusLabel => _configStatusLabel;
        // NUEVA PROPIEDAD ConfigStatusBrush - ID: 20260114_000071
        public Brush ConfigStatusBrush => _configStatusBrush;

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

        // NUEVO METODO IsChatModelId - ID: 20260117_133300
        internal static bool IsChatModelId(string modelId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelId)) return false;
                var s = modelId.Trim().ToLowerInvariant();
                if (s.StartsWith("text-embedding-")) return false;
                if (s.Contains("embedding")) return false;
                if (s.Contains("nomic-embed")) return false;
                if (s.Contains("embed-text")) return false;
                if (s.Contains("-embed-")) return false;
                if (s.EndsWith("-embed")) return false;
                return true;
            }
            catch { return false; }
        }

        // NUEVO METODO FilterChatModelsInPlace - ID: 20260117_133310
        internal static void FilterChatModelsInPlace(System.Collections.Generic.List<string> models)
        {
            try
            {
                if (models == null || models.Count == 0) return;
                for (int i = models.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var id = models[i];
                        if (!IsChatModelId(id)) models.RemoveAt(i);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // MODIFICADO METODO TypeActivitie_SelectionChanged - ID: 20260116_173000
        private void TypeActivitie_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                if (_isRefreshingFromSettings) return; // avoid persisting during programmatic refresh

                var cb = sender as ComboBox;
                var selected = cb?.SelectedItem as ComboBoxItem;
                string text = null;
                try { text = selected?.Content?.ToString(); } catch { text = null; }
                if (string.IsNullOrWhiteSpace(text))
                {
                    try { text = cb?.SelectedItem?.ToString(); } catch { text = null; }
                }

                if (string.IsNullOrWhiteSpace(text)) return;

                string normalized = null;
                if (string.Equals(text, "Agente", StringComparison.OrdinalIgnoreCase)) normalized = "agente";
                else normalized = "preguntar";

                try
                {
                    var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                    var current = settings.GlobalSettings != null ? settings.GlobalSettings.Value<string>("runMode") : null;
                    if (!string.Equals(current ?? string.Empty, normalized ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        // Persist new runMode value (this will raise SettingsSaved event)
                        try
                        {
                            if (settings.GlobalSettings == null) settings.GlobalSettings = new Newtonsoft.Json.Linq.JObject();
                            settings.GlobalSettings["runMode"] = normalized;
                            AgentSettingsStore.Save(settings);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            catch { }
        }

        // MODIFICADO METODO GetRunModeNormalized - ID: 20260116_173000
        public string GetRunModeNormalized()
        {
            try
            {
                // 1) Prefer explicit UI selection if available
                try
                {
                    if (TypeActivitie != null)
                    {
                        string txt = null;
                        try
                        {
                            var cbi = TypeActivitie.SelectedItem as ComboBoxItem;
                            if (cbi != null) txt = cbi.Content?.ToString();
                            if (string.IsNullOrWhiteSpace(txt)) txt = TypeActivitie.SelectedItem?.ToString();
                        }
                        catch { txt = null; }

                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            if (string.Equals(txt, "Agente", StringComparison.OrdinalIgnoreCase)) return "agente";
                            return "preguntar";
                        }
                    }
                }
                catch { }

                // 2) Fallback to persisted settings
                try
                {
                    var settings = AgentSettingsStore.Load();
                    var runMode = settings != null && settings.GlobalSettings != null ? settings.GlobalSettings.Value<string>("runMode") : null;
                    if (string.IsNullOrWhiteSpace(runMode)) return "preguntar";
                    return runMode;
                }
                catch { return "preguntar"; }
            }
            catch { return "preguntar"; }
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

        // Unsubscribe from settings notifications when control is unloaded to avoid leaks
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            try
            {
                if (this.VisualParent == null)
                {
                    try { AgentSettingsStore.SettingsSaved -= OnSettingsSaved; } catch { }
                }
            }
            catch { }
        }

        // Handler for settings saved notifications; refresh UI from canonical settings
        private void OnSettingsSaved(string reason)
        {
            try
            {
                if (!IsLoaded) return;

                // Prevent reentrancy
                if (_isRefreshingFromSettings) return;
                _isRefreshingFromSettings = true;

                try
                {
                    // Use JoinableTaskFactory to safely switch to UI thread without Dispatcher usage
                    var jt = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        try
                        {
                            // Load settings and decide whether activeServerId changed
                            var settings = AgentSettingsStore.Load();
                            var activeId = settings != null ? settings.ActiveServerId : null;
                            if (string.Equals(_lastActiveServerIdUi ?? string.Empty, activeId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                            {
                                // No change in active server -> refresh UI but skip models fetch
                                RefreshFromSettings(refreshModels: false);
                            }
                            else
                            {
                                // Active server changed -> remember and allow one models refresh
                                _lastActiveServerIdUi = activeId;
                                RefreshFromSettings(refreshModels: true);
                            }
                            try { AgentComposition.Verbose("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "SettingsSaved handler executed: " + reason); } catch { }
                        }
                        catch { }
                    });

                    // Observe faults to avoid unobserved task exceptions (VSTHRD110)
                    _ = jt.Task.ContinueWith(t => { var _e = t.Exception; }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, System.Threading.Tasks.TaskScheduler.Default);
                }
                catch { }
                finally
                {
                    _isRefreshingFromSettings = false;
                }
            }
            catch { }
        }

        // NUEVO METODO NormalizeBaseUri - ID: 20260116_103000
        private static Uri NormalizeBaseUri(string baseUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return null;
                Uri uri;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri))
                {
                    // try with http scheme
                    if (Uri.TryCreate("http://" + baseUrl, UriKind.Absolute, out uri)) return uri;
                    return null;
                }
                return uri;
            }
            catch { return null; }
        }

        // NUEVO METODO BuildModelsUri - ID: 20260116_103000
        // MODIFICADO BuildModelsUri - ID: 20260116_103000
        private static string BuildModelsUri(Uri baseUri)
        {
            try
            {
                if (baseUri == null) return null;

                // Extra validation to be defensive
                var normalized = NormalizeBaseUri(baseUri.ToString());
                if (normalized == null) return null;

                var s = baseUri.ToString().TrimEnd('/');
                var path = baseUri.AbsolutePath ?? string.Empty;
                // If already contains /v1/models -> return as-is
                if (path.IndexOf("/v1/models", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return s;
                }
                // If ends with /v1 -> append /models
                if (path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                {
                    return s + "/models";
                }
                // default -> append /v1/models
                return s + "/v1/models";
            }
            catch { return null; }
        }

        // NUEVO METODO TryGetFallbackHost - ID: 20260116_103000
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

        // NUEVO METODO IsConnectionRefused - ID: 20260116_103000
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

                // look for SocketException with connection refused (10061) or ECONNREFUSED
                var socketEx = ex as System.Net.Sockets.SocketException;
                if (socketEx != null)
                {
                    // 10061 Windows WSAECONNREFUSED
                    if (socketEx.ErrorCode == 10061) return true;
                }

                // check inner exceptions
                if (ex.InnerException != null) return IsConnectionRefused(ex.InnerException);

                var msg = ex.Message ?? string.Empty;
                if (msg.IndexOf("Connection refused", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (msg.IndexOf("ECONNREFUSED", StringComparison.OrdinalIgnoreCase) >= 0) return true;

                return false;
            }
            catch { return false; }


        } // <-- Cierre de la propiedad CurrentExecutionState faltante

        // MODIFICADO ServerLLM_SelectionChanged - ID: 20260116_103000
        private void ServerLLM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                if (_isRefreshingUiProvider) return; // avoid loop when UI is being hydrated
                if (_isRefreshingFromSettings) return; // avoid persisting during programmatic refresh

                var cb = sender as ComboBox;
                var selected = cb?.SelectedItem as string ?? cb?.SelectedItem?.ToString() ?? string.Empty;

                // Preserve verbose log previously implemented in Helpers
                try { AppendLog($"[VERBOSE] ServerLLM selection changed -> {selected}"); } catch { }

                if (string.IsNullOrWhiteSpace(selected)) return;

                // Map UI selection to activeServerId
                string newActiveId = null;
                if (string.Equals(selected, "LM Studio", StringComparison.OrdinalIgnoreCase)) newActiveId = "lmstudio-local";
                else if (string.Equals(selected, "JAN", StringComparison.OrdinalIgnoreCase)) newActiveId = "jan-local";
                if (string.IsNullOrEmpty(newActiveId)) return;

                try
                {
                    var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                    var prev = settings.ActiveServerId ?? string.Empty;
                    if (string.Equals(prev, newActiveId, StringComparison.OrdinalIgnoreCase))
                    {
                        // no change
                        return;
                    }

                    settings.ActiveServerId = newActiveId;
                    AgentSettingsStore.Save(settings);

                    // Refresh UI and trigger recompose (explicit tag for toolwindow)
                    try { RefreshFromSettings(refreshModels: true); } catch { }
                    try { AgentComposition.RecomposeFromSettings("ui:provider-changed-toolwindow"); } catch { }

                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"Server provider changed: {prev} -> {newActiveId}"); } catch { }
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ServerLLM selection handler failed: " + ex.Message, ex); } catch { }
                }
            }
            catch { }
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
                    FireAndForget(UiAsync(() =>
                    {
                        try
                        {
                            ResponseJsonText.ScrollToEnd();
                            _lastAutoScrollTotalChars = totalChars;
                        }
                        catch { }
                    }), "AutoScroll");
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

            // Wire up footer RunMode selector persistence
            try
            {
                if (TypeActivitie != null)
                {
                    TypeActivitie.SelectionChanged -= TypeActivitie_SelectionChanged;
                    TypeActivitie.SelectionChanged += TypeActivitie_SelectionChanged;
                }
            }
            catch { }

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
                // Subscribe to settings saved notifications to refresh UI across windows
                try { AgentSettingsStore.SettingsSaved += OnSettingsSaved; } catch { }
                // MODIFICADO METODO PopulateSettingsPanel call to ensure ServerLLM refleja activeServerId sin activar el controlador de seleccion - ID: 20260115_123000
                PopulateSettingsPanel(settings);
                try
                {
                    _isRefreshingUiProvider = true;
                    // Hydrate ServerLLM selection from settings.ActiveServerId
                    try
                    {
                        var activeId = settings != null ? settings.ActiveServerId : null;
                        if (!string.IsNullOrEmpty(activeId) && ServerLLM != null)
                        {
                            // Map stored activeServerId to UI item text
                            string uiText = null;
                            if (string.Equals(activeId, "lmstudio-local", StringComparison.OrdinalIgnoreCase)) uiText = "LM Studio";
                            else if (string.Equals(activeId, "jan-local", StringComparison.OrdinalIgnoreCase)) uiText = "JAN";

                            if (!string.IsNullOrEmpty(uiText))
                            {
                                if (ServerLLM.Items.Contains(uiText))
                                {
                                    ServerLLM.SelectedItem = uiText;
                                }
                            }
                        }
                    }
                    catch { }
                }
                finally
                {
                    _isRefreshingUiProvider = false;
                }

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

            FireAndForget(RunLogRefreshLoopAsync(ct), "AgenteIALocalControl.LogRefreshLoop");
        }

        // NUEVO METODO RunLogRefreshLoopAsync - ID: 20250310_000005
        private async Task RunLogRefreshLoopAsync(CancellationToken ct)
        {
            // Ensure UI buffer and subscription to hub
            try { EnsureUiLogBuffer(); } catch { }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

                    var bytes = TryGetLogFileSizeBytes();
                    UpdateLogFileSizeLabelFromBytes(bytes);

                    // LogText is updated via collection changed handler from the UiLogBuffer
                    try
                    {
                        if (_uiLogBuffer == null || _uiLogBuffer.Items.Count == 0)
                        {
                            LogText.Text = "(no logs)";
                        }
                    }
                    catch { }

                    ScrollLogToEnd(force: false);
                }
                catch
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                    UpdateLogFileSizeLabelFromBytes(0);
                    try { LogText.Text = "(unable to read logs)"; } catch { }
                    ScrollLogToEnd(force: false);
                }

                try { await Task.Delay(2000, ct); } catch { }
            }
        }

        // NUEVO METODO EnsureUiLogBuffer - ID: 20260118_190200
        private void EnsureUiLogBuffer()
        {
            try
            {
                if (_uiLogBuffer != null) return;
                _uiLogBuffer = new AgenteIALocal.Infrastructure.LoggingV2.UiLogBuffer((a) => { try { this.Dispatcher.BeginInvoke(a); } catch { try { this.Dispatcher.Invoke(a); } catch { } } }, 250);

                // Bind Items to LogText by listening changes and re-joining lines
                _uiLogBuffer.Items.CollectionChanged += (s, e) =>
                {
                    try
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                // join lines into the single LogText.Text for backward compatibility
                                LogText.Text = string.Join(Environment.NewLine, _uiLogBuffer.Items);
                                ScrollLogToEnd(force: false);
                            }
                            catch { }
                        }));
                    }
                    catch { }
                };

                // subscribe hub
                AgenteIALocal.Infrastructure.LoggingV2.LogEventHub.OnLog += (line, entry) =>
                {
                    try { _uiLogBuffer.Publish(line, entry); } catch { }
                };
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
        public void RefreshFromSettings(bool refreshModels = true)
        {
            try
            {
                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] RefreshFromSettings invoked.");
                var settings = AgentSettingsStore.Load();
                PopulateSettingsPanel(settings);
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);
                try { AgentComposition.Verbose("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigStatus: RefreshFromSettings completed label={ConfigLabel} isConfigured={IsLlmConfigured}"); } catch { }
                // Update footer UI controls: Provider (ServerLLM), Model (ModelOfLLM), RunMode (TypeActivitie)
                try
                {
                    // Provider -> map activeServerId to UI text
                    var activeId = settings != null ? settings.ActiveServerId : null;
                    string providerText = null;
                    if (!string.IsNullOrEmpty(activeId))
                    {
                        if (string.Equals(activeId, "lmstudio-local", StringComparison.OrdinalIgnoreCase)) providerText = "LM Studio";
                        else if (string.Equals(activeId, "jan-local", StringComparison.OrdinalIgnoreCase)) providerText = "JAN";
                    }

                    if (!string.Equals(_lastLoggedProviderInfo ?? string.Empty, providerText ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastLoggedProviderInfo = providerText;
                    }

                    if (ServerLLM != null)
                    {
                        try
                        {
                            _isRefreshingUiProvider = true;
                            if (!string.IsNullOrEmpty(providerText))
                            {
                                // select existing item by display text
                                TrySelectComboByText(ServerLLM, providerText);
                            }
                        }
                        finally { _isRefreshingUiProvider = false; }
                    }

                    // RunMode -> map globalSettings.runMode to TypeActivitie options
                    try
                    {
                        var runMode = settings != null && settings.GlobalSettings != null ? settings.GlobalSettings.Value<string>("runMode") : null;
                        if (string.IsNullOrEmpty(runMode)) runMode = "preguntar";
                        string runModeUi = string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase) ? "Agente" : "Preguntar";
                        if (TypeActivitie != null)
                        {
                            // avoid triggering persistence handlers
                            var prev = _isRefreshingFromSettings;
                            _isRefreshingFromSettings = true;
                            try { TrySelectComboByText(TypeActivitie, runModeUi); } catch { }
                            _isRefreshingFromSettings = prev;
                        }
                    }
                    catch { }

                    // Model -> try select saved model if present in list
                    try
                    {
                        var srv = (settings != null && settings.Servers != null && !string.IsNullOrEmpty(settings.ActiveServerId)) ? settings.Servers.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase)) : null;
                        var savedModel = srv != null ? srv.Model : null;
                        if (ModelOfLLM != null)
                        {
                            if (!string.IsNullOrEmpty(savedModel))
                            {
                                // try select by text if present, otherwise clear selection to avoid inconsistent model shown
                                var selOk = TrySelectComboByText(ModelOfLLM, savedModel);
                                if (!selOk)
                                {
                                    try { ModelOfLLM.SelectedItem = null; } catch { }
                                }
                            }
                            else
                            {
                                try { ModelOfLLM.SelectedItem = null; } catch { }
                            }
                        }
                    }
                    catch { }
                }
                catch { }

                // Refresh models for active server asynchronously (fire-and-forget) only when requested
                if (refreshModels)
                {
                    try { FireAndForget(RefreshModelsForActiveServerAsync("RefreshFromSettings"), "RefreshFromSettings.RefreshModels"); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"[AgenteIALocalControl] RefreshFromSettings error: {ex.Message}", ex); } catch { }
            }
        }

        // Fetch models from baseUrl (same parsing logic as modal) and return list of ids
        // Fetch models from baseUrl (same parsing logic as modal) and return list of ids
        private async Task<List<string>> FetchModelsFromBaseUrlAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;

                var rawBaseUrl = baseUrl;
                var baseUri = NormalizeBaseUri(baseUrl);
                if (baseUri == null)
                {
                    try { AgentComposition.Warning("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: invalid baseUrl. raw='{rawBaseUrl}'"); } catch { }
                    return result;
                }

                var primary = BuildModelsUri(baseUri);
                if (string.IsNullOrWhiteSpace(primary))
                {
                    try { AgentComposition.Warning("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: cannot build models uri. raw='{rawBaseUrl}' normalized='{baseUri}'"); } catch { }
                    return result;
                }

                var fallbackHost = TryGetFallbackHost(baseUri);

                // Resolve API key for active server (optional)
                string apiKey = null;
                try
                {
                    var settings = AgentSettingsStore.Load();
                    if (settings != null && !string.IsNullOrWhiteSpace(settings.ActiveServerId) && settings.Servers != null)
                    {
                        var srv = settings.Servers.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase));
                        if (srv != null) apiKey = srv.ApiKey;
                    }
                }
                catch { }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(8);

                    HttpResponseMessage resp = null;
                    Exception firstEx = null;

                    try
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: GET {primary} (raw='{rawBaseUrl}' normalized='{baseUri}')"); } catch { }
                        using (var req = new HttpRequestMessage(HttpMethod.Get, primary))
                        {
                            req.Headers.Accept.Clear();
                            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            if (!string.IsNullOrWhiteSpace(apiKey))
                            {
                                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                            }
                            resp = await client.SendAsync(req).ConfigureAwait(false);
                        }
                    }
                    catch (Exception exPrimary)
                    {
                        firstEx = exPrimary;
                    }

                    if (firstEx != null && IsConnectionRefused(firstEx) && !string.IsNullOrEmpty(fallbackHost))
                    {
                        var altBase = new UriBuilder(baseUri) { Host = fallbackHost }.Uri;
                        var alt = BuildModelsUri(altBase);
                        try
                        {
                            try { AgentComposition.Warning("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: primary failed (connection refused). Retrying alt='{alt}'"); } catch { }
                            using (var req = new HttpRequestMessage(HttpMethod.Get, alt))
                            {
                                req.Headers.Accept.Clear();
                                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                if (!string.IsNullOrWhiteSpace(apiKey))
                                {
                                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                }
                                resp = await client.SendAsync(req).ConfigureAwait(false);
                            }
                            firstEx = null;
                        }
                        catch (Exception exAlt)
                        {
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: primary and fallback failed: {exAlt.Message}", exAlt); } catch { }
                            return result;
                        }
                    }

                    if (firstEx != null)
                    {
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: error: {firstEx.Message}", firstEx); } catch { }
                        return result;
                    }

                    if (resp == null) return result;

                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgentComposition.Warning("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: non-success status {(int)resp.StatusCode} {resp.ReasonPhrase}"); } catch { }
                        return result;
                    }

                    var txt = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
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
                                    if (!string.IsNullOrWhiteSpace(id)) result.Add(id);
                                }
                                catch { }
                            }
                            try { FilterChatModelsInPlace(result); } catch { }
                            return result;
                        }

                        var models = root["models"] as Newtonsoft.Json.Linq.JArray;
                        if (models != null)
                        {
                            foreach (var item in models)
                            {
                                try
                                {
                                    var id = item.Type == Newtonsoft.Json.Linq.JTokenType.String ? item.ToString() : item.Value<string>("id");
                                    if (!string.IsNullOrWhiteSpace(id)) result.Add(id);
                                }
                                catch { }
                            }
                            try { FilterChatModelsInPlace(result); } catch { }
                            return result;
                        }

                        var arr = root as Newtonsoft.Json.Linq.JArray;
                        if (arr != null)
                        {
                            foreach (var item in arr)
                            {
                                try
                                {
                                    var id = item.ToString();
                                    if (!string.IsNullOrWhiteSpace(id)) result.Add(id);
                                }
                                catch { }
                            }
                            try { FilterChatModelsInPlace(result); } catch { }
                            return result;
                        }
                    }
                    catch (Exception exParse)
                    {
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: parse error: {exParse.Message}", exParse); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ModelsFetch: fatal: {ex.Message}", ex); } catch { }
            }

            try { FilterChatModelsInPlace(result); } catch { }
            return result;
        }

        // NUEVO METODO PromptTextBox_KeyDown - ID: 20260121_230600
        private void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                {
                    e.Handled = true;
                    FireAndForget(_runExecutor.RunAsync(sender, null), "PromptTextBox_KeyDown->RunAsync");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9100, "PromptKeyDown"), "PromptTextBox_KeyDown failed: " + ex.Message, ex);
                }
                catch { }
            }
        }

        // NUEVO METODO ModelOfLLM_SelectionChanged - ID: 20260121_230601
        private void ModelOfLLM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                if (_isRefreshingUiProvider) return; // avoid persisting during programmatic refresh

                var cb = sender as ComboBox;
                var selected = cb?.SelectedItem;
                if (selected == null) return;

                string modelId = null;
                try { modelId = selected.ToString(); } catch { modelId = null; }
                if (string.IsNullOrWhiteSpace(modelId)) return;

                // Persist selected model to settings
                try
                {
                    var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                    if (settings.GlobalSettings == null) settings.GlobalSettings = new Newtonsoft.Json.Linq.JObject();
                    settings.GlobalSettings["selectedModel"] = modelId;
                    AgentSettingsStore.Save(settings);
                    AgentComposition.LoggerV2.Info(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9101, "ModelChanged"), "Model selection changed to: " + modelId);
                }
                catch (Exception ex)
                {
                    AgentComposition.LoggerV2.Warning(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9102, "ModelChangeFailed"), "Failed to persist model selection: " + ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9103, "ModelSelectionError"), "ModelOfLLM_SelectionChanged failed: " + ex.Message, ex);
                }
                catch { }
            }
        }

        // NUEVO METODO RunButton_Click - ID: 20260121_230602
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_runExecutor == null)
                {
                    AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9104, "RunExecutorMissing"), "RunButton_Click: _runExecutor is null");
                    return;
                }
                FireAndForget(_runExecutor.RunAsync(sender, e), "RunButton_Click->RunAsync");
            }
            catch (Exception ex)
            {
                try
                {
                    AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9105, "RunButtonError"), "RunButton_Click failed: " + ex.Message, ex);
                }
                catch { }
            }
        }

        // NUEVO METODO UpdateStateProperties - ID: 20260121_233100
        private void UpdateStateProperties(ExecutionState state)
        {
            try
            {
                switch (state)
                {
                    case ExecutionState.Running:
                        StateIconKind = PackIconKind.PlayCircleOutline;
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
                    case ExecutionState.Idle:
                    default:
                        StateIconKind = PackIconKind.CircleOutline;
                        StateColor = Brushes.Gray;
                        StateLabel = "Idle";
                        break;
                }
                RaisePropertyChanged(nameof(StateIconKind));
                RaisePropertyChanged(nameof(StateColor));
                RaisePropertyChanged(nameof(StateLabel));
            }
            catch { }
        }

        // NUEVO METODO RefreshModelsForActiveServerAsync - ID: 20260121_233200
        private async Task RefreshModelsForActiveServerAsync(string source)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var settings = AgentSettingsStore.Load();
                if (settings == null || string.IsNullOrEmpty(settings.ActiveServerId) || settings.Servers == null)
                {
                    return;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase));
                if (srv == null || string.IsNullOrWhiteSpace(srv.BaseUrl))
                {
                    return;
                }

                var models = await FetchModelsFromBaseUrlAsync(srv.BaseUrl).ConfigureAwait(false);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (ModelOfLLM != null)
                {
                    try
                    {
                        _isRefreshingUiProvider = true;
                        ModelOfLLM.Items.Clear();
                        foreach (var m in models)
                        {
                            try { ModelOfLLM.Items.Add(m); } catch { }
                        }

                        // Try to select saved model if present
                        var savedModel = srv.Model;
                        if (!string.IsNullOrEmpty(savedModel))
                        {
                            TrySelectComboByText(ModelOfLLM, savedModel);
                        }
                    }
                    finally
                    {
                        _isRefreshingUiProvider = false;
                    }
                }

                AgentComposition.LoggerV2.Info(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9110, "ModelsRefresh"), $"RefreshModelsForActiveServerAsync: {models.Count} models fetched from {srv.BaseUrl} (source: {source})");
            }
            catch (Exception ex)
            {
                AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9111, "ModelsRefreshError"), $"RefreshModelsForActiveServerAsync failed: {ex.Message}", ex);
            }
        }

        // NUEVO METODO TryGetActiveOpenAiCompatibleServer - ID: 20260121_233300
        internal bool TryGetActiveOpenAiCompatibleServer(out ServerConfig server)
        {
            server = null;
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null || string.IsNullOrEmpty(settings.ActiveServerId) || settings.Servers == null)
                {
                    return false;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    return false;
                }

                // Check if provider is OpenAI-compatible (lmstudio or jan)
                var provider = (srv.Provider ?? string.Empty).ToLowerInvariant();
                if (!string.Equals(provider, "lmstudio", StringComparison.OrdinalIgnoreCase) && !string.Equals(provider, "jan", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Must have baseUrl and model
                if (string.IsNullOrWhiteSpace(srv.BaseUrl) || string.IsNullOrWhiteSpace(srv.Model))
                {
                    return false;
                }

                server = srv;
                return true;
            }
            catch (Exception ex)
            {
                AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9112, "GetServerError"), $"TryGetActiveOpenAiCompatibleServer failed: {ex.Message}", ex);
                return false;
            }
        }

        // NUEVO METODO ExecuteLmStudioStreamingAsync - ID: 20260121_233400
        internal async Task<AgentHostResponse> ExecuteLmStudioStreamingAsync(AgentHostRequest req, ServerConfig server, ChatSession chat, ChatMessage aiBubble, CancellationToken ct)
        {
            // TODO: Implement actual streaming logic
            // For now, return a mock response to avoid compilation errors
            try
            {
                AgentComposition.LoggerV2.Info(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9113, "StreamingExec"), $"ExecuteLmStudioStreamingAsync called (not yet implemented): provider={server?.Provider} model={server?.Model}");

                await Task.Delay(100, ct).ConfigureAwait(false);

                return new AgentHostResponse
                {
                    Success = true,
                    Output = "ExecuteLmStudioStreamingAsync: Not yet implemented. This is a placeholder to satisfy compilation.",
                    Error = null,
                    RequestId = Guid.NewGuid().ToString("N"),
                    Timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception ex)
            {
                AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9114, "StreamingExecError"), $"ExecuteLmStudioStreamingAsync failed: {ex.Message}", ex);
                throw;
            }
        }

        // NUEVO METODO TryRemoveEmptyAiBubble - ID: 20260121_233500
        internal void TryRemoveEmptyAiBubble(ChatSession chat, ChatMessage aiBubble)
        {
            try
            {
                if (chat == null || aiBubble == null || chat.Messages == null)
                {
                    return;
                }

                var list = chat.Messages as System.Collections.IList;
                if (list == null)
                {
                    return;
                }

                // Check if bubble is empty
                var content = TryGetStringProp(aiBubble, "Content");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return; // Not empty, don't remove
                }

                // Try to remove
                try
                {
                    list.Remove(aiBubble);
                    TryPersistChat(chat);
                    AgentComposition.LoggerV2.Info(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9115, "RemoveEmptyBubble"), "Removed empty AI bubble from chat");
                }
                catch (Exception ex)
                {
                    AgentComposition.LoggerV2.Warning(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9116, "RemoveEmptyBubbleError"), $"TryRemoveEmptyAiBubble: failed to remove: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                AgentComposition.LoggerV2.Error(activeCorrelationId ?? "-", new AgenteIALocal.Core.Logging.LogEventId(9117, "RemoveEmptyBubbleError"), $"TryRemoveEmptyAiBubble failed: {ex.Message}", ex);
            }
        }
    }
}

