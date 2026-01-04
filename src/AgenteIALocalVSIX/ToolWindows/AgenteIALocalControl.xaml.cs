using AgenteIALocal.Core.Logging;
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

        // Log file size label (bound in XAML, auto-updated)
        private string logFileSizeLabel = "0 K";
        public string LogFileSizeLabel
        {
            get { return logFileSizeLabel; }
            private set { if (logFileSizeLabel == value) return; logFileSizeLabel = value ?? "0 K"; RaisePropertyChanged(nameof(LogFileSizeLabel)); }
        }

        private bool _logScrollInitialized;

        // Chat state
        private List<ChatSession> chats = new List<ChatSession>();
        private ChatSession activeChat = null;

        // UI state persisted locally (last chat + token usage map)
        private sealed class UiState
        {
            public string LastChatId { get; set; }
            public Dictionary<string, int> MessageTokens { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        private readonly object _uiStateGate = new object();
        private UiState _uiState = new UiState();
        private bool _isSyncingChatCombo;

        private static string UiStateFilePath()
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AgenteIALocal");
                System.IO.Directory.CreateDirectory(dir);
                return System.IO.Path.Combine(dir, "ui_state.json");
            }
            catch
            {
                return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AgenteIALocal.ui_state.json");
            }
        }

        private void LoadUiStateSafe()
        {
            lock (_uiStateGate)
            {
                try
                {
                    var path = UiStateFilePath();
                    if (!System.IO.File.Exists(path))
                    {
                        _uiState = new UiState();
                        return;
                    }

                    var json = System.IO.File.ReadAllText(path);
                    var st = JsonConvert.DeserializeObject<UiState>(json);
                    _uiState = st ?? new UiState();
                    if (_uiState.MessageTokens == null)
                    {
                        _uiState.MessageTokens = new Dictionary<string, int>(StringComparer.Ordinal);
                    }
                }
                catch
                {
                    _uiState = new UiState();
                }
            }
        }

        private void SaveUiStateSafe()
        {
            lock (_uiStateGate)
            {
                try
                {
                    var path = UiStateFilePath();
                    var json = JsonConvert.SerializeObject(_uiState, Formatting.Indented);
                    System.IO.File.WriteAllText(path, json);
                }
                catch
                {
                    // ignore persistence errors
                }
            }
        }

        private static string TruncateWithEllipsis(string s, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(s)) return "New chat";
            s = s.Trim();
            if (s.Length <= maxChars) return s;
            return s.Substring(0, maxChars) + "...";
        }

        private static string ComputeMd5Hex(string text)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var hash = md5.ComputeHash(bytes);
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }
            catch
            {
                return (text ?? string.Empty).Length.ToString();
            }
        }

        private static string ComputeMessageTokenKey(string chatId, DateTime timestampUtc, string sender, string content)
        {
            // Stable key across sessions: chat + timestamp + sender + content hash
            var ticks = timestampUtc.ToUniversalTime().Ticks;
            return (chatId ?? string.Empty) + "|" + ticks.ToString() + "|" + (sender ?? string.Empty) + "|" + ComputeMd5Hex(content ?? string.Empty);
        }

        private int? TryGetTokensForMessage(string chatId, DateTime timestampUtc, string sender, string content)
        {
            try
            {
                var key = ComputeMessageTokenKey(chatId, timestampUtc, sender, content);
                lock (_uiStateGate)
                {
                    if (_uiState != null && _uiState.MessageTokens != null && _uiState.MessageTokens.TryGetValue(key, out var v))
                    {
                        return v;
                    }
                }
            }
            catch { }
            return null;
        }

        private void TrySetTokensForMessage(string chatId, DateTime timestampUtc, string sender, string content, int tokens)
        {
            try
            {
                var key = ComputeMessageTokenKey(chatId, timestampUtc, sender, content);
                lock (_uiStateGate)
                {
                    if (_uiState == null) _uiState = new UiState();
                    if (_uiState.MessageTokens == null) _uiState.MessageTokens = new Dictionary<string, int>(StringComparer.Ordinal);
                    _uiState.MessageTokens[key] = tokens;
                }
                SaveUiStateSafe();
            }
            catch { }
        }

        private void TryPersistChat(ChatSession chat)
        {
            try
            {
                if (chat == null) return;

                var t = typeof(ChatStore);
                var mi =
                    t.GetMethod("Save", BindingFlags.Public | BindingFlags.Static) ??
                    t.GetMethod("Update", BindingFlags.Public | BindingFlags.Static) ??
                    t.GetMethod("Upsert", BindingFlags.Public | BindingFlags.Static);

                if (mi == null) return;
                mi.Invoke(null, new object[] { chat });
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private void EnsureActiveChatExists()
        {
            try
            {
                if (activeChat != null) return;
                activeChat = ChatStore.CreateNew();
                chats.Add(activeChat);
                _uiState.LastChatId = activeChat.Id;
                SaveUiStateSafe();
            }
            catch
            {
                // ignore
            }
        }

        private void AddChatMessage(ChatSession chat, string sender, string content, int? tokens)
        {
            try
            {
                if (chat == null) return;
                var listObj = chat.Messages;
                if (listObj == null) return;

                var list = listObj as System.Collections.IList;
                if (list == null) return;

                var msgType = listObj.GetType().IsGenericType
                    ? listObj.GetType().GetGenericArguments()[0]
                    : typeof(object);

                var msg = Activator.CreateInstance(msgType);
                var tsUtc = DateTime.UtcNow;

                TrySetProp(msg, "Timestamp", tsUtc);
                TrySetProp(msg, "Sender", sender ?? string.Empty);
                TrySetProp(msg, "Content", content ?? string.Empty);

                list.Add(msg);

                if (tokens.HasValue && tokens.Value >= 0)
                {
                    TrySetTokensForMessage(chat.Id, tsUtc, sender ?? string.Empty, content ?? string.Empty, tokens.Value);
                }

                TryPersistChat(chat);
            }
            catch
            {
                // never throw from UI
            }
        }

        private static void TrySetProp(object obj, string propName, object value)
        {
            try
            {
                if (obj == null) return;
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return;

                if (value == null)
                {
                    p.SetValue(obj, null, null);
                    return;
                }

                var targetType = p.PropertyType;
                if (targetType.IsAssignableFrom(value.GetType()))
                {
                    p.SetValue(obj, value, null);
                    return;
                }

                // try common conversion (DateTimeOffset -> DateTime)
                if (targetType == typeof(DateTime) && value is DateTimeOffset dto)
                {
                    p.SetValue(obj, dto.UtcDateTime, null);
                    return;
                }

                if (targetType.IsEnum && value is string s)
                {
                    try
                    {
                        var enumValue = Enum.Parse(targetType, s, ignoreCase: true);
                        p.SetValue(obj, enumValue, null);
                    }
                    catch { }
                    return;
                }

                try
                {
                    var converted = Convert.ChangeType(value, targetType);
                    p.SetValue(obj, converted, null);
                }
                catch
                {
                    // ignore
                }
            }
            catch { }
        }

        private static string TryGetStringProp(object obj, string propName)
        {
            try
            {
                if (obj == null) return string.Empty;
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null) return string.Empty;
                var v = p.GetValue(obj, null);
                return v != null ? v.ToString() : string.Empty;
            }
            catch { return string.Empty; }
        }

        private static DateTime TryGetDateTimeProp(object obj, string propName)
        {
            try
            {
                if (obj == null) return DateTime.UtcNow;
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null) return DateTime.UtcNow;
                var v = p.GetValue(obj, null);
                if (v is DateTime dt) return dt;
                if (v is DateTimeOffset dto) return dto.UtcDateTime;
                if (v != null && DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return DateTime.UtcNow;
        }

        private static bool TryIsHttp200(object response)
        {
            try
            {
                if (response == null) return false;

                // If an Error property exists and has content, treat as non-OK even if status is 200.
                try
                {
                    var pErr = response.GetType().GetProperty("Error", BindingFlags.Public | BindingFlags.Instance);
                    if (pErr != null)
                    {
                        var err = pErr.GetValue(response, null)?.ToString();
                        if (!string.IsNullOrWhiteSpace(err)) return false;
                    }
                }
                catch { }

                // Numeric status codes (preferred)
                foreach (var propName in new[] { "StatusCode", "HttpStatusCode", "Code", "HttpCode", "ResponseCode", "ResultCode" })
                {
                    var p = response.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (p == null) continue;

                    var v = p.GetValue(response, null);
                    if (v is int i) return i == 200;
                    if (v is System.Net.HttpStatusCode h) return (int)h == 200;
                    if (v != null && int.TryParse(v.ToString(), out var pi)) return pi == 200;
                }

                // String status (fallback)
                foreach (var propName in new[] { "Status", "ReasonPhrase", "Message", "Result", "State" })
                {
                    var p = response.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (p == null) continue;

                    var s = p.GetValue(response, null)?.ToString();
                    if (string.IsNullOrWhiteSpace(s)) continue;

                    var t = s.Trim();
                    if (string.Equals(t, "OK", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(t, "200", StringComparison.OrdinalIgnoreCase)) return true;
                    if (string.Equals(t, "200 OK", StringComparison.OrdinalIgnoreCase)) return true;
                    if (t.StartsWith("200 ", StringComparison.OrdinalIgnoreCase)) return true;
                    if (t.Contains("200")) return true;
                }

                // Boolean success flags (last resort when no explicit code exists)
                foreach (var propName in new[] { "Ok", "Success", "IsSuccess", "Succeeded", "IsSuccessStatusCode" })
                {
                    var p = response.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (p == null) continue;

                    var v = p.GetValue(response, null);
                    if (v is bool b) return b;
                    if (v != null && bool.TryParse(v.ToString(), out var pb)) return pb;
                }
            }
            catch { }

            return false;
        }

        private static int? TryExtractTotalTokens(object response, string outputText)
        {
            try
            {
                if (response != null)
                {
                    // direct properties
                    foreach (var propName in new[] { "TotalTokens", "Tokens", "TokensUsed", "PromptTokens", "CompletionTokens" })
                    {
                        var p = response.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (p == null) continue;
                        var v = p.GetValue(response, null);
                        if (v is int i) return i;
                        if (v != null && int.TryParse(v.ToString(), out var pi)) return pi;
                    }

                    // Usage object
                    var u = response.GetType().GetProperty("Usage", BindingFlags.Public | BindingFlags.Instance);
                    if (u != null)
                    {
                        var usage = u.GetValue(response, null);
                        if (usage != null)
                        {
                            var pTotal = usage.GetType().GetProperty("TotalTokens", BindingFlags.Public | BindingFlags.Instance)
                                ?? usage.GetType().GetProperty("total_tokens", BindingFlags.Public | BindingFlags.Instance);
                            if (pTotal != null)
                            {
                                var v = pTotal.GetValue(usage, null);
                                if (v is int i2) return i2;
                                if (v != null && int.TryParse(v.ToString(), out var pi2)) return pi2;
                            }
                        }
                    }
                }
            }
            catch { }

            // Try parse from output JSON (LM Studio / OpenAI style usage object)
            try
            {
                if (string.IsNullOrWhiteSpace(outputText)) return null;
                var trimmed = outputText.Trim();
                if (!(trimmed.StartsWith("{") || trimmed.StartsWith("["))) return null;

                var tok = JToken.Parse(trimmed);

                // { usage: { total_tokens: 123 } }
                var usage = tok["usage"] ?? tok.SelectToken("usage");
                if (usage != null)
                {
                    var total = usage["total_tokens"] ?? usage["totalTokens"] ?? usage["TotalTokens"];
                    if (total != null && int.TryParse(total.ToString(), out var ti)) return ti;
                }

                // some providers: { meta: { tokens: 123 } }
                var meta = tok["meta"] ?? tok.SelectToken("meta");
                if (meta != null)
                {
                    var t = meta["tokens"] ?? meta["total_tokens"] ?? meta["totalTokens"];
                    if (t != null && int.TryParse(t.ToString(), out var ti2)) return ti2;
                }
            }
            catch { }

            return null;
        }

        private void RenderActiveChatToUi()
        {
            try
            {
                if (activeChat == null)
                {
                    ResponseJsonText.Document = CreatePlainDocument(string.Empty);
                    return;
                }

                ResponseJsonText.Document = RenderChatSessionToDocument(activeChat);
                ScrollResponseToEnd();
            }
            catch
            {
                // ignore
            }
        }

        private FlowDocument RenderChatSessionToDocument(ChatSession chat)
        {
            var fd = new FlowDocument { PagePadding = new Thickness(0) };
            try
            {
                if (chat == null || chat.Messages == null) return fd;

                foreach (var m in chat.Messages)
                {
                    var sender = TryGetStringProp(m, "Sender");
                    var content = TryGetStringProp(m, "Content");
                    var ts = TryGetDateTimeProp(m, "Timestamp");

                    var isUser = false;
                    var sLower = (sender ?? string.Empty).Trim().ToLowerInvariant();
                    if (sLower == "tú" || sLower == "tu" || sLower == "user" || sLower == "you") isUser = true;

                    // Tokens (only typically for AI messages)
                    int? tokens = null;
                    try
                    {
                        tokens = TryGetTokensForMessage(chat.Id, ts, sender ?? string.Empty, content ?? string.Empty);
                    }
                    catch { }

                    fd.Blocks.Add(CreateBubbleBlock(isUser, content ?? string.Empty, tokens));
                }
            }
            catch
            {
                // ignore
            }
            return fd;
        }

        private static PackIconKind ParseIconKindOrFallback(string name, PackIconKind fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return fallback;
                return (PackIconKind)Enum.Parse(typeof(PackIconKind), name, ignoreCase: true);
            }
            catch
            {
                return fallback;
            }
        }

        private BlockUIContainer CreateBubbleBlock(bool isUser, string text, int? tokens)
        {
            // Brushes from resources (fallback to reasonable dark defaults)
            Brush surfaceBg = TryFindResource("Brush.LayoutBg") as Brush ?? new SolidColorBrush(Color.FromRgb(40, 40, 40));
            Brush borderBrush = TryFindResource("HeaderMediumEmphasisBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(66, 66, 66));
            Brush textBrush = TryFindResource("HeaderHighEmphasisBrush") as Brush ?? Brushes.White;
            Brush boneBrush = TryFindResource("HeaderBoneEmphasisBrush") as Brush ?? Brushes.Bisque;
            Brush aiIconBrush = TryFindResource("HeaderCyanHoverBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0, 188, 212));
            Brush bubbleBgUser = TryFindResource("Brush.FooterBg") as Brush ?? new SolidColorBrush(Color.FromRgb(56, 56, 56));
            Brush bubbleBgAi = TryFindResource("HeaderBackgroundBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(30, 30, 34));

            var block = new BlockUIContainer();
            var outer = new Grid
            {
                Margin = new Thickness(0, 6, 0, 6),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new PackIcon
            {
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                Foreground = isUser ? boneBrush : aiIconBrush,
                Kind = isUser ? ParseIconKindOrFallback("AccountOutline", PackIconKind.Send) : ParseIconKindOrFallback("RobotOutline", PackIconKind.CogOutline)
            };

            var bubble = new Border
            {
                Background = isUser ? bubbleBgUser : bubbleBgAi,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 8),
                MaxWidth = 980
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // content renderer (reuse existing preprocessing + renderer pipeline)
            FlowDocument doc;
            try
            {
                var _prevCorr = activeCorrelationId;
                try
                {
                    if (string.IsNullOrEmpty(activeCorrelationId)) activeCorrelationId = "-";
                    doc = RenderResponseToDocument(text ?? string.Empty);
                }
                finally
                {
                    activeCorrelationId = _prevCorr;
                }
            }
            catch
            {
                doc = CreatePlainDocument(text ?? string.Empty);
            }

            try { doc.PagePadding = new Thickness(0); } catch { }

            var viewer = new FlowDocumentScrollViewer
            {
                Document = doc,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                IsToolBarVisible = false,
                Focusable = false
            };

            // ensure default foreground
            viewer.Foreground = textBrush;

            viewer.PreviewMouseWheel += BubbleViewer_PreviewMouseWheel;

            stack.Children.Add(viewer);

            if (tokens.HasValue && tokens.Value >= 0)
            {
                var meta = new TextBlock
                {
                    Text = "Tokens: " + tokens.Value.ToString(),
                    Margin = new Thickness(0, 6, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = boneBrush,
                    FontSize = 12,
                    Opacity = 0.9
                };
                stack.Children.Add(meta);
            }

            bubble.Child = stack;

            // Layout order: AI (icon left, bubble right). User (bubble left, icon right).
            if (!isUser)
            {
                Grid.SetColumn(icon, 0);
                Grid.SetColumn(bubble, 1);
                outer.Children.Add(icon);
                outer.Children.Add(bubble);
            }
            else
            {
                // swap positions
                outer.ColumnDefinitions.Clear();
                outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                icon.Margin = new Thickness(8, 8, 0, 0);

                Grid.SetColumn(bubble, 0);
                Grid.SetColumn(icon, 1);
                outer.Children.Add(bubble);
                outer.Children.Add(icon);
            }

            block.Child = outer;
            return block;
        }

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

        // Active correlation id for the current Run execution (used by logging in this control)
        private string activeCorrelationId = null;
        private CancellationTokenSource _runCts;
        private int _runVersion;

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

        
        private void RefreshChatCombo()
        {
            try
            {
                _isSyncingChatCombo = true;

                ChatComboBox.Items.Clear();
                foreach (var c in chats)
                {
                    if (c == null) continue;
                    ChatComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = c.Title,
                        Tag = c.Id
                    });
                }

                // select active
                var desiredId = activeChat != null ? activeChat.Id : (_uiState != null ? _uiState.LastChatId : null);

                if (!string.IsNullOrEmpty(desiredId))
                {
                    foreach (var it in ChatComboBox.Items.OfType<ComboBoxItem>())
                    {
                        if (string.Equals(it.Tag as string, desiredId, StringComparison.Ordinal))
                        {
                            ChatComboBox.SelectedItem = it;
                            break;
                        }
                    }
                }
                else if (ChatComboBox.Items.Count > 0)
                {
                    ChatComboBox.SelectedIndex = 0;
                }
            }
            catch
            {
                // ignore
            }
            finally
            {
                _isSyncingChatCombo = false;
            }
        }


        
        private void LoadActiveChatToUi()
        {
            try
            {
                if (activeChat == null)
                {
                    ResponseJsonText.Document = CreatePlainDocument(string.Empty);
                    ScrollResponseToEnd();
                    return;
                }

                RenderActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }


        
        private void ChatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingChatCombo) return;

            try
            {
                var selected = ChatComboBox.SelectedItem as ComboBoxItem;
                var id = selected != null ? selected.Tag as string : null;
                if (string.IsNullOrEmpty(id)) return;

                var found = chats.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
                if (found == null) return;

                activeChat = found;

                lock (_uiStateGate)
                {
                    if (_uiState == null) _uiState = new UiState();
                    _uiState.LastChatId = activeChat.Id;
                }
                SaveUiStateSafe();

                LoadActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }


        
        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = ChatStore.CreateNew();
                if (c == null) return;

                // make it the active chat
                activeChat = c;

                // ensure it appears first
                chats.RemoveAll(x => x != null && string.Equals(x.Id, c.Id, StringComparison.Ordinal));
                chats.Insert(0, c);

                lock (_uiStateGate)
                {
                    if (_uiState == null) _uiState = new UiState();
                    _uiState.LastChatId = activeChat.Id;
                }
                SaveUiStateSafe();

                RefreshChatCombo();
                LoadActiveChatToUi();
            }
            catch
            {
                // ignore
            }
        }


        
        private void DeleteChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (activeChat == null) return;

                try { ChatStore.Delete(activeChat.Id); } catch { }

                chats.RemoveAll(c => c != null && string.Equals(c.Id, activeChat.Id, StringComparison.Ordinal));

                if (chats.Count > 0)
                {
                    activeChat = chats[0];
                }
                else
                {
                    activeChat = ChatStore.CreateNew();
                    if (activeChat != null) chats.Add(activeChat);
                }

                lock (_uiStateGate)
                {
                    if (_uiState == null) _uiState = new UiState();
                    _uiState.LastChatId = activeChat != null ? activeChat.Id : null;
                }
                SaveUiStateSafe();

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

                try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigStatus: computed configured={configured} activeServerId={activeId ?? "(none)"} baseUrlPresent={baseUrlPresent} modelPresent={modelPresent}"); } catch { }
            }
            catch
            {
                // never throw from UI
                IsLlmConfigured = false;
                ConfigLabel = "Not Config";
                try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigStatus: compute error, defaulted to Not Config"); } catch { }
            }
        }

        private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Settings button clicked (open modal).");

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                var title = settings.ActiveServerId ?? string.Empty;

                var owner = Window.GetWindow(this);
                var win = new AgenteIALocalConfigWindow(title);
                if (owner != null) win.Owner = owner;
                win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                try
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"[AgenteIALocalControl] Opening config modal with title '{win.Title}'");
                    win.ShowDialog();
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Config modal closed.");
                }
                catch (Exception ex)
                {
                    AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"[AgenteIALocalControl] Error showing config modal: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"[AgenteIALocalControl] SettingsButton_Click failure: {ex.Message}", ex);
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

        private void RefreshLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshLogFromFile();
            }
            catch (Exception ex)
            {
                AppendLog("Refresh log failed: " + ex.Message);
            }
        }

        private void CopyLogAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = LogText?.Text ?? string.Empty;
                Clipboard.SetText(text);
                AppendLog("Log copied to clipboard.");
            }
            catch (Exception ex)
            {
                AppendLog("Copy log failed: " + ex.Message);
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

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // Fire-and-forget wrapper to avoid async void (VSTHRD100).
            // If running, treat click as STOP.
            if (CurrentExecutionState == ExecutionState.Running)
            {
                RequestStopExecution();
                return;
            }

            _ = RunButton_ClickAsync(sender, e);
        }

        private void RequestStopExecution()
        {
            try
            {
                // Invalidate any in-flight completion (ignore late results)
                _ = Interlocked.Increment(ref _runVersion);
            }
            catch { }

            try { _runCts?.Cancel(); } catch { }
            try { _runCts?.Dispose(); } catch { }
            _runCts = null;

            // Log while correlation id is still available
            AppendLog("Stop requested.");

            activeCorrelationId = null;

            Ui(() =>
            {
                UpdateUiState(ExecutionState.Idle);
            });
        }



        private async System.Threading.Tasks.Task RunButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (CurrentExecutionState == ExecutionState.Running) return;

            // New run: cancel any previous token (best-effort) and create a fresh one
            try { _runCts?.Cancel(); } catch { }
            try { _runCts?.Dispose(); } catch { }

            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;
            var myVersion = Interlocked.Increment(ref _runVersion);

            activeCorrelationId = Guid.NewGuid().ToString("N");
            var myCorrelationId = activeCorrelationId;

            AppendLog("Run clicked.");
            UpdateUiState(ExecutionState.Running);
            AppendLog("Execution started.");

            try
            {
                AgentComposition.EnsureComposition();

                var userInput = PromptTextBox.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    return;
                }

                // Ensure we have an active chat and remember it
                EnsureActiveChatExists();

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

                // If chat has default title, name it from the first prompt (truncate 200 chars + ...)
                try
                {
                    if (activeChat != null)
                    {
                        var title = activeChat.Title ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(title) || string.Equals(title.Trim(), "New chat", StringComparison.OrdinalIgnoreCase))
                        {
                            activeChat.Title = TruncateWithEllipsis(userInput, 200);
                            TryPersistChat(activeChat);
                            RefreshChatCombo();
                        }
                    }
                }
                catch { }

                // Append user message as a bubble immediately
                try
                {
                    AddChatMessage(activeChat, "Tú", userInput, null);
                    RenderActiveChatToUi();
                }
                catch { }


                var req = new AgentHostRequest
                {
                    RequestId = myCorrelationId,
                    CorrelationId = myCorrelationId,
                    Action = userInput,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    SolutionName = SolutionNameText.Text ?? string.Empty,
                    ProjectCount = int.TryParse(ProjectCountText.Text, out var pc) ? pc : 0
                };

                var execTask = Task.Run(() =>
                {
                    try
                    {
                        if (AgentComposition.AgentService != null)
                        {
                            return AgentComposition.AgentService.Execute(req);
                        }

                        AppendLog("AgentService not composed; using MockAgentExecutor fallback.");
                        return MockAgentExecutor.Execute(req);
                    }
                    catch (Exception ex)
                    {
                        var corr =
                            !string.IsNullOrEmpty(req?.CorrelationId) ? req.CorrelationId :
                            !string.IsNullOrEmpty(req?.RequestId) ? req.RequestId :
                            "-";

                        try
                        {
                            AgentComposition.Error(
                                corr,
                                new AgenteIALocal.Core.Logging.LogEventId(9102, "VSIX.UI.Exception"),
                                "[AgenteIALocalControl] Execution exception in background task: " + ex.Message,
                                ex);
                        }
                        catch { }

                        AppendLog("Execution exception in background task: " + ex.Message);
                        throw;
                    }
                });

                // If Stop is requested, stop waiting and ignore late results.
                var completed = await Task.WhenAny(execTask, Task.Delay(Timeout.Infinite, ct));
                if (completed != execTask)
                {
                    // Observe background exception (VSTHRD105: specify scheduler explicitly)
                    _ = execTask.ContinueWith(
                        t => { _ = t.Exception; },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);

                    return;
                }

                var response = await execTask;

                // Ignore stale/canceled completions
                if (ct.IsCancellationRequested) return;
                if (myVersion != _runVersion) return;
                if (!string.Equals(activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) return;

                string display;
                if (response == null) display = "(no response)";
                else if (!string.IsNullOrEmpty(response.Output)) display = response.Output;
                else if (!string.IsNullOrEmpty(response.Error)) display = "Error: " + response.Error;
                else display = "(empty response)";

                Ui(() =>
                {
                    // Re-check on UI thread (Stop could have been requested between awaits)
                    if (ct.IsCancellationRequested) return;
                    if (myVersion != _runVersion) return;
                    if (!string.Equals(activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) return;

                    try
                    {
                        AppendLog("[VERBOSE] RenderResponse: start; len=" + (display?.Length ?? 0));
                        
                        var ok200 = false;
                        try { ok200 = TryIsHttp200(response); } catch { }

                        // Append AI response as a bubble, show tokens
                        try
                        {
                            var tokens = TryExtractTotalTokens(response, display);

                            AddChatMessage(activeChat, "IA", display, tokens);

                            RenderActiveChatToUi();
                        }
                        catch
                        {
                            // fallback to previous rendering
                            try
                            {
                                ResponseJsonText.Document = RenderResponseToDocument(display);
                                ScrollResponseToEnd();
                            }
                            catch { }
                        }

                        // Clear prompt ONLY on 200 OK (cancel is already guarded above)
                        if (ok200)
                        {
                            try
                            {
                                PromptTextBox.Text = string.Empty;
                                PromptTextBox.Focus();
                            }
                            catch { }
                        }
AppendLog("[VERBOSE] RenderResponse: done; len=" + (display?.Length ?? 0));
                    }
                    catch (Exception exRender)
                    {
                        AppendLog("[VERBOSE] RenderResponse failed: " + exRender.Message);
                        ResponseJsonText.Document = CreatePlainDocument(display ?? string.Empty);
                    }

                    UpdateUiState(ExecutionState.Completed);
                    AppendLog("Execution completed successfully.");

                    activeCorrelationId = null;

                    try { _runCts?.Dispose(); } catch { }
                    _runCts = null;

                    try { RefreshLogFromFile(); } catch { }
                });
            }
            catch (Exception ex)
            {
                // If Stop was requested, do not surface as error
                if (ct.IsCancellationRequested) return;
                if (myVersion != _runVersion) return;
                if (!string.Equals(activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) return;

                Ui(() =>
                {
                    UpdateUiState(ExecutionState.Error);
                    AppendLog("Execution failed: " + ex.Message);

                    try
                    {
                        AgentComposition.Error(
                            activeCorrelationId ?? "-",
                            AgenteIALocal.Core.Logging.LogEvents.Vsix_UI,
                            "[AgenteIALocalControl] Execution failed: " + ex.Message,
                            ex);
                    }
                    catch { }

                    try
                    {
                        ResponseJsonText.Document = RenderResponseToDocument("{ \"error\": \"Execution failed\" }");
                    }
                    catch
                    {
                        ResponseJsonText.Document = CreatePlainDocument("{ \"error\": \"Execution failed\" }");
                    }

                    activeCorrelationId = null;

                    try { _runCts?.Dispose(); } catch { }
                    _runCts = null;

                    try { RefreshLogFromFile(); } catch { }
                });
            }
        }



        // Response format enum (Phase 1 + Markdown)
        private enum ResponseFormat { PlainText, Json, Markdown }

        // Simple format detector (JSON vs Markdown vs PlainText)
        private static class FormatDetector
        {
            public static ResponseFormat DetectFormat(string content)
            {
                if (string.IsNullOrWhiteSpace(content)) return ResponseFormat.PlainText;

                var trimmed = content.TrimStart();

                // Heuristic: Markdown markers
                bool looksLikeMarkdown = false;
                try
                {
                    if (content.Contains("```")) looksLikeMarkdown = true;
                    var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.TrimStart();
                        if (line.StartsWith("# ") || line.StartsWith("## ") || line.StartsWith("### ") ||
                            line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("> ") ||
                            Regex.IsMatch(line, "^\\d+\\.\\s"))
                        {
                            looksLikeMarkdown = true;
                            break;
                        }
                    }
                }
                catch { }

                // Heuristic: JSON structural + parse attempt
                bool isJson = false;
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    try
                    {
                        JToken.Parse(content);
                        isJson = true;
                    }
                    catch { isJson = false; }
                }

                // Preference: if both detected, prefer JSON; if markdown detected and NOT json, return Markdown
                if (looksLikeMarkdown && !isJson)
                {
                    return ResponseFormat.Markdown;
                }

                if (isJson)
                {
                    return ResponseFormat.Json;
                }

                if (looksLikeMarkdown)
                {
                    return ResponseFormat.Markdown;
                }

                return ResponseFormat.PlainText;
            }
        }

        // Renderer interface and implementations (modified to accept correlationId)
        private interface IResponseRenderer
        {
            FlowDocument Render(string content, string correlationId);
        }

        private class PlainTextResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                return CreatePlainDocument(content ?? string.Empty);
            }
        }

        private class JsonResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                if (content == null) content = string.Empty;
                try
                {
                    var token = JToken.Parse(content);
                    var pretty = token.ToString(Formatting.Indented);

                    var fd = new FlowDocument { PagePadding = new Thickness(0) };
                    var p = new Paragraph { Margin = new Thickness(0) };
                    p.FontFamily = new FontFamily("Consolas");

                    var lines = pretty.Split(new[] { '\n' });
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i] ?? string.Empty;
                        var run = new Run(line);
                        p.Inlines.Add(run);
                        if (i < lines.Length - 1) p.Inlines.Add(new LineBreak());
                    }

                    fd.Blocks.Clear();
                    fd.Blocks.Add(p);

                    try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] JsonResponseRenderer: rendered json"); } catch { }

                    return fd;
                }
                catch (Exception)
                {
                    return CreatePlainDocument(content);
                }
            }
        }

        private class MarkdownResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                try
                {
                    if (content == null) content = string.Empty;

                    var fd = new FlowDocument { PagePadding = new Thickness(0) };

                    var lines = Regex.Split(content, "\r?\n");
                    bool inCodeFence = false;
                    var codeFenceBuilder = new StringBuilder();
                    string codeFenceLang = null;

                    List currentList = null;

                    foreach (var raw in lines)
                    {
                        var line = raw ?? string.Empty;

                        // Code fence handling
                        var trimmed = line.TrimStart();
                        if (!inCodeFence && trimmed.StartsWith("```"))
                        {
                            inCodeFence = true;
                            codeFenceLang = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : string.Empty;
                            codeFenceBuilder.Clear();
                            continue;
                        }
                        if (inCodeFence)
                        {
                            if (trimmed.StartsWith("```"))
                            {
                                var p = new Paragraph { Margin = new Thickness(0) };
                                p.FontFamily = new FontFamily("Consolas");
                                p.Inlines.Add(new Run(codeFenceBuilder.ToString()));
                                ApplyCodeBlockStyle(p);
                                fd.Blocks.Add(p);
                                try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: code fence styled"); } catch { }
                                inCodeFence = false;
                                codeFenceLang = null;
                                continue;
                            }
                            codeFenceBuilder.AppendLine(line);
                            continue;
                        }

                        // Headings
                        if (Regex.IsMatch(trimmed, "^#{1,3}\\s+"))
                        {
                            int level = 1;
                            if (trimmed.StartsWith("###")) level = 3;
                            else if (trimmed.StartsWith("##")) level = 2;

                            var text = trimmed.TrimStart('#').Trim();
                            var p = new Paragraph { Margin = new Thickness(0) };
                            switch (level)
                            {
                                case 1: p.FontSize = 18; break;
                                case 2: p.FontSize = 15; break;
                                case 3: p.FontSize = 13; break;
                            }
                            p.FontWeight = FontWeights.Bold;
                            AddInlinesToParagraph(p, text);
                            ApplyHeaderStyle(p, level);
                            fd.Blocks.Add(p);

                            try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: header styled level=" + level); } catch { }

                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            continue;
                        }

                        // Blockquote
                        if (trimmed.StartsWith("> "))
                        {
                            var text = trimmed.Substring(2).Trim();
                            var p = new Paragraph { Margin = new Thickness(12, 0, 0, 0), Foreground = Brushes.Gray };
                            var borderRun = new Run("│ ") { Foreground = HexBrush("#3F3F46") };
                            p.Inlines.Add(borderRun);
                            AddInlinesToParagraph(p, text);
                            ApplyBlockQuoteStyle(p);
                            fd.Blocks.Add(p);
                            try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: blockquote styled"); } catch { }
                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            continue;
                        }

                        // Unordered list
                        if (Regex.IsMatch(trimmed, "^[-*]\\s+"))
                        {
                            var itemText = Regex.Replace(trimmed, "^[-*]\\s+", "");
                            if (currentList == null || currentList.MarkerStyle != TextMarkerStyle.Disc)
                            {
                                if (currentList != null) { fd.Blocks.Add(currentList); }
                                currentList = new List { MarkerStyle = TextMarkerStyle.Disc };
                                ApplyListStyle(currentList);
                            }
                            var li = new ListItem();
                            var p = new Paragraph { Margin = new Thickness(0) };
                            AddInlinesToParagraph(p, itemText);
                            ApplyParagraphStyle(p);
                            li.Blocks.Add(p);
                            currentList.ListItems.Add(li);
                            continue;
                        }

                        // Ordered list
                        if (Regex.IsMatch(trimmed, "^\\d+\\.\\s+"))
                        {
                            var itemText = Regex.Replace(trimmed, "^\\d+\\.\\s+", "");
                            if (currentList == null || currentList.MarkerStyle != TextMarkerStyle.Decimal)
                            {
                                if (currentList != null) { fd.Blocks.Add(currentList); }
                                currentList = new List { MarkerStyle = TextMarkerStyle.Decimal };
                                ApplyListStyle(currentList);
                            }
                            var li = new ListItem();
                            var p = new Paragraph { Margin = new Thickness(0) };
                            AddInlinesToParagraph(p, itemText);
                            ApplyParagraphStyle(p);
                            li.Blocks.Add(p);
                            currentList.ListItems.Add(li);
                            continue;
                        }

                        // Empty line -> close current list and add paragraph break
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            var empty = new Paragraph { Margin = new Thickness(0) };
                            ApplyParagraphStyle(empty);
                            fd.Blocks.Add(empty);
                            continue;
                        }

                        // Regular paragraph line
                        var para = new Paragraph { Margin = new Thickness(0) };
                        AddInlinesToParagraph(para, line);
                        ApplyParagraphStyle(para);
                        fd.Blocks.Add(para);
                        if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                    }

                    if (inCodeFence)
                    {
                        return CreatePlainDocument(content);
                    }

                    try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: render complete"); } catch { }

                    return fd;
                }
                catch (Exception)
                {
                    return CreatePlainDocument(content);
                }
            }

            // Simple inline parser to add Runs/Bold/Italic/InlineCode into paragraph
            private void AddInlinesToParagraph(Paragraph p, string text)
            {
                try
                {
                    if (string.IsNullOrEmpty(text)) return;

                    // Handle inline code first using backticks
                    var parts = Regex.Split(text, "(`[^`]+`)");

                    foreach (var part in parts)
                    {
                        if (part.StartsWith("`") && part.EndsWith("`"))
                        {
                            var code = part.Substring(1, part.Length - 2);
                            var run = new Run(code) { FontFamily = new FontFamily("Consolas") };
                            // inline code styling
                            run.Background = HexBrush("#2D2D30");
                            run.Foreground = HexBrush("#DCDCDC");
                            p.Inlines.Add(run);
                        }
                        else
                        {
                            // handle bold **text**
                            var pattern = new Regex("(\\*\\*([^\\*]+)\\*\\*)");
                            var m = pattern.Match(part);
                            if (!m.Success)
                            {
                                var ital = new Regex("(\\*([^\\*]+)\\*)");
                                var mi = ital.Match(part);
                                if (!mi.Success)
                                {
                                    p.Inlines.Add(new Run(part));
                                }
                                else
                                {
                                    var segs = Regex.Split(part, "(\\*[^\\*]+\\*)");
                                    foreach (var s in segs)
                                    {
                                        if (s.StartsWith("*") && s.EndsWith("*"))
                                        {
                                            var inner = s.Substring(1, s.Length - 2);
                                            var it = new Italic(new Run(inner));
                                            p.Inlines.Add(it);
                                        }
                                        else
                                        {
                                            p.Inlines.Add(new Run(s));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var segs = Regex.Split(part, "(\\*\\*[^\\*]+\\*\\*)");
                                foreach (var s in segs)
                                {
                                    if (s.StartsWith("**") && s.EndsWith("**"))
                                    {
                                        var inner = s.Substring(2, s.Length - 4);
                                        var b = new Bold(new Run(inner));
                                        p.Inlines.Add(b);
                                    }
                                    else
                                    {
                                        p.Inlines.Add(new Run(s));
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    p.Inlines.Add(new Run(text));
                }
            }

            // Styling helpers
            private void ApplyHeaderStyle(Paragraph p, int level)
            {
                try
                {
                    switch (level)
                    {
                        case 1:
                            p.FontSize = 18;
                            p.FontWeight = FontWeights.Bold;
                            p.Margin = new Thickness(0, 12, 0, 6);
                            break;
                        case 2:
                            p.FontSize = 16;
                            p.FontWeight = FontWeights.Bold;
                            p.Margin = new Thickness(0, 10, 0, 6);
                            break;
                        default:
                            p.FontSize = 14;
                            p.FontWeight = FontWeights.SemiBold;
                            p.Margin = new Thickness(0, 8, 0, 4);
                            break;
                    }
                }
                catch { }
            }

            private void ApplyCodeBlockStyle(Paragraph p)
            {
                try
                {
                    p.FontFamily = new FontFamily("Consolas");
                    p.Background = HexBrush("#1E1E1E");
                    p.Foreground = HexBrush("#DCDCDC");
                    p.Margin = new Thickness(0, 6, 0, 6);
                }
                catch { }
            }

            private void ApplyBlockQuoteStyle(Paragraph p)
            {
                try
                {
                    p.Foreground = HexBrush("#9DA5B4");
                    p.Margin = new Thickness(12, 4, 0, 6);
                }
                catch { }
            }

            private void ApplyParagraphStyle(Paragraph p)
            {
                try
                {
                    p.Margin = new Thickness(0, 2, 0, 6);
                    p.LineHeight = 18;
                }
                catch { }
            }

            private void ApplyListStyle(List list)
            {
                try
                {
                    list.Margin = new Thickness(0, 2, 0, 6);
                    // left padding simulated by marker indent
                }
                catch { }
            }

            private Brush HexBrush(string hex)
            {
                try
                {
                    var bc = new BrushConverter();
                    var b = bc.ConvertFrom(hex) as Brush;
                    return b ?? Brushes.Transparent;
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }

        private static class RendererFactory
        {
            public static IResponseRenderer Get(ResponseFormat fmt)
            {
                switch (fmt)
                {
                    case ResponseFormat.Json: return new JsonResponseRenderer();
                    case ResponseFormat.Markdown: return new MarkdownResponseRenderer();
                    case ResponseFormat.PlainText:
                    default: return new PlainTextResponseRenderer();
                }
            }
        }

        // Orchestrator: normalize -> detect -> render -> fallback
        private System.Windows.Documents.FlowDocument RenderResponseToDocument(string raw)
        {
            AppendLog("[VERBOSE] RenderResponseToDocument: start; rawLen=" + (raw?.Length ?? 0));

            string content = null;
            try
            {
                // 1) Detect format using RAW input (do not normalize before detection)
                var fmt = FormatDetector.DetectFormat(raw);
                AppendLog("[VERBOSE] RenderResponseToDocument: detected format=" + fmt.ToString());

                // 2) Decide normalization strategy
                if (fmt == ResponseFormat.Markdown)
                {
                    // For Markdown we must preserve original raw text exactly
                    content = raw ?? string.Empty;
                    AppendLog("[VERBOSE] RenderResponseToDocument: Markdown detected -> normalization bypassed");
                }
                else
                {
                    // For JSON and PlainText use normalizer
                    content = ResponseNormalizer.Normalize(raw, activeCorrelationId ?? "-") ?? string.Empty;
                    AppendLog("[VERBOSE] RenderResponseToDocument: normalization applied");
                }

                AppendLog("[VERBOSE] RenderResponseToDocument: rawLen=" + (raw?.Length ?? 0) + " contentLen=" + (content?.Length ?? 0));

                // 3) Select renderer
                var renderer = RendererFactory.Get(fmt);
                AppendLog("[VERBOSE] RenderResponseToDocument: renderer selected=" + renderer.GetType().Name);

                // 4) Render
                try
                {
                    var doc = renderer.Render(content, activeCorrelationId);
                    if (doc == null)
                    {
                        AppendLog("[VERBOSE] RenderResponseToDocument: renderer returned null, fallback to plain text");
                        return CreatePlainDocument(content);
                    }

                    AppendLog("[VERBOSE] RenderResponseToDocument: render OK; outLen=" + (content?.Length ?? 0));
                    return doc;
                }
                catch (Exception exRender)
                {
                    AppendLog("[VERBOSE] RenderResponseToDocument: renderer threw -> " + exRender.Message);
                    return CreatePlainDocument(content);
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] RenderResponseToDocument: unexpected error -> " + ex.Message, ex); } catch { }
                return CreatePlainDocument(content ?? raw ?? string.Empty);
            }
        }

        private static System.Windows.Documents.FlowDocument CreatePlainDocument(string text)
        {
            var fd = new System.Windows.Documents.FlowDocument();
            try
            {
                fd.PagePadding = new System.Windows.Thickness(0);
                var p = new System.Windows.Documents.Paragraph();
                p.Margin = new System.Windows.Thickness(0);
                p.Inlines.Add(new System.Windows.Documents.Run(text ?? string.Empty));
                fd.Blocks.Clear();
                fd.Blocks.Add(p);
            }
            catch
            {
                try
                {
                    fd.Blocks.Clear();
                    fd.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text ?? string.Empty)));
                }
                catch { }
            }

            return fd;
        }

        // Raise property changed helper to avoid name collisions
        private void RaisePropertyChanged(string propertyName)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        

        private void BubbleViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (ResponseJsonText == null) return;

                e.Handled = true;
                var evt = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                ResponseJsonText.RaiseEvent(evt);
            }
            catch { }
        }

        private void ScrollResponseToEnd()
        {
            try
            {
                if (ResponseJsonText == null) return;
                ResponseJsonText.ScrollToEnd();
            }
            catch { }
        }

// UI-thread marshal helper (safe to call from background threads)
        private void Ui(Action action)
        {
            try
            {
                if (action == null) return;

                var dispatcher = this.Dispatcher;
                if (dispatcher == null)
                {
                    action();
                    return;
                }

                if (dispatcher.CheckAccess())
                {
                    action();
                    return;
                }

                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    action();
                });
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
                    RunButtonEnabled = true;
                    ClearButtonEnabled = false;
                    IsPromptReadOnly = true;
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

        
        private void ScrollLogToEnd(bool force)
        {
            try
            {
                if (LogText == null) return;

                // Requirement: scroll to end at least once on startup.
                if (force || !_logScrollInitialized)
                {
                    _logScrollInitialized = true;
                    LogText.ScrollToEnd();
                }
                else
                {
                    // Keep simplest behavior: always follow tail.
                    LogText.ScrollToEnd();
                }
            }
            catch { }
        }

        private void UpdateLogFileSizeLabelFromBytes(long bytes)
        {
            try { LogFileSizeLabel = FormatFileSizeLabel(bytes); } catch { LogFileSizeLabel = "0 K"; }
        }

        private static long TryGetLogFileSizeBytes()
        {
            try
            {
                var path = GetLogFilePath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatFileSizeLabel(long bytes)
        {
            try
            {
                if (bytes <= 0) return "0 K";

                const long KB = 1024;
                const long MB = 1024 * KB;
                const long GB = 1024 * MB;

                long value;
                string unit;

                if (bytes < MB)
                {
                    value = (bytes + KB - 1) / KB;
                    unit = "K";
                }
                else if (bytes < GB)
                {
                    value = (bytes + MB - 1) / MB;
                    unit = "Mb";
                }
                else
                {
                    value = (bytes + GB - 1) / GB;
                    unit = "GB";
                }

                if (value > 9999) value = 9999;
                return value.ToString() + " " + unit;
            }
            catch
            {
                return "0 K";
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
            var line = ts + " - " + (message ?? string.Empty);

            Ui(() =>
            {
                try
                {
                    if (LogText != null)
                    {
                        // Keep file order (append) and keep view on tail.
                        LogText.Text = (LogText.Text ?? string.Empty) + line + "\n";
                        ScrollLogToEnd(force: false);
                    }
                }
                catch { }
            });

            try
            {
                try { AgentComposition.Info(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] " + (message ?? string.Empty)); } catch { }
                AppendLogFileLine("[AgenteIALocalControl] " + (message ?? string.Empty));
            }
            catch { }
        }
        private void RefreshLogFromFile()
        {
            try
            {
                var content = ReadLogFile();
                Ui(() =>
                {
                    try
                    {
                        var bytes = TryGetLogFileSizeBytes();
                        UpdateLogFileSizeLabelFromBytes(bytes);

                        if (string.IsNullOrEmpty(content))
                        {
                            LogText.Text = "(no logs)";
                        }
                        else
                        {
                            LogText.Text = content;
                        }

                        ScrollLogToEnd(force: false);
                    }
                    catch { }
                });
            }
            catch
            {
                Ui(() =>
                {
                    try
                    {
                        UpdateLogFileSizeLabelFromBytes(0);
                        LogText.Text = "(unable to read logs)";
                        ScrollLogToEnd(force: false);
                    }
                    catch { }
                });
            }
        }
        private void ServerLLM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                var cb = sender as ComboBox;
                var selected = cb?.SelectedItem as string ?? cb?.SelectedItem?.ToString() ?? string.Empty;
                AppendLog($"[VERBOSE] ServerLLM selection changed -> {selected}");
            }
            catch { }
        }
    }
}