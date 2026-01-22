using System;

namespace AgenteIALocalVSIX.Commands
{
    internal static class VsctConsistencyValidator
    {
        // Constantes declaradas en src/AgenteIALocalVSIX/AgenteIALocalVSIX.vsct
        private static readonly Guid VsctPackageGuid = new Guid("12E93CCA-8723-4160-AC43-96FE08854111");
        private static readonly Guid VsctCommandSetGuid = new Guid("B1A6E1D0-3F4B-4C2B-9E1A-2C7F9D4F6A2B");
        private static readonly int VsctCommandId = 0x0100;

        public static void LogConsistency()
        {
            try
            {
                var codePackageGuid = new Guid(AgenteIALocalVSIX.AgenteIALocalVSIXPackage.PackageGuidString);
                if (VsctPackageGuid != codePackageGuid)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator",
                        "VSCT mismatch: Package GUID VSCT=" + VsctPackageGuid.ToString("B") +
                        " Code=" + codePackageGuid.ToString("B"), null);
                }
                else
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator", "VSCT match: Package GUID=" + codePackageGuid.ToString("B"), null);
                }

                if (VsctCommandSetGuid != OpenAgenteIALocalCommand.CommandSet)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator",
                        "VSCT mismatch: CommandSet GUID VSCT=" + VsctCommandSetGuid.ToString("B") +
                        " Code=" + OpenAgenteIALocalCommand.CommandSet.ToString("B"), null);
                }
                else
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator", "VSCT match: CommandSet GUID=" + OpenAgenteIALocalCommand.CommandSet.ToString("B"), null);
                }

                if (VsctCommandId != OpenAgenteIALocalCommand.CommandId)
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator",
                        "VSCT mismatch: CommandId VSCT=0x" + VsctCommandId.ToString("X") +
                        " Code=0x" + OpenAgenteIALocalCommand.CommandId.ToString("X"), null);
                }
                else
                {
                    AgenteIALocal.Logging.Log.Information("-", 9200, "Command.VsctValidator", "VSCT match: CommandId=0x" + OpenAgenteIALocalCommand.CommandId.ToString("X"), null);
                }
            }
            catch { }
        }
    }
}
