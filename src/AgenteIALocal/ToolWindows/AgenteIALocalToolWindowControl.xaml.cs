using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AgenteIALocal.ViewModels;

namespace AgenteIALocal.ToolWindows
{
    /// <summary>
    /// Code-behind for the minimal ToolWindow user control.
    /// Uses programmatic UI and binding to avoid modifying XAML file in this step.
    /// </summary>
    public partial class AgenteIALocalToolWindowControl : UserControl
    {
        public AgenteIALocalToolWindowControl()
        {
            // Create viewmodel
            var vm = new AgenteIALocalToolWindowViewModel();
            this.DataContext = vm;

            // Create UI programmatically and bind TextBlock.Text to StatusText
            var textBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 16
            };

            var binding = new Binding("StatusText") { Source = vm };
            textBlock.SetBinding(TextBlock.TextProperty, binding);

            // Set the control content
            this.Content = textBlock;
        }
    }
}
