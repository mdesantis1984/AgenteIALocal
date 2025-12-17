using System;
using System.Collections.Generic;
using System.IO;
using AgenteIALocal.Core.Interfaces;

namespace AgenteIALocal.Infrastructure.Workspace
{
    /// <summary>
    /// Read-only implementation of IDocumentContext used by infrastructure layer.
    /// This class intentionally does not depend on Visual Studio SDK. It represents
    /// metadata about a single document. A lightweight provider is included that
    /// returns open documents or the active document when detectable; otherwise it
    /// safely returns an empty list or null.
    /// </summary>
    public class VisualStudioDocumentContext : IDocumentContext
    {
        public VisualStudioDocumentContext(string path, bool isDirty = false)
        {
            Path = string.IsNullOrEmpty(path) ? null : System.IO.Path.GetFullPath(path);
            FileName = string.IsNullOrEmpty(path) ? null : System.IO.Path.GetFileName(path);
            Language = InferLanguageFromExtension(Path);
            IsDirty = isDirty;
        }

        public string Path { get; }

        public string FileName { get; }

        public string Language { get; }

        public bool IsDirty { get; }

        private static string InferLanguageFromExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            switch (ext)
            {
                case ".cs": return "C#";
                case ".vb": return "VB";
                case ".fs": return "F#";
                case ".xaml": return "XAML";
                case ".json": return "JSON";
                case ".xml": return "XML";
                case ".txt": return "Text";
                case ".md": return "Markdown";
                default: return null;
            }
        }

        public static IReadOnlyList<IDocumentContext> GetOpenDocumentsBestEffort(object dte = null)
        {
            try
            {
                if (dte != null && VsSdkAvailability.IsVsSdkPresent())
                {
                    var adapterType = Type.GetType("AgenteIALocal.Infrastructure.Workspace.VsSdkDocumentContextAdapter, AgenteIALocal.Infrastructure");
                    if (adapterType != null)
                    {
                        var adapter = Activator.CreateInstance(adapterType, new[] { dte });
                        var method = adapterType.GetMethod("GetOpenDocuments");
                        var result = method?.Invoke(adapter, null) as IReadOnlyList<IDocumentContext>;
                        if (result != null) return result;
                    }
                }
            }
            catch
            {
                // Fall back to empty
            }

            return VisualStudioDocumentContextProvider.GetOpenDocuments();
        }

        public static IDocumentContext GetActiveDocumentBestEffort(object dte = null)
        {
            try
            {
                if (dte != null && VsSdkAvailability.IsVsSdkPresent())
                {
                    var adapterType = Type.GetType("AgenteIALocal.Infrastructure.Workspace.VsSdkDocumentContextAdapter, AgenteIALocal.Infrastructure");
                    if (adapterType != null)
                    {
                        var adapter = Activator.CreateInstance(adapterType, new[] { dte });
                        var method = adapterType.GetMethod("GetActiveDocument");
                        var result = method?.Invoke(adapter, null) as IDocumentContext;
                        if (result != null) return result;
                    }
                }
            }
            catch
            {
                // fall back
            }

            return VisualStudioDocumentContextProvider.GetActiveDocument();
        }
    }

    /// <summary>
    /// Provider for document contexts. This lightweight implementation does not use
    /// the Visual Studio SDK; it exposes methods that return an empty set by default
    /// and can be extended later to use editor APIs when available.
    /// </summary>
    public static class VisualStudioDocumentContextProvider
    {
        /// <summary>
        /// Attempts to detect currently open documents. This fallback implementation
        /// does not have access to the editor and returns an empty list to satisfy
        /// read-only contract expectations. Implementations that run inside Visual
        /// Studio can provide an adapter to return real data.
        /// </summary>
        public static IReadOnlyList<IDocumentContext> GetOpenDocuments()
        {
            // Fallback: no reliable way to enumerate editor buffers without the VS SDK.
            // Return an empty list to indicate 'no determinable open documents'.
            return Array.Empty<IDocumentContext>();
        }

        /// <summary>
        /// Attempts to detect the active document. Returns null when not available.
        /// </summary>
        public static IDocumentContext GetActiveDocument()
        {
            // Fallback: no reliable way to detect active document without the VS SDK.
            return null;
        }
    }
}
