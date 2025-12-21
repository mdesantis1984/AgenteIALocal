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

                // Ensure version present and supported
                var version = root.Value<string>("version");
                if (string.IsNullOrEmpty(version) || !version.Equals(SchemaVersion, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to upgrade minimally: set version if missing
                    root["version"] = SchemaVersion;
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

                // preserve raw root
                settings._raw = root;

                // If no servers, populate default and save
                if (settings.Servers.Count == 0)
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
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

                File.WriteAllText(path, root.ToString(Formatting.Indented));
            }
            catch
            {
                // never throw
            }
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
                    BaseUrl = "http://127.0.0.1:8080",
                    ApiKey = string.Empty,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            s.GlobalSettings = new JObject
            {
                ["defaultTimeoutMs"] = 60000,
                ["useProxy"] = false
            };

            s.TaskProfiles = new JArray();

            // prepare raw representation for future preservation
            var root = JObject.FromObject(new
            {
                version = s.Version,
                servers = s.Servers,
                globalSettings = s.GlobalSettings,
                taskProfiles = s.TaskProfiles
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
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
