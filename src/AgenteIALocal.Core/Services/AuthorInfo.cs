using System;

namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Author information DTO with social links
    /// NUEVO - ID: 20260126_123000
    /// </summary>
    public class AuthorInfo
    {
        /// <summary>Full name</summary>
        public string Name { get; set; }

        /// <summary>Birth date (02/02/1984)</summary>
        public DateTime BirthDate { get; set; }

        /// <summary>Calculated age (auto-computed from BirthDate)</summary>
        public int Age => DateTime.Now.Year - BirthDate.Year - 
                          (DateTime.Now.DayOfYear < BirthDate.DayOfYear ? 1 : 0);

        /// <summary>Photo path (circular display)</summary>
        public string PhotoPath { get; set; }

        /// <summary>LinkedIn profile URL</summary>
        public string LinkedInUrl { get; set; }

        /// <summary>Visual Studio Marketplace extension URL</summary>
        public string MarketplaceUrl { get; set; }

        /// <summary>GitHub repository URL</summary>
        public string GitHubUrl { get; set; }

        /// <summary>YouTube channel URL</summary>
        public string YouTubeUrl { get; set; }

        /// <summary>Personal website URL</summary>
        public string WebPageUrl { get; set; }
    }
}
