using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgenteIALocalVSIX
{
    /// <summary>
    /// Lightweight settings store that persists a versioned JSON settings file under %LOCALAPPDATA%\AgenteIALocal\settings.json
    /// - Schema version: v1
    /// - Preserves unknown fields when saving
    /// - Never throws from Load/Save
    /// </summary>
    public static class AgentSettingsStore
    {
        private const string FileName = "settings.json";
        private const string FolderName = "AgenteIALocal";
        private const string SchemaVersion = "v1";

        public static string GetSettingsFilePath()
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? ".";
                var dir = Path.Combine(local, FolderName);
                return Path.Combine(dir, FileName);
            }
            catch
            {
                return Path.Combine(".", FolderName, FileName);
            }
        }

        public static AgentSettings Load()
        {
            try
            {
                var path = GetSettingsFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    // create default file
                    var defaults = CreateDefaultSettings();
                    var j = JObject.FromObject(defaults, JsonSerializer.CreateDefault(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
                    // ensure version
                    j["version"] = SchemaVersion;
                    File.WriteAllText(path, j.ToString(Formatting.Indented));
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

                // Ensure version present and supported
                var version = root.Value<string>("version");
                if (string.IsNullOrEmpty(version) || !version.Equals(SchemaVersion, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to upgrade minimally: set version if missing
                    root["version"] = SchemaVersion;
                    changed = true;
                }

                // Deserialize known parts into typed model but keep root for unknown fields preservation
                var settings = new AgentSettings();
                settings.Version = root.Value<string>("version") ?? SchemaVersion;

                // servers
                settings.Servers = new List<ServerConfig>();
                var serversToken = root["servers"] as JArray;
                if (serversToken != null)
                {
                    foreach (var s in serversToken)
                    {
                        try
                        {
                            var sc = s.ToObject<ServerConfig>();
                            if (sc != null) settings.Servers.Add(sc);
                        }
                        catch { }
                    }
                }

                // globalSettings
                settings.GlobalSettings = root["globalSettings"] as JObject ?? new JObject();

                // taskProfiles
                settings.TaskProfiles = root["taskProfiles"] as JArray ?? new JArray();

                // activeServerId
                settings.ActiveServerId = root.Value<string>("activeServerId");

                // preserve raw root
                settings._raw = root;

                changed |= EnsureGlobalSettings(settings);
                changed |= EnsureServers(settings);
                changed |= EnsureActiveServerId(settings);

                if (changed)
                {
                    try
                    {
                        Save(settings);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                return settings;
            }
            catch
            {
                try
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }
                catch
                {
                    // final fallback
                    return CreateDefaultSettings();
                }
            }
        }

        public static void Save(AgentSettings settings)
        {
            if (settings == null) return;

            try
            {
                var path = GetSettingsFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                JObject root = null;

                // If we have preserved raw JSON from Load, start from it to preserve unknown fields
                if (settings._raw != null)
                {
                    root = (JObject)settings._raw.DeepClone();
                }
                else if (File.Exists(path))
                {
                    try { root = JObject.Parse(File.ReadAllText(path)); } catch { root = new JObject(); }
                }

                if (root == null) root = new JObject();

                root["version"] = settings.Version ?? SchemaVersion;

                // servers
                var arr = new JArray();
                if (settings.Servers != null)
                {
                    foreach (var s in settings.Servers)
                    {
                        try { arr.Add(JObject.FromObject(s)); } catch { }
                    }
                }
                root["servers"] = arr;

                // globalSettings
                root["globalSettings"] = settings.GlobalSettings ?? new JObject();

                // taskProfiles
                root["taskProfiles"] = settings.TaskProfiles ?? new JArray();

                // activeServerId
                if (!string.IsNullOrEmpty(settings.ActiveServerId)) root["activeServerId"] = settings.ActiveServerId;

                File.WriteAllText(path, root.ToString(Formatting.Indented));
            }
            catch
            {
                // never throw
            }
        }

        // NUEVO METODO EnsureGlobalSettings - ID: 20250304_120000
        private static bool EnsureGlobalSettings(AgentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var changed = false;
            var global = settings.GlobalSettings;

            if (global == null || global.Type != JTokenType.Object)
            {
                global = new JObject();
                settings.GlobalSettings = global;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(global.Value<string>("runMode")))
            {
                global["runMode"] = "preguntar";
                changed = true;
            }

            var requestDefaultsToken = global["requestDefaults"];
            var requestDefaults = requestDefaultsToken as JObject;
            if (requestDefaults == null)
            {
                requestDefaults = new JObject();
                global["requestDefaults"] = requestDefaults;
                changed = true;
            }

            if (requestDefaults["stream"] == null || requestDefaults["stream"].Type == JTokenType.Null || requestDefaults["stream"].Type == JTokenType.Undefined)
            {
                requestDefaults["stream"] = true;
                changed = true;
            }

            if (requestDefaults["temperature"] == null || requestDefaults["temperature"].Type == JTokenType.Null || requestDefaults["temperature"].Type == JTokenType.Undefined)
            {
                requestDefaults["temperature"] = 0.2;
                changed = true;
            }

            if (requestDefaults["maxTokens"] == null || requestDefaults["maxTokens"].Type == JTokenType.Null || requestDefaults["maxTokens"].Type == JTokenType.Undefined)
            {
                requestDefaults["maxTokens"] = 0;
                changed = true;
            }

            var agentToken = global["agent"];
            var agent = agentToken as JObject;
            if (agent == null)
            {
                agent = new JObject();
                global["agent"] = agent;
                changed = true;
            }

            if (agent["ideIntegration"] == null || agent["ideIntegration"].Type == JTokenType.Null || agent["ideIntegration"].Type == JTokenType.Undefined)
            {
                agent["ideIntegration"] = true;
                changed = true;
            }

            if (agent["applyChanges"] == null || agent["applyChanges"].Type == JTokenType.Null || agent["applyChanges"].Type == JTokenType.Undefined)
            {
                agent["applyChanges"] = false;
                changed = true;
            }

            if (agent["maxSteps"] == null || agent["maxSteps"].Type == JTokenType.Null || agent["maxSteps"].Type == JTokenType.Undefined)
            {
                agent["maxSteps"] = 5;
                changed = true;
            }

            return changed;
        }

        // NUEVO METODO EnsureServers - ID: 20250304_120000
        private static bool EnsureServers(AgentSettings settings)
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
            ServerConfig lmstudioById = null;
            ServerConfig janById = null;

            foreach (var server in settings.Servers)
            {
                if (server == null) continue;

                if (lmstudioById == null && string.Equals(server.Id, "lmstudio-local", StringComparison.OrdinalIgnoreCase))
                {
                    lmstudioById = server;
                }

                if (janById == null && string.Equals(server.Id, "jan-local", StringComparison.OrdinalIgnoreCase))
                {
                    janById = server;
                }

                if (string.Equals(server.Provider, "lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    hasLmstudio = true;
                }

                if (string.Equals(server.Provider, "jan", StringComparison.OrdinalIgnoreCase))
                {
                    hasJan = true;
                }
            }

            if (lmstudioById != null && string.IsNullOrWhiteSpace(lmstudioById.Provider))
            {
                lmstudioById.Provider = "lmstudio";
                hasLmstudio = true;
                changed = true;
            }

            if (janById != null && string.IsNullOrWhiteSpace(janById.Provider))
            {
                janById.Provider = "jan";
                hasJan = true;
                changed = true;
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

        // NUEVO METODO EnsureActiveServerId - ID: 20250304_120000
        private static bool EnsureActiveServerId(AgentSettings settings)
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
                        return false;
                    }
                }
            }

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

        private static AgentSettings CreateDefaultSettings()
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

            s.GlobalSettings = new JObject
            {
                ["defaultTimeoutMs"] = 60000,
                ["useProxy"] = false,
                ["runMode"] = "preguntar",
                ["requestDefaults"] = new JObject
                {
                    ["stream"] = true,
                    ["temperature"] = 0.2,
                    ["maxTokens"] = 0
                },
                ["agent"] = new JObject
                {
                    ["ideIntegration"] = true,
                    ["applyChanges"] = false,
                    ["maxSteps"] = 5
                }
            };

            s.TaskProfiles = new JArray();

            // default active server
            s.ActiveServerId = s.Servers[0].Id;

            // prepare raw representation for future preservation
            var root = JObject.FromObject(new
            {
                version = s.Version,
                servers = s.Servers,
                globalSettings = s.GlobalSettings,
                taskProfiles = s.TaskProfiles,
                activeServerId = s.ActiveServerId
            });

            s._raw = root;

            // Also persist to disk immediately
            try
            {
                var path = GetSettingsFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, root.ToString(Formatting.Indented));
            }
            catch
            {
                // ignore
            }

            return s;
        }
    }

    public class AgentSettings
    {
        public string Version { get; set; }
        public List<ServerConfig> Servers { get; set; }
        public JObject GlobalSettings { get; set; }
        public JArray TaskProfiles { get; set; }

        // active server id
        public string ActiveServerId { get; set; }

        // internal raw JSON to preserve unknown fields
        internal JObject _raw;
    }

    public class ServerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        // optional model identifier per-server
        public string Model { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
