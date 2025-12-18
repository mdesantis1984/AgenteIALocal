using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using AgenteIALocalVSIX.Application;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane
    {
        public AgenteIALocalToolWindow() : base(null)
        {
            this.Caption = "Agente IA Local";

            object ws = null;

            try
            {
                ws = VisualStudioWorkspaceProvider.TryGetWorkspaceContext();
                var initMethod = ws?.GetType().GetMethod("InitializeAsync");
                if (initMethod != null)
                {
                    try { initMethod.Invoke(ws, null); } catch { }
                }
            }
            catch
            {
                ws = null;
            }

            var adapter = new ApplicationContextAdapter(ws);
            var control = new AgenteIALocalControl();
            control.SetSolutionInfo(adapter.GetSolutionName(), adapter.GetProjectCount());
            this.Content = control;
        }
    }
}
