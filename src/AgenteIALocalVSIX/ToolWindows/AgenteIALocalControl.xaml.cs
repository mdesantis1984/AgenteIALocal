using System.Windows.Controls;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

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

        public AgenteIALocalControl()
        {
            InitializeComponent();

            // Set DataContext for XAML bindings
            this.DataContext = this;

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
            }
            catch { }
        }

        private void PopulateSettingsPanel(AgentSettings settings)
        {
            if (settings == null) return;

            ActiveServerIdTextBox.Text = settings.ActiveServerId ?? string.Empty;

            // find active server details
            if (!string.IsNullOrEmpty(settings.ActiveServerId) && settings.Servers != null)
            {
                var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                if (srv != null)
                {
                    ServerBaseUrlTextBox.Text = srv.BaseUrl ?? string.Empty;
                    ServerModelTextBox.Text = srv.Model ?? string.Empty;
                    ServerApiKeyTextBox.Text = srv.ApiKey ?? string.Empty;
                }
            }
            else if (settings.Servers != null && settings.Servers.Count > 0)
            {
                var srv = settings.Servers[0];
                ServerBaseUrlTextBox.Text = srv.BaseUrl ?? string.Empty;
                ServerModelTextBox.Text = srv.Model ?? string.Empty;
                ServerApiKeyTextBox.Text = srv.ApiKey ?? string.Empty;
                ActiveServerIdTextBox.Text = srv.Id ?? string.Empty;
            }
        }

        private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == System.Windows.Visibility.Visible ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        private void CloseSettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SettingsPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void SaveSettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null) settings = new AgentSettings();

                // update active server id
                settings.ActiveServerId = ActiveServerIdTextBox.Text ?? string.Empty;

                // ensure server exists or update existing
                if (settings.Servers == null) settings.Servers = new System.Collections.Generic.List<ServerConfig>();

                var srv = settings.Servers.Find(s => s.Id == settings.ActiveServerId);
                if (srv == null)
                {
                    srv = new ServerConfig { Id = settings.ActiveServerId, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = ServerBaseUrlTextBox.Text ?? string.Empty;
                srv.Model = ServerModelTextBox.Text ?? string.Empty;
                srv.ApiKey = ServerApiKeyTextBox.Text ?? string.Empty;

                AgentSettingsStore.Save(settings);

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
            logRefreshCts?.Cancel();
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
                logRefreshCts?.Cancel();
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

            // Keep previous enable/disable rules
            RunButton.IsEnabled = CurrentExecutionState == ExecutionState.Idle || CurrentExecutionState == ExecutionState.Completed || CurrentExecutionState == ExecutionState.Error;
            ClearButton.IsEnabled = CurrentExecutionState != ExecutionState.Running;
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
                var userInput = RequestJsonText.Text ?? string.Empty;

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
                    ResponseJsonText.Text = display;
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

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
