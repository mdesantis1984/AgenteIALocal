using System;
using System.Text;

namespace AgenteIALocal.Infrastructure.Agents.StreamingV2
{
    internal static class OpenAiChatStreamParserV2
    {
        internal static bool TryExtractErrorMessage(string json, out string message)
        {
            message = null;
            if (string.IsNullOrEmpty(json)) return false;

            var errorIdx = json.IndexOf("\"error\"", StringComparison.OrdinalIgnoreCase);
            if (errorIdx < 0) return false;

            var msgIdx = json.IndexOf("\"message\"", errorIdx, StringComparison.OrdinalIgnoreCase);
            if (msgIdx < 0) return false;

            var colon = json.IndexOf(':', msgIdx);
            if (colon < 0) return false;

            return TryReadJsonStringValueAt(json, colon, out message);
        }

        internal static bool TryExtractDeltaContent(string json, out string delta)
        {
            delta = null;
            if (string.IsNullOrEmpty(json)) return false;

            var deltaIdx = json.IndexOf("\"delta\"", StringComparison.OrdinalIgnoreCase);
            if (deltaIdx < 0) return false;

            var contentIdx = json.IndexOf("\"content\"", deltaIdx, StringComparison.OrdinalIgnoreCase);
            if (contentIdx < 0) return false;

            var colon = json.IndexOf(':', contentIdx);
            if (colon < 0) return false;

            return TryReadJsonStringValueAt(json, colon, out delta);
        }

        internal static bool TryExtractFinishReason(string json, out string finishReason)
        {
            finishReason = null;
            if (string.IsNullOrEmpty(json)) return false;

            var idx = json.IndexOf("\"finish_reason\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            var colon = json.IndexOf(':', idx);
            if (colon < 0) return false;

            var raw = json.Substring(colon + 1).TrimStart();
            if (raw.StartsWith("null", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryReadJsonStringValueAt(json, colon, out finishReason);
        }

        private static bool TryReadJsonStringValueAt(string json, int indexOfColon, out string value)
        {
            value = null;
            var i = indexOfColon + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != '"') return false;
            i++;

            var sb = new StringBuilder();
            var escaped = false;
            for (; i < json.Length; i++)
            {
                var c = json[i];
                if (escaped)
                {
                    switch (c)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 < json.Length)
                            {
                                var hex = json.Substring(i + 1, 4);
                                int code;
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out code))
                                {
                                    sb.Append((char)code);
                                    i += 4;
                                }
                                else
                                {
                                    sb.Append('u');
                                }
                            }
                            else
                            {
                                sb.Append('u');
                            }
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                sb.Append(c);
            }

            return false;
        }
    }
}
