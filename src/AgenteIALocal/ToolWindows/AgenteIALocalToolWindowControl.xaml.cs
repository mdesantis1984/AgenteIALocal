using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AgenteIALocal.Options;
using AgenteIALocal.Core.Interfaces.Agent;
using Microsoft.VisualStudio.Shell;
using AgenteIALocal.Logging;

namespace AgenteIALocal.ToolWindows
{
    /// <summary>
    /// Code-behind for the minimal ToolWindow user control.
    /// Creates a simple UI with provider and model selectors, goal input, Run/Stop buttons and output area.
    /// </summary>
    public partial class AgenteIALocalToolWindowControl : UserControl
    {
        private AgentOptions packageOptions;
        private readonly List<string> providers = new List<string> { "MockAI", "OpenAI" };
        private readonly List<string> models = new List<string> { "mock-default", "gpt-4.1-mini" };
        private CancellationTokenSource cts;
        private Button runButton;
        private Button stopButton;
        private TextBox outputBox;
        private TextBox goalBox;
        private InMemoryAgentLogger logger;
        private ListBox logListBox;

        public AgenteIALocalToolWindowControl()
        {
            InitializeComponent();
            logger = new InMemoryAgentLogger();

            // Attempt to load AgentOptions from the package. Fall back to a new instance.
            try
            {
                var pkg = Package.GetGlobalService(typeof(AgenteIALocal.AgenteIALocalPackage)) as AgenteIALocal.AgenteIALocalPackage;
                if (pkg != null)
                {
                    packageOptions = pkg.GetDialogPage(typeof(AgentOptions)) as AgentOptions;
                }
            }
            catch
            {
                packageOptions = null;
            }

            if (packageOptions == null)
            {
                packageOptions = new AgentOptions();
            }

            BuildUi();
        }

        private void BuildUi()
        {
            var panel = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock { Text = "Agente IA Local - Control", FontSize = 16, Margin = new Thickness(0, 0, 0, 10) };
            panel.Children.Add(title);

            // Provider selector
            var provLabel = new TextBlock { Text = "Provider:", Margin = new Thickness(0, 5, 0, 2) };
            panel.Children.Add(provLabel);

            var provCombo = new ComboBox { ItemsSource = providers, SelectedValue = packageOptions.DefaultProvider };
            provCombo.SelectionChanged += (s, e) =>
            {
                var selected = provCombo.SelectedItem as string;
                if (!string.IsNullOrEmpty(selected))
                {
                    packageOptions.DefaultProvider = selected;
                    Debug.WriteLine($"AgentOptions.DefaultProvider set to: {selected}");
                }
            };
            panel.Children.Add(provCombo);

            // Model selector
            var modelLabel = new TextBlock { Text = "Model:", Margin = new Thickness(0, 10, 0, 2) };
            panel.Children.Add(modelLabel);

            var modelCombo = new ComboBox { ItemsSource = models, SelectedValue = packageOptions.DefaultModel };
            modelCombo.SelectionChanged += (s, e) =>
            {
                var selected = modelCombo.SelectedItem as string;
                if (!string.IsNullOrEmpty(selected))
                {
                    packageOptions.DefaultModel = selected;
                    Debug.WriteLine($"AgentOptions.DefaultModel set to: {selected}");
                }
            };
            panel.Children.Add(modelCombo);

            // Goal input
            var goalLabel = new TextBlock { Text = "Goal:", Margin = new Thickness(0, 10, 0, 2) };
            panel.Children.Add(goalLabel);
            goalBox = new TextBox { Text = string.Empty };
            panel.Children.Add(goalBox);

            // Run / Stop buttons
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10) };
            runButton = new Button { Content = "Run", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            stopButton = new Button { Content = "Stop", Width = 80, IsEnabled = false };
            runButton.Click += RunButton_Click;
            stopButton.Click += StopButton_Click;
            buttonsPanel.Children.Add(runButton);
            buttonsPanel.Children.Add(stopButton);
            panel.Children.Add(buttonsPanel);

            // Output box
            outputBox = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 120, IsReadOnly = true };
            panel.Children.Add(outputBox);

            // Log section
            var logLabel = new TextBlock { Text = "Logs:", Margin = new Thickness(0, 10, 0, 2) };
            panel.Children.Add(logLabel);

            logListBox = new ListBox { Height = 150 };
            panel.Children.Add(logListBox);

            var note = new TextBlock { Text = "Note: execution is manual and read-only. No external calls are made from this UI.", Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
            panel.Children.Add(note);

            this.Content = panel;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            runButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            outputBox.Text = string.Empty;
            logger.Info("Run started");
            RefreshLog();
            cts = new CancellationTokenSource();

            try
            {
                var goal = goalBox.Text?.Trim();

                // Planner logic (local): decide action
                string actionType;
                Dictionary<string, object> parameters = null;
                if (string.IsNullOrWhiteSpace(goal))
                {
                    actionType = "idle";
                    logger.Info("Planner decision: idle");
                }
                else
                {
                    actionType = "analyze-workspace";
                    parameters = new Dictionary<string, object> { { "goal", goal } };
                    logger.Info("Planner decision: analyze-workspace");
                }

                // Execute action
                logger.Info($"Action execution started: {actionType}");
                RefreshLog();
                var result = await Task.Run(() => ExecuteAction(actionType, parameters, cts.Token), cts.Token);
                logger.Info($"Action execution completed: {actionType} Success={result?.Success}");
                RefreshLog();

                if (result != null)
                {
                    outputBox.Text = result.Output ?? result.Error ?? "(no output)";
                }
            }
            catch (OperationCanceledException)
            {
                logger.Info("Execution canceled");
                outputBox.Text = "Execution canceled.";
                RefreshLog();
            }
            catch (Exception ex)
            {
                logger.Error("Execution error: " + ex.Message);
                outputBox.Text = "Execution error: " + ex.Message;
                RefreshLog();
            }
            finally
            {
                runButton.IsEnabled = true;
                stopButton.IsEnabled = false;
                cts = null;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cts?.Cancel();
                logger.Info("Cancellation requested by user");
                RefreshLog();
            }
            catch { }
        }

        private void RefreshLog()
        {
            var entries = logger.GetEntries();
            logListBox.ItemsSource = entries.Select(e => e.ToString()).ToList();
            if (logListBox.Items.Count > 0)
            {
                logListBox.ScrollIntoView(logListBox.Items[logListBox.Items.Count - 1]);
            }
        }

        private IAgentResult ExecuteAction(string actionType, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Simple executor: support idle and analyze-workspace
            if (actionType == "idle")
            {
                return new LocalAgentResult("idle", true, "Idle: no operation performed.", null);
            }

            if (actionType == "analyze-workspace")
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Find nearest .sln and parse
                var solutionFile = FindSolutionFileUpwards(AppContext.BaseDirectory);
                var sb = new StringBuilder();
                if (string.IsNullOrEmpty(solutionFile))
                {
                    sb.AppendLine("No solution file found.");
                }
                else
                {
                    sb.AppendLine("Solution: " + Path.GetFileNameWithoutExtension(solutionFile));
                    sb.AppendLine("Path: " + solutionFile);

                    var projects = ParseProjectsFromSolution(solutionFile);
                    sb.AppendLine("Projects: " + projects.Count);
                    foreach (var p in projects.Take(10))
                    {
                        sb.AppendLine($"- {p.Name} ({p.Path}) [{p.Language}]");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("Parameters:");
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var kv in parameters)
                    {
                        sb.AppendLine($"- {kv.Key}: {kv.Value}");
                    }
                }
                else
                {
                    sb.AppendLine("(none)");
                }

                sb.AppendLine();
                sb.AppendLine("Prompt Preview:");
                var prompt = BuildLocalPrompt(parameters?.ContainsKey("goal") == true ? parameters["goal"].ToString() : null, actionType, parameters);
                sb.AppendLine(prompt);

                return new LocalAgentResult("analyze-workspace", true, sb.ToString().Trim(), null);
            }

            return new LocalAgentResult(actionType, false, null, "Unsupported action type.");
        }

        private string BuildLocalPrompt(string goal, string actionType, Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SYSTEM: You are an offline deterministic agent planner helper. Follow instructions exactly.");
            sb.AppendLine("CONSTRAINTS: Provide a concise plain-text response. Do not call external services.");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(goal))
            {
                sb.AppendLine("GOAL:");
                sb.AppendLine(goal);
                sb.AppendLine();
            }

            sb.AppendLine("ACTION:");
            sb.AppendLine($"Type: {actionType}");
            sb.AppendLine();
            sb.AppendLine("PARAMETERS:");
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var kv in parameters)
                {
                    sb.AppendLine($"- {kv.Key}: {kv.Value}");
                }
            }
            else
            {
                sb.AppendLine("(none)");
            }

            return sb.ToString().Trim();
        }

        private static string FindSolutionFileUpwards(string startDirectory)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                var slnFiles = dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                if (slnFiles.Length > 0)
                {
                    return slnFiles[0].FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private static List<(string Name, string Path, string Language)> ParseProjectsFromSolution(string solutionFile)
        {
            var list = new List<(string Name, string Path, string Language)>();
            try
            {
                var lines = File.ReadAllLines(solutionFile);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("Project(", StringComparison.Ordinal)) continue;
                    var parts = line.Split('=');
                    if (parts.Length < 2) continue;
                    var rhs = parts[1].Trim();
                    var tokens = SplitCsvPreservingQuotes(rhs);
                    if (tokens.Length >= 2)
                    {
                        var projectName = TrimQuotes(tokens[0]);
                        var projectPath = TrimQuotes(tokens[1]);
                        var solutionDir = Path.GetDirectoryName(solutionFile);
                        var absolutePath = projectPath;
                        if (!Path.IsPathRooted(projectPath) && !string.IsNullOrEmpty(solutionDir))
                        {
                            absolutePath = Path.GetFullPath(Path.Combine(solutionDir, projectPath));
                        }

                        var language = InferLanguageFromProjectFile(absolutePath);
                        list.Add((projectName, absolutePath, language));
                    }
                }
            }
            catch { }

            return list;
        }

        private static string[] SplitCsvPreservingQuotes(string input)
        {
            var results = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            foreach (var ch in input)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    results.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
                results.Add(current.ToString().Trim());

            return results.ToArray();
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Trim().Trim('"').Trim();
        }

        private static string InferLanguageFromProjectFile(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return null;
            var ext = Path.GetExtension(projectPath)?.ToLowerInvariant();
            if (ext == ".csproj") return "C#";
            if (ext == ".vbproj") return "VB";
            return null;
        }

        private class LocalAgentResult : IAgentResult
        {
            public LocalAgentResult(string actionId, bool success, string output, string error)
            {
                ActionId = actionId;
                Success = success;
                Output = output;
                Error = error;
            }

            public string ActionId { get; }
            public bool Success { get; }
            public string Output { get; }
            public string Error { get; }
        }
    }
}
