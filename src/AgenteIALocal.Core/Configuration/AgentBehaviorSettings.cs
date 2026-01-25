// NUEVO DTO AgentBehaviorSettings - ID: 20260123_225200
// DTO tipado para configuración de comportamiento del agente
// ARQUITECTURA: Core - DTOs puros SIN dependencias de serialización
using System;

namespace AgenteIALocal.Core.Configuration
{
    /// <summary>
    /// DTO para configuración de comportamiento del agente IA
    /// Usado en GlobalSettings.Agent
    /// </summary>
    public class AgentBehaviorSettings
    {
        /// <summary>
        /// Habilitar integración con IDE (acceso a archivos, proyectos, solución)
        /// true: agente puede leer contexto del IDE
        /// false: agente opera sin acceso al IDE
        /// Default: true
        /// </summary>
        public bool IdeIntegration { get; set; } = true;

        /// <summary>
        /// Permitir que el agente aplique cambios de código automáticamente
        /// true: agente puede modificar archivos directamente
        /// false: agente solo sugiere cambios (modo seguro)
        /// Default: false (modo seguro)
        /// </summary>
        public bool ApplyChanges { get; set; } = false;

        /// <summary>
        /// Máximo de pasos de razonamiento del agente antes de finalizar
        /// Rango: 1-20
        /// Default: 5
        /// </summary>
        public int MaxSteps { get; set; } = 5;
    }
}
