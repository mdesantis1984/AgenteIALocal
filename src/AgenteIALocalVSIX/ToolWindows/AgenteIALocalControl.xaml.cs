using System.Windows.Controls;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Core.Settings;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;
using System.IO;
using System.Windows;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl : UserControl
    {
        private enum UiState { Idle, Running, Completed, Error }
        private UiState state = UiState.Idle;

        private IAgentService agentService;

        private IAgentSettingsProvider settingsProvider;

        public AgenteIALocalControl(IAgentSettingsProvider settingsProvider)
    : this()
        {
            // Constructor requerido por ToolWindow lifecycle
        }


        public AgenteIALocalControl()
        {
            InitializeComponent();
            UpdateUiState(UiState.Idle);

            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: ctor");
            }
            catch { }

            try
            {
                this.Loaded += AgenteIALocalControl_Loaded;
            }
            catch { }

            try
            {
                agentService = AgentComposition.AgentService;
                AgentComposition.Logger?.Info("ToolWindowControl: AgentService=" + (agentService == null ? "null" : agentService.GetType().FullName));
            }
            catch (Exception ex)
            {
                agentService = null;
                try { AgentComposition.Logger?.Error("ToolWindowControl: AgentService read failed", ex); } catch { }
            }

            // Centralized decision: evaluate and display exactly one clear message for current configuration state
            EvaluateAndDisplayStatus();
        }

        public void AttachSettingsProvider(IAgentSettingsProvider provider)
        {
            settingsProvider = provider;

            try
            {
                AgentComposition.Logger?.Info(
                    "ToolWindowControl: SettingsProvider attached (" +
                    (provider == null ? "null" : provider.GetType().Name) + ")");
            }
            catch { }
        }

        private void AgenteIALocalControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: Loaded");
            }
            catch { }

            // Re-evaluate status when control is loaded in case options changed
            EvaluateAndDisplayStatus();
        }

        // Tab selection changed - when Log tab is selected, load the log file content
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MainTabControl.SelectedItem is TabItem ti && ti.Header != null && ti.Header.ToString() == "Log")
                {
                    LoadLogFile();
                }
            }
            catch { }
        }

        private void LoadLogFile()
        {
            var path = GetLogFilePath();
            LogPathText.Text = path ?? "(sin ruta)";

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                LogViewerTextBox.Text = "Log vacío / no encontrado";
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
                return;
            }

            try
            {
                var text = File.ReadAllText(path);
                LogViewerTextBox.Text = string.IsNullOrEmpty(text) ? "Log vacío / no encontrado" : text;

                var fi = new FileInfo(path);
                LogSizeText.Text = FormatSize(fi.Length);

                CopyLogButton.IsEnabled = true;
                DeleteLogButton.IsEnabled = true;
            }
            catch
            {
                LogViewerTextBox.Text = "No se pudo leer el archivo de log.";
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
            }
        }

        private string GetLogFilePath()
        {
            try
            {
                // Replicate the same path logic used by FileAgentLogger to avoid hardcoding a different path
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? string.Empty;
                var dir = Path.Combine(baseDir, "AgenteIALocal", "logs");
                var logFilePath = Path.Combine(dir, "AgenteIALocal.log");
                return logFilePath;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatSize(long bytes)
        {
            try
            {
                if (bytes < 1024) return bytes + " B";
                double kb = bytes / 1024.0;
                if (kb < 1024) return Math.Round(kb, 1) + " KB";
                double mb = kb / 1024.0;
                return Math.Round(mb, 2) + " MB";
            }
            catch
            {
                return "0 KB";
            }
        }

        private void CopyLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var text = LogViewerTextBox.Text ?? string.Empty;
                if (string.IsNullOrEmpty(text)) return;
                Clipboard.SetText(text);
                try { AgentComposition.Logger?.Info("ToolWindowControl: Log copied to clipboard"); } catch { }
            }
            catch
            {
                // fail-safe: do not show raw exceptions
            }
        }

        private void DeleteLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var path = GetLogFilePath();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    try { AgentComposition.Logger?.Info("ToolWindowControl: Log file deleted: " + path); } catch { }
                }

                // Update UI
                LogViewerTextBox.Text = string.Empty;
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
            }
            catch
            {
                // If deletion failed, keep UI consistent but do not show exception details
                LogViewerTextBox.Text = "No se pudo borrar el archivo de log.";
            }
        }

        public void EvaluateAndDisplayStatus()
        {
            // Default reset
            ErrorText.Text = string.Empty;
            ErrorText.Foreground = Brushes.Red;

            // State 3 — Backend no disponible
            if (agentService == null)
            {
                StateText.Text = "Backend: n/a";
                ErrorText.Text = "Backend no disponible. Verificá la configuración o el proveedor.";
                RunButton.IsEnabled = false;
                return;
            }

            AgentSettings settings = null;
            try
            {
                if (settingsProvider != null)
                {
                    settings = settingsProvider.Load();
                }
                else
                {
                    var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(AgenteIALocalVSIXPackage)) as AgenteIALocalVSIXPackage;
                    if (pkg?.AgentSettingsProvider != null)
                    {
                        settings = pkg.AgentSettingsProvider.Load();
                    }
                }
            }
            catch
            {
                settings = null;
            }

            bool hasProvider;
            bool hasBaseUrl;
            bool hasModel;
            ValidateSettings(settings, out hasProvider, out hasBaseUrl, out hasModel);

            if (hasProvider && hasBaseUrl && hasModel)
            {
                // Estado 1 — Configurado
                StateText.Text = "Configuración: OK";
                ErrorText.Foreground = Brushes.Green;
                ErrorText.Text = "Configuración OK. Listo para enviar mensajes.";
                RunButton.IsEnabled = true;
                return;
            }

            // Estado 2 — Configuración incompleta
            StateText.Text = "Configuración: incompleta";
            ErrorText.Foreground = Brushes.Orange;
            ErrorText.Text = "Configuración incompleta. Revisá las Opciones.";
            RunButton.IsEnabled = false;
        }

        private static void ValidateSettings(AgentSettings settings, out bool hasProvider, out bool hasBaseUrl, out bool hasModel)
        {
            hasProvider = false;
            hasBaseUrl = false;
            hasModel = false;

            if (settings == null)
            {
                return;
            }

            hasProvider = true;

            try
            {
                if (settings.Provider == AgentProviderType.JanServer)
                {
                    // JanServer: validate ONLY JanServer.BaseUrl; do NOT require model
                    hasBaseUrl = !string.IsNullOrWhiteSpace(settings.JanServer?.BaseUrl);
                    hasModel = true;
                    return;
                }

                // LmStudio: validate ONLY LM Studio settings; completely ignore JanServer
                hasBaseUrl = !string.IsNullOrWhiteSpace(settings.LmStudio?.BaseUrl);
                hasModel = !string.IsNullOrWhiteSpace(settings.LmStudio?.Model);
            }
            catch
            {
                hasBaseUrl = false;
                hasModel = false;
            }
        }

        private static Package GetGlobalVsixPackage()
        {
            try
            {
                // Save/Load uses the same VS settings store, which requires a sited package.
                // We get our AsyncPackage instance from the global service provider.
                return Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(AgenteIALocalVSIXPackage)) as Package;
            }
            catch
            {
                return null;
            }
        }

        public void SetSolutionInfo(string solutionName, int projectCount)
        {
            SolutionNameText.Text = solutionName;
            ProjectCountText.Text = projectCount.ToString();

            // Keep existing request generation (not modifying contracts)
            var req = BuildRequest(solutionName, projectCount);
            PromptTextBox.Text = req.Action + " - " + req.SolutionName;
            ResponseTextBox.Text = string.Empty;
            LogText.Text = "";

            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: SetSolutionInfo solution='" + (solutionName ?? string.Empty) + "' projects=" + projectCount);
            }
            catch { }
        }

        private Contracts.CopilotRequest BuildRequest(string solutionName, int projectCount)
        {
            return new Contracts.CopilotRequest
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
            try { AgentComposition.Logger?.Info("ToolWindowControl: RunButton clicked"); } catch { }

            if (state == UiState.Running) return;

            UpdateUiState(UiState.Running);
            ErrorText.Text = string.Empty;
            Log("Execution started.");

            try
            {
                var prompt = PromptTextBox.Text ?? string.Empty;

                try
                {
                    AgentComposition.Logger?.Info($"ToolWindowControl: Prompt length = {prompt.Length}");
                }
                catch { }

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "Prompt vacío.";
                    Log("Execution error: Prompt is empty.");
                    return;
                }

                if (agentService == null)
                {
                    ErrorText.Text = "Agent no configurado. Revisá Tools → Options → Agente IA Local.";
                    UpdateUiState(UiState.Error);

                    try
                    {
                        AgentComposition.Logger?.Warn("ToolWindowControl: Run clicked but AgentService is NULL");
                    }
                    catch { }

                    Log("Execution error: Agent service not available.");
                    return;
                }

                AgentComposition.Logger?.Info("ToolWindowControl: AgentService.RunAsync start");
                var resp = await agentService.RunAsync(prompt, System.Threading.CancellationToken.None);
                AgentComposition.Logger?.Info("ToolWindowControl: AgentService.RunAsync end");

                if (resp == null)
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "No response from agent.";
                    AgentComposition.Logger?.Warn("ToolWindowControl: null response");
                    return;
                }

                try
                {
                    AgentComposition.Logger?.Info($"ToolWindowControl: Agent response success = {resp.IsSuccess}");
                }
                catch { }

                if (!resp.IsSuccess)
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = resp.Error ?? "Agent returned error";
                    ResponseTextBox.Text = string.Empty;
                    AgentComposition.Logger?.Warn("ToolWindowControl: agent error=" + (resp.Error ?? string.Empty));
                    return;
                }

                ResponseTextBox.Text = resp.Content ?? string.Empty;
                UpdateUiState(UiState.Completed);
                Log("Execution completed successfully.");
            }
            catch (Exception ex)
            {
                UpdateUiState(UiState.Error);
                ErrorText.Text = ex.Message;
                ResponseTextBox.Text = string.Empty;
                Log("Execution error: " + ex.Message);

                try
                {
                    AgentComposition.Logger?.Error("ToolWindowControl: Error during RunAsync", ex);
                }
                catch { }
            }
            finally
            {
                // Re-evaluate status to ensure UI stays consistent after an execution attempt
                EvaluateAndDisplayStatus();
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PromptTextBox.Text = string.Empty;
            ResponseTextBox.Text = string.Empty;
            LogText.Text = string.Empty;
            UpdateUiState(UiState.Idle);

            try { AgentComposition.Logger?.Info("ToolWindowControl: Clear click"); } catch { }

            EvaluateAndDisplayStatus();
        }

        private void OptionsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Package)) as Package;
                if (pkg != null)
                {
                    pkg.ShowOptionPage(typeof(AgenteIALocalVSIX.Options.AgenteIALocalOptionsPage));
                }
            }
            catch { }
        }

        private void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            LogText.Text = ts + " - " + message + "\n" + LogText.Text;
        }
    }
}
