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
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command,
                        "VSCT mismatch: Package GUID VSCT=" + VsctPackageGuid.ToString("B") +
                        " Code=" + codePackageGuid.ToString("B"));
                }
                else
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command, "VSCT match: Package GUID=" + codePackageGuid.ToString("B"));
                }

                if (VsctCommandSetGuid != OpenAgenteIALocalCommand.CommandSet)
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command,
                        "VSCT mismatch: CommandSet GUID VSCT=" + VsctCommandSetGuid.ToString("B") +
                        " Code=" + OpenAgenteIALocalCommand.CommandSet.ToString("B"));
                }
                else
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command, "VSCT match: CommandSet GUID=" + OpenAgenteIALocalCommand.CommandSet.ToString("B"));
                }

                if (VsctCommandId != OpenAgenteIALocalCommand.CommandId)
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command,
                        "VSCT mismatch: CommandId VSCT=0x" + VsctCommandId.ToString("X") +
                        " Code=0x" + OpenAgenteIALocalCommand.CommandId.ToString("X"));
                }
                else
                {
                    AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_Command, "VSCT match: CommandId=0x" + OpenAgenteIALocalCommand.CommandId.ToString("X"));
                }
            }
            catch { }
        }
    }
}
