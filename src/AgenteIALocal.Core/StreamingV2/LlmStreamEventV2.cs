namespace AgenteIALocal.Core.StreamingV2
{
    public class LlmStreamEventV2
    {
        public LlmStreamEventTypeV2 Type { get; set; }

        public string DeltaText { get; set; }

        public string ErrorMessage { get; set; }

        public string RawJson { get; set; }
    }
}
