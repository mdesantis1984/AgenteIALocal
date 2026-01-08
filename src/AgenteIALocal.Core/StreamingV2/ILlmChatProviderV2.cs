using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgenteIALocal.Core.StreamingV2
{
    public interface ILlmChatProviderV2
    {
        Task<LlmChatResultV2> StreamChatAsync(LlmChatRequestV2 request, LlmRunContextV2 context, Action<LlmStreamEventV2> onEvent, CancellationToken cancellationToken);
    }
}
