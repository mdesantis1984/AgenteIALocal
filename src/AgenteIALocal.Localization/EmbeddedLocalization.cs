// NUEVA CLASE EmbeddedLocalization - ID: 20260123_121820
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
                ["flag"] = "es-AR.png"
            },
            ["ui"] = new Dictionary<string, object>
            {
                ["config"] = new Dictionary<string, object>
                {
                    ["window"] = new Dictionary<string, object>
                    {
                        ["title"] = "Chat de Agente IA Local - Configuración"
                    }
                }
            }
        };
    }
}
