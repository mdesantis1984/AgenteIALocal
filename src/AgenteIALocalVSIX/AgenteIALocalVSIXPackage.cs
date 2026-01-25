using AgenteIALocalVSIX.Commands;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// COMENTADO - ID: 20260123_190000 - Localization deshabilitado temporalmente para diagnosticar UI vacía
// using AgenteIALocal.Localization;
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

        private static int _serilogConfigured;
        // COMENTADO - ID: 20260123_190000 - Localization deshabilitado
        // private static LocalizationService _localizationService;
        
        // COMENTADO - ID: 20260123_190000 - Property pública deshabilitada
        // public static ILocalizationService LocalizationService => _localizationService;
        public static object LocalizationService => null; // Placeholder para compilar

        // NUEVO METODO ConfigureSerilogOnce - ID: 20260122_000301
        // MODIFICADO - ID: 20260122_010300 - Diagnóstico mejorado para troubleshooting
        // MODIFICADO - ID: 20260123_184000 - Diagnóstico con archivo físico (Trace no visible)
        // Inicializa Serilog en paralelo al logging legacy (coexistencia temporal durante migración)
        private static void ConfigureSerilogOnce()
        {
            if (Interlocked.Exchange(ref _serilogConfigured, 1) == 1) return;

            // DIAGNÓSTICO FÍSICO: Crear archivo para verificar ejecución
            var diagPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgenteIALocal", "diag_serilog.txt");
            try
            {
                var dir = System.IO.Path.GetDirectoryName(diagPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ConfigureSerilogOnce INICIO\r\n");
            }
            catch { }

            System.Diagnostics.Trace.TraceInformation("[VSIX.Startup] ConfigureSerilogOnce: INICIO - empaquetado VSIX verificado");

            try
            {
                var settings = new AgenteIALocal.Logging.LogSettings
                {
                    AppName = "AgenteIALocal",
                    Enabled = true,
                    All = false, // niveles individuales
                    Verbose = false,
                    Debug = true, // HABILITADO para debugging SaveButton - ID: 20260122_030200
                    Information = true,
                    Warning = true,
                    Error = true,
                    Critical = true,
                    RollingFileSizeBytes = 3L * 1024 * 1024, // 3MB
                    RetainedFileCount = 10
                };

                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] Antes Log.Configure()\r\n"); } catch { }

                System.Diagnostics.Trace.TraceInformation("[VSIX.Startup] ConfigureSerilogOnce: Llamando AgenteIALocal.Logging.Log.Configure()...");

                AgenteIALocal.Logging.Log.Configure(settings);
                
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] Configure() OK\r\n"); } catch { }

                System.Diagnostics.Trace.TraceInformation("[VSIX.Startup] ConfigureSerilogOnce: Configure() exitoso - intentando primer log Serilog...");

                // Log inicial usando nuevo API Serilog
                AgenteIALocal.Logging.Log.Information("-", 9000, "VSIX.Startup", "Serilog configured successfully", null);

                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] Log.Information() OK - SUCCESS\r\n"); } catch { }

                System.Diagnostics.Trace.TraceInformation("[VSIX.Startup] ConfigureSerilogOnce: SUCCESS - Serilog.Information() ejecutado sin excepciones");
            }
            catch (System.IO.FileNotFoundException fnfEx)
            {
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR FileNotFound: {fnfEx.FileName} - {fnfEx.Message}\r\n"); } catch { }
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] ConfigureSerilogOnce: FileNotFoundException - Assembly: {fnfEx.FileName}, Message: {fnfEx.Message}");
            }
            catch (TypeLoadException tlEx)
            {
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR TypeLoad: {tlEx.TypeName} - {tlEx.Message}\r\n"); } catch { }
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] ConfigureSerilogOnce: TypeLoadException - Type: {tlEx.TypeName}, Message: {tlEx.Message}");
            }
            catch (System.Reflection.ReflectionTypeLoadException rtlEx)
            {
                var details = string.Join("; ", rtlEx.LoaderExceptions?.Select(e => e?.Message ?? "null") ?? new[] { "no loader exceptions" });
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR ReflectionTypeLoad: {details}\r\n"); } catch { }
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] ConfigureSerilogOnce: ReflectionTypeLoadException - LoaderExceptions: {details}");
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "";
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR Generic: {ex.GetType().Name} - {ex.Message}{innerMsg}\r\n"); } catch { }
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] ConfigureSerilogOnce: {ex.GetType().Name} - {ex.Message}{innerMsg}");
            }
        }

        // ELIMINADO METODO ConfigureLoggerV2Once - ID: 20260122_195500
        // Reemplazado completamente por Serilog (ConfigureSerilogOnce)
        // AssemblyResolve handler movido a ConfigureSerilogOnce
        
        // NUEVO METODO RegisterAssemblyResolveHandler - ID: 20260122_195501
        // Separado para claridad: handler AssemblyResolve + global exception handlers
        private static void RegisterAssemblyResolveHandler()
        {
            // AssemblyResolve handler para Serilog dependencies
            // VSIX context: netstandard2.0 assemblies (AgenteIALocal.Logging.dll) no pueden resolver
            // sus dependencies (Serilog*.dll) automáticamente en .NET Framework 4.7.2 host.
            try
            {
                var extensionFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                System.Diagnostics.Trace.TraceInformation($"[VSIX.Startup] AssemblyResolve: Extension folder = {extensionFolder}");

                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    try
                    {
                        var assemblyName = new System.Reflection.AssemblyName(args.Name);
                        
                        // Solo interceptar Serilog assemblies
                        if (!assemblyName.Name.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase))
                            return null;

                        System.Diagnostics.Trace.TraceInformation($"[VSIX.Startup] AssemblyResolve: Intercepted {assemblyName.Name}, Version={assemblyName.Version}");

                        var dllPath = System.IO.Path.Combine(extensionFolder, assemblyName.Name + ".dll");
                        
                        if (System.IO.File.Exists(dllPath))
                        {
                            System.Diagnostics.Trace.TraceInformation($"[VSIX.Startup] AssemblyResolve: Loading from {dllPath}");
                            var loadedAssembly = System.Reflection.Assembly.LoadFrom(dllPath);
                            System.Diagnostics.Trace.TraceInformation($"[VSIX.Startup] AssemblyResolve: SUCCESS loaded {loadedAssembly.FullName}");
                            return loadedAssembly;
                        }
                        else
                        {
                            System.Diagnostics.Trace.TraceWarning($"[VSIX.Startup] AssemblyResolve: NOT FOUND {dllPath}");
                            return null;
                        }
                    }
                    catch (Exception resolveEx)
                    {
                        System.Diagnostics.Trace.TraceError($"[VSIX.Startup] AssemblyResolve: Exception resolving {args.Name} - {resolveEx.Message}");
                        return null;
                    }
                };

                System.Diagnostics.Trace.TraceInformation("[VSIX.Startup] AssemblyResolve: Handler registered successfully");
            }
            catch (Exception resolveSetupEx)
            {
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] Failed to register AssemblyResolve handler: {resolveSetupEx.Message}");
            }

            // Global exception handlers
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try
                    {
                        var ex = e.ExceptionObject as Exception;
                        System.Diagnostics.Trace.TraceError($"[VSIX.Unhandled] Unhandled exception: {ex?.Message}");
                        AgenteIALocal.Logging.Log.Critical("-", 9999, "VSIX.Unhandled", "Unhandled AppDomain exception", ex);
                    }
                    catch (Exception exTop) { System.Diagnostics.Trace.TraceWarning("UnhandledException handler failed: " + exTop.Message); }
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    try
                    {
                        try { e.SetObserved(); } catch (Exception exObserve) { System.Diagnostics.Trace.TraceWarning("SetObserved failed: " + exObserve.Message); }
                        System.Diagnostics.Trace.TraceError($"[VSIX.UnobservedTask] Unobserved task exception: {e.Exception?.Message}");
                        AgenteIALocal.Logging.Log.Critical("-", 9999, "VSIX.UnobservedTask", "Unobserved task exception", e.Exception);
                    }
                    catch (Exception exTop) { System.Diagnostics.Trace.TraceWarning("UnobservedTaskException handler failed: " + exTop.Message); }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] Failed to hook global exception handlers: {ex.Message}");
            }
        }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // DIAGNÓSTICO CRÍTICO - ID: 20260123_184500
            // Archivo diagnóstico en PRIMER PUNTO para confirmar que InitializeAsync se ejecuta
            try
            {
                var diagPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AgenteIALocal", "diag_init.txt");
                var dir = System.IO.Path.GetDirectoryName(diagPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] === InitializeAsync INICIO ===\r\n");
            }
            catch { }

            // PASO 1: Registrar AssemblyResolve PRIMERO (antes de cargar Serilog)
            RegisterAssemblyResolveHandler();
            
            
            // PASO 2: Configurar Serilog (único sistema de logging)
            ConfigureSerilogOnce();

            // PASO 3: Log diagnóstico ANTES de switch to main thread
            AgenteIALocal.Logging.Log.Information("-", 9005, "VSIX.Startup", "InitializeAsync: Logging ready, switching to main thread", null);

            // PASO 4: Switch to main thread (CON logging ya disponible)
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // PASO 5: Log diagnóstico DESPUÉS de switch to main thread
            AgenteIALocal.Logging.Log.Information("-", 9006, "VSIX.Startup", "InitializeAsync: On main thread now", null);

            // PASO 6: Resto de inicialización
            try
            {
                // MODIFICADO - ID: 20260123_183000 - Inicializar Serilog ANTES de LocalizationService
                ConfigureSerilogOnce();
                
                /* COMENTADO - ID: 20260123_190000 - LocalizationService deshabilitado para diagnosticar UI vacía
                // DIAGNÓSTICO FÍSICO - ID: 20260123_184000
                var diagPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AgenteIALocal", "diag_i18n.txt");
                try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] LocalizationService init INICIO\r\n"); } catch { }
                
                try
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var appDataRoot = System.IO.Path.Combine(localAppData, "AgenteIALocal");
                    var languagesRoot = System.IO.Path.Combine(appDataRoot, "languages");
                    var languageSettingsPath = System.IO.Path.Combine(appDataRoot, "language.json");

                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] Paths: {languagesRoot}\r\n"); } catch { }

                    System.Diagnostics.Trace.TraceInformation($"[VSIX.i18n.Init] Paths: languages={languagesRoot}, settings={languageSettingsPath}");
                    AgenteIALocal.Logging.Log.Debug("-", 1001, "VSIX.i18n.Init", $"Paths: languages={languagesRoot}, settings={languageSettingsPath}", null);

                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] Antes new LocalizationService()\r\n"); } catch { }

                    _localizationService = new LocalizationService(languagesRoot, languageSettingsPath);

                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] LocalizationService creado OK. Idioma: {_localizationService.CurrentLanguageCode}\r\n"); } catch { }

                    System.Diagnostics.Trace.TraceInformation($"[VSIX.i18n.Init] LocalizationService OK. Idioma: {_localizationService.CurrentLanguageCode}");
                    AgenteIALocal.Logging.Log.Information("-", 1002, "VSIX.i18n.Init", $"LocalizationService OK. Idioma: {_localizationService.CurrentLanguageCode}", null);
                    
                    var available = _localizationService.GetAvailableLanguages();
                    var count = System.Linq.Enumerable.Count(available);
                    AgenteIALocal.Logging.Log.Debug("-", 1003, "VSIX.i18n.Init", $"Idiomas disponibles: {count}", null);
                }
                catch (Exception exLoc)
                {
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR i18n: {exLoc.GetType().Name} - {exLoc.Message}\r\n"); } catch { }
                    AgenteIALocal.Logging.Log.Error("-", 1099, "VSIX.i18n.Init", "Error inicializar LocalizationService (fallback embedded activo)", exLoc);
                    System.Diagnostics.Trace.TraceError($"[VSIX.i18n] Init failed: {exLoc.Message}");
                }
                */

                AgentComposition.EnsureComposition();
                AgenteIALocal.Logging.Log.Information("-", 9000, "VSIX.Startup", "VSIX initialized", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[VSIX.Startup] InitializeAsync failed: {ex.Message}");
                AgenteIALocal.Logging.Log.Error("-", 9000, "VSIX.Startup", "InitializeAsync failed", ex);
            }

            await OpenAgenteIALocalCommand.InitializeAsync(this);
        }
    }
}
