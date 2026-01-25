using System;

namespace AgenteIALocal.Core.Configuration
{
    /// <summary>
    /// NUEVO - ID: 20260123_211500 - Interfaz para persistencia de settings (Clean Architecture)
    /// Implementaciones: FileAgentSettingsProvider (Application layer - netstandard2.0 + Newtonsoft.Json)
    /// Consumidores: AgentSettingsStore (VSIX layer - fachada estática que delega a implementación vía DI)
    /// </summary>
    public interface IAgentSettingsProvider
    {
        /// <summary>
        /// Carga settings desde persistencia. Si no existe, retorna defaults.
        /// </summary>
        AgentSettings Load();

        /// <summary>
        /// Guarda settings a persistencia y dispara evento SettingsSaved.
        /// </summary>
        void Save(AgentSettings settings);

        /// <summary>
        /// Guarda settings a persistencia con opción de suprimir evento SettingsSaved.
        /// </summary>
        /// <param name="settings">Settings a guardar</param>
        /// <param name="raiseEvent">Si true, dispara SettingsSaved después de guardar</param>
        void Save(AgentSettings settings, bool raiseEvent);

        /// <summary>
        /// Retorna ruta completa del archivo de persistencia (ej: %LOCALAPPDATA%/AgenteIALocal/settings.json)
        /// </summary>
        string GetSettingsFilePath();

        /// <summary>
        /// Evento disparado después de guardar settings exitosamente (si raiseEvent=true).
        /// Argumento: razón del guardado (ej: "save", "auto-save", etc.)
        /// </summary>
        event Action<string> SettingsSaved;
    }
}
