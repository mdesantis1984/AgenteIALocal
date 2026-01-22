using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Networking;
using AgenteIALocal.Core.Settings;
using AgenteIALocal.Core.StreamingV2;
using AgenteIALocal.Logging;

namespace AgenteIALocal.Infrastructure.Agents.StreamingV2
{
    public sealed class LmStudioChatProviderV2 : ILlmChatProviderV2
    {
        private readonly LmStudioSettings settings;

        public LmStudioChatProviderV2(LmStudioSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<LlmChatResultV2> StreamChatAsync(LlmChatRequestV2 req, LlmRunContextV2 ctx, Action<LlmStreamEventV2> onEvent, CancellationToken ct)
        {
            return ExecuteAsync(req, ctx, onEvent, ct);
        }

        private async Task<LlmChatResultV2> ExecuteAsync(LlmChatRequestV2 req, LlmRunContextV2 ctx, Action<LlmStreamEventV2> onEvent, CancellationToken ct)
        {
            var safeOnEvent = onEvent ?? (_ => { });
            // MODIFICADO - ID: 20260122_030011 - Eliminado ctx.Logger (ahora usa Log estático)
            var correlationId = ctx == null ? null : ctx.CorrelationId;
            var ctxMap = ctx == null ? null : ctx.Ctx;
            var sb = new StringBuilder();
            string finishReason = null;
            HttpWebRequest httpRequest = null;
            HttpWebResponse httpResponse = null;
            StreamReader reader = null;

            try
            {
                req = req ?? new LlmChatRequestV2();

                var endpointResolver = new LmStudioEndpointResolver(settings);
                var endpoint = endpointResolver.GetChatCompletionsEndpoint();
                if (endpoint == null)
                {
                    SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = "EndpointNotResolved" });
                    // MODIFICADO - ID: 20260122_030012 - Migrado a Serilog
                    Log.Error(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Error, "LmStudio.Execute", "StreamingV2 LMStudio endpoint not resolved.", null);
                    return new LlmChatResultV2 { FullText = string.Empty, FinishReason = "error" };
                }

                var effectiveModel = string.IsNullOrEmpty(req.Model) ? (settings.Model ?? string.Empty) : req.Model;
                var payload = BuildPayload(req, effectiveModel);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                httpRequest = (HttpWebRequest)WebRequest.Create(endpoint);
                httpRequest.Method = "POST";
                httpRequest.ContentType = "application/json";
                httpRequest.Accept = "text/event-stream";
                httpRequest.Timeout = (int)HttpTimeouts.LmStudioChatRequestTimeout.TotalMilliseconds;
                httpRequest.ReadWriteTimeout = (int)HttpTimeouts.LmStudioChatRequestTimeout.TotalMilliseconds;

                var apiKey = settings.ApiKey;
                if (string.IsNullOrEmpty(apiKey)) apiKey = "lm-studio";
                httpRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + apiKey;

                using (var requestStream = await httpRequest.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    await requestStream.WriteAsync(payloadBytes, 0, payloadBytes.Length, ct).ConfigureAwait(false);
                }

                httpResponse = (HttpWebResponse)await httpRequest.GetResponseAsync().ConfigureAwait(false);
                var responseStream = httpResponse.GetResponseStream() ?? Stream.Null;
                reader = new StreamReader(responseStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false);

                // MODIFICADO - ID: 20260122_030013 - Migrado a Serilog
                Log.Information(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Start, "LmStudio.Execute", "StreamingV2 LMStudio start: model=" + effectiveModel + " endpoint=" + endpoint, null);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var data = await SseLineReaderV2.ReadNextDataLineAsync(reader, ct).ConfigureAwait(false);
                    if (data == null)
                    {
                        break;
                    }

                    if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    string errorMessage;
                    if (OpenAiChatStreamParserV2.TryExtractErrorMessage(data, out errorMessage) && !string.IsNullOrEmpty(errorMessage))
                    {
                        SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = errorMessage, RawJson = data });
                        // MODIFICADO - ID: 20260122_030014 - Migrado a Serilog
                        Log.Error(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Error, "LmStudio.Execute", "StreamingV2 LMStudio error: " + errorMessage, null);
                        return new LlmChatResultV2 { FullText = sb.ToString(), FinishReason = "error" };
                    }

                    string delta;
                    if (OpenAiChatStreamParserV2.TryExtractDeltaContent(data, out delta) && !string.IsNullOrEmpty(delta))
                    {
                        sb.Append(delta);
                        SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Delta, DeltaText = delta, RawJson = data });
                    }

                    string fr;
                    if (OpenAiChatStreamParserV2.TryExtractFinishReason(data, out fr) && !string.IsNullOrEmpty(fr))
                    {
                        finishReason = fr;
                    }
                }

                SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Done });
                // MODIFICADO - ID: 20260122_030015 - Migrado a Serilog
                Log.Information(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Done, "LmStudio.Execute", "StreamingV2 LMStudio done", null);

                return new LlmChatResultV2 { FullText = sb.ToString(), FinishReason = finishReason };
            }
            catch (OperationCanceledException)
            {
                try { httpRequest?.Abort(); } catch { }
                SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = "Canceled" });
                // MODIFICADO - ID: 20260122_030016 - Migrado a Serilog
                Log.Warning(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Canceled, "LmStudio.Execute", "StreamingV2 LMStudio canceled", null);
                return new LlmChatResultV2 { FullText = sb.ToString(), FinishReason = "canceled" };
            }
            catch (WebException wex)
            {
                try { httpRequest?.Abort(); } catch { }
                var body = ReadBodySafe(wex);
                var errMsg = string.IsNullOrEmpty(body) ? wex.Message : wex.Message + " - " + body;
                SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = errMsg, RawJson = body });
                // MODIFICADO - ID: 20260122_030017 - Migrado a Serilog
                Log.Error(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Error, "LmStudio.Execute", "StreamingV2 LMStudio exception: " + errMsg, wex);
                return new LlmChatResultV2 { FullText = sb.ToString(), FinishReason = "error" };
            }
            catch (Exception ex)
            {
                try { httpRequest?.Abort(); } catch { }
                SafeOnEvent(safeOnEvent, new LlmStreamEventV2 { Type = LlmStreamEventTypeV2.Error, ErrorMessage = ex.Message });
                // MODIFICADO - ID: 20260122_030018 - Migrado a Serilog
                Log.Error(correlationId ?? "-", StreamingV2LogEventIds.StreamingV2_Error, "LmStudio.Execute", "StreamingV2 LMStudio exception: " + ex.Message, ex);
                return new LlmChatResultV2 { FullText = sb.ToString(), FinishReason = "error" };
            }
            finally
            {
                try { reader?.Dispose(); } catch { }
                try { httpResponse?.Dispose(); } catch { }
                try { httpRequest?.Abort(); } catch { }
            }
        }

        private static void SafeOnEvent(Action<LlmStreamEventV2> handler, LlmStreamEventV2 evt)
        {
            try { handler(evt); } catch { }
        }

        private static string BuildPayload(LlmChatRequestV2 request, string model)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"model\":\"");
            sb.Append(EscapeJson(model ?? string.Empty));
            sb.Append("\"");

            sb.Append(",\"stream\":true");

            if (request.Temperature.HasValue)
            {
                sb.Append(",\"temperature\":");
                sb.Append(request.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (request.MaxTokens.HasValue)
            {
                sb.Append(",\"max_tokens\":");
                sb.Append(request.MaxTokens.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            sb.Append(",\"messages\":[");

            var messages = request.Messages ?? (IReadOnlyList<LlmChatMessageV2>)Array.Empty<LlmChatMessageV2>();
            var first = true;
            foreach (var m in messages)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('{');
                sb.Append("\"role\":\"");
                sb.Append(MapRole(m.Role));
                sb.Append("\",\"content\":\"");
                sb.Append(EscapeJson(m.Content ?? string.Empty));
                sb.Append("\"}");
            }

            sb.Append(']');
            sb.Append('}');
            return sb.ToString();
        }

        private static string MapRole(LlmMessageRoleV2 role)
        {
            switch (role)
            {
                case LlmMessageRoleV2.System: return "system";
                case LlmMessageRoleV2.User: return "user";
                case LlmMessageRoleV2.Assistant: return "assistant";
                default: return "user";
            }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string ReadBodySafe(WebException wex)
        {
            try
            {
                using (var resp = wex.Response as HttpWebResponse)
                using (var stream = resp?.GetResponseStream())
                using (var reader = stream == null ? null : new StreamReader(stream))
                {
                    return reader?.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
