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
                
                System.Diagnostics.Trace.TraceInformation("[i18n.EnsureDir] Estructura OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[i18n.EnsureDir] ERROR: {ex.Message}");
            }
        }

        private string DetectLanguage(LanguageSettings settings)
        {
            System.Diagnostics.Trace.TraceInformation($"[i18n.Detect] Current={settings.Current}, AutoDetect={settings.AutoDetect}");
            
            if (!string.IsNullOrEmpty(settings.Current) && _external.ContainsKey(settings.Current))
            {
                System.Diagnostics.Trace.TraceInformation($"[i18n.Detect] → Guardado: {settings.Current}");
                return settings.Current;
            }

            if (settings.AutoDetect)
            {
                try
                {
                    var os = System.Globalization.CultureInfo.CurrentUICulture.Name;
                    System.Diagnostics.Trace.TraceInformation($"[i18n.Detect] OS Culture: {os}");
                    
                    if (_external.ContainsKey(os))
                    {
                        System.Diagnostics.Trace.TraceInformation($"[i18n.Detect] → OS: {os}");
                        return os;
                    }
                    
                    System.Diagnostics.Trace.TraceWarning($"[i18n.Detect] OS {os} no disponible");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"[i18n.Detect] Error OS culture: {ex.Message}");
                }
            }

            System.Diagnostics.Trace.TraceInformation("[i18n.Detect] → Fallback: es-AR");
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

        private void ActivateLanguage(string code)
        {
            if (string.Equals(code, "es-AR", StringComparison.OrdinalIgnoreCase))
            {
                _active = JObject.FromObject(EmbeddedLocalization.EsAR);
            }
            else if (_external.TryGetValue(code, out var obj))
            {
                _active = obj;
            }
            else
            {
                _active = JObject.FromObject(EmbeddedLocalization.EsAR);
            }

            CurrentLanguageCode = code;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool IsLanguageAvailable(string code) => _external.ContainsKey(code) || string.Equals(code, "es-AR", StringComparison.OrdinalIgnoreCase);

        public IEnumerable<LanguageInfo> GetAvailableLanguages()
        {
            var list = _external.Keys.Select(k => new LanguageInfo { Code = k, Name = _external[k]["metadata"]?["name"]?.Value<string>() ?? k, NativeName = _external[k]["metadata"]?["nativeName"]?.Value<string>() ?? k, FlagPath = Path.Combine(_languagesRoot, "flags", "img", _external[k]["metadata"]?["flag"]?.Value<string>() ?? ""), IsAvailable = true });
            var es = new LanguageInfo { Code = "es-AR", Name = "Spanish (Argentina)", NativeName = "Español (Argentina)", FlagPath = Path.Combine(_languagesRoot, "flags", "img", "es-AR.png"), IsAvailable = true };
            return new[] { es }.Concat(list);
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
