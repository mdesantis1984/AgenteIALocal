namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Service for retrieving About window information
    /// Provides product info, libraries, license, compatibility, and repository data
    /// NUEVO - ID: 20260126_122100
    /// MODIFICADO - ID: 20260126_123001 - Added GetAuthorInfo()
    /// </summary>
    public interface IAboutInfoService
    {
        /// <summary>
        /// Get product information (name, version, author, etc.)
        /// </summary>
        AboutInfo GetProductInfo();

        /// <summary>
        /// Get author detailed information (photo, social links, age)
        /// NUEVO - ID: 20260126_123001
        /// </summary>
        AuthorInfo GetAuthorInfo();

        /// <summary>
        /// Get list of third-party libraries used
        /// </summary>
        System.Collections.Generic.IEnumerable<LibraryInfo> GetLibraries();

        /// <summary>
        /// Get license information (type, text, url)
        /// </summary>
        LicenseInfo GetLicense();

        /// <summary>
        /// Get compatibility information (VS versions, frameworks, etc.)
        /// </summary>
        CompatibilityInfo GetCompatibility();

        /// <summary>
        /// Get repository information (GitHub, docs, issues URLs)
        /// </summary>
        RepositoryInfo GetRepository();
    }
}

