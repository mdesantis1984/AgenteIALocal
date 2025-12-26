using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Commons;
using AgenteIALocalVSIX.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        public AgenteIALocalToolWindow() : base(null)
        {
            var version = typeof(AgenteIALocalVSIXPackage).GetVsixVersionString();
            this.Caption = $"Chat de Agente IA Local {version}";
            this.Content = new AgenteIALocalControl();
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            try { AgentComposition.Logger?.Invoke("AgenteIALocalToolWindow: OnToolWindowCreated"); } catch { }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var frame = this.Frame as IVsWindowFrame;
                if (frame == null)
                {
                    return;
                }

                // Register this instance as the frame's notify helper.
                frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, this);
            });

            try
            {
                AgentComposition.Logger?.Invoke("ToolWindow created; skipping optional settings provider injection in this build.");
            }
            catch { }

            if (Content is AgenteIALocalControl control2)
            {
                try { /* RefreshLogView may be unavailable in this build; skip call. */ } catch { }
            }
        }

        public int OnShow(int fShow)
        {
            // fShow == 1: real show/activate
            if (fShow == 1)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var control = this.Content as AgenteIALocalControl;
                    if (control != null)
                    {
                        try { /* EvaluateAndDisplayStatus may be unavailable; skip. */ } catch { }
                    }
                });
            }

            return VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h) => VSConstants.S_OK;

        public int OnSize(int x, int y, int w, int h) => VSConstants.S_OK;

        public int OnDockableChange(int x, int y, int w, int h, int fDockable) => VSConstants.S_OK;

        public int OnClose(ref uint pgrfSaveOptions) => VSConstants.S_OK;

        public int OnDockableChangeEx(int x, int y, int w, int h, int fDockableAlways) => VSConstants.S_OK;

        public int OnStatusChange(uint dwStatus) => VSConstants.S_OK;

        public int OnPropertyChange(int propid, object var) => VSConstants.S_OK;

        public int OnModalChange(int fModal) => VSConstants.S_OK;

        public int OnFrameEnabled(int fEnabled) => VSConstants.S_OK;
    }
}
