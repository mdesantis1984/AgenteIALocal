using System;
using System.IO;
using System.Text;
using AgenteIALocal.Core.Logging;

namespace AgenteIALocal.Infrastructure.LoggingV2
{
    // NUEVO COMPONENTE RollingFileWriter - ID: 20260118_190300
    internal sealed class RollingFileWriter : IDisposable
    {
        private readonly object gate = new object();
        private readonly string basePath;
        private readonly long maxBytes;
        private StreamWriter writer;

        public RollingFileWriter(string path, long maxBytes)
        {
            this.basePath = path ?? throw new ArgumentNullException(nameof(path));
            this.maxBytes = maxBytes > 0 ? maxBytes : 3145728;
            OpenWriter();
        }

        private void OpenWriter()
        {
            try
            {
                var dir = Path.GetDirectoryName(basePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var fs = new FileStream(basePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
            }
            catch
            {
                writer = null;
            }
        }

        public void WriteLine(string line)
        {
            if (line == null) return;
            lock (gate)
            {
                try
                {
                    if (writer == null) OpenWriter();
                    if (writer == null) return;

                    writer.WriteLine(line);
                    writer.Flush();

                    try
                    {
                        var fi = new FileInfo(basePath);
                        if (fi.Exists && fi.Length >= maxBytes)
                        {
                            Rotate(fi);
                        }
                    }
                    catch { }
                }
                catch { }
            }
        }

        private void Rotate(FileInfo fi)
        {
            try
            {
                try { writer.Flush(); } catch { }
                try { writer.Close(); } catch { }
                writer = null;

                var dir = fi.DirectoryName ?? string.Empty;
                var nameNoExt = Path.GetFileNameWithoutExtension(fi.Name);
                var ext = fi.Extension;
                var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                var rolled = Path.Combine(dir, $"{nameNoExt}-{stamp}{ext}");
                try
                {
                    if (File.Exists(rolled)) rolled = Path.Combine(dir, $"{nameNoExt}-{stamp}-{Guid.NewGuid():N}{ext}");
                }
                catch { }

                try { File.Move(basePath, rolled); } catch { }

                // open new writer
                OpenWriter();
            }
            catch { }
        }

        public void Dispose()
        {
            lock (gate)
            {
                try { writer?.Flush(); } catch { }
                try { writer?.Close(); } catch { }
                writer = null;
            }
        }
    }
}
