using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocal.ToolWindows
{
    /// <summary>
    /// Minimal ToolWindowPane for Agente IA Local. This class is a placeholder and
    /// is not registered in the package yet. Do not register it in the package in this step.
    /// </summary>
    public class AgenteIALocalToolWindow : ToolWindowPane
    {
        public const string ToolWindowGuidString = "E3D9B6A2-4F1C-4B2E-A8B9-1234567890AB";

        public AgenteIALocalToolWindow() : base(null)
        {
            this.Caption = "Agente IA Local";

            // Set the content to the user control defined below.
            this.Content = new AgenteIALocalToolWindowControl();
        }
    }
}
