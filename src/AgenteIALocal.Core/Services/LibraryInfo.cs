namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Third-party library information DTO
    /// NUEVO - ID: 20260126_122103
    /// </summary>
    public class LibraryInfo
    {
        /// <summary>Library name (e.g., "Newtonsoft.Json")</summary>
        public string Name { get; set; }

        /// <summary>Version (e.g., "13.0.3")</summary>
        public string Version { get; set; }

        /// <summary>License type (e.g., "MIT", "Apache 2.0")</summary>
        public string License { get; set; }

        /// <summary>Library website URL</summary>
        public string Url { get; set; }
    }
}
