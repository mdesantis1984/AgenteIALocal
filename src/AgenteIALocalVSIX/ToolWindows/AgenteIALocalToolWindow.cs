using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Commons;
using AgenteIALocalVSIX.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using System.IO;
using System.Threading.Tasks;

namespace AgenteIALocalVSIX.ToolWindows
{
    [Guid("D3F2A9B1-8C7E-4F5A-9C0B-3B2A1C4D5E6F")]
    public class AgenteIALocalToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        // Keep references to DTE events to avoid GC
        private bool solutionEventsHooked = false;
        private EnvDTE.Events dteEvents;
        private EnvDTE.SolutionEvents dteSolutionEvents;
        // Ensure we update solution info only once until solution changes
        private volatile bool solutionInfoSet = false;

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

                // Hook solution events once
                try
                {
                    HookSolutionEventsIfNeeded();
                }
                catch { }
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

            // Attempt to update solution info at creation time (best-effort)
            try
            {
                var control = this.Content as AgenteIALocalControl;
                if (control != null)
                {
                    UpdateSolutionInfo(control, "OnToolWindowCreated");
                }
            }
            catch
            {
                // never throw
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

                        try
                        {
                            // Update solution info on show (non-blocking)
                            UpdateSolutionInfo(control, "OnShow");
                        }
                        catch
                        {
                            // swallow all exceptions to remain robust
                        }
                    }
                });
            }

            return VSConstants.S_OK;
        }

        private void HookSolutionEventsIfNeeded()
        {
            try
            {
                if (solutionEventsHooked) return;

                var dteObj = ServiceProvider.GlobalProvider.GetService(typeof(SDTE));
                var dte = dteObj as EnvDTE.DTE;
                if (dte == null) return;

                dteEvents = dte.Events;
                dteSolutionEvents = dteEvents.SolutionEvents;
                if (dteSolutionEvents != null)
                {
                    dteSolutionEvents.Opened += OnSolutionOpened;
                    dteSolutionEvents.AfterClosing += OnSolutionAfterClosing;
                    solutionEventsHooked = true;
                    try { AgentComposition.Logger?.Invoke("AgenteIALocalToolWindow: Hooked SolutionEvents"); } catch { }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void OnSolutionOpened()
        {
            try
            {
                var control = this.Content as AgenteIALocalControl;
                if (control != null)
                {
                    UpdateSolutionInfo(control, "SolutionEvents.Opened");
                }
            }
            catch { }
        }

        private void OnSolutionAfterClosing()
        {
            try
            {
                var control = this.Content as AgenteIALocalControl;
                if (control != null)
                {
                    UpdateSolutionInfo(control, "SolutionEvents.AfterClosing");
                    // reset flag so next solution open will trigger update
                    solutionInfoSet = false;
                }
            }
            catch { }
        }

        private void UpdateSolutionInfo(AgenteIALocalControl control, string source)
        {
            // Avoid duplicate work if already set
            if (solutionInfoSet) return;

            // Schedule async update; do not block caller
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TryUpdateSolutionInfoAsync(control, source).ConfigureAwait(false);
            });
        }

        private async Task TryUpdateSolutionInfoAsync(AgenteIALocalControl control, string source)
        {
            try
            {
                AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: UpdateSolutionInfo start ({source})");

                // Retry loop if solution not yet open/loaded
                const int maxAttempts = 6;
                const int delayMs = 500;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        // Ensure we're on UI thread before accessing DTE and UI
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var dteObj = ServiceProvider.GlobalProvider.GetService(typeof(SDTE));
                        var dte = dteObj as EnvDTE.DTE;

                        if (dte == null)
                        {
                            AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: DTE null (attempt={attempt + 1})");
                            // wait and retry
                            try { await Task.Delay(delayMs).ConfigureAwait(false); } catch { }
                            continue;
                        }

                        var sol = dte.Solution;
                        bool hasSolution = sol != null;
                        bool isOpen = hasSolution && sol.IsOpen;
                        string fullName = hasSolution ? (sol.FullName ?? string.Empty) : string.Empty;
                        string name = hasSolution ? (sol.Properties != null ? SafeGetSolutionName(sol) : string.Empty) : string.Empty;

                        if (hasSolution && isOpen && (!string.IsNullOrEmpty(fullName) || !string.IsNullOrEmpty(name)))
                        {
                            // Prefer FullName-derived name but fallback to Solution.Name
                            string solutionName = !string.IsNullOrEmpty(fullName) ? Path.GetFileNameWithoutExtension(fullName) : name ?? string.Empty;
                            int projectCount = CountSolutionProjects(sol);

                            AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: Solution '{solutionName}' projects={projectCount} (attempt={attempt + 1})");

                            try
                            {
                                // Ensure SetSolutionInfo runs on UI thread
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                control.SetSolutionInfo(solutionName, projectCount);
                                solutionInfoSet = true;
                                AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: SetSolutionInfo done ({source})");
                            }
                            catch (Exception ex)
                            {
                                AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: SetSolutionInfo failed: {ex.Message}");
                            }

                            return; // done
                        }

                        // Log state for diagnostics
                        AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: No open solution (attempt={attempt + 1}) - hasSolution={hasSolution} IsOpen={isOpen} FullNameLen={fullName?.Length ?? 0} Name='{name}'");
                    }
                    catch (Exception ex)
                    {
                        AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: Error checking solution (attempt={attempt + 1}): {ex.Message}");
                    }

                    // wait before next attempt (do not block UI thread)
                    try { await Task.Delay(delayMs).ConfigureAwait(false); } catch { }
                }

                AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: UpdateSolutionInfo giving up after retries ({source})");
            }
            catch (Exception ex)
            {
                try { AgentComposition.Logger?.Invoke($"AgenteIALocalToolWindow: Unexpected error in TryUpdateSolutionInfoAsync: {ex.Message}"); } catch { }
            }
        }

        private string SafeGetSolutionName(Solution sol)
        {
            try
            {
                // Some solutions expose Name property; otherwise derive from FullName
                var prop = sol.Properties?.Item("Name");
                if (prop != null && prop.Value is string s && !string.IsNullOrEmpty(s)) return s;
            }
            catch { }

            try { return Path.GetFileNameWithoutExtension(sol.FullName ?? string.Empty) ?? string.Empty; } catch { return string.Empty; }
        }

        private int CountSolutionProjects(Solution solution)
        {
            if (solution == null) return 0;

            int count = 0;
            try
            {
                var projects = solution.Projects;
                if (projects == null) return 0;

                for (int i = 1; i <= projects.Count; i++)
                {
                    try
                    {
                        var proj = projects.Item(i);
                        count += CountProjectRecursive(proj);
                    }
                    catch
                    {
                        // ignore individual project errors
                    }
                }
            }
            catch
            {
                // ignore
            }

            return count;
        }

        private int CountProjectRecursive(Project project)
        {
            if (project == null) return 0;

            try
            {
                // Solution folders: use EnvDTE80 ProjectKinds constant
                if (string.Equals(project.Kind, EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder, StringComparison.OrdinalIgnoreCase))
                {
                    int subtotal = 0;
                    var items = project.ProjectItems;
                    if (items != null)
                    {
                        for (int i = 1; i <= items.Count; i++)
                        {
                            try
                            {
                                var pi = items.Item(i);
                                if (pi == null) continue;
                                var sub = pi.SubProject;
                                if (sub != null)
                                {
                                    subtotal += CountProjectRecursive(sub);
                                }
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }

                    return subtotal;
                }

                // Regular project: count as 1
                return 1;
            }
            catch
            {
                return 0;
            }
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
