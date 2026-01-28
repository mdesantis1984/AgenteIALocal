using AgenteIALocal.Core.Models.Agent;
using AgenteIALocalVSIX.Chats;
using AgenteIALocalVSIX.Execution;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualStudio.Shell;
// MODIFICADO - ID: 20260123_215600 - ROLLBACK System.Text.Json → Newtonsoft.Json + agregar Core.Configuration
using AgenteIALocal.Core.Configuration;
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

        // NUEVO - ID: 20260123_230700 - Parser streaming SSE (elimina lógica JSON de UI)
        private AgenteIALocal.Core.Streaming.IStreamingResponseParser _streamingParser;

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
        // ELIMINADO CAMPO _uiLogBuffer - ID: 20260122_223000 - Ya no se usa, RefreshLogPanel lee directo desde Serilog
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
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.IsChatModel", "IsChatModelId failed: " + ex.Message, ex); return false; }
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
                    catch (Exception exItem) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.FilterChat", "Filter item failed: " + exItem.Message, exItem); }
                }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.FilterChat", "FilterChatModelsInPlace failed: " + ex.Message, ex); }
        }

        // MODIFICADO METODO TypeActivitie_SelectionChanged - ID: 20260126_031800
        // FIX: Leer Tag invariante de ComboBoxItem (NO texto traducido)
        // MEJORADO - ID: 20260128_000400 - Logging diagnóstico mejorado
        private void TypeActivitie_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded)
                {
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TypeActivitie", "SKIP - UserControl not loaded yet", null);
                    return;
                }
                
                if (_isRefreshingFromSettings)
                {
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TypeActivitie", "SKIP - Refreshing from settings (avoid loop)", null);
                    return; // avoid persisting during programmatic refresh
                }

                var cb = sender as ComboBox;
                var cbi = cb?.SelectedItem as ComboBoxItem;
                
                if (cbi == null)
                {
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TypeActivitie", "SelectedItem is null - skipping", null);
                    return;
                }
                
                // NUEVO - ID: 20260126_031800 - Leer Tag invariante ("agente"/"preguntar")
                var tag = cbi.Tag as string;
                if (string.IsNullOrWhiteSpace(tag))
                {
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TypeActivitie", "Tag is null - skipping", null);
                    return;
                }

                string normalized = tag.ToLowerInvariant();
                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.TypeActivitie", 
                    $"USER changed RunMode in toolbox: Tag={tag}, Normalized={normalized}, SelectedIndex={cb.SelectedIndex}", null);

                try
                {
                    var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                    var current = settings.GlobalSettings?.RunMode ?? string.Empty;
                    
                    if (!string.Equals(current, normalized ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "Control.TypeActivitie", 
                            $"PERSISTING RunMode change: '{current}' → '{normalized}'", null);
                        
                        // Persist new runMode value (this will raise SettingsSaved event) - MODIFICADO - ID: 20260123_225700
                        try
                        {
                            if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();
                            settings.GlobalSettings.RunMode = normalized;
                            AgentSettingsStore.Save(settings);
                            
                            AgenteIALocal.Logging.Log.Information("-", 9100, "Control.TypeActivitie", 
                                $"✓ RunMode saved to settings: {normalized}", null);
                        }
                        catch (Exception exSave) 
                        { 
                            AgenteIALocal.Logging.Log.Error("-", 9100, "Control.TypeActivitie", 
                                "Save runMode FAILED: " + exSave.Message, exSave); 
                        }
                    }
                    else
                    {
                        AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TypeActivitie", 
                            $"SKIP save - RunMode unchanged: {normalized}", null);
                    }
                }
                catch (Exception exOuter) 
                { 
                    AgenteIALocal.Logging.Log.Error("-", 9100, "Control.TypeActivitie", 
                        "Load settings FAILED: " + exOuter.Message, exOuter); 
                }
            }
            catch (Exception ex) 
            { 
                AgenteIALocal.Logging.Log.Error("-", 9100, "Control.TypeActivitie", 
                    "TypeActivitie_SelectionChanged FAILED: " + ex.Message, ex); 
            }
        }

        // MODIFICADO METODO GetRunModeNormalized - ID: 20260126_031700
        // FIX: Leer Tag invariante de ComboBoxItem (NO texto traducido)
        public string GetRunModeNormalized()
        {
            try
            {
                // 1) Prefer explicit UI selection if available
                try
                {
                    if (TypeActivitie != null)
                    {
                        var cbi = TypeActivitie.SelectedItem as ComboBoxItem;
                        if (cbi != null)
                        {
                            // NUEVO - ID: 20260126_031700 - Leer Tag invariante ("agente"/"preguntar")
                            var tag = cbi.Tag as string;
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.GetRunMode", $"RunMode from UI Tag: {tag}", null);
                                return tag.ToLowerInvariant();
                            }
                        }
                    }
                }
                catch (Exception exUi) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.GetRunMode", "UI access failed: " + exUi.Message, exUi); }

                // 2) Fallback to persisted settings - MODIFICADO - ID: 20260123_225701
                try
                {
                    var settings = AgentSettingsStore.Load();
                    var runMode = settings?.GlobalSettings?.RunMode ?? "preguntar";
                    if (string.IsNullOrWhiteSpace(runMode)) return "preguntar";
                    return runMode;
                }
                catch (Exception exSettings) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.GetRunMode", "Settings load failed: " + exSettings.Message, exSettings); return "preguntar"; }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.GetRunMode", "GetRunModeNormalized failed: " + ex.Message, ex); return "preguntar"; }
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
                            try { s = cbi.Content?.ToString(); } catch (Exception exContent) { s = null; AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TrySelect", "Content failed: " + exContent.Message, exContent); }
                        }
                        if (string.IsNullOrEmpty(s))
                        {
                            try { s = item?.ToString(); } catch (Exception exToString) { s = null; AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.TrySelect", "ToString failed: " + exToString.Message, exToString); }
                        }
                        if (string.IsNullOrEmpty(s)) continue;
                        if (string.Equals(s, text, StringComparison.OrdinalIgnoreCase))
                        {
                            cb.SelectedItem = item;
                            return true;
                        }
                    }
                    catch (Exception exItem) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.TrySelect", "Item processing failed: " + exItem.Message, exItem); }
                }
            }
            catch (Exception exLoop) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.TrySelect", "TrySelectComboByText failed: " + exLoop.Message, exLoop); }
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
                    try { AgentSettingsStore.SettingsSaved -= OnSettingsSaved; } catch (Exception exUnsub) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.OnVisualParent", "Unsubscribe failed: " + exUnsub.Message, exUnsub); }
                }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.OnVisualParent", "OnVisualParentChanged failed: " + ex.Message, ex); }
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
                            
                            // NUEVO - ID: 20260127_234500 - Sincronizar RunMode entre Config y Toolbox
                            // CORREGIDO - ID: 20260128_000100 - Mapping índices: agente=0, preguntar=1
                            // MEJORADO - ID: 20260128_000300 - Logging diagnóstico detallado
                            try
                            {
                                var runMode = settings?.GlobalSettings?.RunMode ?? "preguntar";
                                // FIX: agente=index 0, preguntar=index 1 (orden en XAML)
                                int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                                
                                AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                    $"DIAGNOSTICO: runMode={runMode}, targetIndex={targetIndex}, TypeActivitie.SelectedIndex={TypeActivitie?.SelectedIndex ?? -999}, IsLoaded={IsLoaded}", null);
                                
                                if (TypeActivitie == null)
                                {
                                    AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                        "TypeActivitie combo es NULL - no se puede sincronizar", null);
                                    return;
                                }
                                
                                if (TypeActivitie.SelectedIndex != targetIndex)
                                {
                                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                        $"SINCRONIZANDO: {TypeActivitie.SelectedIndex} → {targetIndex} (runMode={runMode})", null);
                                    
                                    TypeActivitie.SelectedIndex = targetIndex;
                                    
                                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                        $"✓ RunMode sincronizado exitosamente: {runMode} → index {targetIndex}", null);
                                }
                                else
                                {
                                    AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                        $"SKIP sincronización - índice ya correcto: {targetIndex} (runMode={runMode})", null);
                                }
                            }
                            catch (Exception exRunMode)
                            {
                                AgenteIALocal.Logging.Log.Error("-", 9100, "Control.OnSettingsSaved.RunModeSync", 
                                    "RunMode sync FAILED: " + exRunMode.Message, exRunMode);
                            }
                            
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
                            AgenteIALocal.Logging.Log.Verbose("-", 9100, "Control.SettingsSaved", "SettingsSaved handler executed: " + reason, null);
                        }
                        catch (Exception exRefresh) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.SettingsSaved", "RefreshFromSettings failed: " + exRefresh.Message, exRefresh); }
                    });

                    // Observe faults to avoid unobserved task exceptions (VSTHRD110)
                    _ = jt.Task.ContinueWith(t => { var _e = t.Exception; }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, System.Threading.Tasks.TaskScheduler.Default);
                }
                catch (Exception exJt) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.SettingsSaved", "JoinableTask failed: " + exJt.Message, exJt); }
                finally
                {
                    _isRefreshingFromSettings = false;
                }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.SettingsSaved", "OnSettingsSaved failed: " + ex.Message, ex); }
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
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Normalize", "NormalizeBaseUri failed: " + ex.Message, ex); return null; }
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
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Fallback", "TryGetFallbackHost failed: " + ex.Message, ex); return null; }
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
            catch (Exception exOuter) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ConnRefused", "IsConnectionRefused check failed: " + exOuter.Message, exOuter); return false; }


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
                AgenteIALocal.Logging.Log.Verbose("-", 9100, "Control.ServerLLMChanged", $"ServerLLM selection changed -> {selected}", null);

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
                    try { RefreshFromSettings(refreshModels: true); } catch (Exception exRefresh) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ServerChanged", "RefreshFromSettings failed: " + exRefresh.Message, exRefresh); }
                    try { AgentComposition.RecomposeFromSettings("ui:provider-changed-toolwindow"); } catch (Exception exRecomp) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ServerChanged", "RecomposeFromSettings failed: " + exRecomp.Message, exRecomp); }

                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.ServerChanged", $"Server provider changed: {prev} -> {newActiveId}", null);
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ServerChanged", "ServerLLM selection handler failed: " + ex.Message, ex);
                }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ServerLLM", "ServerLLM_SelectionChanged failed: " + ex.Message, ex); }
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
                try { delta = sb.ToString(prev, deltaLen); } catch (Exception exDelta) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Streaming", "Delta extraction failed: " + exDelta.Message, exDelta); delta = sb.ToString(); }

                if (para == null)
                {
                    try { _streamingAiRun.Text = sb.ToString(); } catch (Exception exText) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Streaming", "Run.Text set failed: " + exText.Message, exText); }
                    _streamingFlushedLength = curr;
                    try { AutoScrollIfSticky(deltaLen); } catch (Exception exScroll) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Streaming", "AutoScroll (simple) failed: " + exScroll.Message, exScroll); }
                    return;
                }

                if (lastRun.Text != null && lastRun.Text.Length < 4096)
                {
                    try { lastRun.Text += delta; }
                    catch (Exception exAppend)
                    {
                        AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Streaming", "Run.Text append failed: " + exAppend.Message, exAppend);
                        var nr = new Run(delta);
                        try { para.Inlines.Add(nr); } catch (Exception exAdd) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Streaming", "Inlines.Add (fallback) failed: " + exAdd.Message, exAdd); }
                        lastRun = nr;
                    }
                }
                else
                {
                    var nr = new Run(delta);
                    try { para.Inlines.Add(nr); } catch (Exception exAdd2) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Streaming", "Inlines.Add (new Run) failed: " + exAdd2.Message, exAdd2); }
                    lastRun = nr;
                }

                _streamingAiLastRun = lastRun;
                _streamingFlushedLength = curr;
                try { AutoScrollIfSticky(deltaLen); } catch (Exception exScroll2) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Streaming", "AutoScroll (final) failed: " + exScroll2.Message, exScroll2); }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Streaming", "ApplyStreamingDeltaFrom failed: " + ex.Message, ex); }
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
                catch (Exception exDoc) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.AutoScroll", "Doc range extraction failed: " + exDoc.Message, exDoc); }

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
                        catch (Exception exScroll) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.AutoScroll", "ScrollToEnd failed: " + exScroll.Message, exScroll); }
                    }), "AutoScroll");
                }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.AutoScroll", "AutoScrollIfSticky failed: " + ex.Message, ex); }
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
                        catch (Exception exScrollChanged) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.ScrollViewer", "ScrollChanged handler failed: " + exScrollChanged.Message, exScrollChanged); }
                    };
                    ResponseJsonText.PreviewMouseWheel += (s, e) =>
                    {
                        try { EnsureResponseScrollViewer(); } catch (Exception exWheel) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.ScrollViewer", "PreviewMouseWheel failed: " + exWheel.Message, exWheel); }
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
                        catch (Exception exResolve)
                        {
                            AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.MahApps", "AssemblyResolve failed: " + exResolve.Message, exResolve);
                            return null;
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.MahApps", "EnsureMahAppsIconPacksLoaded failed: " + ex.Message, ex);
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
            // MODIFICADO - ID: 20260123_230701 - Inicializar parser streaming
            _streamingParser = new AgenteIALocal.Infrastructure.Streaming.OpenAIStreamingParser();

            EnsureMahAppsIconPacksLoaded();
            InitializeComponent();

            // Set DataContext for XAML bindings
            this.DataContext = this;

            // NUEVO - ID: 20260125_003400 - Inicializar ConfigLabel con valor traducido
            try
            {
                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                if (locService != null)
                {
                    ConfigLabel = locService.GetString("ui.chat.status.not_configured");
                }
            }
            catch (Exception exLoc)
            {
                AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Constructor", "ConfigLabel i18n init failed: " + exLoc.Message, exLoc);
            }

            // Wire up footer RunMode selector persistence
            try
            {
                if (TypeActivitie != null)
                {
                    TypeActivitie.SelectionChanged -= TypeActivitie_SelectionChanged;
                    TypeActivitie.SelectionChanged += TypeActivitie_SelectionChanged;
                }
            }
            catch (Exception exWire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "TypeActivitie wire failed: " + exWire.Message, exWire); }

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
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.Constructor", "AgentService available at control construction.", null);
                }
                else
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "AgentService is null at control construction.", null);
                }
            }
            catch (Exception ex)
            {
                // Replace Trace with V2 logger
                try { AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9100, "Control.xaml", "Error ensuring composition: " + ex.Message, ex); } catch { }
            }

            // Load current log file content into the Log tab asynchronously
            StartLogRefreshLoop();

            // Ensure initial size label is correct even before first refresh tick
            try { UpdateLogFileSizeLabelFromBytes(TryGetLogFileSizeBytes()); } catch (Exception exInit) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "UpdateLogFileSizeLabelFromBytes failed: " + exInit.Message, exInit); }

            // Wire up response scroll viewer detection after control is loaded
            try
            {
                this.Loaded += (s, e) =>
                {
                    try { EnsureResponseScrollViewer(); } catch (Exception exLoadEvt) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.Constructor", "EnsureResponseScrollViewer (Loaded) failed: " + exLoadEvt.Message, exLoadEvt); }
                };
            }
            catch (Exception exLoadWire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "Loaded event wire failed: " + exLoadWire.Message, exLoadWire); }

            // Load current settings into settings panel (but keep panel hidden)
            try
            {
                var settings = AgentSettingsStore.Load();
                // Subscribe to settings saved notifications to refresh UI across windows
                try { AgentSettingsStore.SettingsSaved += OnSettingsSaved; } catch (Exception exSub) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "SettingsSaved subscribe failed: " + exSub.Message, exSub); }
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
                    catch (Exception exHydrate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "Hydrate ServerLLM selection failed: " + exHydrate.Message, exHydrate); }
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
                catch (Exception exUiState) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "SaveUiStateSafe failed: " + exUiState.Message, exUiState); }

                RefreshChatCombo();
                LoadActiveChatToUi();
            }
            catch (Exception exChat) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.Constructor", "Load chats failed: " + exChat.Message, exChat); }
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
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.OpenLogFile", "Open log file: path not available.", null);
                    return;
                }

                // Ensure the file exists so Explorer can select it.
                if (!File.Exists(path))
                {
                    try { AgenteIALocal.Logging.Log.Information("-", 9100, "Control.OpenLogFile", "(log file created)", null); } catch { }
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
                AgenteIALocal.Logging.Log.Error("-", 9100, "Control.OpenLogFile", "Open log file failed: " + ex.Message, ex);
            }
        }

        private void DeleteLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = GetLogFilePath();
                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", $"DELETE LOG: path={path}", null);
                
                if (string.IsNullOrWhiteSpace(path))
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.DeleteLogFile", "Delete log file: path not available.", null);
                    return;
                }

                if (!File.Exists(path))
                {
                    UpdateLogFileSizeLabelFromBytes(0);
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", $"Delete log file: file does not exist. Path checked: {path}", null);
                    return;
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", $"File exists, showing confirmation dialog. Size: {new FileInfo(path).Length} bytes", null);
                
                var res = MessageBox.Show(
                    "This will permanently delete the log file from disk. Are you sure?",
                    "Delete log file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (res != MessageBoxResult.Yes)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", "User cancelled delete operation", null);
                    return;
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", $"User confirmed - CLOSING SERILOG to release file lock", null);
                
                // CRÍTICO: Cerrar Serilog para liberar el FileStream antes de borrar
                try
                {
                    AgenteIALocal.Logging.Log.CloseAndFlush();
                    System.Threading.Thread.Sleep(100); // Pequeña pausa para asegurar que el archivo se cierra
                }
                catch (Exception exClose)
                {
                    // Log legacy como fallback porque Serilog está cerrado
                    try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.xaml", "CloseAndFlush failed: " + exClose.Message, exClose); } catch { }
                }
                
                // Ahora borrar el archivo (ya no está bloqueado)
                File.Delete(path);
                
                // Verificar que se borró
                var stillExists = File.Exists(path);
                
                // Reconfigurar Serilog con nuevo archivo
                try
                {
                    var settings = new AgenteIALocal.Logging.LogSettings
                    {
                        Enabled = true,
                        Information = true,
                        Warning = true,
                        Error = true,
                        Critical = true
                    };
                    AgenteIALocal.Logging.Log.Configure(settings);
                    
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", $"File deleted successfully. File.Exists after delete: {stillExists}", null);
                }
                catch (Exception exReconfig)
                {
                    try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.xaml", "Serilog reconfigure failed: " + exReconfig.Message, exReconfig); } catch { }
                }

                // Limpiar buffer Serilog UI
                try
                {
                    AgenteIALocal.Logging.Log.ClearUiBuffer();
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", "ClearUiBuffer() completed", null);
                }
                catch (Exception exClear)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "Control.DeleteLogFile", "ClearUiBuffer failed: " + exClear.Message, exClear);
                }

                Ui(() =>
                {
                    try
                    {
                        UpdateLogFileSizeLabelFromBytes(0);
                        RefreshLogPanel(); // Actualizar desde Serilog
                        ScrollLogToEnd(force: true);
                        AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", "UI updated - RefreshLogPanel called", null);
                    }
                    catch (Exception exUi)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "Control.DeleteLogFile", "UI update failed: " + exUi.Message, exUi);
                    }
                });

                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.DeleteLogFile", "Log file deletion completed successfully.", null);
            }
            catch (Exception ex)
            {
                try
                {
                    // Intentar loguear con Serilog
                    AgenteIALocal.Logging.Log.Error("-", 9100, "Control.DeleteLogFile", "Delete log file failed: " + ex.Message, ex);
                }
                catch
                {
                    // Si Serilog falla, usar legacy
                    try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.xaml", "Delete log failed: " + ex.Message, ex); } catch { }
                }
            }
        }

        // NUEVO METODO RefreshLogPanel - ID: 20260122_020000
        // Lee últimas 250 líneas desde Serilog.Log.GetRecentLogs() y actualiza LogText
        private void RefreshLogPanel()
        {
            try
            {
                var logs = AgenteIALocal.Logging.Log.GetRecentLogs(250);
                if (logs == null || logs.Count == 0)
                {
                    if (LogText != null)
                    {
                        LogText.Text = "(no logs)";
                    }
                    return;
                }

                var text = string.Join(Environment.NewLine, logs);
                if (LogText != null)
                {
                    LogText.Text = text;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "Control.RefreshLogPanel", "RefreshLogPanel failed: " + ex.Message, ex);
                    if (LogText != null)
                    {
                        LogText.Text = "(error loading logs)";
                    }
                }
                catch { }
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

        // MODIFICADO RunLogRefreshLoopAsync - ID: 20260122_020001
        // Ahora usa RefreshLogPanel() que lee desde Serilog.Log.GetRecentLogs()
        private async Task RunLogRefreshLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

                    var bytes = TryGetLogFileSizeBytes();
                    UpdateLogFileSizeLabelFromBytes(bytes);

                    // Actualizar LogText desde Serilog
                    try
                    {
                        RefreshLogPanel();
                    }
                    catch (Exception exRefresh) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.LogRefresh", "RefreshLogPanel failed: " + exRefresh.Message, exRefresh); }

                    ScrollLogToEnd(force: false);
                }
                catch
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                    UpdateLogFileSizeLabelFromBytes(0);
                    try { LogText.Text = "(unable to read logs)"; } catch { }
                    ScrollLogToEnd(force: false);
                }
                
                try { await Task.Delay(2000, ct); } catch (Exception exDelay) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.LogRefresh", "Delay interrupted: " + exDelay.Message, exDelay); }
            }
        }

        // ELIMINADO METODO EnsureUiLogBuffer - ID: 20260122_223001
        // Ya NO se necesita - RefreshLogPanel() lee directamente desde AgenteIALocal.Logging.Log.GetRecentLogs()
        // LogEventHub.OnLog tampoco se usa - todo migrado a Serilog

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
                AgenteIALocal.Logging.Log.Information("-", 9100, "Control.xaml", "RefreshFromSettings invoked.", null);
                var settings = AgentSettingsStore.Load();
                PopulateSettingsPanel(settings);
                ComputeIsLlmConfigured(settings);
                UpdateUiState(CurrentExecutionState);
                try { AgenteIALocal.Logging.Log.Verbose("-", 9100, "Control.RefreshSettings", $"ConfigStatus: RefreshFromSettings completed label={ConfigLabel} isConfigured={IsLlmConfigured}", null); } catch { }
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

                    // RunMode -> map globalSettings.runMode to TypeActivitie options - MODIFICADO - ID: 20260123_225702
                    try
                    {
                        var runMode = settings?.GlobalSettings?.RunMode ?? "preguntar";
                        if (string.IsNullOrEmpty(runMode)) runMode = "preguntar";
                        string runModeUi = string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase) ? "Agente" : "Preguntar";
                        if (TypeActivitie != null)
                        {
                            // avoid triggering persistence handlers
                            var prev = _isRefreshingFromSettings;
                            _isRefreshingFromSettings = true;
                            try { TrySelectComboByText(TypeActivitie, runModeUi); } catch (Exception exSelect) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.RefreshSettings", "TrySelectComboByText(TypeActivitie) failed: " + exSelect.Message, exSelect); }
                            _isRefreshingFromSettings = prev;
                        }
                    }
                    catch (Exception exRunMode) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.RefreshSettings", "RunMode mapping failed: " + exRunMode.Message, exRunMode); }

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
                                    try { ModelOfLLM.SelectedItem = null; } catch (Exception exClearSel) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.RefreshSettings", "ModelOfLLM.SelectedItem clear failed: " + exClearSel.Message, exClearSel); }
                                }
                            }
                            else
                            {
                                try { ModelOfLLM.SelectedItem = null; } catch (Exception exClear) { AgenteIALocal.Logging.Log.Debug("-", 9100, "Control.RefreshSettings", "ModelOfLLM.SelectedItem clear failed (empty savedModel): " + exClear.Message, exClear); }
                            }
                        }
                    }
                    catch { }
                }
                catch { }

                // Refresh models for active server asynchronously (fire-and-forget) only when requested
                if (refreshModels)
                {
                    try
                    {
                        FireAndForget(RefreshModelsForActiveServerAsync("RefreshFromSettings"), "RefreshFromSettings.RefreshModels");
                    }
                    catch (Exception exFire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.RefreshSettings", "Could not start RefreshModelsForActiveServerAsync: " + exFire.Message, exFire); }
                }
            }
            catch (Exception ex)
            {
                try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.RefreshSettings", $"[AgenteIALocalControl] RefreshFromSettings error: {ex.Message}", ex); } catch { }
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
                    try { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ModelsFetch", $"ModelsFetch: invalid baseUrl. raw='{rawBaseUrl}'", null); } catch { }
                    return result;
                }

                var primary = BuildModelsUri(baseUri);
                if (string.IsNullOrWhiteSpace(primary))
                {
                    try { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ModelsFetch", $"ModelsFetch: cannot build models uri. raw='{rawBaseUrl}' normalized='{baseUri}'", null); } catch { }
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
                        try { AgenteIALocal.Logging.Log.Information("-", 9100, "Control.ModelsFetch", $"ModelsFetch: GET {primary} (raw='{rawBaseUrl}' normalized='{baseUri}')", null); } catch { }
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
                            try { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ModelsFetch", $"ModelsFetch: primary failed (connection refused). Retrying alt='{alt}'", null); } catch { }
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
                            try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ModelsFetch", $"ModelsFetch: primary and fallback failed: {exAlt.Message}", exAlt); } catch { }
                            return result;
                        }
                    }

                    if (firstEx != null)
                    {
                        try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ModelsFetch", $"ModelsFetch: error: {firstEx.Message}", firstEx); } catch { }
                        return result;
                    }

                    if (resp == null) return result;

                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgenteIALocal.Logging.Log.Warning("-", 9100, "Control.ModelsFetch", $"ModelsFetch: non-success status {(int)resp.StatusCode} {resp.ReasonPhrase}", null); } catch { }
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
                        try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ModelsFetch", $"ModelsFetch: parse error: {exParse.Message}", exParse); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgenteIALocal.Logging.Log.Error("-", 9100, "Control.ModelsFetch", $"ModelsFetch: fatal: {ex.Message}", ex); } catch { }
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
                    AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9100, "Control.PromptKeyDown", "PromptTextBox_KeyDown failed: " + ex.Message, ex);
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

                // ELIMINADO - ID: 20260123_225703 - selectedModel legacy (modelo se persiste en ServerConfig.Model)
                // Persist selected model to settings
                // ARQUITECTURA: Modelo se persiste en activeServer.Model, NO en globalSettings
                // UI Config modal maneja persistencia de modelo correctamente
                AgenteIALocal.Logging.Log.Information(activeCorrelationId ?? "-", 9101, "Control.ModelChanged", "Model selection changed to: " + modelId + " (not persisted - UI Config handles it)", null);
            }
            catch (Exception ex)
            {
                try
                {
                    AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9103, "Control.ModelChanged", "ModelOfLLM_SelectionChanged failed: " + ex.Message, ex);
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
                    AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9104, "Control.RunButton", "RunButton_Click: _runExecutor is null", null);
                    return;
                }
                FireAndForget(_runExecutor.RunAsync(sender, e), "RunButton_Click->RunAsync");
            }
            catch (Exception ex)
            {
                try
                {
                    AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9105, "Control.RunButton", "RunButton_Click failed: " + ex.Message, ex);
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

                AgenteIALocal.Logging.Log.Information(activeCorrelationId ?? "-", 9110, "Control.ModelsRefresh", $"RefreshModelsForActiveServerAsync: {models.Count} models fetched from {srv.BaseUrl} (source: {source})", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9111, "Control.ModelsRefresh", $"RefreshModelsForActiveServerAsync failed: {ex.Message}", ex);
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
                AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9112, "Control.GetServer", $"TryGetActiveOpenAiCompatibleServer failed: {ex.Message}", ex);
                return false;
            }
        }

        // NUEVO METODO BuildStreamingPayload - ID: 20260122_000100
        // Construye payload JSON para OpenAI-compatible streaming (LM Studio + Jan)
        // Aplica requestDefaults: temperature, maxTokens, includeUsage (solo LM Studio)
        private string BuildStreamingPayload(string model, string userPrompt, string provider, double? temperature, int? maxTokens, bool includeUsage)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append('{');

                // model (required)
                if (!string.IsNullOrWhiteSpace(model))
                {
                    sb.Append("\"model\":\"");
                    sb.Append(JsonEscape(model));
                    sb.Append("\",");
                }

                // messages[] (required)
                sb.Append("\"messages\":[{\"role\":\"user\",\"content\":\"");
                sb.Append(JsonEscape(userPrompt ?? string.Empty));
                sb.Append("\"}],");

                // stream: true (required)
                sb.Append("\"stream\":true");

                // temperature (optional - solo si existe)
                if (temperature.HasValue)
                {
                    sb.Append(",\"temperature\":");
                    sb.Append(temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                // max_tokens (optional - solo si >0)
                if (maxTokens.HasValue && maxTokens.Value > 0)
                {
                    sb.Append(",\"max_tokens\":");
                    sb.Append(maxTokens.Value.ToString());
                }

                // stream_options.include_usage (solo LM Studio)
                if (includeUsage && string.Equals(provider, "lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(",\"stream_options\":{\"include_usage\":true}");
                }

                sb.Append('}');
                return sb.ToString();
            }
            catch
            {
                // Fallback minimal payload si falla construcción
                return "{\"model\":\"" + JsonEscape(model ?? string.Empty) + "\",\"messages\":[{\"role\":\"user\",\"content\":\"" + JsonEscape(userPrompt ?? string.Empty) + "\"}],\"stream\":true}";
            }
        }

        // NUEVO METODO JsonEscape - ID: 20260122_000101
        // Escapa caracteres especiales para JSON sin dependencias externas
        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        // NUEVO METODO ExecuteOpenAiCompatibleStreamingAsync - ID: 20260122_000200
        // Streaming SSE genérico para OpenAI-compatible providers (LM Studio + Jan)
        // Aplica requestDefaults: temperature, maxTokens, stream_options.include_usage (solo LM Studio)
        // MIGRADO A SERILOG - ID: 20260122_000303
        internal async Task<AgentHostResponse> ExecuteLmStudioStreamingAsync(AgentHostRequest req, ServerConfig server, ChatSession chat, ChatMessage aiBubble, CancellationToken ct)
        {
            var startTime = DateTime.UtcNow;
            var responseBuilder = new StringBuilder();
            int? promptTokensExtracted = null;
            int? completionTokensExtracted = null;
            int? totalTokensExtracted = null;

            try
            {
                if (server == null || string.IsNullOrWhiteSpace(server.BaseUrl))
                {
                    return new AgentHostResponse
                    {
                        Success = false,
                        Error = "Server configuration missing or invalid",
                        RequestId = req?.RequestId,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    };
                }

                // Leer requestDefaults desde settings
                double? temperature = null;
                int? maxTokens = null;
                bool includeUsage = false;
                try
                {
                    var settings = AgentSettingsStore.Load();
                    var global = settings?.GlobalSettings;
                    // MODIFICADO - ID: 20260123_225704 - Usar DTOs en lugar de JObject
                    if (global != null)
                    {
                        temperature = global.RequestDefaults.Temperature;
                        var mt = global.RequestDefaults.MaxTokens;
                        if (mt > 0) maxTokens = mt;
                        includeUsage = global.RequestDefaults.StreamOptions.IncludeUsage;
                    }
                }
                catch (Exception exSettings)
                {
                    try { AgenteIALocal.Logging.Log.Warning(activeCorrelationId ?? "-", 9116, "Control.StreamRequest", $"Failed to read requestDefaults: {exSettings.Message}", null); } catch { }
                }

                var provider = (server.Provider ?? string.Empty).ToLowerInvariant();
                var model = server.Model ?? string.Empty;
                var baseUrl = (server.BaseUrl ?? string.Empty).TrimEnd('/');
                var apiKey = server.ApiKey ?? string.Empty;
                if (string.IsNullOrWhiteSpace(apiKey)) apiKey = "lm-studio"; // default

                var endpoint = baseUrl + "/v1/chat/completions";
                var payload = BuildStreamingPayload(model, req?.Action ?? string.Empty, provider, temperature, maxTokens, includeUsage);

                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                var httpReq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(endpoint);
                httpReq.Method = "POST";
                httpReq.ContentType = "application/json";
                httpReq.Accept = "text/event-stream";
                httpReq.Headers[System.Net.HttpRequestHeader.Authorization] = "Bearer " + apiKey;
                httpReq.ContentLength = payloadBytes.Length;
                httpReq.Timeout = 300000; // 5 min timeout
                httpReq.ReadWriteTimeout = 300000;

                using (var reqStream = await httpReq.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    await reqStream.WriteAsync(payloadBytes, 0, payloadBytes.Length).ConfigureAwait(false);
                }

                using (var httpResp = (System.Net.HttpWebResponse)await httpReq.GetResponseAsync().ConfigureAwait(false))
                using (var respStream = httpResp.GetResponseStream())
                using (var reader = new StreamReader(respStream, Encoding.UTF8))
                {
                    if (httpResp.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        var errorBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                        return new AgentHostResponse
                        {
                            Success = false,
                            Error = $"HTTP {(int)httpResp.StatusCode} {httpResp.StatusDescription}: {errorBody}",
                            RequestId = req?.RequestId,
                            Timestamp = DateTime.UtcNow.ToString("o")
                        };
                    }

                    string line;
                    while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        if (ct.IsCancellationRequested) break;

                        // SSE format: "data: {...}" or "data: [DONE]"
                        if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

                        var jsonPart = line.Substring(5).TrimStart();

                        // REFACTORIZADO - ID: 20260123_230702 - Usar parser en lugar de lógica JSON en UI
                        var parsedChunk = _streamingParser.ParseChunk(jsonPart);

                        if (parsedChunk.IsDone) break;

                        if (parsedChunk.HasError)
                        {
                            try { AgenteIALocal.Logging.Log.Warning(activeCorrelationId ?? "-", 9117, "Control.StreamRequest", $"Failed to parse SSE chunk: {parsedChunk.ErrorMessage}", null); } catch { }
                            continue;
                        }

                        // Aplicar contenido si existe
                        if (!string.IsNullOrEmpty(parsedChunk.Content))
                        {
                            responseBuilder.Append(parsedChunk.Content);

                            // Update UI incrementally usando helper existente
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                            try { ApplyStreamingDeltaFrom(responseBuilder); } catch { }
                        }

                        // Extract usage tokens si vienen en el chunk
                        if (parsedChunk.Usage != null)
                        {
                            promptTokensExtracted = parsedChunk.Usage.PromptTokens;
                            completionTokensExtracted = parsedChunk.Usage.CompletionTokens;
                            totalTokensExtracted = parsedChunk.Usage.TotalTokens;
                        }
                    }
                }

                var finalOutput = responseBuilder.ToString();

                return new AgentHostResponse
                {
                    Success = true,
                    Output = finalOutput,
                    Error = null,
                    RequestId = req?.RequestId,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    PromptTokens = promptTokensExtracted,
                    CompletionTokens = completionTokensExtracted,
                    TotalTokens = totalTokensExtracted
                };
            }
            catch (System.Net.WebException wex)
            {
                try
                {
                    string errorBody = null;
                    try
                    {
                        using (var errStream = wex.Response?.GetResponseStream())
                        {
                            if (errStream != null)
                            {
                                using (var errReader = new StreamReader(errStream))
                                {
                                    errorBody = await errReader.ReadToEndAsync().ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch { }

                    var errorMsg = wex.Message + (string.IsNullOrWhiteSpace(errorBody) ? string.Empty : " - " + errorBody);

                    if (wex.Status == System.Net.WebExceptionStatus.Timeout)
                    {
                        errorMsg = "Timeout: " + errorMsg;
                    }

                    AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9118, "Control.StreamRequest", $"Streaming request failed (WebException): {errorMsg}", wex);

                    return new AgentHostResponse
                    {
                        Success = false,
                        Error = errorMsg,
                        RequestId = req?.RequestId,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    };
                }
                catch
                {
                    return new AgentHostResponse
                    {
                        Success = false,
                        Error = wex.Message,
                        RequestId = req?.RequestId,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    };
                }
            }
            catch (OperationCanceledException)
            {
                try { AgenteIALocal.Logging.Log.Information(activeCorrelationId ?? "-", 9119, "Control.StreamRequest", "Streaming request cancelled by user", null); } catch { }
                return new AgentHostResponse
                {
                    Success = false,
                    Error = "Operación cancelada",
                    RequestId = req?.RequestId,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception ex)
            {
                // MODIFICADO - ID: 20260122_010803 - Migrado a Serilog
                try { AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9120, "Control.StreamRequest", $"Streaming request failed: {ex.Message}", ex); } catch { }
                return new AgentHostResponse
                {
                    Success = false,
                    Error = ex.Message,
                    RequestId = req?.RequestId,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };
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
                    AgenteIALocal.Logging.Log.Information(activeCorrelationId ?? "-", 9115, "Control.ChatCleanup", "Removed empty AI bubble from chat", null);
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Warning(activeCorrelationId ?? "-", 9116, "Control.ChatCleanup", $"TryRemoveEmptyAiBubble: failed to remove: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error(activeCorrelationId ?? "-", 9117, "Control.ChatCleanup", $"TryRemoveEmptyAiBubble failed: {ex.Message}", ex);
            }
        }
    }
}

