namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// License information DTO
    /// NUEVO - ID: 20260126_122104
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>License type (e.g., "ISC License")</summary>
        public string Type { get; set; }

        /// <summary>Full license text</summary>
        public string Text { get; set; }

        /// <summary>URL to LICENSE file in repository</summary>
        public string Url { get; set; }
    }
}
