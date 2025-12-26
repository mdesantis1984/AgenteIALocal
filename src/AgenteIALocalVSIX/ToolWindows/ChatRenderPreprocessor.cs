using System;
using System.Collections.Generic;

namespace AgenteIALocalVSIX.ToolWindows
{
    public static class ChatRenderPreprocessor
    {
        public static string Preprocess(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;

            string normalized = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            var result = new List<string>();
            bool inCode = false;
            int emptyCount = 0;
            const string separator = "────────";

            foreach (var originalLine in lines)
            {
                var line = originalLine ?? string.Empty;

                if (line.Trim() == "```")
                {
                    if (!inCode)
                    {
                        emptyCount = 0;
                        result.Add(separator);
                        inCode = true;
                    }
                    else
                    {
                        inCode = false;
                        result.Add(separator);
                    }
                    continue;
                }

                if (inCode)
                {
                    result.Add("    " + line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    emptyCount++;
                    if (emptyCount <= 2)
                    {
                        result.Add(string.Empty);
                    }
                    continue;
                }

                emptyCount = 0;

                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed == "-" || trimmed == "*")
                {
                    result.Add("  " + trimmed);
                }
                else
                {
                    result.Add(line);
                }
            }

            if (inCode)
            {
                result.Add(separator);
            }

            return string.Join(Environment.NewLine, result);
        }
    }
}
