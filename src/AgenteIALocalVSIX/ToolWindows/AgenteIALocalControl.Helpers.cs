// NUEVO ARCHIVO AgenteIALocalControl.Helpers.cs - ID: 20250110_000006
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        // Raise property changed helper to avoid name collisions
        private void RaisePropertyChanged(string propertyName)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            Ui(() =>
            {
                try
                {
                    if (activeChat == null)
                    {
                        ResponseJsonText.Document = CreatePlainDocument(string.Empty);
                        return;
                    }
                    ResponseJsonText.ScrollToEnd();
                }
                catch { }
            });
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

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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

                if (force || !_logScrollInitialized)
                {
                    _logScrollInitialized = true;
                    LogText.ScrollToEnd();
                }
                else
                {
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollResponseToEnd();
        }

        private static bool TryIsHttp200(object response)
        {
            try
            {
                if (response == null) return false;

                try
                {
                    var pErr = response.GetType().GetProperty("Error", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (pErr != null)
                    {
                        var err = pErr.GetValue(response, null)?.ToString();
                        if (!string.IsNullOrWhiteSpace(err)) return false;
                    }
                }
                catch { }

                foreach (var propName in new[] { "StatusCode", "HttpStatusCode", "Code", "HttpCode", "ResponseCode", "ResultCode" })
                {
                    var p = response.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (p == null) continue;

                    var v = p.GetValue(response, null);
                    if (v is int i) return i == 200;
                    if (v is System.Net.HttpStatusCode h) return (int)h == 200;
                    if (v != null && int.TryParse(v.ToString(), out var pi)) return pi == 200;
                }

                foreach (var propName in new[] { "Status", "ReasonPhrase", "Message", "Result", "State" })
                {
                    var p = response.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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

                foreach (var propName in new[] { "Ok", "Success", "IsSuccess", "Succeeded", "IsSuccessStatusCode" })
                {
                    var p = response.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (p == null) continue;

                    var v = p.GetValue(response, null);
                    if (v is bool b) return b;
                    if (v != null && bool.TryParse(v.ToString(), out var pb)) return pb;
                }
            }
            catch { }

            return false;
        }

        private static string TryReadStringProp(object obj, params string[] propNames)
        {
            try
            {
                if (obj == null) return null;
                if (propNames == null || propNames.Length == 0) return null;

                foreach (var name in propNames)
                {
                    var p = obj.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (p == null) continue;
                    var v = p.GetValue(obj, null);
                    if (v is string s) return s;
                    if (v != null) return v.ToString();
                }
            }
            catch { }
            return null;
        }

        private static int? TryReadIntProp(object obj, params string[] propNames)
        {
            try
            {
                if (obj == null) return null;
                if (propNames == null || propNames.Length == 0) return null;

                foreach (var name in propNames)
                {
                    var p = obj.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (p == null) continue;
                    var v = p.GetValue(obj, null);
                    if (v is int i) return i;
                    if (v is long l)
                    {
                        if (l > int.MaxValue) return int.MaxValue;
                        if (l < int.MinValue) return int.MinValue;
                        return (int)l;
                    }
                    if (v != null && int.TryParse(v.ToString(), out var pi)) return pi;
                }
            }
            catch { }
            return null;
        }

        private static void TryExtractUsageTokens(object response, out int? promptTokens, out int? completionTokens, out int? totalTokens)
        {
            promptTokens = null;
            completionTokens = null;
            totalTokens = null;

            try
            {
                if (response != null)
                {
                    promptTokens = TryReadIntProp(response, "PromptTokens", "prompt_tokens", "PromptTokenCount");
                    completionTokens = TryReadIntProp(response, "CompletionTokens", "completion_tokens", "CompletionTokenCount");
                    totalTokens = TryReadIntProp(response, "TotalTokens", "total_tokens", "Tokens", "TokensUsed", "TotalTokenCount", "TokenCount");
                    if (promptTokens.HasValue || completionTokens.HasValue || totalTokens.HasValue) return;
                }
            }
            catch { }

            try
            {
                if (response != null)
                {
                    string raw = null;
                    foreach (var propName in new[] { "RawResponse", "RawResponseJson", "RawJson", "ResponseJson", "ResponseText", "Body", "Json", "Payload", "Raw" })
                    {
                        var p = response.GetType().GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (p == null) continue;
                        var v = p.GetValue(response, null);
                        if (v is string s && !string.IsNullOrWhiteSpace(s)) { raw = s; break; }
                    }

                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        TryExtractUsageTokensFromJson(raw, out promptTokens, out completionTokens, out totalTokens);
                        if (promptTokens.HasValue || completionTokens.HasValue || totalTokens.HasValue) return;
                    }
                }
            }
            catch { }

            try
            {
                if (response != null)
                {
                    var outStr = TryReadStringProp(response, "Output", "Body", "Json", "Response");
                    if (!string.IsNullOrWhiteSpace(outStr))
                    {
                        TryExtractUsageTokensFromJson(outStr, out promptTokens, out completionTokens, out totalTokens);
                    }
                }
            }
            catch { }
        }

        private static void TryExtractUsageTokensFromJson(string json, out int? promptTokens, out int? completionTokens, out int? totalTokens)
        {
            promptTokens = null;
            completionTokens = null;
            totalTokens = null;

            try
            {
                if (string.IsNullOrWhiteSpace(json)) return;
                var trimmed = json.Trim();
                if (!(trimmed.StartsWith("{") || trimmed.StartsWith("["))) return;

                var tok = Newtonsoft.Json.Linq.JToken.Parse(trimmed);
                var usage = tok["usage"] ?? tok.SelectToken("usage");
                if (usage == null) return;

                var p = usage["prompt_tokens"] ?? usage["promptTokens"] ?? usage["PromptTokens"];
                var c = usage["completion_tokens"] ?? usage["completionTokens"] ?? usage["CompletionTokens"];
                var t = usage["total_tokens"] ?? usage["totalTokens"] ?? usage["TotalTokens"];

                if (p != null && int.TryParse(p.ToString(), out var pi)) promptTokens = pi;
                if (c != null && int.TryParse(c.ToString(), out var ci)) completionTokens = ci;
                if (t != null && int.TryParse(t.ToString(), out var ti)) totalTokens = ti;
            }
            catch { }
        }

        private T GetElement<T>(string name) where T : FrameworkElement
        {
            try
            {
                return this.FindName(name) as T;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshLogButton_Click(object sender, RoutedEventArgs e)
        {
            try { RefreshLogFromFile(); } catch { }
        }
        // NUEVO METODO SettingsButton_Click - ID: 20260114_000050
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var w = new AgenteIALocalConfigWindow();

                try
                {
                    w.BaseUrlHealthChanged += async (ok, baseUrl, models) =>
                    {
                        try
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            ApplyConfigHealthFromModal(ok, models);
                        }
                        catch { }
                    };
                }
                catch { }

                try
                {
                    var owner = Window.GetWindow(this);
                    if (owner != null)
                    {
                        w.Owner = owner;
                    }
                }
                catch { }

                w.ShowDialog();
            }
            catch (Exception ex)
            {
                if (!_settingsOpenErrorLogged)
                {
                    _settingsOpenErrorLogged = true;
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "Failed to open config window", ex); } catch { }
                }
            }
        }

        private void CopyLogAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var txt = LogText?.Text ?? string.Empty;
                Clipboard.SetText(txt);
            }
            catch { }
        }
    }
}
