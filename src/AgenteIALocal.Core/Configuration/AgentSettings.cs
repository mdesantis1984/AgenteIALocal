// MODIFICADO ARCHIVO AgentSettings - ID: 20260123_225400
// DTOs de configuración en Core (Clean Architecture)
// CAMBIO CRÍTICO: GlobalSettings object → GlobalSettings DTO tipado
// ARQUITECTURA: Core solo DTOs puros - Application maneja conversión JObject ↔ DTOs
using System;
using System.Collections.Generic;

namespace AgenteIALocal.Core.Configuration
{
    /// <summary>
    /// DTO raíz de configuración del agente
    /// Persistido en %LOCALAPPDATA%/AgenteIALocal/settings.json
    /// </summary>
    public class AgentSettings
    {
        /// <summary>
        /// Versión del schema (ej: "v1")
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Lista de servidores LLM configurados (LM Studio, Jan, etc.)
        /// </summary>
        public List<ServerConfig> Servers { get; set; }

        /// <summary>
        /// Configuración global (runMode, requestDefaults, agent, logging)
        /// MODIFICADO - ID: 20260123_225400 - object → GlobalSettings (DTO tipado)
        /// ARQUITECTURA: UI consume propiedades tipadas, Application convierte JObject ↔ DTO
        /// </summary>
        public GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();

        /// <summary>
        /// ID del servidor activo actualmente seleccionado
        /// </summary>
        public string ActiveServerId { get; set; }
    }

    /// <summary>
    /// DTO de configuración de servidor LLM
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// ID único del servidor (ej: "lmstudio-local")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nombre descriptivo (ej: "LM Studio (local)")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tipo de proveedor (ej: "lmstudio", "jan", "ollama")
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// URL base del servidor (ej: "http://127.0.0.1:1234")
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// API Key (vacío para servidores locales sin auth)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// SINGLETON DTO: Identificador del modelo seleccionado (ej: "llama-3.2-3b")
        /// IMPORTANTE: Única ubicación de persistencia - NUNCA usar globalSettings.selectedModel
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Si este servidor es el default al crear nuevos settings
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Fecha de creación del registro (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
