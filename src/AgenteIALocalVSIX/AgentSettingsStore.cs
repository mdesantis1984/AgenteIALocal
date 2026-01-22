using System;
using System.Collections.Generic;
using System.IO;
using AgenteIALocal.Core.Logging;
using AgenteIALocal.Infrastructure.LoggingV2;
using Microsoft.VisualStudio.Shell;
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
        // In-process notification: raised after settings are saved. Argument contains reason (e.g. "save").
        internal static event Action<string> SettingsSaved;

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
                    // Ensure the first write uses canonical (camelCase) keys.
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

                // Canonicalize legacy PascalCase keys to camelCase to avoid duplication/mismatch
                try
                {
                    // If canonical key missing but PascalCase present, copy value and mark changed
                    JToken t;
                    if (root["version"] == null && (t = root["Version"]) != null) { root["version"] = t; changed = true; }
                    if (root["servers"] == null && (t = root["Servers"]) != null) { root["servers"] = t; changed = true; }
                    if (root["globalSettings"] == null && (t = root["GlobalSettings"]) != null) { root["globalSettings"] = t; changed = true; }
                    if (root["taskProfiles"] == null && (t = root["TaskProfiles"]) != null) { root["taskProfiles"] = t; changed = true; }
                    if (root["activeServerId"] == null && (t = root["ActiveServerId"]) != null) { root["activeServerId"] = t; changed = true; }
                }
                catch
                {
                    // ignore canonicalization failures
                }

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
                        catch (Exception ex)
                        {
                            // keep loading other entries
                            VsixSafeLog.Error("Settings.Load.ServerParse", "Failed to parse server entry", ex, 9102);
                        }
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
                        // Auto-save during Load should not raise SettingsSaved events to avoid startup recursion
                        Save(settings, false);
                    }
                    catch (Exception ex)
                    {
                        VsixSafeLog.Error("Settings.Load.AutoSave", "Auto-save during Load failed", ex, 9103);
                    }
                }

                return settings;
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("Settings.Load", "Load failed; falling back to defaults", ex, 9101);
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
            Save(settings, true);
        }

        // Internal overload that controls whether to raise SettingsSaved event after writing
        internal static void Save(AgentSettings settings, bool raiseEvent)
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

                // Ensure canonical keys and remove legacy PascalCase duplicates before writing
                try
                {
                    // set canonical keys
                    root["version"] = settings.Version ?? SchemaVersion;

                    // servers
                    root["servers"] = BuildServersArrayPreservingUnknown(root, settings);

                    // globalSettings
                    root["globalSettings"] = settings.GlobalSettings ?? new JObject();

                    // taskProfiles
                    root["taskProfiles"] = settings.TaskProfiles ?? new JArray();

                    // activeServerId
                    if (!string.IsNullOrEmpty(settings.ActiveServerId)) root["activeServerId"] = settings.ActiveServerId;

                    // Remove legacy PascalCase duplicate keys if present
                    try { root.Remove("Version"); } catch { }
                    try { root.Remove("Servers"); } catch { }
                    try { root.Remove("GlobalSettings"); } catch { }
                    try { root.Remove("TaskProfiles"); } catch { }
                    try { root.Remove("ActiveServerId"); } catch { }
                }
                catch
                {
                    // fallback to previous behavior if anything unexpected
                    root["version"] = settings.Version ?? SchemaVersion;
                    root["servers"] = BuildServersArrayBestEffort(settings);
                    root["globalSettings"] = settings.GlobalSettings ?? new JObject();
                    root["taskProfiles"] = settings.TaskProfiles ?? new JArray();
                    if (!string.IsNullOrEmpty(settings.ActiveServerId)) root["activeServerId"] = settings.ActiveServerId;
                }

                // Prepare final text and avoid writing if identical to existing file to prevent event storms
                var newText = root.ToString(Formatting.Indented);
                try
                {
                    if (File.Exists(path))
                    {
                        var existing = File.ReadAllText(path);
                        if (string.Equals(existing, newText, StringComparison.Ordinal))
                        {
                            // no change -> do not rewrite or raise event
                            return;
                        }
                    }

                    File.WriteAllText(path, newText);
                    if (raiseEvent)
                    {
                        try { SettingsSaved?.Invoke("save"); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    VsixSafeLog.Error("Settings.Save.IO", "Save failed while writing settings.json", ex, 9111);
                }
            }
            catch (Exception ex)
            {
                VsixSafeLog.Error("Settings.Save", "Save failed", ex, 9110);
            }
        }

        // NUEVO METODO BuildServersArrayPreservingUnknown - ID: 20260121_081100
        private static JArray BuildServersArrayPreservingUnknown(JObject root, AgentSettings settings)
        {
            var arr = new JArray();
            if (settings == null) return arr;

            var rawServers = root != null ? (root["servers"] as JArray) : null;

            if (settings.Servers == null) return arr;

            foreach (var s in settings.Servers)
            {
                if (s == null) continue;

                JObject baseObj = null;
                if (rawServers != null)
                {
                    foreach (var token in rawServers)
                    {
                        var o = token as JObject;
                        if (o == null) continue;

                        var rawId = o.Value<string>("id") ?? o.Value<string>("Id");
                        if (!string.IsNullOrWhiteSpace(rawId) && string.Equals(rawId, s.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            baseObj = (JObject)o.DeepClone();
                            break;
                        }
                    }
                }

                var merged = baseObj ?? new JObject();

                // canonical keys
                merged["id"] = s.Id ?? string.Empty;
                merged["name"] = s.Name ?? string.Empty;
                merged["provider"] = s.Provider ?? string.Empty;
                merged["baseUrl"] = s.BaseUrl ?? string.Empty;
                merged["apiKey"] = s.ApiKey ?? string.Empty;
                merged["model"] = s.Model ?? string.Empty;
                merged["isDefault"] = s.IsDefault;

                // Ensure stable, JSON-friendly timestamp
                var created = s.CreatedAt == default(DateTime) ? DateTime.UtcNow : s.CreatedAt;
                merged["createdAt"] = created.ToUniversalTime().ToString("o");

                // Remove legacy PascalCase duplicates but preserve unknown fields
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

        // NUEVO METODO BuildServersArrayBestEffort - ID: 20260121_081100
        private static JArray BuildServersArrayBestEffort(AgentSettings settings)
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

        // NUEVO METODO TryRemove - ID: 20260121_081100
        private static void TryRemove(JObject o, string key)
        {
            try
            {
                if (o == null || string.IsNullOrWhiteSpace(key)) return;
                o.Remove(key);
            }
            catch
            {
                // ignore
            }
        }

        // NUEVO METODO EnsureGlobalSettings - ID: 20250304_120000
        // MODIFICADO METODO EnsureGlobalSettings - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
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

            // FORZAR stream=true (stream-only mode)
            try
            {
                var currentStream = requestDefaults.Value<bool?>("stream");
                if (!currentStream.HasValue || currentStream.Value != true)
                {
                    requestDefaults["stream"] = true;
                    changed = true;
                }
            }
            catch
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

            // Ensure streamOptions.includeUsage exists (default false) - used by LM Studio streaming requests
            try
            {
                var streamOptions = requestDefaults["streamOptions"] as JObject;
                if (streamOptions == null)
                {
                    streamOptions = new JObject();
                    requestDefaults["streamOptions"] = streamOptions;
                    changed = true;
                }

                if (streamOptions["includeUsage"] == null || streamOptions["includeUsage"].Type == JTokenType.Null || streamOptions["includeUsage"].Type == JTokenType.Undefined)
                {
                    streamOptions["includeUsage"] = false;
                    changed = true;
                }
            }
            catch
            {
                // ignore
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

            // Logging defaults (Serilog wrapper). This enables end-to-end diagnostics without requiring UI work.
            try
            {
                var logging = global["logging"] as JObject;
                if (logging == null)
                {
                    logging = new JObject();
                    global["logging"] = logging;
                    changed = true;
                }

                if (logging["enabled"] == null || logging["enabled"].Type == JTokenType.Null || logging["enabled"].Type == JTokenType.Undefined)
                {
                    logging["enabled"] = true;
                    changed = true;
                }

                // Optional: "on" | "off" (string). If omitted, wrapper uses per-level flags.
                if (logging["all"] == null || logging["all"].Type == JTokenType.Null || logging["all"].Type == JTokenType.Undefined)
                {
                    logging["all"] = "off";
                    changed = true;
                }

                var levels = logging["levels"] as JObject;
                if (levels == null)
                {
                    levels = new JObject();
                    logging["levels"] = levels;
                    changed = true;
                }

                if (levels["verbose"] == null) { levels["verbose"] = false; changed = true; }
                if (levels["debug"] == null) { levels["debug"] = false; changed = true; }
                if (levels["info"] == null) { levels["info"] = true; changed = true; }
                if (levels["warning"] == null) { levels["warning"] = true; changed = true; }
                if (levels["error"] == null) { levels["error"] = true; changed = true; }
                if (levels["critical"] == null) { levels["critical"] = true; changed = true; }
            }
            catch (Exception ex)
            {
                VsixSafeLog.Warning("Settings.Load.LoggingDefaults", "Failed to ensure globalSettings.logging defaults", ex, 9104);
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
                },
                ["logging"] = new JObject
                {
                    ["enabled"] = true,
                    ["all"] = "off",
                    ["levels"] = new JObject
                    {
                        ["verbose"] = false,
                        ["debug"] = false,
                        ["info"] = true,
                        ["warning"] = true,
                        ["error"] = true,
                        ["critical"] = true
                    }
                }
            };

            s.TaskProfiles = new JArray();

            // default active server
            s.ActiveServerId = s.Servers[0].Id;

            // prepare raw representation for future preservation (canonical keys)
            var root = new JObject
            {
                ["version"] = s.Version,
                ["servers"] = BuildServersArrayBestEffort(s),
                ["globalSettings"] = s.GlobalSettings,
                ["taskProfiles"] = s.TaskProfiles,
                ["activeServerId"] = s.ActiveServerId
            };

            s._raw = root;

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

    /// <summary>
    /// Minimal, always-safe logger for early startup paths.
    /// - Never throws
    /// - Writes to VS ActivityLog when available
    /// - Writes to %LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log
    /// </summary>
    internal static class VsixSafeLog
    {
        private static readonly object Gate = new object();
        private static VsixFileLogSink _fileSink;

        private static void EnsureFileSink()
        {
            if (_fileSink != null) return;

            lock (Gate)
            {
                if (_fileSink != null) return;
                try { _fileSink = new VsixFileLogSink("AgenteIALocal"); }
                catch { _fileSink = null; }
            }
        }

        internal static void Info(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Info, source, message, ex, eventId);

        internal static void Warning(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Warning, source, message, ex, eventId);

        internal static void Error(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Error, source, message, ex, eventId);

        internal static void Critical(string source, string message, Exception ex = null, int eventId = 9000)
            => Write(LogLevel.Critical, source, message, ex, eventId);

        private static void Write(LogLevel level, string source, string message, Exception ex, int eventId)
        {
            // Always safe: never throw.
            try
            {
                var src = string.IsNullOrWhiteSpace(source) ? "AgenteIALocal" : source.Trim();
                var msg = message ?? string.Empty;

                try
                {
                    var detail = ex == null ? msg : msg + Environment.NewLine + ex;

                    if (level == LogLevel.Error || level == LogLevel.Critical)
                        ActivityLog.LogError(src, detail);
                    else if (level == LogLevel.Warning)
                        ActivityLog.LogWarning(src, detail);
                    else
                        ActivityLog.LogInformation(src, detail);
                }
                catch { }

                try
                {
                    EnsureFileSink();
                    if (_fileSink == null) return;

                    var entry = new LogEntry(
                        timestampUtc: DateTime.UtcNow,
                        level: level,
                        correlationId: "-",
                        eventId: new LogEventId(eventId, src),
                        message: msg,
                        exception: ex,
                        source: new LogSource(assemblyName: nameof(AgenteIALocalVSIX), @namespace: nameof(AgenteIALocalVSIX), typeName: nameof(VsixSafeLog), memberName: "Write"),
                        context: null);

                    _fileSink.Write(entry);
                }
                catch { }
            }
            catch { }
        }
    }
}
