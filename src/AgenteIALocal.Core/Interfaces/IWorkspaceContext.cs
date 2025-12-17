using System.Collections.Generic;

namespace AgenteIALocal.Core.Interfaces
{
    /// <summary>
    /// Root abstraction for workspace access. Read-only contract exposing solution and open documents.
    /// </summary>
    public interface IWorkspaceContext
    {
        /// <summary>
        /// Current solution context; may be null if no solution is open.
        /// </summary>
        ISolutionContext Solution { get; }

        /// <summary>
        /// Read-only list of open documents in the IDE process.
        /// </summary>
        IReadOnlyList<IDocumentContext> OpenDocuments { get; }

        /// <summary>
        /// Currently active document, or null if none.
        /// </summary>
        IDocumentContext ActiveDocument { get; }
    }
}
