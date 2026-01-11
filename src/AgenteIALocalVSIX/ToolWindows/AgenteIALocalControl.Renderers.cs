// NUEVO ARCHIVO AgenteIALocalControl.Renderers.cs - ID: 20250110_000005
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl
    {
        // Response format enum (Phase 1 + Markdown)
        private enum ResponseFormat { PlainText, Json, Markdown }

        // Simple format detector (JSON vs Markdown vs PlainText)
        private static class FormatDetector
        {
            public static ResponseFormat DetectFormat(string content)
            {
                if (string.IsNullOrWhiteSpace(content)) return ResponseFormat.PlainText;

                var trimmed = content.TrimStart();

                // Heuristic: Markdown markers
                bool looksLikeMarkdown = false;
                try
                {
                    if (content.Contains("```")) looksLikeMarkdown = true;
                    var lines = content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.TrimStart();
                        if (line.StartsWith("# ") || line.StartsWith("## ") || line.StartsWith("### ") ||
                            line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("> ") ||
                            Regex.IsMatch(line, "^\\d+\\.\\s"))
                        {
                            looksLikeMarkdown = true;
                            break;
                        }
                    }
                }
                catch { }

                // Heuristic: JSON structural + parse attempt
                bool isJson = false;
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    try
                    {
                        JToken.Parse(content);
                        isJson = true;
                    }
                    catch { isJson = false; }
                }

                // Preference: if both detected, prefer JSON; if markdown detected and NOT json, return Markdown
                if (looksLikeMarkdown && !isJson)
                {
                    return ResponseFormat.Markdown;
                }

                if (isJson)
                {
                    return ResponseFormat.Json;
                }

                if (looksLikeMarkdown)
                {
                    return ResponseFormat.Markdown;
                }

                return ResponseFormat.PlainText;
            }
        }

        // Renderer interface and implementations (modified to accept correlationId)
        private interface IResponseRenderer
        {
            FlowDocument Render(string content, string correlationId);
        }

        private class PlainTextResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                return CreatePlainDocument(content ?? string.Empty);
            }
        }

        private class JsonResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                if (content == null) content = string.Empty;
                try
                {
                    var token = JToken.Parse(content);
                    var pretty = token.ToString(Newtonsoft.Json.Formatting.Indented);

                    var fd = new FlowDocument { PagePadding = new System.Windows.Thickness(0) };
                    var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
                    p.FontFamily = new FontFamily("Consolas");

                    var lines = pretty.Split(new[] { '\n' });
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i] ?? string.Empty;
                        var run = new Run(line);
                        p.Inlines.Add(run);
                        if (i < lines.Length - 1) p.Inlines.Add(new LineBreak());
                    }

                    fd.Blocks.Clear();
                    fd.Blocks.Add(p);

                    try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] JsonResponseRenderer: rendered json"); } catch { }

                    return fd;
                }
                catch (System.Exception)
                {
                    return CreatePlainDocument(content);
                }
            }
        }

        private class MarkdownResponseRenderer : IResponseRenderer
        {
            public FlowDocument Render(string content, string correlationId)
            {
                try
                {
                    if (content == null) content = string.Empty;

                    var fd = new FlowDocument { PagePadding = new System.Windows.Thickness(0) };

                    var lines = Regex.Split(content, "\\r?\\n");
                    bool inCodeFence = false;
                    var codeFenceBuilder = new StringBuilder();
                    string codeFenceLang = null;

                    List currentList = null;

                    foreach (var raw in lines)
                    {
                        var line = raw ?? string.Empty;

                        // Code fence handling
                        var trimmed = line.TrimStart();
                        if (!inCodeFence && trimmed.StartsWith("```") )
                        {
                            inCodeFence = true;
                            codeFenceLang = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : string.Empty;
                            codeFenceBuilder.Clear();
                            continue;
                        }
                        if (inCodeFence)
                        {
                            if (trimmed.StartsWith("```") )
                            {
                                var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
                                p.FontFamily = new FontFamily("Consolas");
                                p.Inlines.Add(new Run(codeFenceBuilder.ToString()));
                                ApplyCodeBlockStyle(p);
                                fd.Blocks.Add(p);
                                try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: code fence styled"); } catch { }
                                inCodeFence = false;
                                codeFenceLang = null;
                                continue;
                            }
                            codeFenceBuilder.AppendLine(line);
                            continue;
                        }

                        // Headings
                        if (Regex.IsMatch(trimmed, "^#{1,3}\\s+"))
                        {
                            int level = 1;
                            if (trimmed.StartsWith("###")) level = 3;
                            else if (trimmed.StartsWith("##")) level = 2;

                            var text = trimmed.TrimStart('#').Trim();
                            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
                            switch (level)
                            {
                                case 1: p.FontSize = 18; break;
                                case 2: p.FontSize = 15; break;
                                case 3: p.FontSize = 13; break;
                            }
                            p.FontWeight = FontWeights.Bold;
                            AddInlinesToParagraph(p, text);
                            ApplyHeaderStyle(p, level);
                            fd.Blocks.Add(p);

                            try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: header styled level=" + level); } catch { }

                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            continue;
                        }

                        // Blockquote
                        if (trimmed.StartsWith("> "))
                        {
                            var text = trimmed.Substring(2).Trim();
                            var p = new Paragraph { Margin = new System.Windows.Thickness(12, 0, 0, 0), Foreground = Brushes.Gray };
                            var borderRun = new Run("│ ") { Foreground = HexBrush("#3F3F46") };
                            p.Inlines.Add(borderRun);
                            AddInlinesToParagraph(p, text);
                            ApplyBlockQuoteStyle(p);
                            fd.Blocks.Add(p);
                            try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: blockquote styled"); } catch { }
                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            continue;
                        }

                        // Unordered list
                        if (Regex.IsMatch(trimmed, "^[-*]\\s+"))
                        {
                            var itemText = Regex.Replace(trimmed, "^[-*]\\s+", "");
                            if (currentList == null || currentList.MarkerStyle != TextMarkerStyle.Disc)
                            {
                                if (currentList != null) { fd.Blocks.Add(currentList); }
                                currentList = new List { MarkerStyle = TextMarkerStyle.Disc };
                                ApplyListStyle(currentList);
                            }
                            var li = new ListItem();
                            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
                            AddInlinesToParagraph(p, itemText);
                            ApplyParagraphStyle(p);
                            li.Blocks.Add(p);
                            currentList.ListItems.Add(li);
                            continue;
                        }

                        // Ordered list
                        if (Regex.IsMatch(trimmed, "^\\d+\\.\\s+"))
                        {
                            var itemText = Regex.Replace(trimmed, "^\\d+\\.\\s+", "");
                            if (currentList == null || currentList.MarkerStyle != TextMarkerStyle.Decimal)
                            {
                                if (currentList != null) { fd.Blocks.Add(currentList); }
                                currentList = new List { MarkerStyle = TextMarkerStyle.Decimal };
                                ApplyListStyle(currentList);
                            }
                            var li = new ListItem();
                            var p = new Paragraph { Margin = new System.Windows.Thickness(0) };
                            AddInlinesToParagraph(p, itemText);
                            ApplyParagraphStyle(p);
                            li.Blocks.Add(p);
                            currentList.ListItems.Add(li);
                            continue;
                        }

                        // Empty line -> close current list and add paragraph break
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                            var empty = new Paragraph { Margin = new System.Windows.Thickness(0) };
                            ApplyParagraphStyle(empty);
                            fd.Blocks.Add(empty);
                            continue;
                        }

                        // Regular paragraph line
                        var para = new Paragraph { Margin = new System.Windows.Thickness(0) };
                        AddInlinesToParagraph(para, line);
                        ApplyParagraphStyle(para);
                        fd.Blocks.Add(para);
                        if (currentList != null) { fd.Blocks.Add(currentList); currentList = null; }
                    }

                    if (inCodeFence)
                    {
                        return CreatePlainDocument(content);
                    }

                    try { AgentComposition.Verbose(correlationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] MarkdownResponseRenderer: render complete"); } catch { }

                    return fd;
                }
                catch (System.Exception)
                {
                    return CreatePlainDocument(content);
                }
            }

            // Simple inline parser to add Runs/Bold/Italic/InlineCode into paragraph
            private void AddInlinesToParagraph(Paragraph p, string text)
            {
                try
                {
                    if (string.IsNullOrEmpty(text)) return;

                    // Handle inline code first using backticks
                    var parts = Regex.Split(text, "(`[^`]+`)");

                    foreach (var part in parts)
                    {
                        if (part.StartsWith("`") && part.EndsWith("`"))
                        {
                            var code = part.Substring(1, part.Length - 2);
                            var run = new Run(code) { FontFamily = new FontFamily("Consolas") };
                            // inline code styling
                            run.Background = HexBrush("#2D2D30");
                            run.Foreground = HexBrush("#DCDCDC");
                            p.Inlines.Add(run);
                        }
                        else
                        {
                            // handle bold **text**
                            var pattern = new Regex("(\\*\\*([^\\*]+)\\*\\*)");
                            var m = pattern.Match(part);
                            if (!m.Success)
                            {
                                var ital = new Regex("(\\*([^\\*]+)\\*)");
                                var mi = ital.Match(part);
                                if (!mi.Success)
                                {
                                    p.Inlines.Add(new Run(part));
                                }
                                else
                                {
                                    var segs = Regex.Split(part, "(\\*[^\\*]+\\*)");
                                    foreach (var s in segs)
                                    {
                                        if (s.StartsWith("*") && s.EndsWith("*"))
                                        {
                                            var inner = s.Substring(1, s.Length - 2);
                                            var it = new Italic(new Run(inner));
                                            p.Inlines.Add(it);
                                        }
                                        else
                                        {
                                            p.Inlines.Add(new Run(s));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var segs = Regex.Split(part, "(\\*\\*[^\\*]+\\*\\*)");
                                foreach (var s in segs)
                                {
                                    if (s.StartsWith("**") && s.EndsWith("**"))
                                    {
                                        var inner = s.Substring(2, s.Length - 4);
                                        var b = new Bold(new Run(inner));
                                        p.Inlines.Add(b);
                                    }
                                    else
                                    {
                                        p.Inlines.Add(new Run(s));
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    p.Inlines.Add(new Run(text));
                }
            }

            // Styling helpers
            private void ApplyHeaderStyle(Paragraph p, int level)
            {
                try
                {
                    switch (level)
                    {
                        case 1:
                            p.FontSize = 18;
                            p.FontWeight = FontWeights.Bold;
                            p.Margin = new System.Windows.Thickness(0, 12, 0, 6);
                            break;
                        case 2:
                            p.FontSize = 16;
                            p.FontWeight = FontWeights.Bold;
                            p.Margin = new System.Windows.Thickness(0, 10, 0, 6);
                            break;
                        default:
                            p.FontSize = 14;
                            p.FontWeight = FontWeights.SemiBold;
                            p.Margin = new System.Windows.Thickness(0, 8, 0, 4);
                            break;
                    }
                }
                catch { }
            }

            private void ApplyCodeBlockStyle(Paragraph p)
            {
                try
                {
                    p.FontFamily = new FontFamily("Consolas");
                    p.Background = HexBrush("#1E1E1E");
                    p.Foreground = HexBrush("#DCDCDC");
                    p.Margin = new System.Windows.Thickness(0, 6, 0, 6);
                }
                catch { }
            }

            private void ApplyBlockQuoteStyle(Paragraph p)
            {
                try
                {
                    p.Foreground = HexBrush("#9DA5B4");
                    p.Margin = new System.Windows.Thickness(12, 4, 0, 6);
                }
                catch { }
            }

            private void ApplyParagraphStyle(Paragraph p)
            {
                try
                {
                    p.Margin = new System.Windows.Thickness(0, 2, 0, 6);
                    p.LineHeight = 18;
                }
                catch { }
            }

            private void ApplyListStyle(List list)
            {
                try
                {
                    list.Margin = new System.Windows.Thickness(0, 2, 0, 6);
                    // left padding simulated by marker indent
                }
                catch { }
            }

            private Brush HexBrush(string hex)
            {
                try
                {
                    var bc = new BrushConverter();
                    var b = bc.ConvertFrom(hex) as Brush;
                    return b ?? Brushes.Transparent;
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }

        private static class RendererFactory
        {
            public static IResponseRenderer Get(ResponseFormat fmt)
            {
                switch (fmt)
                {
                    case ResponseFormat.Json: return new JsonResponseRenderer();
                    case ResponseFormat.Markdown: return new MarkdownResponseRenderer();
                    case ResponseFormat.PlainText:
                    default: return new PlainTextResponseRenderer();
                }
            }
        }

        // Orchestrator: normalize -> detect -> render -> fallback
        private FlowDocument RenderResponseToDocument(string raw)
        {
            AppendLog("[VERBOSE] RenderResponseToDocument: start; rawLen=" + (raw?.Length ?? 0));

            string content = null;
            try
            {
                // 1) Detect format using RAW input (do not normalize before detection)
                var fmt = FormatDetector.DetectFormat(raw);
                AppendLog("[VERBOSE] RenderResponseToDocument: detected format=" + fmt.ToString());

                // 2) Decide normalization strategy
                if (fmt == ResponseFormat.Markdown)
                {
                    // For Markdown we must preserve original raw text exactly
                    content = raw ?? string.Empty;
                    AppendLog("[VERBOSE] RenderResponseToDocument: Markdown detected -> normalization bypassed");
                }
                else
                {
                    // For JSON and PlainText use normalizer
                    content = ResponseNormalizer.Normalize(raw, activeCorrelationId ?? "-") ?? string.Empty;
                    AppendLog("[VERBOSE] RenderResponseToDocument: normalization applied");
                }

                AppendLog("[VERBOSE] RenderResponseToDocument: rawLen=" + (raw?.Length ?? 0) + " contentLen=" + (content?.Length ?? 0));

                // 3) Select renderer
                var renderer = RendererFactory.Get(fmt);
                AppendLog("[VERBOSE] RenderResponseToDocument: renderer selected=" + renderer.GetType().Name);

                // 4) Render
                try
                {
                    var doc = renderer.Render(content, activeCorrelationId);
                    if (doc == null)
                    {
                        AppendLog("[VERBOSE] RenderResponseToDocument: renderer returned null, fallback to plain text");
                        return CreatePlainDocument(content);
                    }

                    AppendLog("[VERBOSE] RenderResponseToDocument: render OK; outLen=" + (content?.Length ?? 0));
                    return doc;
                }
                catch (System.Exception exRender)
                {
                    AppendLog("[VERBOSE] RenderResponseToDocument: renderer threw -> " + exRender.Message);
                    return CreatePlainDocument(content);
                }
            }
            catch (System.Exception ex)
            {
                try { AgentComposition.Error(activeCorrelationId ?? "-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "[VERBOSE] RenderResponseToDocument: unexpected error -> " + ex.Message, ex); } catch { }
                return CreatePlainDocument(content ?? raw ?? string.Empty);
            }
        }

        private static FlowDocument CreatePlainDocument(string text)
        {
            var fd = new FlowDocument();
            try
            {
                fd.PagePadding = new System.Windows.Thickness(0);
                var p = new Paragraph();
                p.Margin = new System.Windows.Thickness(0);
                p.Inlines.Add(new Run(text ?? string.Empty));
                fd.Blocks.Clear();
                fd.Blocks.Add(p);
            }
            catch
            {
                try
                {
                    fd.Blocks.Clear();
                    fd.Blocks.Add(new Paragraph(new Run(text ?? string.Empty)));
                }
                catch { }
            }

            return fd;
        }
    }
}
