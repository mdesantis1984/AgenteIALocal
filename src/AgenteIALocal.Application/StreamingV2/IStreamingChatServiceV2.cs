using System;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.StreamingV2;

namespace AgenteIALocal.Application.StreamingV2
{
    public interface IStreamingChatServiceV2
    {
        Task<LlmChatResultV2> RunAsync(LlmChatRequestV2 request, Action<LlmStreamEventV2> onEvent, CancellationToken ct);
    }
}
