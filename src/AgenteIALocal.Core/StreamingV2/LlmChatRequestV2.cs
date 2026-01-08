using System;
using System.Collections.Generic;

namespace AgenteIALocal.Core.StreamingV2
{
    public class LlmChatRequestV2
    {
        public string ProviderName { get; set; }

        public string Model { get; set; }

        public IReadOnlyList<LlmChatMessageV2> Messages { get; set; } = Array.Empty<LlmChatMessageV2>();

        public double? Temperature { get; set; }

        public int? MaxTokens { get; set; }

        public bool Stream { get; set; } = true;
    }
}
