// NUEVA CLASE LocalizationService - ID: 20260123_121930
// MODIFICADO - ID: 20260123_134500 - Agregar EnsureDirectoryStructure()
// MODIFICADO - ID: 20260123_141600 - Logs con System.Diagnostics.Trace (no Serilog por refs circulares)
// MODIFICADO - ID: 20260123_210000 - ROLLBACK System.Text.Json → Newtonsoft.Json
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgenteIALocal.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private readonly string _languagesRoot;
        private readonly LanguageSettingsStore _settingsStore;
        private readonly Dictionary<string, JObject> _external = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        private JObject _active;

        public event EventHandler LanguageChanged;

        public string CurrentLanguageCode { get; private set; }

        public LocalizationService(string languagesRoot, string languageSettingsPath)
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] Iniciando LocalizationService. Root={languagesRoot}");
                
                _languagesRoot = languagesRoot;
                _settingsStore = new LanguageSettingsStore(languageSettingsPath);
                
                // MODIFICADO - ID: 20260126_171000 - Auto-discovery dinámico de TODOS los idiomas embebidos (Reflection)
                // Razón: Escalabilidad - Agregar nuevo idioma = solo agregar propiedad en EmbeddedLocalization.cs (SIN recompilar este archivo)
                // Arquitectura: Escanea todas las propiedades estáticas públicas tipo Dictionary<string, object> en EmbeddedLocalization
                try
                {
                    var embeddedType = typeof(EmbeddedLocalization);
                    var properties = embeddedType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        .Where(p => p.PropertyType == typeof(Dictionary<string, object>));

                    System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] Auto-discovery: {properties.Count()} idiomas embebidos encontrados");

                    foreach (var prop in properties)
                    {
                        try
                        {
                            var dict = prop.GetValue(null) as Dictionary<string, object>;
                            if (dict != null && dict.ContainsKey("metadata"))
                            {
                                var metadata = dict["metadata"] as Dictionary<string, object>;
                                var code = metadata?["code"] as string ?? prop.Name;
                                
                                var jObject = Newtonsoft.Json.Linq.JObject.FromObject(dict);
                                _external[code] = jObject;
                                
                                var nativeName = metadata?["nativeName"] as string ?? code;
                                System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] ✓ {code} embebido ({nativeName}) - auto-discovery");
                            }
                        }
                        catch (Exception exProp)
                        {
                            System.Diagnostics.Trace.TraceWarning($"[i18n.Ctor] Skip property {prop.Name}: {exProp.Message}");
                        }
                    }
                    
                    System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] Total embebidos cargados: {_external.Count}");
                }
                catch (Exception exEmbed)
                {
                    System.Diagnostics.Trace.TraceWarning($"[i18n.Ctor] WARNING: auto-discovery failed: {exEmbed.Message}");
                    
                    // Fallback ultra-defensivo: cargar solo es-AR si Reflection falla
                    try
                    {
                        var esArDict = EmbeddedLocalization.EsAR;
                        _external["es-AR"] = Newtonsoft.Json.Linq.JObject.FromObject(esArDict);
                        System.Diagnostics.Trace.TraceWarning("[i18n.Ctor] Fallback: solo es-AR cargado");
                    }
                    catch { }
                }
                
                EnsureDirectoryStructure();
                LoadExternalLanguages();
                
                var settings = _settingsStore.Load();
                System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] Settings: Current={settings.Current}, AutoDetect={settings.AutoDetect}");
                
                CurrentLanguageCode = DetectLanguage(settings);
                System.Diagnostics.Trace.TraceInformation($"[i18n.Ctor] Idioma detectado: {CurrentLanguageCode}");
                
                ActivateLanguage(CurrentLanguageCode);
                WatchFiles();
                
                System.Diagnostics.Trace.TraceInformation("[i18n.Ctor] LocalizationService inicializado OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[i18n.Ctor] ERROR CRÍTICO: {ex.Message}");
                throw;
            }
        }


        // MODIFICADO METODO EnsureDirectoryStructure - ID: 20260124_002100
        // Agregar copia inicial desde instalación VSIX si runtime folders vacías
        private void EnsureDirectoryStructure()
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation($"[i18n.EnsureDir] Verificando: {_languagesRoot}");
                
                if (!Directory.Exists(_languagesRoot))
                {
                    Directory.CreateDirectory(_languagesRoot);
                    System.Diagnostics.Trace.TraceInformation($"[i18n.EnsureDir] ✓ Creada: {_languagesRoot}");
                }

                var flagsPath = Path.Combine(_languagesRoot, "flags", "img");
                if (!Directory.Exists(flagsPath))
                {
                    Directory.CreateDirectory(flagsPath);
                    System.Diagnostics.Trace.TraceInformation($"[i18n.EnsureDir] ✓ Banderas: {flagsPath}");
                }

                var esArPath = Path.Combine(_languagesRoot, "es-AR");
                if (!Directory.Exists(esArPath))
                {
                    Directory.CreateDirectory(esArPath);
                    System.Diagnostics.Trace.TraceInformation($"[i18n.EnsureDir] ✓ es-AR: {esArPath}");
                }

                var enUsPath = Path.Combine(_languagesRoot, "en-US");
                if (!Directory.Exists(enUsPath))
                {
                    Directory.CreateDirectory(enUsPath);
                    System.Diagnostics.Trace.TraceInformation($"[i18n.EnsureDir] ✓ en-US: {enUsPath}");
                }
                
                // NUEVO - ID: 20260124_002101 - Copiar archivos desde instalación VSIX si no existen
                CopyDefaultLanguageFilesFromVsixInstallation();
                
                System.Diagnostics.Trace.TraceInformation("[i18n.EnsureDir] Estructura OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[i18n.EnsureDir] ERROR: {ex.Message}");
            }
        }

        // NUEVO METODO CopyDefaultLanguageFilesFromVsixInstallation - ID: 20260124_002102
        // Copia strings.json + banderas PNG desde instalación VSIX a %LOCALAPPDATA% si no existen
        private void CopyDefaultLanguageFilesFromVsixInstallation()
        {
            try
            {
                // Obtener ruta instalación VSIX (donde está AgenteIALocal.Localization.dll)
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var vsixInstallDir = Path.GetDirectoryName(assemblyLocation);
                var vsixLanguagesDir = Path.Combine(vsixInstallDir, "Languages");

                System.Diagnostics.Trace.TraceInformation($"[i18n.CopyDefaults] VSIX dir: {vsixInstallDir}");

                if (!Directory.Exists(vsixLanguagesDir))
                {
                    System.Diagnostics.Trace.TraceWarning($"[i18n.CopyDefaults] VSIX Languages/ no existe - skip copy");
                    return;
                }

                // MODIFICADO - ID: 20260126_013000 - Copiar TODOS los idiomas disponibles en VSIX (dinámico)
                // Itera cada subdirectorio en VSIX\Languages\ y copia strings.json si no existe en runtime
                foreach (var vsixLangDir in Directory.GetDirectories(vsixLanguagesDir))
                {
                    var langCode = Path.GetFileName(vsixLangDir);
                    
                    // Skip carpeta "flags" (no es idioma)
                    if (langCode.Equals("flags", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var vsixJson = Path.Combine(vsixLangDir, "strings.json");
                    var runtimeLangDir = Path.Combine(_languagesRoot, langCode);
                    var runtimeJson = Path.Combine(runtimeLangDir, "strings.json");

                    if (File.Exists(vsixJson) && !File.Exists(runtimeJson))
                    {
                        Directory.CreateDirectory(runtimeLangDir);
                        File.Copy(vsixJson, runtimeJson, overwrite: false);
                        System.Diagnostics.Trace.TraceInformation($"[i18n.CopyDefaults] ✓ Copiado: {langCode}/strings.json ({new FileInfo(vsixJson).Length} bytes)");
                    }
                }

                // Copiar banderas PNG si no existen
                var vsixFlagsDir = Path.Combine(vsixLanguagesDir, "flags", "img");
                var runtimeFlagsDir = Path.Combine(_languagesRoot, "flags", "img");

                if (Directory.Exists(vsixFlagsDir))
                {
                    Directory.CreateDirectory(runtimeFlagsDir);

                    foreach (var vsixFlag in Directory.GetFiles(vsixFlagsDir, "*.png"))
                    {
                        var flagName = Path.GetFileName(vsixFlag);
                        var runtimeFlag = Path.Combine(runtimeFlagsDir, flagName);

                        if (!File.Exists(runtimeFlag))
                        {
                            File.Copy(vsixFlag, runtimeFlag, overwrite: false);
                            System.Diagnostics.Trace.TraceInformation($"[i18n.CopyDefaults] ✓ Copiada bandera: {flagName} ({new FileInfo(vsixFlag).Length} bytes)");
                        }
                    }
                }

                System.Diagnostics.Trace.TraceInformation("[i18n.CopyDefaults] Copy defaults OK");
            }
            catch (Exception exCopy)
            {
                // No critical - runtime puede funcionar sin archivos externos (usa embedded es-AR)
                System.Diagnostics.Trace.TraceWarning($"[i18n.CopyDefaults] Copy failed (no crítico): {exCopy.Message}");
            }
        }


        // MODIFICADO METODO DetectLanguage - ID: 20260125_003800
        // Agregar: 1) Detección VS (DTE.LocaleID), 2) Fallback idioma base (es-ES → es-AR), 3) Logs físicos
        private string DetectLanguage(LanguageSettings settings)
        {
            var diagPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgenteIALocal", "diag_i18n.txt");

            try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] DetectLanguage: Current={settings.Current}, AutoDetect={settings.AutoDetect}\r\n"); } catch { }
            
            // 1. Settings guardados (prioridad máxima)
            if (!string.IsNullOrEmpty(settings.Current))
            {
                if (_external.ContainsKey(settings.Current))
                {
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] → Guardado EXACTO: {settings.Current}\r\n"); } catch { }
                    return settings.Current;
                }
                
                // Fallback idioma base: es-ES → buscar cualquier es-* (es-AR, es-MX, etc.)
                var langBase = settings.Current.Split('-')[0]; // "es-ES" → "es"
                var fallback = _external.Keys.FirstOrDefault(k => k.StartsWith(langBase + "-", StringComparison.OrdinalIgnoreCase));
                if (fallback != null)
                {
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] → Guardado FALLBACK: {settings.Current} → {fallback}\r\n"); } catch { }
                    return fallback;
                }
            }

            // 2. AutoDetect: VS → OS → default
            if (settings.AutoDetect)
            {
                // 2a. Visual Studio locale (NUEVO - prioridad sobre OS)
                try
                {
                    // NOTA: DTE no disponible desde Localization layer (sin refs VSIX SDK)
                    // Alternativa: pasar VS locale como parámetro en constructor (futuro)
                    // Por ahora: solo OS detection
                }
                catch { }

                // 2b. Sistema Operativo
                try
                {
                    var os = System.Globalization.CultureInfo.CurrentUICulture.Name;
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] OS Culture: {os}\r\n"); } catch { }
                    
                    if (_external.ContainsKey(os))
                    {
                        try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] → OS EXACTO: {os}\r\n"); } catch { }
                        return os;
                    }
                    
                    // Fallback idioma base: es-ES → buscar cualquier es-* (es-AR, es-MX, etc.)
                    var langBase = os.Split('-')[0]; // "es-ES" → "es"
                    var fallback = _external.Keys.FirstOrDefault(k => k.StartsWith(langBase + "-", StringComparison.OrdinalIgnoreCase));
                    if (fallback != null)
                    {
                        try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] → OS FALLBACK: {os} → {fallback}\r\n"); } catch { }
                        return fallback;
                    }
                    
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] WARNING: OS {os} no disponible\r\n"); } catch { }
                }
                catch (Exception ex)
                {
                    try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] ERROR OS culture: {ex.Message}\r\n"); } catch { }
                }
            }

            // 3. Fallback final: es-AR embebido
            try { System.IO.File.AppendAllText(diagPath, $"[{DateTime.Now:HH:mm:ss.fff}] → Fallback FINAL: es-AR\r\n"); } catch { }
            return "es-AR";
        }

        private void LoadExternalLanguages()
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation($"[i18n.LoadExt] Escaneando: {_languagesRoot}");
                
                if (!Directory.Exists(_languagesRoot))
                {
                    System.Diagnostics.Trace.TraceWarning("[i18n.LoadExt] Carpeta no existe");
                    return;
                }
                
                var dirs = Directory.GetDirectories(_languagesRoot);
                System.Diagnostics.Trace.TraceInformation($"[i18n.LoadExt] {dirs.Length} carpetas");
                
                foreach (var dir in dirs)
                {
                    var code = Path.GetFileName(dir);
                    var path = Path.Combine(dir, "strings.json");
                    
                    if (!File.Exists(path)) continue;
                    
                    try
                    {
                        var txt = File.ReadAllText(path);
                        var obj = JObject.Parse(txt);
                        if (obj != null)
                        {
                            _external[code] = obj;
                            
                            var name = obj["metadata"]?["nativeName"]?.Value<string>() ?? code;
                            System.Diagnostics.Trace.TraceInformation($"[i18n.LoadExt] ✓ {code}: {name}");
                        }
                    }
                    catch (Exception exParse)
                    {
                        System.Diagnostics.Trace.TraceError($"[i18n.LoadExt] Parse error {code}: {exParse.Message}");
                    }
                }
                
                System.Diagnostics.Trace.TraceInformation($"[i18n.LoadExt] Total: {_external.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[i18n.LoadExt] ERROR: {ex.Message}");
            }
        }

        // MODIFICADO - ID: 20260126_171001 - ActivateLanguage dinámico (usa _external siempre)
        // Razón: Ya no necesita if/else hardcoded - todos los idiomas están en _external (auto-discovery)
        private void ActivateLanguage(string code)
        {
            if (_external.TryGetValue(code, out var obj))
            {
                _active = obj;
                CurrentLanguageCode = code;
            }
            else
            {
                // Fallback: primer idioma disponible en _external (normalmente es-AR)
                var fallback = _external.Keys.FirstOrDefault() ?? "es-AR";
                _active = _external.TryGetValue(fallback, out var fallbackObj) 
                    ? fallbackObj 
                    : JObject.FromObject(EmbeddedLocalization.EsAR);
                CurrentLanguageCode = fallback;
                
                System.Diagnostics.Trace.TraceWarning($"[i18n.Activate] {code} no encontrado, fallback: {fallback}");
            }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        // MODIFICADO - ID: 20260126_171002 - IsLanguageAvailable dinámico
        public bool IsLanguageAvailable(string code) => _external.ContainsKey(code);

        // MODIFICADO GetAvailableLanguages - ID: 20260126_023100
        // NUEVA ARQUITECTURA ÓPTIMA: Escanear SOLO Languages/*/strings.json (idiomas traducidos)
        // Lógica invertida: en lugar de escanear 306 PNG y filtrar, escanear carpetas traducidas
        // Ventajas: performance + sin hardcode + sin recompilar + solo muestra idiomas disponibles
        // FIX 20260126_023100: Metadata es-AR leída desde EmbeddedLocalization (NO hardcoded)
        public IEnumerable<LanguageInfo> GetAvailableLanguages()
        {
            var result = new List<LanguageInfo>();

            try
            {
                if (!Directory.Exists(_languagesRoot))
                {
                    System.Diagnostics.Trace.TraceWarning($"[i18n.GetAvailable] Languages root not found: {_languagesRoot}");
                    
                    // MODIFICADO - ID: 20260126_023100 - Leer metadata desde EmbeddedLocalization (NO hardcode)
                    result.Add(CreateEmbeddedLanguageInfo());
                    return result;
                }

                // NÚCLEO: Escanear carpetas Languages/*/ que tengan strings.json
                foreach (var langDir in Directory.GetDirectories(_languagesRoot))
                {
                    try
                    {
                        var langCode = Path.GetFileName(langDir);
                        
                        // Skip carpeta "flags" (no es idioma)
                        if (langCode.Equals("flags", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Verificar si existe strings.json (determina si idioma está traducido)
                        var stringsJson = Path.Combine(langDir, "strings.json");
                        if (!File.Exists(stringsJson))
                        {
                            System.Diagnostics.Trace.TraceInformation($"[i18n.GetAvailable] Skipping {langCode} (no strings.json)");
                            continue; // Idioma sin traducción → no mostrar
                        }

                        // Buscar bandera PNG correspondiente
                        var flagPath = Path.Combine(_languagesRoot, "flags", "img", $"{langCode}.png");
                        if (!File.Exists(flagPath))
                        {
                            System.Diagnostics.Trace.TraceWarning($"[i18n.GetAvailable] {langCode} has strings.json but missing flag PNG");
                            // Continuar de todos modos (mostrar sin bandera)
                        }

                        // Obtener metadata desde JSON (si está cargado en _external)
                        string name = langCode;
                        string nativeName = langCode;

                        if (_external.ContainsKey(langCode))
                        {
                            try
                            {
                                name = _external[langCode]["metadata"]?["name"]?.Value<string>() ?? langCode;
                                nativeName = _external[langCode]["metadata"]?["nativeName"]?.Value<string>() ?? langCode;
                            }
                            catch
                            {
                                // Fallback a langCode si metadata falla
                            }
                        }

                        result.Add(new LanguageInfo
                        {
                            Code = langCode,
                            Name = name,
                            NativeName = nativeName,
                            FlagPath = flagPath,
                            IsAvailable = true // Si llegó aquí, strings.json existe
                        });

                        System.Diagnostics.Trace.TraceInformation($"[i18n.GetAvailable] ✓ {langCode} (name={name}, flag={File.Exists(flagPath)})");
                    }
                    catch (Exception exLang)
                    {
                        System.Diagnostics.Trace.TraceWarning($"[i18n.GetAvailable] Error processing {langDir}: {exLang.Message}");
                    }
                }

                System.Diagnostics.Trace.TraceInformation($"[i18n.GetAvailable] Total: {result.Count} idiomas disponibles (dinámico, sin hardcode)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[i18n.GetAvailable] ERROR: {ex.Message}");
            }

            // MODIFICADO - ID: 20260126_023100 - Fallback final sin hardcode
            if (result.Count == 0)
            {
                result.Add(CreateEmbeddedLanguageInfo());
            }

            return result;
        }

        // NUEVO METODO CreateEmbeddedLanguageInfo - ID: 20260126_023100
        // Factory: Crea LanguageInfo para es-AR embebido leyendo metadata desde EmbeddedLocalization
        // Razón: Eliminar strings hardcoded ("Spanish (Argentina)", "Español (Argentina)")
        // Escalabilidad: Si se agregan más idiomas embebidos, este método es genérico
        private LanguageInfo CreateEmbeddedLanguageInfo()
        {
            try
            {
                var esArDict = EmbeddedLocalization.EsAR;
                var metadata = esArDict["metadata"] as Dictionary<string, object>;
                
                var code = metadata?["code"] as string ?? "es-AR";
                var name = metadata?["name"] as string ?? "es-AR";
                var nativeName = metadata?["nativeName"] as string ?? "es-AR";
                var flagFile = metadata?["flag"] as string ?? "es-AR.png";
                
                return new LanguageInfo
                {
                    Code = code,
                    Name = name,
                    NativeName = nativeName,
                    FlagPath = Path.Combine(_languagesRoot, "flags", "img", flagFile),
                    IsAvailable = true
                };
            }
            catch (Exception exFactory)
            {
                System.Diagnostics.Trace.TraceWarning($"[i18n.CreateEmbedded] Factory failed: {exFactory.Message}");
                
                // Fallback ultra-defensivo si falla lectura de metadata
                return new LanguageInfo
                {
                    Code = "es-AR",
                    Name = "es-AR",
                    NativeName = "es-AR",
                    FlagPath = Path.Combine(_languagesRoot, "flags", "img", "es-AR.png"),
                    IsAvailable = true
                };
            }
        }





        public string GetString(string key)
        {
            try
            {
                var parts = key.Split('.');
                JToken cur = _active;
                foreach (var p in parts)
                {
                    if (cur == null || cur[p] == null) return key;
                    cur = cur[p];
                }
                return cur?.Value<string>() ?? key;
            }
            catch
            {
                return key;
            }
        }

        public void SetLanguage(string code)
        {
            ActivateLanguage(code);
            var settings = new LanguageSettings { Current = code, AutoDetect = false };
            _settingsStore.Save(settings);
        }

        private void WatchFiles()
        {
            try
            {
                var watcher = new FileSystemWatcher(_languagesRoot)
                {
                    IncludeSubdirectories = true,
                    Filter = "*.json",
                    EnableRaisingEvents = true
                };
                var debounce = new System.Timers.Timer(500) { AutoReset = false };
                watcher.Changed += (s, e) => { debounce.Stop(); debounce.Start(); };
                watcher.Created += (s, e) => { debounce.Stop(); debounce.Start(); };
                watcher.Deleted += (s, e) => { debounce.Stop(); debounce.Start(); };
                debounce.Elapsed += (s, e) => { LoadExternalLanguages(); LanguageChanged?.Invoke(this, EventArgs.Empty); };
            }
            catch { }
        }
    }
}
