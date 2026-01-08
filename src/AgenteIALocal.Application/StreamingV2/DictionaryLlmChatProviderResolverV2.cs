using System;
using System.Collections.Generic;
using AgenteIALocal.Core.StreamingV2;

namespace AgenteIALocal.Application.StreamingV2
{
    public sealed class DictionaryLlmChatProviderResolverV2 : ILlmChatProviderResolverV2
    {
        private readonly Dictionary<string, ILlmChatProviderV2> _providers;

        public DictionaryLlmChatProviderResolverV2(IDictionary<string, ILlmChatProviderV2> providers)
        {
            if (providers == null) throw new ArgumentNullException(nameof(providers));
            _providers = new Dictionary<string, ILlmChatProviderV2>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in providers)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                _providers[kvp.Key] = kvp.Value;
            }
        }

        public bool TryResolve(string providerName, out ILlmChatProviderV2 provider)
        {
            provider = null;
            if (string.IsNullOrWhiteSpace(providerName)) return false;
            return _providers.TryGetValue(providerName, out provider);
        }
    }
}
