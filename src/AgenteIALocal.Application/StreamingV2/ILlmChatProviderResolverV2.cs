using AgenteIALocal.Core.StreamingV2;

namespace AgenteIALocal.Application.StreamingV2
{
    public interface ILlmChatProviderResolverV2
    {
        bool TryResolve(string providerName, out ILlmChatProviderV2 provider);
    }
}
