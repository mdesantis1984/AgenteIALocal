namespace AgenteIALocal.Core.Models.AI
{
    /// <summary>
    /// Token and usage accounting structure. Optional and provider-specific mapping.
    /// </summary>
    public sealed class AIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
    }
}
