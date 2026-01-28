// MODIFICADO - ID: 20260128_020300 - Delegar a VsixVersionHelper compartido (evitar duplicación)
using System;
using System.Reflection;
using AgenteIALocal.Application.Utilities;

namespace AgenteIALocalVSIX.Commons
{
    /// <summary>
    /// VSIX Extension methods
    /// MODIFICADO - ID: 20260128_020300 - Ahora delega a VsixVersionHelper compartido
    /// </summary>
    public static class VsixExtensions
    {
        /// <summary>
        /// Get VSIX version string from Type
        /// MODIFICADO - ID: 20260128_020300 - Delegar a helper compartido
        /// </summary>
        public static string GetVsixVersionString(this Type typeFromYourVsix, string fallback = "desconocida")
        {
            return VsixVersionHelper.GetVsixVersionString(typeFromYourVsix, fallback);
        }

        /// <summary>
        /// Get VSIX version string from Assembly
        /// MODIFICADO - ID: 20260128_020300 - Delegar a helper compartido
        /// </summary>
        public static string GetVsixVersionString(this Assembly assembly, string fallback = "desconocida")
        {
            return VsixVersionHelper.GetVsixVersionString(assembly, fallback);
        }
    }
}

