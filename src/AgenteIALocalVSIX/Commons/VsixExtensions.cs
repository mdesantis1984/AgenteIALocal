using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AgenteIALocalVSIX.Commons
{
    public static class VsixExtensions
    {
        // Uso: typeof(MiPackage).GetVsixVersionString()
        public static string GetVsixVersionString(this Type typeFromYourVsix, string fallback = "desconocida")
        {
            if (typeFromYourVsix == null) return fallback;
            return GetVsixVersionString(typeFromYourVsix.Assembly, fallback);
        }

        // Uso: typeof(MiPackage).Assembly.GetVsixVersionString()
        public static string GetVsixVersionString(this Assembly assembly, string fallback = "desconocida")
        {
            if (assembly == null) return fallback;

            // 1) Intentar leer la versión desde el manifest del VSIX instalado
            var v = TryReadVsixManifestVersion(assembly);
            if (!string.IsNullOrWhiteSpace(v)) return v;

            // 2) Fallback: versiones del assembly (si las sincronizas con el VSIX, coincidirán)
            v = TryReadAssemblyVersion(assembly);
            if (!string.IsNullOrWhiteSpace(v)) return v;

            return fallback;
        }

        private static string TryReadVsixManifestVersion(Assembly assembly)
        {
            var asmPath = assembly.Location;
            if (string.IsNullOrWhiteSpace(asmPath)) return null;

            var dir = Path.GetDirectoryName(asmPath);
            if (string.IsNullOrWhiteSpace(dir)) return null;

            // En instalado suele ser "extension.vsixmanifest"
            // En algunos escenarios de dev puede existir "source.extension.vsixmanifest"
            var candidates = new[]
            {
                Path.Combine(dir, "extension.vsixmanifest"),
                Path.Combine(dir, "source.extension.vsixmanifest")
            };

            var manifestPath = candidates.FirstOrDefault(File.Exists);
            if (manifestPath == null) return null;

            try
            {
                var doc = XDocument.Load(manifestPath);

                // No dependemos de namespaces: buscamos por LocalName
                var identity = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Identity");
                if (identity == null) return null;

                var versionAttr = identity.Attributes().FirstOrDefault(a => a.Name.LocalName == "Version");
                return versionAttr == null ? null : versionAttr.Value;
            }
            catch
            {
                return null;
            }
        }

        private static string TryReadAssemblyVersion(Assembly assembly)
        {
            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion))
                return info.InformationalVersion;

            var file = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (file != null && !string.IsNullOrWhiteSpace(file.Version))
                return file.Version;

            var v = assembly.GetName().Version;
            return v == null ? null : v.ToString();
        }
    }
}
