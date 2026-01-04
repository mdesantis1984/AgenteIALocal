namespace AgenteIALocal.Core.Agents
{
    public sealed class AgentResponse
    {
        public string Content { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        // Optional token usage and raw payload captured from provider responses
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public string RawResponse { get; set; }
    }
}
