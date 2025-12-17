using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AgenteIALocal.Options;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocal.ToolWindows
{
    /// <summary>
    /// Code-behind for the minimal ToolWindow user control.
    /// Creates a simple UI with provider and model selectors that update AgentOptions when possible.
    /// </summary>
    public partial class AgenteIALocalToolWindowControl : UserControl
    {
        private AgentOptions packageOptions;
        private readonly List<string> providers = new List<string> { "MockAI", "OpenAI" };
        private readonly List<string> models = new List<string> { "mock-default", "gpt-4.1-mini" };

        public AgenteIALocalToolWindowControl()
        {
            InitializeComponent();

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

            var title = new TextBlock { Text = "Agente IA Local - Settings", FontSize = 16, Margin = new Thickness(0, 0, 0, 10) };
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

            // API Key display (masked length)
            var keyLabel = new TextBlock { Text = "OpenAI API Key:", Margin = new Thickness(0, 10, 0, 2) };
            panel.Children.Add(keyLabel);
            var keyBox = new TextBox { Text = (string.IsNullOrEmpty(packageOptions.OpenAIApiKey) ? "(not set)" : new string('*', Math.Min(8, packageOptions.OpenAIApiKey.Length))), IsReadOnly = true };
            panel.Children.Add(keyBox);

            // Simple instruction
            var note = new TextBlock { Text = "Note: changes update the local options instance. Use Tools > Options to persist settings.", Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
            panel.Children.Add(note);

            this.Content = panel;
        }
    }
}
