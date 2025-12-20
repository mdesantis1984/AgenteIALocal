using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Windows.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane, IVsWindowFrameNotify3
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

            try
            {
                if (this.Frame is IVsWindowFrame frame)
                {
                    frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, this);
                }
            }
            catch { }
        }

        private void RefreshStatusIfPossible()
        {
            var control = this.Content as AgenteIALocalControl;
            if (control != null)
            {
                control.EvaluateAndDisplayStatus();
            }
        }

        public int OnShow(int fShow)
        {
            if (fShow == (int)__FRAMESHOW.FRAMESHOW_WinShown || fShow == (int)__FRAMESHOW.FRAMESHOW_TabActivated)
            {
                RefreshStatusIfPossible();
            }

            return VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        public int OnSize(int x, int y, int w, int h)
        {
            return VSConstants.S_OK;
        }

        public int OnDockableChange(int x, int y, int w, int h, int fDockable)
        {
            return VSConstants.S_OK;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            return VSConstants.S_OK;
        }

        public int OnDockableChangeEx(int x, int y, int w, int h, int fDockableAlways)
        {
            return VSConstants.S_OK;
        }

        public int OnStatusChange(uint dwStatus)
        {
            return VSConstants.S_OK;
        }

        public int OnPropertyChange(int propid, object var)
        {
            return VSConstants.S_OK;
        }

        public int OnModalChange(int fModal)
        {
            return VSConstants.S_OK;
        }

        public int OnFrameEnabled(int fEnabled)
        {
            return VSConstants.S_OK;
        }
    }
}
