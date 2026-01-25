// NUEVA CLASE OpenAIStreamingParser - ID: 20260123_230600
// ARQUITECTURA: Infrastructure - Implementación de IStreamingResponseParser
// PROPÓSITO: Encapsular lógica JSON parsing de respuestas SSE OpenAI-compatible
using AgenteIALocal.Core.Streaming;
using Newtonsoft.Json.Linq;
using System;

namespace AgenteIALocal.Infrastructure.Streaming
{
    /// <summary>
    /// Parser de respuestas streaming SSE para APIs OpenAI-compatible
    /// Funciona con: LM Studio, Jan, OpenAI, cualquier provider compatible
    /// </summary>
    public class OpenAIStreamingParser : IStreamingResponseParser
    {
        /// <summary>
        /// Parsea un chunk de streaming SSE (después de quitar "data: " prefix)
        /// </summary>
        /// <param name="jsonLine">Línea JSON sin el prefix "data: "</param>
        /// <returns>Chunk parseado con contenido y/o usage</returns>
        public StreamingChunk ParseChunk(string jsonLine)
        {
            if (string.IsNullOrWhiteSpace(jsonLine))
            {
                return new StreamingChunk { IsDone = false };
            }

            // Detectar marcador [DONE]
            if (string.Equals(jsonLine.Trim(), "[DONE]", StringComparison.Ordinal))
            {
                return new StreamingChunk { IsDone = true };
            }

            try
            {
                var chunk = JObject.Parse(jsonLine);

                var result = new StreamingChunk();

                // Extraer contenido de delta (choices[0].delta.content)
                var choices = chunk["choices"] as JArray;
                if (choices != null && choices.Count > 0)
                {
                    var first = choices[0] as JObject;
                    if (first != null)
                    {
                        var delta = first["delta"] as JObject;
                        if (delta != null)
                        {
                            var content = delta.Value<string>("content");
                            if (!string.IsNullOrEmpty(content))
                            {
                                result.Content = content;
                            }
                        }
                    }
                }

                // Extraer usage tokens (solo en último chunk si includeUsage=true)
                var usage = chunk["usage"] as JObject;
                if (usage != null)
                {
                    result.Usage = new TokenUsage
                    {
                        PromptTokens = usage.Value<int?>("prompt_tokens"),
                        CompletionTokens = usage.Value<int?>("completion_tokens"),
                        TotalTokens = usage.Value<int?>("total_tokens")
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Error parsing - retornar chunk con error
                return new StreamingChunk
                {
                    HasError = true,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
