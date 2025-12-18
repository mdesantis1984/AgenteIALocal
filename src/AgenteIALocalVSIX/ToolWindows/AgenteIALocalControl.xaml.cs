using System.Windows.Controls;
using AgenteIALocalVSIX.Contracts;
using AgenteIALocalVSIX.Execution;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;

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
        }

        public void SetSolutionInfo(string solutionName, int projectCount)
        {
            SolutionNameText.Text = solutionName;
            ProjectCountText.Text = projectCount.ToString();

            // Prepare initial request but do not execute
            var req = BuildRequest(solutionName, projectCount);
            RequestJsonText.Text = SerializeToJson(req);
            ResponseJsonText.Text = string.Empty;
            LogText.Text = "";
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

            UpdateUiState(UiState.Running);
            Log("Execution started.");

            try
            {
                var req = JsonConvert.DeserializeObject<CopilotRequest>(RequestJsonText.Text);
                if (req == null) throw new InvalidOperationException("Invalid request JSON.");

                // Simulate asynchronous execution
                await Task.Run(() => {
                    var resp = MockCopilotExecutor.Execute(req);
                    var respJson = SerializeToJson(resp);

                    // Update UI on UI thread
                    this.Dispatcher.BeginInvoke(new Action(() => {
                        ResponseJsonText.Text = respJson;
                        UpdateUiState(UiState.Completed);
                        Log("Execution completed successfully.");
                    }));
                });
            }
            catch (Exception ex)
            {
                UpdateUiState(UiState.Error);
                Log("Execution error: " + ex.Message);
                ResponseJsonText.Text = "{ \"error\": \"Execution failed\" }";
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RequestJsonText.Text = string.Empty;
            ResponseJsonText.Text = string.Empty;
            LogText.Text = string.Empty;
            UpdateUiState(UiState.Idle);
        }

        private void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            LogText.Text = ts + " - " + message + "\n" + LogText.Text;
        }


    }
}
