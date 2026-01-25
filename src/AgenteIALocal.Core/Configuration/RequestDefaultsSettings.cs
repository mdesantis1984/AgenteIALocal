// NUEVO DTO RequestDefaultsSettings - ID: 20260123_225100
// DTO tipado para configuración de defaults de requests LLM
// ARQUITECTURA: Core - DTOs puros SIN dependencias de serialización
using System;

namespace AgenteIALocal.Core.Configuration
{
    /// <summary>
    /// DTO para configuración de defaults de requests a LLM
    /// Usado en GlobalSettings.RequestDefaults
    /// </summary>
    public class RequestDefaultsSettings
    {
        /// <summary>
        /// Temperatura de generación (creatividad)
        /// Rango: 0.0 (determinista) - 2.0 (muy creativo)
        /// Default: 0.2
        /// </summary>
        public double Temperature { get; set; } = 0.2;

        /// <summary>
        /// Máximo de tokens en respuesta
        /// 0 = sin límite
        /// Default: 0
        /// </summary>
        public int MaxTokens { get; set; } = 0;

        /// <summary>
        /// Habilitar streaming de respuesta
        /// SIEMPRE true (no configurable por usuario)
        /// </summary>
        public bool Stream { get; set; } = true;

        /// <summary>
        /// Opciones adicionales de streaming
        /// </summary>
        public StreamOptionsSettings StreamOptions { get; set; } = new StreamOptionsSettings();
    }

    /// <summary>
    /// DTO para opciones de streaming
    /// </summary>
    public class StreamOptionsSettings
    {
        /// <summary>
        /// Incluir métricas de uso (tokens consumidos/generados)
        /// Solo soportado por LM Studio
        /// Default: false
        /// </summary>
        public bool IncludeUsage { get; set; } = false;
    }
}
