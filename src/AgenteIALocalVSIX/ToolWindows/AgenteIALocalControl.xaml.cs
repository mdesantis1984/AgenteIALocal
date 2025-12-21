using System.Windows.Controls;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl : UserControl
    {
        private enum UiState { Idle, Running, Completed, Error }
        private UiState state = UiState.Idle;

        public AgenteIALocalControl()
        {
            InitializeComponent();
            UpdateUiState(UiState.Idle);

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

            // Load current log file content into the Log tab
            RefreshLogFromFile();
        }

        public void SetSolutionInfo(string solutionName, int projectCount)
        {
            SolutionNameText.Text = solutionName;
            ProjectCountText.Text = projectCount.ToString();

            // Prepare initial request but do not execute
            var req = BuildRequest(solutionName, projectCount);
            RequestJsonText.Text = SerializeToJson(req);
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

        private void UpdateUiState(UiState newState)
        {
            state = newState;
            StateText.Text = state.ToString();
            RunButton.IsEnabled = state == UiState.Idle || state == UiState.Completed || state == UiState.Error;
            ClearButton.IsEnabled = state != UiState.Running;
        }

        private async void RunButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (state == UiState.Running) return;

            Log("Run clicked.");
            UpdateUiState(UiState.Running);
            Log("Execution started.");

            try
            {
                // Ensure composition is available before attempting execution
                AgentComposition.EnsureComposition();

                var req = JsonConvert.DeserializeObject<CopilotRequest>(RequestJsonText.Text);
                if (req == null)
                {
                    // Build minimal request if parsing failed
                    req = BuildRequest(SolutionNameText.Text ?? string.Empty, int.TryParse(ProjectCountText.Text, out var pc) ? pc : 0);
                }

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

                var respJson = SerializeToJson(response);

                // Update UI on UI thread
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResponseJsonText.Text = respJson;
                    UpdateUiState(UiState.Completed);
                    Log("Execution completed successfully.");

                    // Refresh log tab to show newly appended entries
                    RefreshLogFromFile();
                }));
            }
            catch (Exception ex)
            {
                UpdateUiState(UiState.Error);
                Log("Execution failed: " + ex.Message);
                Trace.TraceError("[AgenteIALocalControl] Execution failed: " + ex);
                ResponseJsonText.Text = "{ \"error\": \"Execution failed\" }";

                // Attempt to refresh log view even on error
                try { RefreshLogFromFile(); } catch { }
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RequestJsonText.Text = string.Empty;
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

            UpdateUiState(UiState.Idle);
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
                // never throw from logger
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


    }
}
