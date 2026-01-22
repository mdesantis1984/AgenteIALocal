using System;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.StreamingV2;
using AgenteIALocal.Logging;

namespace AgenteIALocal.Application.StreamingV2
{
    public sealed class StreamingChatServiceV2 : IStreamingChatServiceV2
    {
        private readonly ILlmChatProviderResolverV2 _resolver;

        // MODIFICADO CONSTRUCTOR - ID: 20260122_030001 - Eliminado IAgentLoggerV2, ahora usa AgenteIALocal.Logging.Log directamente
        public StreamingChatServiceV2(ILlmChatProviderResolverV2 resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
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
                // MODIFICADO - ID: 20260122_030002 - Migrado a Serilog
                Log.Error(correlationIdNotFound, StreamingV2LogEventIds.StreamingV2_Error, "StreamingV2.RunAsync", "StreamingV2 provider not found: " + request.ProviderName, null);
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "error" };
            }

            var correlationId = Guid.NewGuid().ToString("N");
            // MODIFICADO - ID: 20260122_030003 - Eliminada propiedad Logger (no existe en LlmRunContextV2)
            var ctx = new LlmRunContextV2
            {
                CorrelationId = correlationId,
                Ctx = null
            };

            try
            {
                return await provider.StreamChatAsync(request, ctx, SafeEmit, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SafeEmit(new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = "Canceled" });
                // MODIFICADO - ID: 20260122_030004 - Migrado a Serilog
                Log.Warning(correlationId, StreamingV2LogEventIds.StreamingV2_Canceled, "StreamingV2.RunAsync", "StreamingV2 canceled", null);
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "canceled" };
            }
            catch (Exception ex)
            {
                SafeEmit(new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = ex.Message });
                // MODIFICADO - ID: 20260122_030005 - Migrado a Serilog
                Log.Error(correlationId, StreamingV2LogEventIds.StreamingV2_Error, "StreamingV2.RunAsync", "StreamingV2 exception: " + ex.Message, ex);
                return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "error" };
            }
        }
    }
}
