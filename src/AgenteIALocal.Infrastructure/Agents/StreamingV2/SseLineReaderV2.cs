using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgenteIALocal.Infrastructure.Agents.StreamingV2
{
    internal static class SseLineReaderV2
    {
        internal static async Task<string> ReadNextDataLineAsync(StreamReader reader, CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    return null;
                }

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("data:"))
                {
                    return line.Substring(5).Trim();
                }
            }
        }
    }
}
