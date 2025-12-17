namespace AgenteIALocal.Core.Interfaces
{
    /// <summary>
    /// Read-only access to open document metadata in the IDE.
    /// </summary>
    public interface IDocumentContext
    {
        /// <summary>
        /// Full path to the document on disk, if applicable.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Document file name.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Language or content type (e.g., C#, VB, JSON, plaintext).
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Indicates whether the document has unsaved changes.
        /// </summary>
        bool IsDirty { get; }
    }
}
