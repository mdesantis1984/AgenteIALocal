// NUEVO ARCHIVO FileAgentSettingsProvider - ID: 20260123_212000
// Implementación de IAgentSettingsProvider usando Newtonsoft.Json en Application layer
// ARQUITECTURA: Clean Architecture - persistencia JSON encapsulada, VSIX consume solo interfaz
using System;
using System.Collections.Generic;
using System.IO;
using AgenteIALocal.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgenteIALocal.Application.Settings
{
    /// <summary>
    /// Proveedor de persistencia de settings basado en archivo JSON (%LOCALAPPDATA%/AgenteIALocal/settings.json)
    /// - Schema version: v1
    /// - Preserva campos desconocidos al guardar (round-trip)
    /// - Nunca lanza excepciones desde Load/Save (defensive programming)
    /// </summary>
    public class FileAgentSettingsProvider : IAgentSettingsProvider
    {
        private const string FileName = "settings.json";
        private const string FolderName = "AgenteIALocal";
        private const string SchemaVersion = "v1";

        // Evento disparado después de guardar settings exitosamente
        public event Action<string> SettingsSaved;

        public string GetSettingsFilePath()
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? ".";
                var dir = Path.Combine(local, FolderName);
                return Path.Combine(dir, FileName);
            }
            catch
            {
                // Fallback si LocalApplicationData falla
                return Path.Combine(".", FolderName, FileName);
            }
        }

        public AgentSettings Load()
        {
            try
            {
                var path = GetSettingsFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults, false);
                    return defaults;
                }

                var text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }

                var root = JObject.Parse(text);
                var changed = false;

                // Canonicalizar claves legacy PascalCase → camelCase
                try
                {
                    JToken t;
                    if (root["version"] == null && (t = root["Version"]) != null) { root["version"] = t; changed = true; }
                    if (root["servers"] == null && (t = root["Servers"]) != null) { root["servers"] = t; changed = true; }
                    if (root["globalSettings"] == null && (t = root["GlobalSettings"]) != null) { root["globalSettings"] = t; changed = true; }
                    if (root["taskProfiles"] == null && (t = root["TaskProfiles"]) != null) { root["taskProfiles"] = t; changed = true; }
                    if (root["activeServerId"] == null && (t = root["ActiveServerId"]) != null) { root["activeServerId"] = t; changed = true; }
                }
                catch
                {
                    // Ignorar fallos de canonicalización
                }

                // Asegurar versión
                var version = root["version"]?.Value<string>();
                if (string.IsNullOrEmpty(version) || !version.Equals(SchemaVersion, StringComparison.OrdinalIgnoreCase))
                {
                    root["version"] = SchemaVersion;
                    changed = true;
                }

                // Deserializar a modelo tipado
                var settings = new AgentSettings();
                settings.Version = root["version"]?.Value<string>() ?? SchemaVersion;

                // Servers
                settings.Servers = new List<ServerConfig>();
                var serversArray = root["servers"] as JArray;
                if (serversArray != null)
                {
                    foreach (var s in serversArray)
                    {
                        try
                        {
                            var sc = s.ToObject<ServerConfig>();
                            if (sc != null) settings.Servers.Add(sc);
                        }
                        catch
                        {
                            // Continuar cargando otros entries
                        }
                    }
                }

                // GlobalSettings - MODIFICADO - ID: 20260123_225502 - Conversión JObject → DTO tipado
                settings.GlobalSettings = JObjectToGlobalSettings(root["globalSettings"] as JObject);

                // TaskProfiles - ELIMINADO (obsoleto con DTOs tipados)
                // settings.TaskProfiles = (root["taskProfiles"] as JArray) ?? new JArray();

                // ActiveServerId
                settings.ActiveServerId = root["activeServerId"]?.Value<string>();

                // ELIMINADO - ID: 20260123_225503 - _raw (obsoleto con DTOs tipados)
                // settings._raw = root;

                // Asegurar defaults
                changed |= EnsureGlobalSettings(settings);
                changed |= EnsureServers(settings);
                changed |= EnsureActiveServerId(settings);

                if (changed)
                {
                    try
                    {
                        Save(settings, false);
                    }
                    catch
                    {
                        // Ignorar fallos de auto-save durante Load
                    }
                }

                return settings;
            }
            catch
            {
                // Fallback a defaults si Load falla completamente
                try
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }
                catch
                {
                    return CreateDefaultSettings();
                }
            }
        }

        public void Save(AgentSettings settings)
        {
            Save(settings, true);
        }

        public void Save(AgentSettings settings, bool raiseEvent)
        {
            if (settings == null) return;

            try
            {
                var path = GetSettingsFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                JObject root = null;

                // Leer archivo existente para preservar unknown fields
                if (File.Exists(path))
                {
                    try
                    {
                        root = JObject.Parse(File.ReadAllText(path));
                    }
                    catch
                    {
                        root = new JObject();
                    }
                }

                if (root == null) root = new JObject();

                // Construir JSON actualizado
                try
                {
                    root["version"] = settings.Version ?? SchemaVersion;
                    root["servers"] = BuildServersArrayPreservingUnknown(root, settings);
                    
                    // MODIFICADO - ID: 20260123_225504 - Conversión DTO tipado → JObject
                    var globalToSave = GlobalSettingsToJObject(settings.GlobalSettings);
                    // Remover selectedModel legacy si existe
                    try { globalToSave.Remove("selectedModel"); } catch { }
                    root["globalSettings"] = globalToSave;
                    
                    // ELIMINADO - ID: 20260123_225505 - taskProfiles (obsoleto)
                    // root["taskProfiles"] = (settings.TaskProfiles as JArray) ?? new JArray();

                    if (!string.IsNullOrEmpty(settings.ActiveServerId))
                    {
                        root["activeServerId"] = settings.ActiveServerId;
                    }

                    // Remover claves legacy PascalCase
                    try { root.Remove("Version"); } catch { }
                    try { root.Remove("Servers"); } catch { }
                    try { root.Remove("GlobalSettings"); } catch { }
                    try { root.Remove("TaskProfiles"); } catch { }
                    try { root.Remove("ActiveServerId"); } catch { }
                }
                catch
                {
                    // Fallback si falla construcción compleja
                    root["version"] = settings.Version ?? SchemaVersion;
                    root["servers"] = BuildServersArrayBestEffort(settings);
                    // MODIFICADO - ID: 20260123_225506 - Conversión DTO → JObject en fallback
                    root["globalSettings"] = GlobalSettingsToJObject(settings.GlobalSettings);
                    // ELIMINADO - taskProfiles
                    // root["taskProfiles"] = (settings.TaskProfiles as JArray) ?? new JArray();
                    if (!string.IsNullOrEmpty(settings.ActiveServerId))
                    {
                        root["activeServerId"] = settings.ActiveServerId;
                    }
                }

                // Evitar reescritura si contenido idéntico
                var newText = root.ToString(Formatting.Indented);
                try
                {
                    if (File.Exists(path))
                    {
                        var existing = File.ReadAllText(path);
                        if (string.Equals(existing, newText, StringComparison.Ordinal))
                        {
                            return; // Sin cambios → no reescribir
                        }
                    }

                    File.WriteAllText(path, newText);

                    if (raiseEvent)
                    {
                        try
                        {
                            SettingsSaved?.Invoke("save");
                        }
                        catch
                        {
                            // Ignorar errores en event handlers
                        }
                    }
                }
                catch
                {
                    // Ignorar errores de I/O
                }
            }
            catch
            {
                // Ignorar errores globales de Save
            }
        }

        private JArray BuildServersArrayPreservingUnknown(JObject root, AgentSettings settings)
        {
            var arr = new JArray();
            if (settings == null) return arr;

            var rawServers = root?["servers"] as JArray;

            if (settings.Servers == null) return arr;

            foreach (var s in settings.Servers)
            {
                if (s == null) continue;

                JObject baseObj = null;

                // Buscar server existente en raw para preservar unknown fields
                if (rawServers != null)
                {
                    foreach (var token in rawServers)
                    {
                        var o = token as JObject;
                        if (o == null) continue;

                        var rawId = o["id"]?.Value<string>() ?? o["Id"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(rawId) && string.Equals(rawId, s.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            baseObj = (JObject)o.DeepClone();
                            break;
                        }
                    }
                }

                var merged = baseObj ?? new JObject();

                // Actualizar campos conocidos (canonical keys)
                merged["id"] = s.Id ?? string.Empty;
                merged["name"] = s.Name ?? string.Empty;
                merged["provider"] = s.Provider ?? string.Empty;
                merged["baseUrl"] = s.BaseUrl ?? string.Empty;
                merged["apiKey"] = s.ApiKey ?? string.Empty;
                merged["model"] = s.Model ?? string.Empty;
                merged["isDefault"] = s.IsDefault;

                var created = s.CreatedAt == default(DateTime) ? DateTime.UtcNow : s.CreatedAt;
                merged["createdAt"] = created.ToUniversalTime().ToString("o");

                // Remover duplicados PascalCase
                TryRemove(merged, "Id");
                TryRemove(merged, "Name");
                TryRemove(merged, "Provider");
                TryRemove(merged, "BaseUrl");
                TryRemove(merged, "ApiKey");
                TryRemove(merged, "Model");
                TryRemove(merged, "IsDefault");
                TryRemove(merged, "CreatedAt");

                arr.Add(merged);
            }

            return arr;
        }

        private JArray BuildServersArrayBestEffort(AgentSettings settings)
        {
            var arr = new JArray();
            if (settings == null || settings.Servers == null) return arr;

            foreach (var s in settings.Servers)
            {
                if (s == null) continue;

                var o = new JObject
                {
                    ["id"] = s.Id ?? string.Empty,
                    ["name"] = s.Name ?? string.Empty,
                    ["provider"] = s.Provider ?? string.Empty,
                    ["baseUrl"] = s.BaseUrl ?? string.Empty,
                    ["apiKey"] = s.ApiKey ?? string.Empty,
                    ["model"] = s.Model ?? string.Empty,
                    ["isDefault"] = s.IsDefault,
                    ["createdAt"] = (s.CreatedAt == default(DateTime) ? DateTime.UtcNow : s.CreatedAt).ToUniversalTime().ToString("o")
                };

                arr.Add(o);
            }

            return arr;
        }

        private void TryRemove(JObject o, string key)
        {
            try
            {
                if (o != null && !string.IsNullOrWhiteSpace(key))
                {
                    o.Remove(key);
                }
            }
            catch
            {
                // Ignorar
            }
        }

        // MODIFICADO METODO EnsureGlobalSettings - ID: 20260123_225507
        // Adaptado para trabajar con GlobalSettings DTO tipado
        private bool EnsureGlobalSettings(AgentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var changed = false;

            if (settings.GlobalSettings == null)
            {
                settings.GlobalSettings = new Core.Configuration.GlobalSettings();
                changed = true;
            }

            // Asegurar runMode válido
            if (string.IsNullOrWhiteSpace(settings.GlobalSettings.RunMode))
            {
                settings.GlobalSettings.RunMode = "preguntar";
                changed = true;
            }

            // Forzar stream=true
            if (settings.GlobalSettings.RequestDefaults.Stream != true)
            {
                settings.GlobalSettings.RequestDefaults.Stream = true;
                changed = true;
            }

            return changed;
        }

        private bool EnsureServers(AgentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var changed = false;

            if (settings.Servers == null)
            {
                settings.Servers = new List<ServerConfig>();
                changed = true;
            }

            var hasLmstudio = false;
            var hasJan = false;

            foreach (var server in settings.Servers)
            {
                if (server == null) continue;

                if (string.Equals(server.Provider, "lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    hasLmstudio = true;
                }

                if (string.Equals(server.Provider, "jan", StringComparison.OrdinalIgnoreCase))
                {
                    hasJan = true;
                }
            }

            if (!hasLmstudio)
            {
                settings.Servers.Add(new ServerConfig
                {
                    Id = "lmstudio-local",
                    Name = "LM Studio (local)",
                    Provider = "lmstudio",
                    BaseUrl = "http://127.0.0.1:1234",
                    ApiKey = string.Empty,
                    Model = string.Empty,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            if (!hasJan)
            {
                settings.Servers.Add(new ServerConfig
                {
                    Id = "jan-local",
                    Name = "Jan (local)",
                    Provider = "jan",
                    BaseUrl = "http://127.0.0.1:1337",
                    ApiKey = string.Empty,
                    Model = string.Empty,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                });
                changed = true;
            }

            return changed;
        }

        private bool EnsureActiveServerId(AgentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.Servers == null || settings.Servers.Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(settings.ActiveServerId))
            {
                foreach (var server in settings.Servers)
                {
                    if (server == null) continue;
                    if (string.Equals(server.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))
                    {
                        return false; // ActiveServerId válido
                    }
                }
            }

            // Fallback a lmstudio-local o primer server
            string fallbackId = null;
            foreach (var server in settings.Servers)
            {
                if (server == null) continue;

                if (string.Equals(server.Id, "lmstudio-local", StringComparison.OrdinalIgnoreCase))
                {
                    fallbackId = server.Id;
                    break;
                }

                if (fallbackId == null)
                {
                    fallbackId = server.Id;
                }
            }

            if (string.IsNullOrEmpty(fallbackId))
            {
                return false;
            }

            settings.ActiveServerId = fallbackId;
            return true;
        }

        // NUEVO METODO JObjectToGlobalSettings - ID: 20260123_225500
        // Conversión JObject → GlobalSettings (DTO tipado)
        // ARQUITECTURA: Application layer maneja deserialización JSON → DTOs puros
        private Core.Configuration.GlobalSettings JObjectToGlobalSettings(JObject jobj)
        {
            if (jobj == null) return new Core.Configuration.GlobalSettings();

            var settings = new Core.Configuration.GlobalSettings();

            try
            {
                // RunMode
                settings.RunMode = jobj["runMode"]?.Value<string>() ?? "preguntar";

                // RequestDefaults
                var reqDefaults = jobj["requestDefaults"] as JObject;
                if (reqDefaults != null)
                {
                    settings.RequestDefaults.Temperature = reqDefaults["temperature"]?.Value<double?>() ?? 0.2;
                    settings.RequestDefaults.MaxTokens = reqDefaults["maxTokens"]?.Value<int?>() ?? 0;
                    settings.RequestDefaults.Stream = reqDefaults["stream"]?.Value<bool?>() ?? true;

                    var streamOpts = reqDefaults["streamOptions"] as JObject;
                    if (streamOpts != null)
                    {
                        settings.RequestDefaults.StreamOptions.IncludeUsage = streamOpts["includeUsage"]?.Value<bool?>() ?? false;
                    }
                }

                // Agent
                var agent = jobj["agent"] as JObject;
                if (agent != null)
                {
                    settings.Agent.IdeIntegration = agent["ideIntegration"]?.Value<bool?>() ?? true;
                    settings.Agent.ApplyChanges = agent["applyChanges"]?.Value<bool?>() ?? false;
                    settings.Agent.MaxSteps = agent["maxSteps"]?.Value<int?>() ?? 5;
                }

                // Logging
                var logging = jobj["logging"] as JObject;
                if (logging != null)
                {
                    settings.Logging.Enabled = logging["enabled"]?.Value<bool?>() ?? false;
                    settings.Logging.Verbose = logging["verbose"]?.Value<bool?>() ?? false;
                    settings.Logging.Debug = logging["debug"]?.Value<bool?>() ?? false;
                    settings.Logging.Information = logging["information"]?.Value<bool?>() ?? false;
                    settings.Logging.Warning = logging["warning"]?.Value<bool?>() ?? false;
                    settings.Logging.Error = logging["error"]?.Value<bool?>() ?? true;
                    settings.Logging.Critical = logging["critical"]?.Value<bool?>() ?? true;
                }
            }
            catch
            {
                // Si falla conversión parcial, retornar con defaults
            }

            return settings;
        }

        // NUEVO METODO GlobalSettingsToJObject - ID: 20260123_225501
        // Conversión GlobalSettings (DTO tipado) → JObject
        // ARQUITECTURA: Application layer maneja serialización DTOs → JSON
        private JObject GlobalSettingsToJObject(Core.Configuration.GlobalSettings settings)
        {
            if (settings == null) return new JObject();

            var jobj = new JObject();

            try
            {
                jobj["runMode"] = settings.RunMode;

                // RequestDefaults
                var reqDefaults = new JObject();
                reqDefaults["temperature"] = settings.RequestDefaults.Temperature;
                reqDefaults["maxTokens"] = settings.RequestDefaults.MaxTokens;
                reqDefaults["stream"] = settings.RequestDefaults.Stream;

                var streamOpts = new JObject();
                streamOpts["includeUsage"] = settings.RequestDefaults.StreamOptions.IncludeUsage;
                reqDefaults["streamOptions"] = streamOpts;

                jobj["requestDefaults"] = reqDefaults;

                // Agent
                var agent = new JObject();
                agent["ideIntegration"] = settings.Agent.IdeIntegration;
                agent["applyChanges"] = settings.Agent.ApplyChanges;
                agent["maxSteps"] = settings.Agent.MaxSteps;
                jobj["agent"] = agent;

                // Logging
                var logging = new JObject();
                logging["enabled"] = settings.Logging.Enabled;
                logging["verbose"] = settings.Logging.Verbose;
                logging["debug"] = settings.Logging.Debug;
                logging["information"] = settings.Logging.Information;
                logging["warning"] = settings.Logging.Warning;
                logging["error"] = settings.Logging.Error;
                logging["critical"] = settings.Logging.Critical;
                jobj["logging"] = logging;
            }
            catch
            {
                // Si falla conversión parcial, retornar lo que se pueda
            }

            return jobj;
        }

        private AgentSettings CreateDefaultSettings()
        {
            var s = new AgentSettings();
            s.Version = SchemaVersion;
            s.Servers = new List<ServerConfig>
            {
                new ServerConfig
                {
                    Id = "lmstudio-local",
                    Name = "LM Studio (local)",
                    Provider = "lmstudio",
                    BaseUrl = "http://127.0.0.1:1234",
                    ApiKey = string.Empty,
                    Model = string.Empty,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                },
                new ServerConfig
                {
                    Id = "jan-local",
                    Name = "Jan (local)",
                    Provider = "jan",
                    BaseUrl = "http://127.0.0.1:1337",
                    ApiKey = string.Empty,
                    Model = string.Empty,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            // MODIFICADO - ID: 20260123_230000 - Usar DTOs en lugar de JObject
            s.GlobalSettings = new Core.Configuration.GlobalSettings
            {
                RunMode = "preguntar",
                RequestDefaults = new Core.Configuration.RequestDefaultsSettings
                {
                    Stream = true,
                    Temperature = 0.2,
                    MaxTokens = 0,
                    StreamOptions = new Core.Configuration.StreamOptionsSettings
                    {
                        IncludeUsage = false
                    }
                },
                Agent = new Core.Configuration.AgentBehaviorSettings
                {
                    IdeIntegration = true,
                    ApplyChanges = false,
                    MaxSteps = 5
                },
                Logging = new AgenteIALocal.Logging.LogSettings
                {
                    Enabled = false,
                    Verbose = false,
                    Debug = false,
                    Information = false,
                    Warning = false,
                    Error = true,
                    Critical = true
                }
            };

            s.ActiveServerId = s.Servers[0].Id;

            return s;
        }
    }
}
