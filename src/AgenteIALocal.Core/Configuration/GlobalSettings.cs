// NUEVO DTO GlobalSettings - ID: 20260123_225300
// DTO tipado para configuración global del agente
// ARQUITECTURA: Core - DTOs puros (reutiliza LogSettings de Logging project)
using System;

namespace AgenteIALocal.Core.Configuration
{
    /// <summary>
    /// DTO raíz para configuración global del agente
    /// Contiene todas las configuraciones que no son específicas de servidor
    /// Persistido en settings.json bajo la clave "globalSettings"
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// Modo de ejecución del agente
        /// Valores válidos: "preguntar" | "agente"
        /// - "preguntar": modo Q&A simple (default)
        /// - "agente": modo autónomo multi-step
        /// Default: "preguntar"
        /// </summary>
        public string RunMode { get; set; } = "preguntar";

        /// <summary>
        /// Configuración de defaults para requests a LLM
        /// (temperature, maxTokens, stream, streamOptions)
        /// </summary>
        public RequestDefaultsSettings RequestDefaults { get; set; } = new RequestDefaultsSettings();

        /// <summary>
        /// Configuración de comportamiento del agente
        /// (ideIntegration, applyChanges, maxSteps)
        /// </summary>
        public AgentBehaviorSettings Agent { get; set; } = new AgentBehaviorSettings();

        /// <summary>
        /// Configuración de logging (niveles habilitados)
        /// Reutiliza LogSettings del proyecto Logging
        /// NOTA: Solo propiedades de niveles son relevantes aquí
        /// (AppName, LogDirectory, etc. se configuran en inicialización de Serilog)
        /// </summary>
        public AgenteIALocal.Logging.LogSettings Logging { get; set; } = new AgenteIALocal.Logging.LogSettings();

        /// <summary>
        /// Configuración de Telegram Bot para form de contacto
        /// NUEVO - ID: 20260126_140000
        /// </summary>
        public TelegramSettings Telegram { get; set; } = new TelegramSettings();
    }

    /// <summary>
    /// DTO para configuración de Telegram Bot (form contacto)
    /// MODIFICADO - ID: 20260126_184200 - Defaults VACÍOS (config viene de TelegramConfig.txt ofuscado)
    /// </summary>
    public class TelegramSettings
    {
        /// <summary>
        /// Si el bot de Telegram está habilitado
        /// NOTA: Config real viene de TelegramConfig.txt ofuscado (NO de settings.json)
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Token del bot de Telegram (obtenido de @BotFather)
        /// PLACEHOLDER - Config real viene de TelegramConfig.txt ofuscado
        /// </summary>
        public string BotToken { get; set; } = string.Empty;

        /// <summary>
        /// Chat ID del destinatario (ID de Telegram del soporte)
        /// PLACEHOLDER - Config real viene de TelegramConfig.txt ofuscado
        /// </summary>
        public string ChatId { get; set; } = string.Empty;
    }
}
