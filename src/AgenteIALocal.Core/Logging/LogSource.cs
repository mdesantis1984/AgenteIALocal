using System;

namespace AgenteIALocal.Core.Logging
{
    public class LogSource
    {
        public LogSource(string assemblyName = null, string @namespace = null, string typeName = null, string memberName = null, string filePath = null, int? lineNumber = null)
        {
            AssemblyName = assemblyName ?? string.Empty;
            Namespace = @namespace ?? string.Empty;
            TypeName = typeName ?? string.Empty;
            MemberName = memberName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            LineNumber = lineNumber;
        }

        public string AssemblyName { get; }
        public string Namespace { get; }
        public string TypeName { get; }
        public string MemberName { get; }
        public string FilePath { get; }
        public int? LineNumber { get; }

        public override string ToString()
        {
            return $"{AssemblyName}:{Namespace}.{TypeName}.{MemberName} ({FilePath}:{LineNumber})";
        }
    }
}
