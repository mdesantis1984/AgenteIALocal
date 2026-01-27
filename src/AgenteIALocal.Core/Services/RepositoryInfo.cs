namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Repository information DTO
    /// NUEVO - ID: 20260126_122106
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>GitHub repository URL</summary>
        public string GitHubUrl { get; set; }

        /// <summary>Documentation URL</summary>
        public string DocsUrl { get; set; }

        /// <summary>Issues/bug report URL</summary>
        public string IssuesUrl { get; set; }

        /// <summary>Changelog URL</summary>
        public string ChangelogUrl { get; set; }
    }
}
