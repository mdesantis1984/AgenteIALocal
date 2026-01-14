using AgenteIALocalVSIX.Execution;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AgenteIALocal.Core.Models.Agent;
using AgenteIALocalVSIX.Chats;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        // DefaultRunExecutor moved to separate file but kept as nested class for private access.
        internal sealed class DefaultRunExecutor : IRunExecutor
        {
            private readonly AgenteIALocalControl _owner;

            public DefaultRunExecutor(AgenteIALocalControl owner)
            {
                _owner = owner;
            }

            public void RequestStop()
            {
                var o = _owner;
                try { _ = Interlocked.Increment(ref o._runVersion); } catch { }

                try { o._runCts?.Cancel(); } catch { }
                try { o._runCts?.Dispose(); } catch { }
                o._runCts = null;

                o.AppendLog("Stop requested.");
                o.activeCorrelationId = null;

                o.Ui(() =>
                {
                    o.UpdateUiState(ExecutionState.Idle);
                });
            }

            public async Task RunAsync(object sender, RoutedEventArgs e)
            {
                var o = _owner;
                if (o.CurrentExecutionState == ExecutionState.Running) return;

                try { o._runCts?.Cancel(); } catch { }
                try { o._runCts?.Dispose(); } catch { }

                o._runCts = new CancellationTokenSource();
                var ct = o._runCts.Token;
                var myVersion = Interlocked.Increment(ref o._runVersion);

                o.activeCorrelationId = Guid.NewGuid().ToString("N");
                var myCorrelationId = o.activeCorrelationId;

                o.AppendLog("Run clicked.");
                o.UpdateUiState(ExecutionState.Running);
                o.AppendLog("Execution started.");

                try
                {
                    AgentComposition.EnsureComposition();

                    var userInput = o.PromptTextBox.Text ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        return;
                    }

                    o._chatService.EnsureActiveChatExists();

                    try
                    {
                        lock (o._uiStateGate)
                        {
                            if (o._uiState == null) o._uiState = new UiState();
                            o._uiState.LastChatId = o.activeChat != null ? o.activeChat.Id : null;
                        }
                        o.SaveUiStateSafe();
                    }
                    catch { }

                    try
                    {
                        if (o.activeChat != null)
                        {
                            var title = o.activeChat.Title ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(title) || string.Equals(title.Trim(), "New chat", StringComparison.OrdinalIgnoreCase))
                            {
                                o.activeChat.Title = TruncateWithEllipsis(userInput, 200);
                                o._chatService.TryPersistChat(o.activeChat);
                                o._chatService.RefreshChatCombo();
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        o._chatService.AddChatMessage(o.activeChat, "Tú", userInput, null);
                        o._chatService.RenderActiveChatToUi();
                    }
                    catch { }

                    ChatMessage aiBubble = null;
                    DateTime aiBubbleTsUtc = DateTime.UtcNow;
                    try
                    {
                        o._chatService.AddChatMessage(o.activeChat, "IA", string.Empty, null);
                        aiBubble = o.activeChat != null && o.activeChat.Messages != null ? o.activeChat.Messages.LastOrDefault() : null;
                        var ts = o._chatService.TryGetDateTimeProp(aiBubble, "Timestamp");
                        aiBubbleTsUtc = ts;
                    }
                    catch
                    {
                        // ignore (fallback path will still render ResponseJsonText)
                    }



                    var req = new AgentHostRequest
                    {
                        RequestId = myCorrelationId,
                        CorrelationId = myCorrelationId,
                        Action = userInput,
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        SolutionName = o.SolutionNameText.Text ?? string.Empty,
                        ProjectCount = int.TryParse(o.ProjectCountText.Text, out var pc) ? pc : 0
                    };

                    AgentHostResponse response = null;

                    AgenteIALocalVSIX.ServerConfig lmServer = null;
                    var canStreamLmStudio = o.TryGetActiveLmStudioServer(out lmServer);

                    if (canStreamLmStudio)
                    {
                        o._streamingAiMessage = aiBubble;
                        o._streamingAiRun = null;
                        o._streamingAiViewer = null;
                        o._chatService.RenderActiveChatToUi();
                        response = await o.ExecuteLmStudioStreamingAsync(req, lmServer, o.activeChat, aiBubble, ct);
                    }
                    else
                    {
                        o._streamingAiMessage = null;
                        o._streamingAiRun = null;
                        o._streamingAiViewer = null;
                        o._chatService.RenderActiveChatToUi();
                        var execTask = Task.Run(() =>
                        {
                            try
                            {
                                if (AgentComposition.AgentService != null)
                                {
                                    return AgentComposition.AgentService.Execute(req);
                                }

                                o.AppendLog("AgentService not composed; using MockAgentExecutor fallback.");
                                return AgenteIALocalVSIX.Execution.MockAgentExecutor.Execute(req);
                            }
                            catch (Exception ex)
                            {
                                var corr = !string.IsNullOrEmpty(req?.CorrelationId) ? req.CorrelationId : !string.IsNullOrEmpty(req?.RequestId) ? req.RequestId : "-";
                                try
                                {
                                    AgentComposition.Error(corr, AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Execution exception in background task: " + ex.Message, ex);
                                }
                                catch { }

                                o.AppendLog("Execution exception in background task: " + ex.Message);
                                throw;
                            }
                        });

                        var completed = await Task.WhenAny(execTask, Task.Delay(Timeout.Infinite, ct));
                        if (completed != execTask)
                        {
                            _ = execTask.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                            o._chatService.TryRemoveEmptyAiBubble(o.activeChat, aiBubble);
                            return;
                        }

                        response = await execTask;
                    }

                    if (ct.IsCancellationRequested) { o._chatService.TryRemoveEmptyAiBubble(o.activeChat, aiBubble); return; }
                    if (myVersion != o._runVersion) { o._chatService.TryRemoveEmptyAiBubble(o.activeChat, aiBubble); return; }
                    if (!string.Equals(o.activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) { o._chatService.TryRemoveEmptyAiBubble(o.activeChat, aiBubble); return; }

                    string display;
                    if (response == null) display = "(no response)";
                    else if (!string.IsNullOrEmpty(response.Output)) display = response.Output;
                    else if (!string.IsNullOrEmpty(response.Error)) display = "Error: " + response.Error;
                    else display = "(empty response)";

                    o.Ui(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        if (myVersion != o._runVersion) return;
                        if (!string.Equals(o.activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) return;

                        try
                        {
                            o.AppendLog("[VERBOSE] RenderResponse: start; len=" + (display?.Length ?? 0));

                            var ok200 = false;
                            try { ok200 = TryIsHttp200(response); } catch { }

                            try
                            {
                                TryExtractUsageTokens(response, out var promptTokens, out var completionTokens, out var totalTokens);

                                if (promptTokens.HasValue)
                                {
                                    o._chatService.TryUpdateLastUserBubbleTokens(o.activeChat, promptTokens.Value);
                                }

                                var aiTokens = completionTokens ?? totalTokens;

                                try
                                {
                                    if (aiBubble != null)
                                    {
                                        aiBubble.Content = display;

                                        if (aiTokens.HasValue)
                                        {
                                            try { aiBubble.Tokens = aiTokens.Value; } catch { }
                                            o._chatService.TrySetTokensForMessage(o.activeChat.Id, aiBubbleTsUtc, "IA", display, aiTokens.Value);
                                        }

                                        o._chatService.TryPersistChat(o.activeChat);
                                    }
                                    else
                                    {
                                        o._chatService.AddChatMessage(o.activeChat, "IA", display, aiTokens);
                                    }
                                }
                                catch { }

                                o._chatService.RenderActiveChatToUi();
                            }
                            catch
                            {
                                try
                                {
                                    o.ResponseJsonText.Document = o.RenderResponseToDocument(display);
                                    o.ScrollResponseToEnd();
                                }
                                catch { }
                            }

                            if (ok200)
                            {
                                try
                                {
                                    o.PromptTextBox.Text = string.Empty;
                                    o.PromptTextBox.Focus();
                                }
                                catch { }
                            }
                            o.AppendLog("[VERBOSE] RenderResponse: done; len=" + (display?.Length ?? 0));
                        }
                        catch (Exception exRender)
                        {
                            o.AppendLog("[VERBOSE] RenderResponse failed: " + exRender.Message);
                            o.ResponseJsonText.Document = CreatePlainDocument(display ?? string.Empty);
                        }

                        o.UpdateUiState(ExecutionState.Completed);
                        o.AppendLog("Execution completed successfully.");

                        o.activeCorrelationId = null;

                        try { o._runCts?.Dispose(); } catch { }
                        o._runCts = null;

                        try { o.RefreshLogFromFile(); } catch { }
                    });
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested) return;
                    if (myVersion != o._runVersion) return;
                    if (!string.Equals(o.activeCorrelationId, myCorrelationId, StringComparison.Ordinal)) return;

                    o.Ui(() =>
                    {
                        o.UpdateUiState(ExecutionState.Error);
                        o.AppendLog("Execution failed: " + ex.Message);

                        try
                        {
                            AgentComposition.Error(o.activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[AgenteIALocalControl] Execution failed: " + ex.Message, ex);
                        }
                        catch { }

                        try
                        {
                            o.ResponseJsonText.Document = o.RenderResponseToDocument("{ \"error\": \"Execution failed\" }");
                        }
                        catch
                        {
                            o.ResponseJsonText.Document = CreatePlainDocument("{ \"error\": \"Execution failed\" }");
                        }

                        o.activeCorrelationId = null;

                        try { o._runCts?.Dispose(); } catch { }
                        o._runCts = null;

                        try { o.RefreshLogFromFile(); } catch { }
                    });
                }
            }
        }
    }
}
