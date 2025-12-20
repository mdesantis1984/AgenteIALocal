namespace AgenteIALocal.Core.Agents
{
    public sealed class AgentResponse
    {
        public string Content { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
    }
}
