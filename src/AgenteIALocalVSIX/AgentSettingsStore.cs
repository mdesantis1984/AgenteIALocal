// MODIFICADO - ID: 20260123_220000 - REFACTORING: AgentSettingsStore convertido en fachada (Clean Architecture)
// ANTES: Clase estática con TODA la lógica de persistencia (870 líneas) + Newtonsoft.Json
// DESPUÉS: Fachada estática que delega a IAgentSettingsProvider via DI (80 líneas) + CERO dependencias JSON
using System;
using AgenteIALocal.Core.Configuration;

namespace AgenteIALocalVSIX
{
    /// <summary>
    /// Fachada estática para persistencia de settings
    /// ARQUITECTURA: Delega toda la lógica a IAgentSettingsProvider (inyectado desde Application layer)
    /// BENEFICIOS:
    /// - VSIX sin dependencias JSON (cero Newtonsoft.Json / System.Text.Json)
    /// - Lógica encapsulada en Application layer (netstandard2.0)
    /// - Testeable (mock IAgentSettingsProvider)
    /// - Reusable (CLI, Web, otros consumers)
    /// </summary>
    public static class AgentSettingsStore
    {
        private static IAgentSettingsProvider _provider;

        /// <summary>
        /// Inicializa la fachada con una implementación de IAgentSettingsProvider
        /// DEBE llamarse desde AgenteIALocalVSIXPackage.InitializeAsync() o AgentComposition.EnsureComposition()
        /// </summary>
        public static void Initialize(IAgentSettingsProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Carga settings desde persistencia
        /// </summary>
        public static AgentSettings Load()
        {
            if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
            return _provider.Load();
        }

        /// <summary>
        /// Guarda settings a persistencia y dispara evento SettingsSaved
        /// </summary>
        public static void Save(AgentSettings settings)
        {
            if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
            _provider.Save(settings);
        }

        /// <summary>
        /// Guarda settings a persistencia con opción de suprimir evento SettingsSaved
        /// </summary>
        public static void Save(AgentSettings settings, bool raiseEvent)
        {
            if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
            _provider.Save(settings, raiseEvent);
        }

        /// <summary>
        /// Retorna ruta completa del archivo de persistencia
        /// </summary>
        public static string GetSettingsFilePath()
        {
            if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
            return _provider.GetSettingsFilePath();
        }

        /// <summary>
        /// Evento disparado después de guardar settings exitosamente
        /// </summary>
        public static event Action<string> SettingsSaved
        {
            add
            {
                if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
                _provider.SettingsSaved += value;
            }
            remove
            {
                if (_provider == null) throw new InvalidOperationException("AgentSettingsStore not initialized. Call Initialize() first.");
                _provider.SettingsSaved -= value;
            }
        }
    }
}
