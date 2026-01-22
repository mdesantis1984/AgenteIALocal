using System;

namespace AgenteIALocal.Logging
{
    public sealed class LogSettings
    {
        public string AppName { get; set; } = "AgenteIALocal";

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// If true, all levels are enabled (except when Enabled=false).
        /// </summary>
        public bool All { get; set; } = false;

        public bool Verbose { get; set; } = false;
        public bool Debug { get; set; } = false;
        public bool Information { get; set; } = true;
        public bool Warning { get; set; } = true;
        public bool Error { get; set; } = true;
        public bool Critical { get; set; } = true;

        /// <summary>
        /// Rolling file size limit in bytes.
        /// </summary>
        public long RollingFileSizeBytes { get; set; } = 3L * 1024 * 1024;

        /// <summary>
        /// Retained files count.
        /// </summary>
        public int RetainedFileCount { get; set; } = 10;

        /// <summary>
        /// Optional override for log directory (defaults to %LOCALAPPDATA%\AgenteIALocal\logs)
        /// </summary>
        public string LogDirectory { get; set; }

        internal string ResolveLogDirectory()
        {
            if (!string.IsNullOrWhiteSpace(LogDirectory)) return LogDirectory;

            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? ".";
                return System.IO.Path.Combine(local, AppName ?? "AgenteIALocal", "logs");
            }
            catch
            {
                return System.IO.Path.Combine(".", AppName ?? "AgenteIALocal", "logs");
            }
        }
    }
}
