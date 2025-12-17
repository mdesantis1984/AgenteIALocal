namespace AgenteIALocal.Core.Models.AI
{
    /// <summary>
    /// Typed options for AI requests. Provider adapters can map these to provider-specific parameters.
    /// </summary>
    public sealed class AIRequestOptions
    {
        /// <summary>
        /// Sampling temperature (0.0 - 1.0). Null means provider default.
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Maximum tokens to generate. Null means provider default.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Top P sampling parameter (0.0 - 1.0).
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// Whether to stream responses. Default false.
        /// </summary>
        public bool Stream { get; set; }
    }
}
