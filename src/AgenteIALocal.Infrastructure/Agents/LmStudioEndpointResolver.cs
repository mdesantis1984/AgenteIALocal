using System;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocal.Infrastructure.Agents
{
    public sealed class LmStudioEndpointResolver : IAgentEndpointResolver
    {
        private readonly LmStudioSettings settings;

        public LmStudioEndpointResolver(LmStudioSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        // MODIFICADO METODO GetChatCompletionsEndpoint - ID: 20260123_020300
        // Safe-by-default: usa Uri.TryCreate para validar antes de retornar
        // Retorna null si la URL es inválida (puerto malformado, formato incorrecto, etc.)
        public Uri GetChatCompletionsEndpoint()
        {
            try
            {
                var baseUrl = settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
                var path = settings.ChatCompletionsPath ?? string.Empty;
                
                if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(path))
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9400, "EndpointResolver.GetEndpoint", $"BaseUrl or path is empty: baseUrl='{baseUrl}', path='{path}'", null);
                    return null;
                }

                var combined = baseUrl + (path.StartsWith("/") ? string.Empty : "/") + path;

                // Safe-by-default: validar con Uri.TryCreate antes de retornar
                Uri result;
                if (!Uri.TryCreate(combined, UriKind.Absolute, out result))
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9401, "EndpointResolver.GetEndpoint", $"Invalid URI format: '{combined}'. Check baseUrl (port, scheme, etc.)", null);
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallback final: loguear y retornar null (nunca throw)
                AgenteIALocal.Logging.Log.Error("-", 9402, "EndpointResolver.GetEndpoint", $"Unexpected error building endpoint: {ex.Message}", ex);
                return null;
            }
        }
    }
}
