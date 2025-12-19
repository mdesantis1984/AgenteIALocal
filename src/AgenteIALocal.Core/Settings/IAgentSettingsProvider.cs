namespace AgenteIALocal.Core.Settings
{
    public interface IAgentSettingsProvider
    {
        AgentSettings Load();
        void Save(AgentSettings settings);
    }
}
