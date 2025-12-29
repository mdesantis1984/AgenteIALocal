using System.Runtime.CompilerServices;

namespace AgenteIALocal.Core.Logging
{
    public static class CallerInfo
    {
        public static LogSource Create(string assembly = null, string ns = null, string type = null, string member = null, string file = null, int? line = null)
        {
            return new LogSource(assembly, ns, type, member, file, line);
        }

        public static LogSource CreateFromCaller(string assembly = null, string ns = null, string type = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new LogSource(assembly, ns, type, member, file, line);
        }
    }
}
