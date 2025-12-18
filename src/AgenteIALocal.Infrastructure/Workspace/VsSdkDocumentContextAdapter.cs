using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces;

namespace AgenteIALocal.Infrastructure.Workspace
{
    internal class VsSdkDocumentContextAdapter
    {
        private readonly object dte; // EnvDTE.DTE at runtime

        public VsSdkDocumentContextAdapter(object dte)
        {
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
        }

        public IReadOnlyList<IDocumentContext> GetOpenDocuments()
        {
            try
            {
                var documentsProp = dte.GetType().GetProperty("Documents");
                var docs = documentsProp?.GetValue(dte, null);
                if (docs == null) return Array.Empty<IDocumentContext>();

                var list = new List<IDocumentContext>();
                var enumerator = docs.GetType().GetMethod("GetEnumerator")?.Invoke(docs, null) as System.Collections.IEnumerator;
                if (enumerator != null)
                {
                    while (enumerator.MoveNext())
                    {
                        var doc = enumerator.Current;
                        var fullNameProp = doc.GetType().GetProperty("FullName");
                        var fullName = fullNameProp?.GetValue(doc) as string;
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            list.Add(new VisualStudioDocumentContext(fullName));
                        }
                    }
                }

                return list;
            }
            catch
            {
                return Array.Empty<IDocumentContext>();
            }
        }

        public IDocumentContext GetActiveDocument()
        {
            try
            {
                var activeDocProp = dte.GetType().GetProperty("ActiveDocument");
                var activeDoc = activeDocProp?.GetValue(dte);
                if (activeDoc == null) return null;

                var fullNameProp = activeDoc.GetType().GetProperty("FullName");
                var fullName = fullNameProp?.GetValue(activeDoc) as string;
                if (string.IsNullOrEmpty(fullName)) return null;

                return new VisualStudioDocumentContext(fullName);
            }
            catch
            {
                return null;
            }
        }
    }
}
