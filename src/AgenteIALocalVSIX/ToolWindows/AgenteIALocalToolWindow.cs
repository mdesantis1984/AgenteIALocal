using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Settings;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        public AgenteIALocalToolWindow() : base(null)
        {
            this.Caption = "Agente IA Local";
            this.Content = new AgenteIALocalControl();
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            try { AgentComposition.Logger?.Info("AgenteIALocalToolWindow: OnToolWindowCreated"); } catch { }

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

            var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(AgenteIALocalVSIXPackage)) 
                as AgenteIALocalVSIXPackage;

            if (pkg?.AgentSettingsProvider != null)
            {
                var control = this.Content as AgenteIALocalControl;
                control?.AttachSettingsProvider(pkg.AgentSettingsProvider);

                try
                {
                    AgentComposition.Logger?.Info(
                        "ToolWindow: SettingsProvider injected from Package");
                }
                catch { }
            }

            if (Content is AgenteIALocalControl control2)
            {
                try { control2.RefreshLogView(); } catch { }
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
                        control.EvaluateAndDisplayStatus();
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
