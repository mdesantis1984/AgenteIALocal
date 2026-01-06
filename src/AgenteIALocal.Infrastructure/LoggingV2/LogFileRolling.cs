using System;
using System.IO;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    internal static class LogFileRolling
    {
        internal const long MaxBytesDefault = 5L * 1024 * 1024; // 5 MB

        internal static void EnsureRolled(string path, long maxBytes = MaxBytesDefault)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (maxBytes <= 0) return;

            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists) return;
                if (fi.Length < maxBytes) return;

                var dir = fi.DirectoryName ?? string.Empty;
                var nameNoExt = Path.GetFileNameWithoutExtension(fi.Name);
                var ext = fi.Extension;
                var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

                var rolled = Path.Combine(dir, $"{nameNoExt}-{stamp}{ext}");
                try
                {
                    if (File.Exists(rolled))
                    {
                        rolled = Path.Combine(dir, $"{nameNoExt}-{stamp}-{Guid.NewGuid():N}{ext}");
                    }
                }
                catch { }

                try
                {
                    File.Move(path, rolled);
                }
                catch
                {
                    // If rename fails, do not block logging
                    return;
                }

                try
                {
                    // Create a new empty file so readers/watchers can open it.
                    File.WriteAllText(path, string.Empty);
                }
                catch { }
            }
            catch
            {
                // swallow
            }
        }
    }
}
