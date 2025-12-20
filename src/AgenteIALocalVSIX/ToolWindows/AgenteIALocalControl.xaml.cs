using System.Windows.Controls;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Core.Settings;
using Microsoft.VisualStudio.Shell;

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

            if (agentService == null)
            {
                try
                {
                    AgentComposition.Logger?.Warn("ToolWindowControl: AgentService NULL at load time");
                }
                catch { }

                var msg = "Agent no inicializado. Configure proveedor en Tools → Options.";

                try
                {
                    RunButton.IsEnabled = false;
                    ErrorText.Text = msg;
                }
                catch { }
            }
        }

        private void AgenteIALocalControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: Loaded");
            }
            catch { }
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
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PromptTextBox.Text = string.Empty;
            ResponseTextBox.Text = string.Empty;
            LogText.Text = string.Empty;
            UpdateUiState(UiState.Idle);

            try { AgentComposition.Logger?.Info("ToolWindowControl: Clear click"); } catch { }
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
