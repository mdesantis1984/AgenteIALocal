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
                        ["local_llms"] = "LLMs locales",
                        ["logging"] = "Registro",
                        ["agent"] = "Agente",
                        ["general"] = "General"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["save"] = "Guardar",
                        ["cancel"] = "Cancelar",
                        ["apply"] = "Aplicar",
                        ["reset"] = "Restablecer",
                        ["test_connection"] = "Probar conexión"
                    },
                    ["changes"] = new Dictionary<string, object>
                    {
                        ["title"] = "Cambios",
                        ["count"] = "Cambios ({0})",
                        ["unsaved"] = "Cambios sin guardar",
                        ["saved"] = "Cambios guardados"
                    },
                    ["language"] = new Dictionary<string, object>
                    {
                        ["title"] = "Seleccionar idioma",
                        ["search_placeholder"] = "Buscar idioma...",
                        ["current"] = "Idioma actual",
                        ["available"] = "Idiomas disponibles",
                        ["apply_restart"] = "Los cambios se aplicarán al reiniciar"
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
                            ["en"] = "Inglés",
                            ["pt"] = "Portugués",
                            ["fr"] = "Francés",
                            ["de"] = "Alemán",
                            ["it"] = "Italiano",
                            ["ja"] = "Japonés",
                            ["ko"] = "Coreano",
                            ["zh"] = "Chino",
                            ["ru"] = "Ruso",
                            ["ar"] = "Árabe",
                            ["hi"] = "Hindi",
                            ["tr"] = "Turco",
                            ["nl"] = "Holandés",
                            ["sv"] = "Sueco"
                        }
                    },
                    ["llm"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuración de LLM",
                        ["provider"] = "Proveedor",
                        ["model"] = "Modelo",
                        ["base_url"] = "URL base",
                        ["api_key"] = "Clave API",
                        ["temperature"] = "Temperatura",
                        ["max_tokens"] = "Tokens máximos",
                        ["add_server"] = "Agregar servidor",
                        ["edit_server"] = "Editar servidor",
                        ["delete_server"] = "Eliminar servidor",
                        ["runmode"] = new Dictionary<string, object>
                        {
                            ["label"] = "Modo de Ejecución",
                            ["preguntar"] = "Preguntar",
                            ["agente"] = "Agente"
                        }
                    },
                    ["logging"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuración de registro",
                        ["enabled"] = "Habilitar registro",
                        ["level"] = "Nivel de registro",
                        ["path"] = "Ruta de archivo",
                        ["max_size"] = "Tamaño máximo",
                        ["retention"] = "Retención de archivos",
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuración de Registro",
                            ["description"] = "Configure los niveles de registro del sistema"
                        },
                        ["groups"] = new Dictionary<string, object>
                        {
                            ["basic"] = "Niveles Básicos",
                            ["advanced"] = "Niveles Avanzados"
                        },
                        ["levels"] = new Dictionary<string, object>
                        {
                            ["information"] = "Información",
                            ["warning"] = "Advertencia",
                            ["error"] = "Error",
                            ["critical"] = "Crítico",
                            ["verbose"] = "Detallado",
                            ["debug"] = "Depuración"
                        },
                        ["tooltips"] = new Dictionary<string, object>
                        {
                            ["enabled"] = "Activa todos los niveles de registro (sobreescribe configuración individual)",
                            ["information"] = "Registra eventos informativos generales del sistema",
                            ["warning"] = "Registra advertencias que no detienen la ejecución",
                            ["error"] = "Registra errores recuperables del sistema",
                            ["critical"] = "Registra errores críticos que pueden detener el sistema",
                            ["verbose"] = "Registra información detallada de diagnóstico (alto volumen)",
                            ["debug"] = "Registra información de depuración para desarrollo (muy alto volumen)"
                        }
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["sidebar"] = new Dictionary<string, object>
                        {
                            ["idioma"] = "Configurar idioma de la interfaz",
                            ["llm"] = "Configurar proveedores LLM locales",
                            ["logging"] = "Configurar niveles de registro del sistema"
                        },
                        ["llm"] = new Dictionary<string, object>
                        {
                            ["provider"] = "Seleccione el proveedor de LLM local (LM Studio o Jan)",
                            ["runMode"] = "Modo Preguntar: responde sin ejecutar código. Modo Agente: ejecuta código con confirmación",
                            ["activeServerId"] = "Identificador del servidor activo",
                            ["baseUrl"] = "URL base del servidor OpenAI-compatible (ej: http://localhost:1234/v1)",
                            ["model"] = "Modelo LLM a utilizar para las consultas",
                            ["apiKey"] = "Clave API (opcional para servidores locales)",
                            ["stream"] = "Habilita respuestas en streaming (token por token)",
                            ["includeUsage"] = "Incluye estadísticas de uso de tokens en las respuestas (solo LM Studio)",
                            ["temperature"] = "Controla la creatividad de las respuestas (0.0-2.0). Mayor = más creativo",
                            ["maxTokens"] = "Número máximo de tokens en la respuesta (0 = ilimitado)",
                            ["ideIntegration"] = "Permite al agente acceder a archivos y símbolos del proyecto",
                            ["applyChanges"] = "Aplica automáticamente los cambios sugeridos al código (requiere confirmación)",
                            ["maxSteps"] = "Número máximo de pasos que el agente puede ejecutar"
                        },
                        ["buttons"] = new Dictionary<string, object>
                        {
                            ["save"] = "Guardar configuración y cerrar",
                            ["cancel"] = "Descartar cambios y cerrar"
                        },
                        ["idioma"] = new Dictionary<string, object>
                        {
                            ["esAR"] = "Español (Argentina)",
                            ["enUS"] = "English (United States)",
                            ["ptBR"] = "Português (Brasil)",
                            ["frFR"] = "Français (France)",
                            ["deDE"] = "Deutsch (Deutschland)",
                            ["itIT"] = "Italiano (Italia)",
                            ["jaJP"] = "日本語 (日本)",
                            ["koKR"] = "한국어 (대한민국)",
                            ["zhCN"] = "中文 (简体)",
                            ["ruRU"] = "Русский (Россия)",
                            ["arSA"] = "العربية (السعودية)",
                            ["hiIN"] = "हिन्दी (भारत)",
                            ["trTR"] = "Türkçe (Türkiye)",
                            ["nlNL"] = "Nederlands (Nederland)",
                            ["svSE"] = "Svenska (Sverige)"
                        }
                    }
                },
                ["chat"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Chat de Agente IA Local"
                    },
                    ["new_chat"] = "Nuevo chat",
                    ["placeholder"] = "Escribe tu consulta aquí, puedes presionar # para referenciar una solución / archivo de proyecto",
                    ["ask_button"] = "Preguntar",
                    ["stop_button"] = "Detener",
                    ["clear_button"] = "Limpiar",
                    ["labels"] = new Dictionary<string, object>
                    {
                        ["log"] = "Registro",
                        ["changes_count"] = "Cambios ({0})"
                    },
                    ["status"] = new Dictionary<string, object>
                    {
                        ["not_configured"] = "Sin configurar",
                        ["ready"] = "Listo",
                        ["thinking"] = "Pensando...",
                        ["error"] = "Error"
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["solution"] = "Seleccionar solución activa",
                        ["projects"] = "Ver proyectos de la solución",
                        ["config"] = "Abrir configuración",
                        ["refresh"] = "Actualizar lista de proyectos",
                        ["copyAll"] = "Copiar todo el log",
                        ["openLog"] = "Abrir archivo de log en editor",
                        ["deleteLog"] = "Eliminar archivo de log",
                        ["newChat"] = "Iniciar nuevo chat (limpia historial)",
                        ["settings"] = "Abrir configuración del agente",
                        ["acceptChanges"] = "Aceptar cambios sugeridos",
                        ["rejectChanges"] = "Rechazar cambios sugeridos",
                        ["about"] = "Acerca de Agente IA Local"
                    }
                },
                ["log"] = new Dictionary<string, object>
                {
                    ["panel_title"] = "Registro",
                    ["clear"] = "Limpiar",
                    ["copy"] = "Copiar",
                    ["save"] = "Guardar",
                    ["filter"] = "Filtrar...",
                    ["levels"] = new Dictionary<string, object>
                    {
                        ["verbose"] = "Detallado",
                        ["debug"] = "Depuración",
                        ["info"] = "Información",
                        ["warning"] = "Advertencia",
                        ["error"] = "Error",
                        ["fatal"] = "Fatal"
                    }
                },
                ["common"] = new Dictionary<string, object>
                {
                    ["ok"] = "Aceptar",
                    ["cancel"] = "Cancelar",
                    ["yes"] = "Sí",
                    ["no"] = "No",
                    ["close"] = "Cerrar",
                    ["save"] = "Guardar",
                    ["delete"] = "Eliminar",
                    ["edit"] = "Editar",
                    ["add"] = "Agregar",
                    ["remove"] = "Quitar",
                    ["search"] = "Buscar",
                    ["filter"] = "Filtrar",
                    ["loading"] = "Cargando...",
                    ["error"] = "Error",
                    ["success"] = "Éxito",
                    ["warning"] = "Advertencia",
                    ["info"] = "Información"
                },
                ["about"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Acerca de Agente IA Local"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["close"] = "Cerrar"
                    },
                    ["sections"] = new Dictionary<string, object>
                    {
                        ["productInfo_name"] = "Nombre y Descripción",
                        ["credits"] = "Créditos y Licencias",
                        ["support_contact"] = "Contacto",
                        ["repository_docs"] = "Documentación",
                        ["support_channels"] = "Canales de Ayuda",
                        ["productInfo"] = "Información del Producto",
                        ["productInfo_author"] = "Autor",
                        ["productInfo_version"] = "Versión",
                        ["credits_libraries"] = "Librerías de Terceros",
                        ["repository_issues"] = "Reportar Issues",
                        ["legal_copyright"] = "Copyright",
                        ["legal"] = "Legal",
                        ["credits_license"] = "Licencia del Proyecto",
                        ["repository_source"] = "Código Fuente",
                        ["compatibility"] = "Compatibilidad",
                        ["legal_terms"] = "Términos de Uso",
                        ["support"] = "Soporte",
                        ["repository"] = "Repositorio y Enlaces",
                        ["compatibility_vs"] = "Visual Studio",
                        ["compatibility_requirements"] = "Requisitos del Sistema"
                    },
                    ["content"] = new Dictionary<string, object>
                    {
                        ["repository_source_desc"] = "Ver el código fuente completo en GitHub",
                        ["repository_docs_desc"] = "Guías completas, referencias de API y tutoriales",
                        ["repository_issues_desc"] = "¿Encontraste un error o tienes una solicitud de función? ¡Háznoslo saber!",
                        ["github_repository"] = "Repositorio GitHub",
                        ["documentation"] = "Documentación",
                        ["changelog"] = "Historial de Cambios",
                        ["report_issue"] = "Reportar Problema",
                        ["third_party_libraries"] = "Librerías de Terceros",
                        ["source_code"] = "Código Fuente",
                        ["report_issues"] = "Reportar Problemas",
                        ["website"] = "Sitio Web"
                    }
                }
            }
        };

        // NUEVO - ID: 20260126_165000 - English (United States) translations
        // MODIFICADO - ID: 20260126_174000 - Actualizado con TODAS las keys (estructura completa)
        public static readonly Dictionary<string, object> EnUS = new Dictionary<string, object>
        {
            ["metadata"] = new Dictionary<string, object>
            {
                ["code"] = "en-US",
                ["name"] = "English (United States)",
                ["nativeName"] = "English (United States)",
                ["flag"] = "en-US.png",
                ["version"] = "1.0.0",
                ["author"] = "AgenteIALocal Team"
            },
            ["ui"] = new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuration"
                    },
                    ["sidebar"] = new Dictionary<string, object>
                    {
                        ["idioma"] = "Language",
                        ["local_llms"] = "Local LLMs",
                        ["logging"] = "Logging",
                        ["agent"] = "Agent",
                        ["general"] = "General"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["save"] = "Save",
                        ["cancel"] = "Cancel",
                        ["apply"] = "Apply",
                        ["reset"] = "Reset",
                        ["test_connection"] = "Test connection"
                    },
                    ["changes"] = new Dictionary<string, object>
                    {
                        ["title"] = "Changes",
                        ["count"] = "Changes ({0})",
                        ["unsaved"] = "Unsaved changes",
                        ["saved"] = "Changes saved"
                    },
                    ["language"] = new Dictionary<string, object>
                    {
                        ["title"] = "Select language",
                        ["search_placeholder"] = "Search language...",
                        ["current"] = "Current language",
                        ["available"] = "Available languages",
                        ["apply_restart"] = "Changes will apply after restart"
                    },
                    ["idioma"] = new Dictionary<string, object>
                    {
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Language Configuration",
                            ["description"] = "Select interface language"
                        },
                        ["languages"] = new Dictionary<string, object>
                        {
                            ["es"] = "Spanish",
                            ["en"] = "English",
                            ["pt"] = "Portuguese",
                            ["fr"] = "French",
                            ["de"] = "German",
                            ["it"] = "Italian",
                            ["ja"] = "Japanese",
                            ["ko"] = "Korean",
                            ["zh"] = "Chinese",
                            ["ru"] = "Russian",
                            ["ar"] = "Arabic",
                            ["hi"] = "Hindi",
                            ["tr"] = "Turkish",
                            ["nl"] = "Dutch",
                            ["sv"] = "Swedish"
                        }
                    },
                    ["llm"] = new Dictionary<string, object>
                    {
                        ["title"] = "LLM Configuration",
                        ["provider"] = "Provider",
                        ["model"] = "Model",
                        ["base_url"] = "Base URL",
                        ["api_key"] = "API Key",
                        ["temperature"] = "Temperature",
                        ["max_tokens"] = "Max tokens",
                        ["add_server"] = "Add server",
                        ["edit_server"] = "Edit server",
                        ["delete_server"] = "Delete server",
                        ["runmode"] = new Dictionary<string, object>
                        {
                            ["label"] = "Run Mode",
                            ["preguntar"] = "Ask",
                            ["agente"] = "Agent"
                        }
                    },
                    ["logging"] = new Dictionary<string, object>
                    {
                        ["title"] = "Logging configuration",
                        ["enabled"] = "Enable logging",
                        ["level"] = "Log level",
                        ["path"] = "File path",
                        ["max_size"] = "Max size",
                        ["retention"] = "File retention",
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Logging Configuration",
                            ["description"] = "Configure system log levels"
                        },
                        ["groups"] = new Dictionary<string, object>
                        {
                            ["basic"] = "Basic Levels",
                            ["advanced"] = "Advanced Levels"
                        },
                        ["levels"] = new Dictionary<string, object>
                        {
                            ["information"] = "Information",
                            ["warning"] = "Warning",
                            ["error"] = "Error",
                            ["critical"] = "Critical",
                            ["verbose"] = "Verbose",
                            ["debug"] = "Debug"
                        },
                        ["tooltips"] = new Dictionary<string, object>
                        {
                            ["enabled"] = "Enable all log levels (overrides individual settings)",
                            ["information"] = "Logs general system informational events",
                            ["warning"] = "Logs warnings that don't stop execution",
                            ["error"] = "Logs recoverable system errors",
                            ["critical"] = "Logs critical errors that may halt the system",
                            ["verbose"] = "Logs detailed diagnostic information (high volume)",
                            ["debug"] = "Logs debug information for development (very high volume)"
                        }
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["sidebar"] = new Dictionary<string, object>
                        {
                            ["idioma"] = "Configure interface language",
                            ["llm"] = "Configure local LLM providers",
                            ["logging"] = "Configure system log levels"
                        },
                        ["llm"] = new Dictionary<string, object>
                        {
                            ["provider"] = "Select local LLM provider (LM Studio or Jan)",
                            ["runMode"] = "Ask mode: responds without code execution. Agent mode: executes code with confirmation",
                            ["activeServerId"] = "Active server identifier",
                            ["baseUrl"] = "OpenAI-compatible server base URL (e.g., http://localhost:1234/v1)",
                            ["model"] = "LLM model to use for queries",
                            ["apiKey"] = "API key (optional for local servers)",
                            ["stream"] = "Enable streaming responses (token by token)",
                            ["includeUsage"] = "Include token usage statistics in responses (LM Studio only)",
                            ["temperature"] = "Controls response creativity (0.0-2.0). Higher = more creative",
                            ["maxTokens"] = "Maximum number of tokens in response (0 = unlimited)",
                            ["ideIntegration"] = "Allow agent to access project files and symbols",
                            ["applyChanges"] = "Automatically apply suggested code changes (requires confirmation)",
                            ["maxSteps"] = "Maximum number of steps the agent can execute"
                        },
                        ["buttons"] = new Dictionary<string, object>
                        {
                            ["save"] = "Save configuration and close",
                            ["cancel"] = "Discard changes and close"
                        },
                        ["idioma"] = new Dictionary<string, object>
                        {
                            ["esAR"] = "Spanish (Argentina)",
                            ["enUS"] = "English (United States)",
                            ["ptBR"] = "Portuguese (Brazil)",
                            ["frFR"] = "French (France)",
                            ["deDE"] = "German (Germany)",
                            ["itIT"] = "Italian (Italy)",
                            ["jaJP"] = "Japanese (Japan)",
                            ["koKR"] = "Korean (South Korea)",
                            ["zhCN"] = "Chinese (Simplified)",
                            ["ruRU"] = "Russian (Russia)",
                            ["arSA"] = "Arabic (Saudi Arabia)",
                            ["hiIN"] = "Hindi (India)",
                            ["trTR"] = "Turkish (Turkey)",
                            ["nlNL"] = "Dutch (Netherlands)",
                            ["svSE"] = "Swedish (Sweden)"
                        }
                    }
                },
                ["chat"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Local AI Agent Chat"
                    },
                    ["new_chat"] = "New chat",
                    ["placeholder"] = "Type your query here, you can press # to reference a solution / project file",
                    ["ask_button"] = "Ask",
                    ["stop_button"] = "Stop",
                    ["clear_button"] = "Clear",
                    ["labels"] = new Dictionary<string, object>
                    {
                        ["log"] = "Log",
                        ["changes_count"] = "Changes ({0})"
                    },
                    ["status"] = new Dictionary<string, object>
                    {
                        ["not_configured"] = "Not configured",
                        ["ready"] = "Ready",
                        ["thinking"] = "Thinking...",
                        ["error"] = "Error"
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["solution"] = "Select active solution",
                        ["projects"] = "View solution projects",
                        ["config"] = "Open configuration",
                        ["refresh"] = "Refresh project list",
                        ["copyAll"] = "Copy all log",
                        ["openLog"] = "Open log file in editor",
                        ["deleteLog"] = "Delete log file",
                        ["newChat"] = "Start new chat (clears history)",
                        ["settings"] = "Open agent settings",
                        ["acceptChanges"] = "Accept suggested changes",
                        ["rejectChanges"] = "Reject suggested changes",
                        ["about"] = "About Local AI Agent"
                    }
                },
                ["log"] = new Dictionary<string, object>
                {
                    ["panel_title"] = "Log",
                    ["clear"] = "Clear",
                    ["copy"] = "Copy",
                    ["save"] = "Save",
                    ["filter"] = "Filter...",
                    ["levels"] = new Dictionary<string, object>
                    {
                        ["verbose"] = "Verbose",
                        ["debug"] = "Debug",
                        ["info"] = "Information",
                        ["warning"] = "Warning",
                        ["error"] = "Error",
                        ["fatal"] = "Fatal"
                    }
                },
                ["common"] = new Dictionary<string, object>
                {
                    ["ok"] = "OK",
                    ["cancel"] = "Cancel",
                    ["yes"] = "Yes",
                    ["no"] = "No",
                    ["close"] = "Close",
                    ["save"] = "Save",
                    ["delete"] = "Delete",
                    ["edit"] = "Edit",
                    ["add"] = "Add",
                    ["remove"] = "Remove",
                    ["search"] = "Search",
                    ["filter"] = "Filter",
                    ["loading"] = "Loading...",
                    ["error"] = "Error",
                    ["success"] = "Success",
                    ["warning"] = "Warning",
                    ["info"] = "Information"
                },
                ["about"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "About Local AI Agent"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["close"] = "Close"
                    },
                    ["sections"] = new Dictionary<string, object>
                    {
                        ["productInfo_name"] = "Name and Description",
                        ["credits"] = "Credits and Licenses",
                        ["support_contact"] = "Contact",
                        ["repository_docs"] = "Documentation",
                        ["support_channels"] = "Support Channels",
                        ["productInfo"] = "Product Information",
                        ["productInfo_author"] = "Author",
                        ["productInfo_version"] = "Version",
                        ["credits_libraries"] = "Third-Party Libraries",
                        ["repository_issues"] = "Report Issues",
                        ["legal_copyright"] = "Copyright",
                        ["legal"] = "Legal",
                        ["credits_license"] = "Project License",
                        ["repository_source"] = "Source Code",
                        ["compatibility"] = "Compatibility",
                        ["legal_terms"] = "Terms of Use",
                        ["support"] = "Support",
                        ["repository"] = "Repository and Links",
                        ["compatibility_vs"] = "Visual Studio",
                        ["compatibility_requirements"] = "System Requirements"
                    },
                    ["content"] = new Dictionary<string, object>
                    {
                        ["repository_source_desc"] = "View complete source code on GitHub",
                        ["repository_docs_desc"] = "Complete guides, API references, and tutorials",
                        ["repository_issues_desc"] = "Found a bug or have a feature request? Let us know!",
                        ["github_repository"] = "GitHub Repository",
                        ["documentation"] = "Documentation",
                        ["changelog"] = "Changelog",
                        ["report_issue"] = "Report Issue",
                        ["third_party_libraries"] = "Third-Party Libraries",
                        ["source_code"] = "Source Code",
                        ["report_issues"] = "Report Issues",
                        ["website"] = "Website"
                    }
                }
            }
        };

        // NUEVO - ID: 20260126_165001 - Français (France) translations
        // MODIFICADO - ID: 20260126_174001 - Actualizado con TODAS las keys (estructura completa)
        public static readonly Dictionary<string, object> FrFR = new Dictionary<string, object>
        {
            ["metadata"] = new Dictionary<string, object>
            {
                ["code"] = "fr-FR",
                ["name"] = "Français (France)",
                ["nativeName"] = "Français (France)",
                ["flag"] = "fr-FR.png",
                ["version"] = "1.0.0",
                ["author"] = "AgenteIALocal Team"
            },
            ["ui"] = new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuration"
                    },
                    ["sidebar"] = new Dictionary<string, object>
                    {
                        ["idioma"] = "Langue",
                        ["local_llms"] = "LLMs locaux",
                        ["logging"] = "Enregistrement",
                        ["agent"] = "Agent",
                        ["general"] = "Général"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["save"] = "Enregistrer",
                        ["cancel"] = "Annuler",
                        ["apply"] = "Appliquer",
                        ["reset"] = "Réinitialiser",
                        ["test_connection"] = "Tester la connexion"
                    },
                    ["changes"] = new Dictionary<string, object>
                    {
                        ["title"] = "Modifications",
                        ["count"] = "Modifications ({0})",
                        ["unsaved"] = "Modifications non enregistrées",
                        ["saved"] = "Modifications enregistrées"
                    },
                    ["language"] = new Dictionary<string, object>
                    {
                        ["title"] = "Sélectionner la langue",
                        ["search_placeholder"] = "Rechercher une langue...",
                        ["current"] = "Langue actuelle",
                        ["available"] = "Langues disponibles",
                        ["apply_restart"] = "Les modifications s'appliqueront après le redémarrage"
                    },
                    ["idioma"] = new Dictionary<string, object>
                    {
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuration de la langue",
                            ["description"] = "Sélectionner la langue de l'interface"
                        },
                        ["languages"] = new Dictionary<string, object>
                        {
                            ["es"] = "Espagnol",
                            ["en"] = "Anglais",
                            ["pt"] = "Portugais",
                            ["fr"] = "Français",
                            ["de"] = "Allemand",
                            ["it"] = "Italien",
                            ["ja"] = "Japonais",
                            ["ko"] = "Coréen",
                            ["zh"] = "Chinois",
                            ["ru"] = "Russe",
                            ["ar"] = "Arabe",
                            ["hi"] = "Hindi",
                            ["tr"] = "Turc",
                            ["nl"] = "Néerlandais",
                            ["sv"] = "Suédois"
                        }
                    },
                    ["llm"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuration LLM",
                        ["provider"] = "Fournisseur",
                        ["model"] = "Modèle",
                        ["base_url"] = "URL de base",
                        ["api_key"] = "Clé API",
                        ["temperature"] = "Température",
                        ["max_tokens"] = "Jetons maximum",
                        ["add_server"] = "Ajouter un serveur",
                        ["edit_server"] = "Modifier le serveur",
                        ["delete_server"] = "Supprimer le serveur",
                        ["runmode"] = new Dictionary<string, object>
                        {
                            ["label"] = "Mode d'exécution",
                            ["preguntar"] = "Demander",
                            ["agente"] = "Agent"
                        }
                    },
                    ["logging"] = new Dictionary<string, object>
                    {
                        ["title"] = "Configuration d'enregistrement",
                        ["enabled"] = "Activer l'enregistrement",
                        ["level"] = "Niveau d'enregistrement",
                        ["path"] = "Chemin du fichier",
                        ["max_size"] = "Taille maximale",
                        ["retention"] = "Rétention des fichiers",
                        ["page"] = new Dictionary<string, object>
                        {
                            ["title"] = "Configuration d'enregistrement",
                            ["description"] = "Configurer les niveaux d'enregistrement du système"
                        },
                        ["groups"] = new Dictionary<string, object>
                        {
                            ["basic"] = "Niveaux de base",
                            ["advanced"] = "Niveaux avancés"
                        },
                        ["levels"] = new Dictionary<string, object>
                        {
                            ["information"] = "Information",
                            ["warning"] = "Avertissement",
                            ["error"] = "Erreur",
                            ["critical"] = "Critique",
                            ["verbose"] = "Détaillé",
                            ["debug"] = "Débogage"
                        },
                        ["tooltips"] = new Dictionary<string, object>
                        {
                            ["enabled"] = "Active tous les niveaux d'enregistrement (remplace les paramètres individuels)",
                            ["information"] = "Enregistre les événements informatifs généraux du système",
                            ["warning"] = "Enregistre les avertissements qui n'arrêtent pas l'exécution",
                            ["error"] = "Enregistre les erreurs récupérables du système",
                            ["critical"] = "Enregistre les erreurs critiques qui peuvent arrêter le système",
                            ["verbose"] = "Enregistre des informations de diagnostic détaillées (volume élevé)",
                            ["debug"] = "Enregistre les informations de débogage pour le développement (très haut volume)"
                        }
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["sidebar"] = new Dictionary<string, object>
                        {
                            ["idioma"] = "Configurer la langue de l'interface",
                            ["llm"] = "Configurer les fournisseurs LLM locaux",
                            ["logging"] = "Configurer les niveaux d'enregistrement du système"
                        },
                        ["llm"] = new Dictionary<string, object>
                        {
                            ["provider"] = "Sélectionnez le fournisseur LLM local (LM Studio ou Jan)",
                            ["runMode"] = "Mode Demander: répond sans exécuter de code. Mode Agent: exécute le code avec confirmation",
                            ["activeServerId"] = "Identifiant du serveur actif",
                            ["baseUrl"] = "URL de base du serveur compatible OpenAI (ex: http://localhost:1234/v1)",
                            ["model"] = "Modèle LLM à utiliser pour les requêtes",
                            ["apiKey"] = "Clé API (facultatif pour les serveurs locaux)",
                            ["stream"] = "Active les réponses en streaming (jeton par jeton)",
                            ["includeUsage"] = "Inclut les statistiques d'utilisation des jetons dans les réponses (LM Studio uniquement)",
                            ["temperature"] = "Contrôle la créativité des réponses (0.0-2.0). Plus élevé = plus créatif",
                            ["maxTokens"] = "Nombre maximum de jetons dans la réponse (0 = illimité)",
                            ["ideIntegration"] = "Permet à l'agent d'accéder aux fichiers et symboles du projet",
                            ["applyChanges"] = "Applique automatiquement les modifications suggérées au code (nécessite confirmation)",
                            ["maxSteps"] = "Nombre maximum d'étapes que l'agent peut exécuter"
                        },
                        ["buttons"] = new Dictionary<string, object>
                        {
                            ["save"] = "Enregistrer la configuration et fermer",
                            ["cancel"] = "Annuler les modifications et fermer"
                        },
                        ["idioma"] = new Dictionary<string, object>
                        {
                            ["esAR"] = "Espagnol (Argentine)",
                            ["enUS"] = "Anglais (États-Unis)",
                            ["ptBR"] = "Portugais (Brésil)",
                            ["frFR"] = "Français (France)",
                            ["deDE"] = "Allemand (Allemagne)",
                            ["itIT"] = "Italien (Italie)",
                            ["jaJP"] = "Japonais (Japon)",
                            ["koKR"] = "Coréen (Corée du Sud)",
                            ["zhCN"] = "Chinois (Simplifié)",
                            ["ruRU"] = "Russe (Russie)",
                            ["arSA"] = "Arabe (Arabie Saoudite)",
                            ["hiIN"] = "Hindi (Inde)",
                            ["trTR"] = "Turc (Turquie)",
                            ["nlNL"] = "Néerlandais (Pays-Bas)",
                            ["svSE"] = "Suédois (Suède)"
                        }
                    }
                },
                ["chat"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Chat de l'Agent IA Local"
                    },
                    ["new_chat"] = "Nouveau chat",
                    ["placeholder"] = "Tapez votre requête ici, vous pouvez appuyer sur # pour référencer un fichier de solution / projet",
                    ["ask_button"] = "Demander",
                    ["stop_button"] = "Arrêter",
                    ["clear_button"] = "Effacer",
                    ["labels"] = new Dictionary<string, object>
                    {
                        ["log"] = "Journal",
                        ["changes_count"] = "Modifications ({0})"
                    },
                    ["status"] = new Dictionary<string, object>
                    {
                        ["not_configured"] = "Non configuré",
                        ["ready"] = "Prêt",
                        ["thinking"] = "Réflexion...",
                        ["error"] = "Erreur"
                    },
                    ["tooltips"] = new Dictionary<string, object>
                    {
                        ["solution"] = "Sélectionner la solution active",
                        ["projects"] = "Voir les projets de la solution",
                        ["config"] = "Ouvrir la configuration",
                        ["refresh"] = "Actualiser la liste des projets",
                        ["copyAll"] = "Copier tout le journal",
                        ["openLog"] = "Ouvrir le fichier journal dans l'éditeur",
                        ["deleteLog"] = "Supprimer le fichier journal",
                        ["newChat"] = "Démarrer un nouveau chat (efface l'historique)",
                        ["settings"] = "Ouvrir les paramètres de l'agent",
                        ["acceptChanges"] = "Accepter les modifications suggérées",
                        ["rejectChanges"] = "Rejeter les modifications suggérées",
                        ["about"] = "À propos de l'Agent IA Local"
                    }
                },
                ["log"] = new Dictionary<string, object>
                {
                    ["panel_title"] = "Journal",
                    ["clear"] = "Effacer",
                    ["copy"] = "Copier",
                    ["save"] = "Enregistrer",
                    ["filter"] = "Filtrer...",
                    ["levels"] = new Dictionary<string, object>
                    {
                        ["verbose"] = "Détaillé",
                        ["debug"] = "Débogage",
                        ["info"] = "Information",
                        ["warning"] = "Avertissement",
                        ["error"] = "Erreur",
                        ["fatal"] = "Fatal"
                    }
                },
                ["common"] = new Dictionary<string, object>
                {
                    ["ok"] = "OK",
                    ["cancel"] = "Annuler",
                    ["yes"] = "Oui",
                    ["no"] = "Non",
                    ["close"] = "Fermer",
                    ["save"] = "Enregistrer",
                    ["delete"] = "Supprimer",
                    ["edit"] = "Modifier",
                    ["add"] = "Ajouter",
                    ["remove"] = "Retirer",
                    ["search"] = "Rechercher",
                    ["filter"] = "Filtrer",
                    ["loading"] = "Chargement...",
                    ["error"] = "Erreur",
                    ["success"] = "Succès",
                    ["warning"] = "Avertissement",
                    ["info"] = "Information"
                },
                ["about"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "À propos de l'Agent IA Local"
                    },
                    ["buttons"] = new Dictionary<string, object>
                    {
                        ["close"] = "Fermer"
                    },
                    ["sections"] = new Dictionary<string, object>
                    {
                        ["productInfo_name"] = "Nom et Description",
                        ["credits"] = "Crédits et Licences",
                        ["support_contact"] = "Contact",
                        ["repository_docs"] = "Documentation",
                        ["support_channels"] = "Canaux d'assistance",
                        ["productInfo"] = "Informations sur le produit",
                        ["productInfo_author"] = "Auteur",
                        ["productInfo_version"] = "Version",
                        ["credits_libraries"] = "Bibliothèques tierces",
                        ["repository_issues"] = "Signaler des problèmes",
                        ["legal_copyright"] = "Droits d'auteur",
                        ["legal"] = "Légal",
                        ["credits_license"] = "Licence du projet",
                        ["repository_source"] = "Code source",
                        ["compatibility"] = "Compatibilité",
                        ["legal_terms"] = "Conditions d'utilisation",
                        ["support"] = "Assistance",
                        ["repository"] = "Dépôt et liens",
                        ["compatibility_vs"] = "Visual Studio",
                        ["compatibility_requirements"] = "Configuration requise"
                    },
                    ["content"] = new Dictionary<string, object>
                    {
                        ["repository_source_desc"] = "Voir le code source complet sur GitHub",
                        ["repository_docs_desc"] = "Guides complets, références API et tutoriels",
                        ["repository_issues_desc"] = "Vous avez trouvé un bug ou une demande de fonctionnalité ? Faites-le nous savoir !",
                        ["github_repository"] = "Dépôt GitHub",
                        ["documentation"] = "Documentation",
                        ["changelog"] = "Historique des modifications",
                        ["report_issue"] = "Signaler un problème",
                        ["third_party_libraries"] = "Bibliothèques tierces",
                        ["source_code"] = "Code source",
                        ["report_issues"] = "Signaler des problèmes",
                        ["website"] = "Site Web"
                    }
                }
            }
        };
    }
}
