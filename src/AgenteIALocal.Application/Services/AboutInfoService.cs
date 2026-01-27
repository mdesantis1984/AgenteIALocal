using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AgenteIALocal.Core.Services;

namespace AgenteIALocal.Application.Services
{
    /// <summary>
    /// About information service - provides product, author, libraries, license, compatibility, and repository data
    /// NUEVO - ID: 20260126_123500
    /// </summary>
    public class AboutInfoService : IAboutInfoService
    {
        private readonly string _licenseText;
        
        public AboutInfoService()
        {
            // TODO: Read LICENSE from embedded resource or file system (Application layer can do this)
            _licenseText = "Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted.\n\n" +
                          "THE SOFTWARE IS PROVIDED \"AS IS\" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. " +
                          "IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, " +
                          "WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.";
        }

        /// <summary>
        /// Get product information
        /// NUEVO - ID: 20260126_123501
        /// </summary>
        public AboutInfo GetProductInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            return new AboutInfo
            {
                ProductName = "Agente IA Local",
                Description = "Extensión VSIX para Visual Studio que integra chat con agentes de IA locales directamente en el IDE. " +
                             "Soporta múltiples proveedores LLM (LM Studio, JAN, llama.cpp, Ollama) con capacidades de agente autónomo.",
                Version = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0",
                BuildDate = GetBuildDate(assembly),
                Author = "Marco Alejandro De Santis",
                Organization = null,
                VsixId = "AgenteIALocal.VSIX"
            };
        }

        /// <summary>
        /// Get detailed author information with photo and social links
        /// NUEVO - ID: 20260126_123502
        /// </summary>
        public AuthorInfo GetAuthorInfo()
        {
            return new AuthorInfo
            {
                Name = "Marco Alejandro De Santis",
                BirthDate = new DateTime(1984, 2, 2),
                // Age is auto-calculated in AuthorInfo.Age property
                PhotoPath = "pack://application:,,,/AgenteIALocalVSIX;component/Resources/Images/author-photo.png",
                LinkedInUrl = "https://www.linkedin.com/in/madesantis/",
                MarketplaceUrl = "https://marketplace.visualstudio.com/items?itemName=madesantis.AgenteIALocalVSIX",
                GitHubUrl = "https://github.com/mdesantis1984/AgenteIALocal/",
                YouTubeUrl = "https://www.youtube.com/@codermix_oficial",
                WebPageUrl = "https://mdesantis.com.ar"
            };
        }

        /// <summary>
        /// Get third-party libraries used
        /// NUEVO - ID: 20260126_123503
        /// </summary>
        public IEnumerable<LibraryInfo> GetLibraries()
        {
            return new[]
            {
                new LibraryInfo 
                { 
                    Name = "Newtonsoft.Json", 
                    Version = "13.0.3", 
                    License = "MIT", 
                    Url = "https://www.newtonsoft.com/json" 
                },
                new LibraryInfo 
                { 
                    Name = "Serilog", 
                    Version = "4.2.0", 
                    License = "Apache 2.0", 
                    Url = "https://serilog.net/" 
                },
                new LibraryInfo 
                { 
                    Name = "MaterialDesignThemes", 
                    Version = "5.1.0", 
                    License = "MIT", 
                    Url = "http://materialdesigninxaml.net/" 
                },
                new LibraryInfo 
                { 
                    Name = "Community.VisualStudio.Toolkit", 
                    Version = "17.0", 
                    License = "Apache 2.0", 
                    Url = "https://github.com/VsixCommunity/Community.VisualStudio.Toolkit" 
                }
            };
        }

        /// <summary>
        /// Get license information
        /// NUEVO - ID: 20260126_123504
        /// </summary>
        public LicenseInfo GetLicense()
        {
            return new LicenseInfo
            {
                Type = "ISC License",
                Text = _licenseText,
                Url = "https://github.com/mdesantis1984/AgenteIALocal/blob/master/LICENSE.md"
            };
        }

        /// <summary>
        /// Get compatibility information
        /// NUEVO - ID: 20260126_123505
        /// </summary>
        public CompatibilityInfo GetCompatibility()
        {
            return new CompatibilityInfo
            {
                VsVersions = new[] { "Visual Studio 2022 (v17.8 o superior)" },
                NetFrameworks = new[] { ".NET Framework 4.7.2", ".NET Framework 4.8" },
                NetStandards = new[] { ".NET Standard 2.0" }
            };
        }

        /// <summary>
        /// Get repository information
        /// NUEVO - ID: 20260126_123506
        /// </summary>
        public RepositoryInfo GetRepository()
        {
            return new RepositoryInfo
            {
                GitHubUrl = "https://github.com/mdesantis1984/AgenteIALocal",
                DocsUrl = "https://github.com/mdesantis1984/AgenteIALocal/tree/master/docs",
                IssuesUrl = "https://github.com/mdesantis1984/AgenteIALocal/issues",
                ChangelogUrl = "https://github.com/mdesantis1984/AgenteIALocal/blob/master/CHANGELOG.md"
            };
        }

        /// <summary>
        /// Get build date from assembly
        /// NUEVO - ID: 20260126_123507
        /// </summary>
        private DateTime? GetBuildDate(Assembly assembly)
        {
            try
            {
                // Read build date from assembly metadata if available
                var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (attribute != null)
                {
                    // Parse if format includes date
                    // For now, return current date as fallback
                }
                
                return DateTime.Now;
            }
            catch
            {
                return null;
            }
        }
    }
}
