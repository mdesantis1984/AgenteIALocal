// NUEVA INTERFAZ IStreamingResponseParser - ID: 20260123_230500
// ARQUITECTURA: Core - Interfaz pura para parseo de respuestas streaming
// PROPÓSITO: Eliminar lógica JSON parsing de UI layer
namespace AgenteIALocal.Core.Streaming
{
    /// <summary>
    /// Representa el resultado de parsear un chunk de streaming SSE
    /// </summary>
    public class StreamingChunk
    {
        /// <summary>
        /// Contenido de texto extraído del chunk (delta.content)
        /// Null o vacío si el chunk no contiene contenido
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Información de uso de tokens (solo en último chunk si includeUsage=true)
        /// </summary>
        public TokenUsage Usage { get; set; }

        /// <summary>
        /// Indica si este chunk es el marcador de finalización ([DONE])
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// Indica si hubo un error parseando el chunk
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Mensaje de error si HasError=true
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Información de uso de tokens del LLM
    /// </summary>
    public class TokenUsage
    {
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
    }

    /// <summary>
    /// Parser de respuestas streaming SSE
    /// ARQUITECTURA: Interfaz en Core - Implementación en Infrastructure
    /// </summary>
    public interface IStreamingResponseParser
    {
        /// <summary>
        /// Parsea una línea de streaming SSE (después de quitar "data: " prefix)
        /// </summary>
        /// <param name="jsonLine">Línea JSON (sin el prefix "data: ")</param>
        /// <returns>Chunk parseado con contenido y/o usage</returns>
        StreamingChunk ParseChunk(string jsonLine);
    }
}
