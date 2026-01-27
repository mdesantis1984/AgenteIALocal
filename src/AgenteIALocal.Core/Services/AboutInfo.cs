using System;

namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Product information DTO
    /// NUEVO - ID: 20260126_122102
    /// </summary>
    public class AboutInfo
    {
        /// <summary>Product name (e.g., "Agente IA Local")</summary>
        public string ProductName { get; set; }

        /// <summary>Product description (short)</summary>
        public string Description { get; set; }

        /// <summary>Version (e.g., "2.7-about.1")</summary>
        public string Version { get; set; }

        /// <summary>Build date (optional)</summary>
        public DateTime? BuildDate { get; set; }

        /// <summary>Author name (e.g., "Marco Alejandro De Santis")</summary>
        public string Author { get; set; }

        /// <summary>Organization name (optional)</summary>
        public string Organization { get; set; }

        /// <summary>VSIX package ID</summary>
        public string VsixId { get; set; }
    }
}
