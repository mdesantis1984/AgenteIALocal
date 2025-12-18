using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane
    {
        public AgenteIALocalToolWindow() : base(null)
        {
            this.Caption = "Agente IA Local";
            this.Content = new AgenteIALocalControl();
        }
    }
}
