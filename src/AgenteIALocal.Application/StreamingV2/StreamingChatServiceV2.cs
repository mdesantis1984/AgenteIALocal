using System;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Logging;
using AgenteIALocal.Core.StreamingV2;

namespace AgenteIALocal.Application.StreamingV2
{
    public sealed class StreamingChatServiceV2 : IStreamingChatServiceV2
    {
        private readonly ILlmChatProviderResolverV2 _resolver;
        private readonly IAgentLoggerV2 _logger;

        public StreamingChatServiceV2(ILlmChatProviderResolverV2 resolver, IAgentLoggerV2 logger)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LlmChatResultV2> RunAsync(LlmChatRequestV2 request, Action<LlmStreamEventV2> onEvent, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var handler = onEvent ?? (_ => { });
            Action<LlmStreamEventV2> SafeEmit = e => { try { handler(e); } catch { } };

            ILlmChatProviderV2 provider;
            if (!_resolver.TryResolve(request.ProviderName, out provider) || provider == null)
            {
                var correlationIdNotFound = Guid.NewGuid().ToString("N");
                SafeEmit(new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = "ProviderNotFound:" + request.ProviderName });
                _logger.Error(correlationIdNotFound, StreamingV2LogEventIds.StreamingV2_Error, "StreamingV2 provider not found: " + request.ProviderName);
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "error" };
            }

            var correlationId = Guid.NewGuid().ToString("N");
            var ctx = new LlmRunContextV2
            {
                CorrelationId = correlationId,
                Logger = _logger,
                Ctx = null
            };

            try
            {
                return await provider.StreamChatAsync(request, ctx, SafeEmit, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SafeEmit(new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = "Canceled" });
                _logger.Warning(correlationId, StreamingV2LogEventIds.StreamingV2_Canceled, "StreamingV2 canceled");
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "canceled" };
            }
            catch (Exception ex)
            {
                SafeEmit(new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = ex.Message });
                _logger.Error(correlationId, StreamingV2LogEventIds.StreamingV2_Error, "StreamingV2 exception: " + ex.Message, ex);
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "error" };
            }
        }
    }
}
