using AgenteIALocal.Core.Logging;
using AgenteIALocal.Infrastructure.LoggingV2;
using AgenteIALocalVSIX.Commands;
using AgenteIALocalVSIX.LoggingV2;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq; // NUEVO - ID: 20260122_010301 - Para Select() en diagnóstico
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AgenteIALocalVSIX
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AgenteIALocalVSIXPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ToolWindows.AgenteIALocalToolWindow))]
    public sealed class AgenteIALocalVSIXPackage : AsyncPackage
    {
        public const string PackageGuidString = "12e93cca-8723-4160-ac43-96fe08854111";

        private static int _loggerConfigured;
        private static int _serilogConfigured; // NUEVO CAMPO - ID: 20260122_000300

        // NUEVO METODO ConfigureSerilogOnce - ID: 20260122_000301
        // MODIFICADO - ID: 20260122_010300 - Diagnóstico mejorado para troubleshooting
        // Inicializa Serilog en paralelo al logging legacy (coexistencia temporal durante migración)
        private static void ConfigureSerilogOnce()
        {
            if (Interlocked.Exchange(ref _serilogConfigured, 1) == 1) return;

            // DIAGNÓSTICO: Confirmar entrada al método
            VsixSafeLog.Info("VSIX.Startup", "[DIAG] ConfigureSerilogOnce: INICIO - empaquetado VSIX verificado", null, 9001);

            try
            {
                var settings = new AgenteIALocal.Logging.LogSettings
                {
                    AppName = "AgenteIALocal",
                    Enabled = true,
                    All = false, // niveles individuales
                    Verbose = false,
                    Debug = false,
                    Information = true,
                    Warning = true,
                    Error = true,
                    Critical = true,
                    RollingFileSizeBytes = 3L * 1024 * 1024, // 3MB
                    RetainedFileCount = 10
                };

                // DIAGNÓSTICO: Confirmar antes de Configure()
                VsixSafeLog.Info("VSIX.Startup", "[DIAG] ConfigureSerilogOnce: Llamando AgenteIALocal.Logging.Log.Configure()...", null, 9002);

                AgenteIALocal.Logging.Log.Configure(settings);
                
                // DIAGNÓSTICO: Confirmar éxito Configure()
                VsixSafeLog.Info("VSIX.Startup", "[DIAG] ConfigureSerilogOnce: Configure() exitoso - intentando primer log Serilog...", null, 9003);

                // Log inicial usando nuevo API Serilog
                AgenteIALocal.Logging.Log.Information("-", 9000, "VSIX.Startup", "Serilog configured successfully", null);

                // DIAGNÓSTICO: Confirmar éxito completo
                VsixSafeLog.Info("VSIX.Startup", "[DIAG] ConfigureSerilogOnce: SUCCESS - Serilog.Information() ejecutado sin excepciones", null, 9004);
            }
            catch (System.IO.FileNotFoundException fnfEx)
            {
                VsixSafeLog.Error("VSIX.Startup", $"[DIAG] ConfigureSerilogOnce: FileNotFoundException - Assembly: {fnfEx.FileName}, Message: {fnfEx.Message}", fnfEx, 9010);
            }
            catch (TypeLoadException tlEx)
            {
                VsixSafeLog.Error("VSIX.Startup", $"[DIAG] ConfigureSerilogOnce: TypeLoadException - Type: {tlEx.TypeName}, Message: {tlEx.Message}", tlEx, 9011);
            }
            catch (System.Reflection.ReflectionTypeLoadException rtlEx)
            {
                var details = string.Join("; ", rtlEx.LoaderExceptions?.Select(e => e?.Message ?? "null") ?? new[] { "no loader exceptions" });
                VsixSafeLog.Error("VSIX.Startup", $"[DIAG] ConfigureSerilogOnce: ReflectionTypeLoadException - LoaderExceptions: {details}", rtlEx, 9012);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "";
                VsixSafeLog.Error("VSIX.Startup", $"[DIAG] ConfigureSerilogOnce: {ex.GetType().Name} - {ex.Message}{innerMsg}", ex, 9013);
            }
        }

        // Startup logging MUST never break package load.
        private static void ConfigureLoggerV2Once()
        {
            if (Interlocked.Exchange(ref _loggerConfigured, 1) == 1) return;

            try
            {
                var sinks = new CompositeLogSink(new ILogSink[]
                {
                    new VsActivityLogSink(),
                    new VsixFileLogSink("AgenteIALocal")
                });

                AgentComposition.LoggerV2 = new AgentLoggerV2(sinks);
                VsixSafeLog.Info("VSIX.Startup", "LoggerV2 pipeline configured.", null, 9000);
            }
            catch (Exception ex)
            {
                try { AgentComposition.LoggerV2 = new AgentLoggerV2(new NullLogSink()); } catch { }
                VsixSafeLog.Error("VSIX.Startup", "Failed to configure LoggerV2 pipeline.", ex, 9000);
            }

            // NUEVO - ID: 20260122_010600 - AssemblyResolve handler para Serilog dependencies
            // VSIX context: netstandard2.0 assemblies (AgenteIALocal.Logging.dll) no pueden resolver
            // sus dependencies (Serilog*.dll) automáticamente en .NET Framework 4.7.2 host.
            // Este handler intercepta cargas fallidas y apunta a VSIX extension folder.
            try
            {
                var extensionFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                VsixSafeLog.Info("VSIX.Startup", $"[DIAG] AssemblyResolve: Extension folder = {extensionFolder}", null, 9007);

                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    try
                    {
                        var assemblyName = new System.Reflection.AssemblyName(args.Name);
                        
                        // Solo interceptar Serilog assemblies
                        if (!assemblyName.Name.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase))
                            return null;

                        VsixSafeLog.Info("VSIX.Startup", $"[DIAG] AssemblyResolve: Intercepted {assemblyName.Name}, Version={assemblyName.Version}", null, 9008);

                        var dllPath = System.IO.Path.Combine(extensionFolder, assemblyName.Name + ".dll");
                        
                        if (System.IO.File.Exists(dllPath))
                        {
                            VsixSafeLog.Info("VSIX.Startup", $"[DIAG] AssemblyResolve: Loading from {dllPath}", null, 9009);
                            var loadedAssembly = System.Reflection.Assembly.LoadFrom(dllPath);
                            VsixSafeLog.Info("VSIX.Startup", $"[DIAG] AssemblyResolve: SUCCESS loaded {loadedAssembly.FullName}", null, 9010);
                            return loadedAssembly;
                        }
                        else
                        {
                            VsixSafeLog.Warning("VSIX.Startup", $"[DIAG] AssemblyResolve: NOT FOUND {dllPath}", null, 9011);
                            return null;
                        }
                    }
                    catch (Exception resolveEx)
                    {
                        VsixSafeLog.Error("VSIX.Startup", $"[DIAG] AssemblyResolve: Exception resolving {args.Name}", resolveEx, 9012);
                        return null;
                    }
                };

                VsixSafeLog.Info("VSIX.Startup", "[DIAG] AssemblyResolve: Handler registered successfully", null, 9013);
            }
            catch (Exception resolveSetupEx)
            {
                VsixSafeLog.Error("VSIX.Startup", "Failed to register AssemblyResolve handler.", resolveSetupEx, 9014);
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        // This event is raised in a variety of teardown scenarios.
                        // Logging MUST be best-effort and MUST NEVER throw.
                        var ex = e.ExceptionObject as Exception;
                        VsixSafeLog.Error("VSIX.Unhandled", "Unhandled exception", ex, 9000);
                    }
                    catch { }
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        // Raised on the finalizer thread; VS services may already be torn down.
                        // Always mark as observed and log via the always-safe path.
                        try { e.SetObserved(); } catch { }
                        VsixSafeLog.Error("VSIX.UnobservedTask", "Unobserved task exception", e.Exception, 9000);
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("VSIX.Startup", "Failed to hook global exception handlers.", ex, 9000);
            }
        }


        // MODIFICADO - ID: 20260122_010500 - Reordenado según best practice VSIX + Serilog
        // Best practice: Inicializar logging ANTES de SwitchToMainThreadAsync para capturar logs desde inicio
        // Serilog NO requiere UI thread - puede configurarse en background thread
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // PASO 1: Configurar logging PRIMERO (background thread OK)
            ConfigureLoggerV2Once(); // Legacy fallback
            ConfigureSerilogOnce(); // Serilog pipeline

            // PASO 2: Log diagnóstico ANTES de switch to main thread
            VsixSafeLog.Info("VSIX.Startup", "[DIAG] InitializeAsync: Logging ready, switching to main thread...", null, 9005);
            AgenteIALocal.Logging.Log.Information("-", 9005, "VSIX.Startup", "InitializeAsync: Logging ready, switching to main thread", null);

            // PASO 3: Switch to main thread (CON logging ya disponible)
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // PASO 4: Log diagnóstico DESPUÉS de switch to main thread
            VsixSafeLog.Info("VSIX.Startup", "[DIAG] InitializeAsync: On main thread now", null, 9006);
            AgenteIALocal.Logging.Log.Information("-", 9006, "VSIX.Startup", "InitializeAsync: On main thread now", null);

            // PASO 5: Resto de inicialización
            try
            {
                AgentComposition.EnsureComposition();
                // MODIFICADO - ID: 20260122_010800 - Migrado de AgentComposition.Info → Serilog
                AgenteIALocal.Logging.Log.Information("-", 9000, "VSIX.Startup", "VSIX initialized", null);
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("VSIX.Startup", "InitializeAsync failed.", ex, 9000);
                // MODIFICADO - ID: 20260122_010800 - Agregado log Serilog en catch
                AgenteIALocal.Logging.Log.Error("-", 9000, "VSIX.Startup", "InitializeAsync failed", ex);
            }

            await OpenAgenteIALocalCommand.InitializeAsync(this);
        }
    }
}
