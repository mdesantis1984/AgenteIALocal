using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane
    {
        public AgenteIALocalToolWindow() : base(null)
        {
            try { AgentComposition.Logger?.Info("AgenteIALocalToolWindow: ctor"); } catch { }

            this.Caption = "Agente IA Local";

            try
            {
                this.Content = new AgenteIALocalControl();
                try { AgentComposition.Logger?.Info("AgenteIALocalToolWindow: Content assigned"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Error("AgenteIALocalToolWindow: failed to assign Content", ex); } catch { }

                try
                {
                    this.Content = new TextBlock
                    {
                        Text = "Agente IA Local: failed to create UI. See log file.",
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        Margin = new System.Windows.Thickness(12)
                    };
                }
                catch { }
            }
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            try { AgentComposition.Logger?.Info("AgenteIALocalToolWindow: OnToolWindowCreated"); } catch { }
        }
    }
}
