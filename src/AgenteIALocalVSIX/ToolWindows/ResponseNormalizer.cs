using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgenteIALocalVSIX.ToolWindows
{
    /// <summary>
    /// WARNING: Do not use ResponseNormalizer.Normalize() for Markdown rendering.
    /// ResponseNormalizer performs unescaping and whitespace conversions that may degrade
    /// original Markdown input (e.g. converting literal \n sequences). Markdown rendering
    /// must use the raw response text. ResponseNormalizer is intended for JSON/plain-text
    /// normalization and extraction only.
    /// </summary>
    internal static class ResponseNormalizer
    {
        private const int LogTruncateLength = 300;

        /// <summary>
        /// Normalize raw response text produced by different LLM providers.
        /// - Fail-safe: never throws, returns original raw string on error.
        /// - Handles: plain text, error JSON, OpenAI-style chat completions, embedded JSON strings, and pretty-printing.
        /// Adds verbose logging for diagnostics (routed through AgentComposition.Info to ensure formatted persistence).
        /// </summary>
        public static string Normalize(string raw, string correlationId = null)
        {
            // Use provided correlationId; do NOT generate a new GUID here to ensure alignment with Run correlation.
            // correlationId may be null; callers should pass active run correlation when available.

            if (string.IsNullOrEmpty(raw))
            {
                LogVerbose(correlationId, "Normalize: empty/null raw");
                return raw;
            }

            LogVerbose(correlationId, "Normalize: start", raw);

            try
            {
                // Fast path: try parse as JSON
                JToken root;
                try
                {
                    root = JToken.Parse(raw);
                    LogVerbose(correlationId, "Normalize: JSON parse OK");
                }
                catch (Exception ex)
                {
                    // Not JSON -> return as plain text
                    LogVerbose(correlationId, "Normalize: JSON parse FAIL -> treat as plain text", null, ex);
                    return raw;
                }

                // 1) JSON error shapes: { "error": { "message": "..." } } or { "error": "..." }
                try
                {
                    var errorToken = root["error"];
                    if (errorToken != null)
                    {
                        if (errorToken.Type == JTokenType.Object)
                        {
                            var msg = errorToken.Value<string>("message") ?? errorToken.ToString(Formatting.None);
                            var val = "Error: " + (msg ?? "(unknown error)");
                            LogVerbose(correlationId, "Normalize: detected error object", val);
                            return val;
                        }

                        var errStr = errorToken.ToString(Formatting.None);
                        var val2 = "Error: " + (string.IsNullOrEmpty(errStr) ? "(unknown error)" : errStr);
                        LogVerbose(correlationId, "Normalize: detected error token", val2);
                        return val2;
                    }

                    var altErr = root.Value<string>("error_message") ?? root.Value<string>("errorMessage");
                    if (!string.IsNullOrEmpty(altErr))
                    {
                        var val3 = "Error: " + altErr;
                        LogVerbose(correlationId, "Normalize: detected alt error", val3);
                        return val3;
                    }
                }
                catch (Exception ex)
                {
                    LogVerbose(correlationId, "Normalize: error detection block failed (ignored)", null, ex);
                }

                // 2) OpenAI / Chat-like: { "choices": [ { "message": { "content": "..." } } ] }
                try
                {
                    var choices = root["choices"] as JArray;
                    if (choices != null && choices.Count > 0)
                    {
                        var first = choices[0] as JToken;
                        if (first != null)
                        {
                            var msg = first["message"] as JToken;
                            if (msg != null && msg["content"] != null)
                            {
                                var content = msg["content"].ToString();
                                LogVerbose(correlationId, "Normalize: OpenAI message.content detected");
                                return NormalizeContentString(content, raw, correlationId);
                            }

                            if (first["text"] != null)
                            {
                                var content = first["text"].ToString();
                                LogVerbose(correlationId, "Normalize: OpenAI choices[0].text detected");
                                return NormalizeContentString(content, raw, correlationId);
                            }

                            var fallback = first.ToString(Formatting.Indented);
                            LogVerbose(correlationId, "Normalize: choices present but no content/text -> pretty-print first choice");
                            return fallback;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogVerbose(correlationId, "Normalize: choices block failed (ignored)", null, ex);
                }

                // 3) Top-level common fields that may contain useful text
                try
                {
                    var possible = root["output"] ?? root["result"] ?? root["content"];
                    if (possible != null)
                    {
                        if (possible.Type == JTokenType.String)
                        {
                            LogVerbose(correlationId, "Normalize: top-level output/result/content string detected");
                            return NormalizeContentString(possible.ToString(), raw, correlationId);
                        }

                        var pretty = possible.ToString(Formatting.Indented);
                        LogVerbose(correlationId, "Normalize: top-level output/result/content non-string -> pretty-print");
                        return pretty;
                    }
                }
                catch (Exception ex)
                {
                    LogVerbose(correlationId, "Normalize: top-level field block failed (ignored)", null, ex);
                }

                // 4) Unknown JSON -> pretty-print it
                try
                {
                    var prettyAll = root.ToString(Formatting.Indented);
                    LogVerbose(correlationId, "Normalize: unknown JSON -> pretty-print root");
                    return prettyAll;
                }
                catch (Exception ex)
                {
                    LogVerbose(correlationId, "Normalize: pretty-print root failed -> return raw", null, ex);
                    return raw;
                }
            }
            catch (Exception ex)
            {
                LogVerbose(correlationId, "Normalize: unexpected failure -> return raw", null, ex);
                return raw;
            }
        }

        private static string NormalizeContentString(string content, string fallbackRaw, string correlationId)
        {
            if (content == null) return string.Empty;

            try
            {
                var s = content ?? string.Empty;
                s = s.Trim();

                // Convert escaped whitespace sequences to actual characters (handles mixed real + escaped)
                s = ConvertEscapedWhitespace(s, correlationId);
                s = UnescapeIfDoubleEscaped(s, correlationId);

                // If content looks like JSON, try parse & pretty-print
                try
                {
                    if ((s.StartsWith("{") && s.EndsWith("}")) || (s.StartsWith("[") && s.EndsWith("]")))
                    {
                        try
                        {
                            var token = JToken.Parse(s);
                            var pretty = token.ToString(Formatting.Indented);
                            LogVerbose(correlationId, "NormalizeContentString: content is JSON -> pretty-print");
                            return pretty;
                        }
                        catch (Exception ex)
                        {
                            LogVerbose(correlationId, "NormalizeContentString: JSON parse failed (will try unescape/string-literal paths)", ex: ex);
                            // If direct parse fails, try unescaping (content might be a quoted/escaped JSON string)
                        }
                    }

                    // Try to unescape if content is a JSON string literal (e.g. "\"{\\\"a\\\":1}\"")
                    try
                    {
                        var unescapedLiteral = JsonConvert.DeserializeObject<string>(s);
                        if (!string.IsNullOrEmpty(unescapedLiteral))
                        {
                            var inner = unescapedLiteral.Trim();

                            // Convert escaped whitespace again after literal unescape
                            inner = ConvertEscapedWhitespace(inner, correlationId);
                            inner = UnescapeIfDoubleEscaped(inner, correlationId);

                            if ((inner.StartsWith("{") && inner.EndsWith("}")) || (inner.StartsWith("[") && inner.EndsWith("]")))
                            {
                                try
                                {
                                    var token = JToken.Parse(inner);
                                    var pretty = token.ToString(Formatting.Indented);
                                    LogVerbose(correlationId, "NormalizeContentString: JSON string literal -> pretty-print inner JSON");
                                    return pretty;
                                }
                                catch (Exception ex)
                                {
                                    LogVerbose(correlationId, "NormalizeContentString: inner JSON parse failed -> return unescaped literal", ex: ex);
                                }
                            }

                            LogVerbose(correlationId, "NormalizeContentString: return unescaped literal");
                            return unescapedLiteral;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogVerbose(correlationId, "NormalizeContentString: JsonConvert.DeserializeObject<string> failed (ignored)", ex: ex);
                        // not a quoted JSON string - ignore
                    }
                }
                catch (Exception ex)
                {
                    LogVerbose(correlationId, "NormalizeContentString: outer normalize block failed (ignored)", ex: ex);
                    // ignore
                }

                // Default: return processed content as-is (IMPORTANT: return s, not original content)
                LogVerbose(correlationId, "NormalizeContentString: return processed content");
                return s;
            }
            catch (Exception ex)
            {
                LogVerbose(correlationId, "NormalizeContentString: unexpected failure -> return original content", ex: ex);
                return content;
            }
        }

        private static void LogVerbose(string correlationId, string message, string preview = null, Exception ex = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(message ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(preview))
                {
                    sb.Append(" | preview: ");
                    sb.Append(Truncate(preview, LogTruncateLength));
                }

                if (ex != null)
                {
                    sb.Append(" | ex: ");
                    sb.Append(ex.GetType().Name);
                    sb.Append(": ");
                    sb.Append(ex.Message);
                }

                // Route verbose diagnostics through the canonical V2 logger so persisted lines are produced by the formatter
                // MODIFICADO - ID: 20260122_010801 - Migrado de AgentComposition.Verbose → Serilog
                try
                {
                    var corr = string.IsNullOrEmpty(correlationId) ? "-" : correlationId;
                    AgenteIALocal.Logging.Log.Verbose(corr, 9200, "ResponseNormalizer.Normalize", sb.ToString(), null);
                }
                catch
                {
                    // Swallow any logging failures - must not throw
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= max) return s;
            return s.Substring(0, max) + "...";
        }

        private static string UnescapeIfDoubleEscaped(string s, string correlationId)
        {
            if (string.IsNullOrEmpty(s)) return s;

            try
            {
                // Si contiene secuencias literales \n/\r/\t, intentamos des-escapar SIEMPRE.
                // (No cortar por tener newlines reales; puede venir mezcla.)
                if (s.Contains("\\n") || s.Contains("\\r") || s.Contains("\\t") || s.Contains("\\r\\n"))
                {
                    try
                    {
                        var un = Regex.Unescape(s);
                        return un ?? s;
                    }
                    catch
                    {
                        return s;
                    }
                }

                return s;
            }
            catch
            {
                return s;
            }
        }

        private static string ConvertEscapedWhitespace(string s, string correlationId)
        {
            if (string.IsNullOrEmpty(s)) return s;

            try
            {
                // Si contiene secuencias literales, convertirlas a caracteres reales.
                // Importante: NO salir temprano si hay saltos reales; puede venir mezcla.
                bool containsEscapes =
                    s.Contains("\\r\\n") ||
                    s.Contains("\\n") ||
                    s.Contains("\\r") ||
                    s.Contains("\\t");

                if (!containsEscapes) return s;

                var replaced = s
                    .Replace("\\r\\n", "\r\n")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t");

                // Último paso: Regex.Unescape sobre el resultado, úsalo solo si cambia.
                try
                {
                    var unescaped = Regex.Unescape(replaced);
                    if (!string.IsNullOrEmpty(unescaped) &&
                        !string.Equals(unescaped, replaced, StringComparison.Ordinal))
                    {
                        return unescaped;
                    }
                }
                catch
                {
                    // ignore
                }

                return replaced;
            }
            catch
            {
                return s;
            }
        }
    }
}