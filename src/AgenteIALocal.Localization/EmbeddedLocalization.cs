// NUEVA CLASE EmbeddedLocalization - ID: 20260123_121820
// MODIFICADO - ID: 20260124_001900 - Expandido schema completo con todos los strings UI
using System.Collections.Generic;

namespace AgenteIALocal.Localization
{
    internal static class EmbeddedLocalization
    {
        public static readonly Dictionary<string, object> EsAR = new Dictionary<string, object>
        {
            ["metadata"] = new Dictionary<string, object>
            {
                ["code"] = "es-AR",
                ["name"] = "Español (Argentina)",
                ["nativeName"] = "Español (Argentina)",
                ["flag"] = "es-AR.png",
                ["version"] = "1.0.0",
                ["author"] = "AgenteIALocal Team"
            },
            ["ui"] = new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuración"
                    },
                    ["sidebar"] = new Dictionary<string, object>
                    {
                        ["idioma"] = "Idioma",
                        ["llm"] = "LLM's Local",
                        ["logging"] = "Logging"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["save"] = "Guardar",
                        ["cancel"] = "Cancelar"
                    },
                    ["idioma"] = new Dictionary<string, object>
                    {
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuración de Idioma",
                            ["description"] = "Seleccione el idioma de la interfaz"
                        },
                        ["languages"] = new Dictionary<string, object>
                        {
                            ["es"] = "Español",
                            ["en"] = "English",
                            ["pt"] = "Português",
                            ["fr"] = "Français",
                            ["de"] = "Deutsch"
                        }
                    },
                    ["llm"] = new Dictionary<string, object>
                    {
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuración de los proveedores locales"
                        },
                        ["provider"] = new Dictionary<string, object>
                        {
                            ["label"] = "Provider",
                            ["lmstudio"] = "LM Studio",
                            ["jan"] = "Jan"
                        },
                        ["runmode"] = new Dictionary<string, object>
                        {
                            ["label"] = "Run Mode",
                            ["preguntar"] = "Preguntar",
                            ["agente"] = "Agente"
                        },
                        ["server"] = new Dictionary<string, object>
                        {
                            ["activeServerId"] = "Active Server Id",
                            ["baseUrl"] = "Base URL",
                            ["model"] = "Model",
                            ["apiKey"] = "API Key",
                            ["pingError"] = "Servidor no responde (/v1/models)."
                        },
                        ["requestDefaults"] = new Dictionary<string, object>
                        {
                            ["stream"] = "Stream",
                            ["includeUsage"] = "Include usage",
                            ["temperature"] = "Temperature",
                            ["maxTokens"] = "Max tokens"
                        },
                        ["agent"] = new Dictionary<string, object>
                        {
                            ["ideIntegration"] = "IDE integration",
                            ["applyChanges"] = "Apply changes",
                            ["maxSteps"] = "Max steps"
                        }
                    },
                    ["logging"] = new Dictionary<string, object>
                    {
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuración de Logging"
                        },
                        ["enabled"] = "Logging enabled (activa todos los niveles)",
                        ["groups"] = new Dictionary<string, object>
                        {
                            ["basic"] = "Niveles básicos",
                            ["advanced"] = "Niveles avanzados"
                        },
                        ["levels"] = new Dictionary<string, object>
                        {
                            ["verbose"] = "Verbose",
                            ["debug"] = "Debug",
                            ["information"] = "Información",
                            ["warning"] = "Advertencia",
                            ["error"] = "Error",
                            ["critical"] = "Crítico"
                        },
                        // NUEVO - ID: 20260126_031400 - Claves tooltips faltantes
                        ["tooltips"] = new Dictionary<string, object>
                        {
                            ["enabled"] = "Activa todos los niveles de logging (anula configuración individual)",
                            ["information"] = "Registra eventos informativos generales del sistema",
                            ["warning"] = "Registra advertencias que no detienen la ejecución",
                            ["error"] = "Registra errores recuperables del sistema",
                            ["critical"] = "Registra errores críticos que pueden detener el sistema",
                            ["verbose"] = "Registra información diagnóstica detallada (alto volumen)",
                            ["debug"] = "Registra información de depuración para desarrollo (volumen muy alto)"
                        }
                    },
                    // NUEVO - ID: 20260126_031401 - Tooltips generales faltantes
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["sidebar"] = new Dictionary<string, object>
                        {
                            ["idioma"] = "Configurar idioma de la interfaz",
                            ["llm"] = "Configurar proveedores LLM locales",
                            ["logging"] = "Configurar niveles de logging del sistema"
                        },
                        ["llm"] = new Dictionary<string, object>
                        {
                            ["provider"] = "Seleccionar proveedor LLM local (LM Studio o Jan)",
                            ["runMode"] = "Modo Preguntar: responde sin ejecutar código. Modo Agente: ejecuta código con confirmación",
                            ["activeServerId"] = "Identificador del servidor activo",
                            ["baseUrl"] = "URL base del servidor compatible con OpenAI (ej: http://localhost:1234/v1)",
                            ["model"] = "Modelo LLM a utilizar para las consultas",
                            ["apiKey"] = "Clave API (opcional para servidores locales)",
                            ["stream"] = "Habilitar respuestas en streaming (token por token)",
                            ["includeUsage"] = "Incluir estadísticas de uso de tokens en las respuestas (solo LM Studio)",
                            ["temperature"] = "Controla la creatividad de las respuestas (0.0-2.0). Mayor = más creativo",
                            ["maxTokens"] = "Número máximo de tokens en la respuesta (0 = ilimitado)",
                            ["ideIntegration"] = "Permite al agente acceder a archivos y símbolos del proyecto",
                            ["applyChanges"] = "Aplica automáticamente cambios sugeridos en el código (requiere confirmación)",
                            ["maxSteps"] = "Número máximo de pasos que puede ejecutar el agente"
                        },
                        ["buttons"] = new Dictionary<string, object>
                        {
                            ["save"] = "Guardar configuración y cerrar",
                            ["cancel"] = "Descartar cambios y cerrar"
                        },
                        ["idioma"] = new Dictionary<string, object>
                        {
                            ["esAR"] = "Español (Argentina)",
                            ["enUS"] = "English (Estados Unidos)",
                            ["ptBR"] = "Português (Brasil)",
                            ["frFR"] = "Français (Francia)",
                            ["deDE"] = "Deutsch (Alemania)"
                        }
                    }
                },
                ["chat"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Chat de Agente IA Local"
                    },
                    ["placeholder"] = "Escribe tu consulta aquí, puedes presionar # para hacer referencia a un archivo de la solucion / proyecto",
                    ["labels"] = new Dictionary<string, object>
                    {
                        ["mode"] = "Modo",
                        ["log"] = "Log"
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["solution"] = "Solution",
                        ["projects"] = "Projects",
                        ["config"] = "Config",
                        ["refresh"] = "Refresh",
                        ["copyAll"] = "Copy all",
                        ["openLog"] = "Open log file",
                        ["deleteLog"] = "Delete log file"
                    }
                }
            }
        };
    }
}
