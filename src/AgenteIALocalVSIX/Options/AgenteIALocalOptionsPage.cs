using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Logging;

namespace AgenteIALocalVSIX.Options
{
    public sealed class AgenteIALocalOptionsPage : UIElementDialogPage
    {
        internal IAgentSettingsProvider SettingsProvider { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AgentProviderType Provider { get; set; } = AgentProviderType.LmStudio;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IAgentProviderSettings ActiveSettings { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LmModel
        {
            get => (ActiveSettings as LmStudioSettings)?.Model ?? string.Empty;
            set
            {
                if (ActiveSettings is LmStudioSettings ls) ls.Model = value;
            }
        }

        private AgenteIALocalOptionsControl optionsControl;

        protected override System.Windows.UIElement Child
        {
            get
            {
                try
                {
                    AgentComposition.Logger?.Invoke("OptionsPage: CreateChild/Child getter");
                    ActivityLogHelper.TryLog(this.Site, "AgenteIALocal: OptionsPage CreateChild");
                }
                catch { }

                try
                {
                    if (optionsControl == null)
                    {
                        optionsControl = new AgenteIALocalOptionsControl(null);
                    }

                    return optionsControl;
                }
                catch (Exception ex)
                {
                    try
                    {
                        AgentComposition.Logger?.Invoke("OptionsPage: CreateChild failed: " + ex.Message);
                    }
                    catch { }

                    try { ActivityLogHelper.TryLogError(this.Site, "AgenteIALocal: OptionsPage CreateChild failed", ex); } catch { }

                    return new System.Windows.Controls.TextBlock { Text = "Agente IA Local: failed to create options UI. See ActivityLog/log file." };
                }
            }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            try
            {
                AgentComposition.Logger?.Invoke("OptionsPage: OnActivate");
                ActivityLogHelper.TryLog(this.Site, "AgenteIALocal: OptionsPage OnActivate");
            }
            catch { }

            try
            {
                optionsControl?.LoadSettings(this);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke("OptionsPage: OnActivate LoadSettings failed: " + ex.Message); } catch { }
                try { ActivityLogHelper.TryLogError(this.Site, "AgenteIALocal: OptionsPage OnActivate LoadSettings failed", ex); } catch { }
            }

            try
            {
                optionsControl?.Focus();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke("OptionsPage: OnActivate Focus failed: " + ex.Message); } catch { }
                try { ActivityLogHelper.TryLogError(this.Site, "AgenteIALocal: OptionsPage OnActivate Focus failed", ex); } catch { }
            }
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            try
            {
                AgentComposition.Logger?.Invoke("OptionsPage: OnApply");
                ActivityLogHelper.TryLog(this.Site, "AgenteIALocal: OptionsPage OnApply");
            }
            catch { }

            try
            {
                optionsControl?.SaveSettings(this);
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke("OptionsPage: OnApply SaveSettings failed: " + ex.Message); } catch { }
                try { ActivityLogHelper.TryLogError(this.Site, "AgenteIALocal: OptionsPage OnApply SaveSettings failed", ex); } catch { }
            }
        }
    }
}
