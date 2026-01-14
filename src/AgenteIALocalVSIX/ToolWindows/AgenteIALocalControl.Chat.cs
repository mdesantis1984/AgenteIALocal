// NUEVO ARCHIVO AgenteIALocalControl.Chat.cs - Chat y estado de UI
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AgenteIALocalVSIX.Chats;
using MaterialDesignThemes.Wpf;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        // UI state persisted locally (last chat + token usage map)
        private sealed class UiState
        {
            public string LastChatId { get; set; }
            public System.Collections.Generic.Dictionary<string, int> MessageTokens { get; set; } = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        }

        private readonly object _uiStateGate = new object();
        private UiState _uiState = new UiState();
        private bool _isSyncingChatCombo;
        private ChatMessage _streamingAiMessage;
        private Run _streamingAiRun;
        private FlowDocumentScrollViewer _streamingAiViewer;

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
                    var st = Newtonsoft.Json.JsonConvert.DeserializeObject<UiState>(json);
                    _uiState = st ?? new UiState();
                    if (_uiState.MessageTokens == null)
                    {
                        _uiState.MessageTokens = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
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
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(_uiState, Newtonsoft.Json.Formatting.Indented);
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
                var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
                using (var md5 = MD5.Create())
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
                    if (_uiState.MessageTokens == null) _uiState.MessageTokens = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
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
                    t.GetMethod("Save", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ??
                    t.GetMethod("Update", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ??
                    t.GetMethod("Upsert", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

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
                    TrySetProp(msg, "Tokens", tokens.Value);
                    TrySetProp(msg, "TokenCount", tokens.Value);
                    TrySetProp(msg, "TotalTokens", tokens.Value);

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
                var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
                var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
                var p = obj.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (p == null) return DateTime.UtcNow;
                var v = p.GetValue(obj, null);
                if (v is DateTime dt) return dt;
                if (v is DateTimeOffset dto) return dto.UtcDateTime;
                if (v != null && DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
            }
            catch { }
            return DateTime.UtcNow;
        }

        private void TryUpdateLastUserBubbleTokens(ChatSession chat, int tokens)
        {
            try
            {
                if (chat == null || chat.Messages == null) return;
                var list = chat.Messages as System.Collections.IList;
                if (list == null || list.Count == 0) return;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var m = list[i];
                    if (m == null) continue;

                    var sender = TryGetStringProp(m, "Sender");
                    var sLower = (sender ?? string.Empty).Trim().ToLowerInvariant();
                    var isUser = (sLower == "tú" || sLower == "tu" || sLower == "user" || sLower == "you");
                    if (!isUser) continue;

                    var content = TryGetStringProp(m, "Content") ?? string.Empty;
                    var ts = TryGetDateTimeProp(m, "Timestamp");

                    TrySetProp(m, "Tokens", tokens);
                    TrySetProp(m, "TokenCount", tokens);
                    TrySetProp(m, "PromptTokens", tokens);
                    TrySetTokensForMessage(chat.Id, ts, sender ?? string.Empty, content, tokens);
                    TryPersistChat(chat);
                    return;
                }
            }
            catch { }
        }

        // NUEVO METODO ClearStreamingPlaceholder - ID: 20260114_000006
        private void ClearStreamingPlaceholder()
        {
            _streamingAiMessage = null;
            _streamingAiRun = null;
            _streamingAiViewer = null;
        }

        // NUEVO METODO RepairMissingMessageTokensFromUiState - ID: 20260114_000012
        private void RepairMissingMessageTokensFromUiState(ChatSession chat)
        {
            if (chat == null || chat.Messages == null) return;
            bool didRepair = false;
            try
            {
                for (int i = 0; i < chat.Messages.Count; i++)
                {
                    var m = chat.Messages[i];
                    if (m == null) continue;

                    // check if message already has Tokens
                    var hasTokens = false;
                    try
                    {
                        var p = m.GetType().GetProperty("Tokens", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (p != null)
                        {
                            var v = p.GetValue(m, null);
                            if (v != null) hasTokens = true;
                        }
                    }
                    catch { }

                    if (hasTokens) continue;

                    // compute key from chatId, timestamp, sender, content
                    try
                    {
                        var ts = TryGetDateTimeProp(m, "Timestamp");
                        var sender = TryGetStringProp(m, "Sender");
                        var content = TryGetStringProp(m, "Content");
                        var key = ComputeMessageTokenKey(chat.Id, ts, sender ?? string.Empty, content ?? string.Empty);

                        bool filled = false;
                        lock (_uiStateGate)
                        {
                            if (_uiState != null && _uiState.MessageTokens != null && _uiState.MessageTokens.TryGetValue(key, out var tval))
                            {
                                TrySetProp(m, "Tokens", tval);
                                filled = true;
                                didRepair = true;
                            }
                        }

                        // If ui_state had no entry and this is a completed AI message with non-empty content,
                        // set Tokens = 0 as a safe default (migration of legacy nulls).
                        if (!filled)
                        {
                            try
                            {
                                var sLower = (sender ?? string.Empty).Trim().ToLowerInvariant();
                                var isAi = (sLower == "ia" || sLower == "ai" || sLower == "assistant" || sLower == "system" || sLower == "robot");
                                if (isAi && !string.IsNullOrWhiteSpace(content))
                                {
                                    TrySetProp(m, "Tokens", 0);
                                    didRepair = true;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            if (didRepair)
            {
                try { TryPersistChat(chat); } catch { }
            }
        }

        private void RenderActiveChatToUi()
        {
            Ui(() =>
            {
                try
                {
                    if (activeChat == null)
                    {
                        ResponseJsonText.Document = CreatePlainDocument(string.Empty);
                        ScrollResponseToEnd();
                        return;
                    }

                    ResponseJsonText.Document = RenderChatSessionToDocument(activeChat);
                    ScrollResponseToEnd();
                }
                catch
                {
                    // ignore
                }
            });
        }

        private FlowDocument RenderChatSessionToDocument(ChatSession chat)
        {
            var fd = new FlowDocument { PagePadding = new Thickness(0) };
            try
            {
                if (chat == null || chat.Messages == null) return fd;

                _streamingAiRun = null;
                _streamingAiViewer = null;

                // NUEVO METODO: Repair missing Tokens in messages from UiState cache (one-shot per render)
                try
                {
                    RepairMissingMessageTokensFromUiState(chat);
                }
                catch { }

                foreach (var m in chat.Messages)
                {
                    var sender = TryGetStringProp(m, "Sender");
                    var content = TryGetStringProp(m, "Content");
                    var ts = TryGetDateTimeProp(m, "Timestamp");

                    var isUser = false;
                    var sLower = (sender ?? string.Empty).Trim().ToLowerInvariant();
                    if (sLower == "tú" || sLower == "tu" || sLower == "user" || sLower == "you") isUser = true;

                    int? tokens = null;
                    try
                    {
                        tokens = TryGetTokensForMessage(chat.Id, ts, sender ?? string.Empty, content ?? string.Empty);
                    }
                    catch { }

                    if (!tokens.HasValue)
                    {
                        try
                        {
                            tokens = TryReadIntProp(m, "Tokens", "TokenCount", "TotalTokens", "PromptTokens", "CompletionTokens");
                            if (tokens.HasValue && tokens.Value >= 0)
                            {
                                TrySetTokensForMessage(chat.Id, ts, sender ?? string.Empty, content ?? string.Empty, tokens.Value);
                            }
                        }
                        catch { }
                    }

                    var isStreamingAi = ReferenceEquals(m, _streamingAiMessage);

                    fd.Blocks.Add(CreateBubbleBlock(isUser, content ?? string.Empty, ts, tokens, isStreamingAi));
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

        private BlockUIContainer CreateBubbleBlock(bool isUser, string text, DateTime tsUtc, int? tokens, bool isStreamingAi = false)
        {
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

            FlowDocument doc;
            if (isStreamingAi && !isUser)
            {
                doc = new FlowDocument { PagePadding = new Thickness(0) };
                var p = new Paragraph { Margin = new Thickness(0) };
                var run = new Run(text ?? string.Empty);
                p.Inlines.Add(run);
                doc.Blocks.Add(p);

                _streamingAiRun = run;
            }
            else
            {
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

            if (isStreamingAi && !isUser)
            {
                _streamingAiViewer = viewer;
            }

            viewer.Foreground = textBrush;
            viewer.PreviewMouseWheel += BubbleViewer_PreviewMouseWheel;

            stack.Children.Add(viewer);
            try
            {
                DateTime tsLocal;
                if (tsUtc.Kind == DateTimeKind.Utc) tsLocal = tsUtc.ToLocalTime();
                else if (tsUtc.Kind == DateTimeKind.Local) tsLocal = tsUtc;
                else tsLocal = DateTime.SpecifyKind(tsUtc, DateTimeKind.Local);

                var dtText = tsLocal.ToString("g", System.Globalization.CultureInfo.CurrentCulture);
                string tokText;
                if (tokens.HasValue && tokens.Value >= 0)
                {
                    tokText = tokens.Value.ToString();
                }
                else
                {
                    // If streaming in progress, show '-' to indicate partial content
                    if (isStreamingAi)
                    {
                        tokText = "-";
                    }
                    else
                    {
                        // Completed AI message with no usage info -> show 0 (don't leave as '-')
                        var sLower = (TryGetStringProp(null, "") ?? string.Empty);
                        tokText = "0";
                    }
                }

                var meta = new TextBlock
                {
                    Text = dtText + " · Tokens: " + tokText,
                    Margin = new Thickness(0, 8, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = boneBrush,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Opacity = 1.0
                };

                stack.Children.Add(meta);
            }
            catch { }
            bubble.Child = stack;

            if (!isUser)
            {
                Grid.SetColumn(icon, 0);
                Grid.SetColumn(bubble, 1);
                outer.Children.Add(icon);
                outer.Children.Add(bubble);
            }
            else
            {
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

                activeChat = c;

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
    }
}
