namespace AgenteIALocal.Core.Services
{
    /// <summary>
    /// Compatibility information DTO
    /// NUEVO - ID: 20260126_122105
    /// </summary>
    public class CompatibilityInfo
    {
        /// <summary>Supported Visual Studio versions (e.g., ["Visual Studio 2022 (v17.8+)"])</summary>
        public string[] VsVersions { get; set; }

        /// <summary>Required .NET Framework versions (e.g., [".NET Framework 4.7.2"])</summary>
        public string[] NetFrameworks { get; set; }

        /// <summary>Supported .NET Standard versions (e.g., [".NET Standard 2.0"])</summary>
        public string[] NetStandards { get; set; }
    }
}
