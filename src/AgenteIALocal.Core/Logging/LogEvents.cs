using System;

namespace AgenteIALocal.Core.Logging
{
    public static class LogEvents
    {
        public static readonly LogEventId Vsix_Startup = new LogEventId(9000, "VSIX.Startup");
        public static readonly LogEventId Vsix_Composition = new LogEventId(9001, "VSIX.Composition");
        public static readonly LogEventId Vsix_UI = new LogEventId(9100, "VSIX.UI");
        public static readonly LogEventId Vsix_Command = new LogEventId(9200, "VSIX.Command");
        public static readonly LogEventId Infra_Http = new LogEventId(2000, "Infra.Http");
        public static readonly LogEventId Agent_Execute = new LogEventId(3000, "Agent.Execute");
    }
}
