using System.Windows.Controls;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Core.Settings;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.Windows.Media;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl : UserControl
    {
        private enum UiState { Idle, Running, Completed, Error }
        private UiState state = UiState.Idle;

        private IAgentService agentService;

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

        private void EvaluateAndDisplayStatus()
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

            // Try to read settings via reflection on AgentComposition if available
            object settingsObj = null;

            var acType = typeof(AgentComposition);
            var settingsProp = acType.GetProperty("Settings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (settingsProp != null)
            {
                settingsObj = settingsProp.GetValue(null);
            }
            else
            {
                // fallback: try common alternatives
                var optProp = acType.GetProperty("Options", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (optProp != null)
                {
                    settingsObj = optProp.GetValue(null);
                }
            }

            // If settings object not found, treat as incomplete configuration
            if (settingsObj == null)
            {
                StateText.Text = "Configuración: incompleta";
                ErrorText.Text = "Configuración incompleta. Revisá las Opciones.";
                RunButton.IsEnabled = false;
                return;
            }

            // Inspect required fields (BaseUrl and Model) via reflection against common property names
            string baseUrl = GetStringProperty(settingsObj, "BaseUrl") ?? GetStringProperty(settingsObj, "ApiUrl") ?? GetStringProperty(settingsObj, "Endpoint");
            string model = GetStringProperty(settingsObj, "Model") ?? GetStringProperty(settingsObj, "SelectedModel") ?? GetStringProperty(settingsObj, "ModelId");
            string provider = GetStringProperty(settingsObj, "Provider") ?? GetStringProperty(settingsObj, "ProviderName");

            bool hasBaseUrl = !string.IsNullOrWhiteSpace(baseUrl);
            bool hasModel = !string.IsNullOrWhiteSpace(model);
            bool hasProvider = !string.IsNullOrWhiteSpace(provider);

            if (hasProvider && hasBaseUrl && hasModel)
            {
                // Estado 1 — Configurado
                StateText.Text = "Configuración: OK";
                ErrorText.Foreground = Brushes.Green;
                ErrorText.Text = "Configuración OK. Listo para enviar mensajes.";
                RunButton.IsEnabled = true;
                return;
            }
            else
            {
                // Estado 2 — Configuración incompleta
                StateText.Text = "Configuración: incompleta";
                ErrorText.Foreground = Brushes.Orange;
                ErrorText.Text = "Configuración incompleta. Revisá las Opciones.";
                RunButton.IsEnabled = false;
                return;
            }
        }

        private string GetStringProperty(object obj, string propName)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) return null;
            var val = p.GetValue(obj) as string;
            return val;
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
