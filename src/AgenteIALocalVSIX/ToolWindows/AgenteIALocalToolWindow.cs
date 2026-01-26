using AgenteIALocal.Core.Settings;
using AgenteIALocalVSIX.Commons;
// ELIMINADO - ID: 20260123_222000 - using AgenteIALocalVSIX.Settings (namespace legacy no existente)
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
using Microsoft.VisualStudio.Threading;


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
            // MODIFICADO - ID: 20260125_003007 - Constructor minimalista (evitar deadlock)
            // Version + i18n title se setean en OnToolWindowCreated() después de init
            this.Caption = "Chat de Agente IA Local";
            this.Content = new AgenteIALocalControl();
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            
            // NUEVO - ID: 20260125_003006 - Lazy init LocalizationService AQUÍ (después de constructor)
            try
            {
                AgenteIALocalVSIXPackage.InitializeLocalizationServiceOnce();
            }
            catch (Exception exInit)
            {
                System.Diagnostics.Trace.TraceError($"[ToolWindow.OnCreated] InitializeLocalizationServiceOnce failed: {exInit.Message}");
            }
            
            // NUEVO - ID: 20260125_003007 - Actualizar Caption con versión + i18n DESPUÉS de init
            try
            {
                var version = typeof(AgenteIALocalVSIXPackage).GetVsixVersionString();
                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                var title = locService != null ? locService.GetString("ui.chat.window.title") : "Chat de Agente IA Local";
                this.Caption = $"{title} {version}";
            }
            catch (Exception exCaption)
            {
                System.Diagnostics.Trace.TraceError($"[ToolWindow.OnCreated] Caption update failed: {exCaption.Message}");
            }
            
            // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
            try { AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.OnCreated", "OnToolWindowCreated", null); } catch { }

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

            // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.OnCreated", "ToolWindow created; skipping optional settings provider injection", null);
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
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

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
                    // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                    try { AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.HookEvents", "Hooked SolutionEvents", null); } catch { }
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
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await TryUpdateSolutionInfoAsync(control, source).ConfigureAwait(false);
            });

        }

        private async Task TryUpdateSolutionInfoAsync(AgenteIALocalControl control, string source)
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
             // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
             try
             {
                 AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"UpdateSolutionInfo start ({source})", null);

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
                              // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                              AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"DTE null (attempt={attempt + 1})", null);
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

                              // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                              AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"Solution '{solutionName}' projects={projectCount} (attempt={attempt + 1})", null);

                             try
                             {
                                 // Ensure SetSolutionInfo runs on UI thread
                                 await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                  control.SetSolutionInfo(solutionName, projectCount);
                                  solutionInfoSet = true;
                                  // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                                  AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"SetSolutionInfo done ({source})", null);
                             }
                              catch (Exception ex)
                              {
                                  // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                                  AgenteIALocal.Logging.Log.Error("-", 9100, "ToolWindow.UpdateSolution", $"SetSolutionInfo failed: {ex.Message}", ex);
                             }

                             return; // done
                         }

                          // Log state for diagnostics
                          // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                          AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"No open solution (attempt={attempt + 1}) - hasSolution={hasSolution} IsOpen={isOpen} FullNameLen={fullName?.Length ?? 0} Name='{name}'", null);
                     }
                      catch (Exception ex)
                      {
                          // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                          AgenteIALocal.Logging.Log.Error("-", 9100, "ToolWindow.UpdateSolution", $"Error checking solution (attempt={attempt + 1}): {ex.Message}", ex);
                     }

                     // wait before next attempt (do not block UI thread)
                     try { await Task.Delay(delayMs).ConfigureAwait(false); } catch { }
                  }

                // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                AgenteIALocal.Logging.Log.Information("-", 9100, "ToolWindow.UpdateSolution", $"UpdateSolutionInfo giving up after retries ({source})", null);
            }
            catch (Exception ex)
            {
                // MODIFICADO - ID: 20260122_010802 - Migrado a Serilog
                try { AgenteIALocal.Logging.Log.Error("-", 9100, "ToolWindow.UpdateSolution", $"Unexpected error in TryUpdateSolutionInfoAsync: {ex.Message}", ex); } catch { }
            }
        }

        private string SafeGetSolutionName(Solution sol)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
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
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
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
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
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
