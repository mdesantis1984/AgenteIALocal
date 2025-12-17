using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace AgenteIALocal.Options
{
    /// <summary>
    /// Persistent options for Agente IA Local extension.
    /// Stored via Visual Studio DialogPage mechanism.
    /// </summary>
    public class AgentOptions : DialogPage
    {
        private bool isEnabled = true;
        private string defaultProvider = "MockAI";
        private string defaultModel = "mock-default";
        private string openAiApiKey = string.Empty;

        [Category("General")]
        [DisplayName("Enable agent")]
        [Description("Enable or disable the agent features in the experimental instance.")]
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        [Category("General")]
        [DisplayName("Default provider")]
        [Description("Default AI provider to use (e.g., MockAI, OpenAI).")]
        public string DefaultProvider
        {
            get => defaultProvider;
            set => defaultProvider = value ?? string.Empty;
        }

        [Category("General")]
        [DisplayName("Default model")]
        [Description("Default model identifier to use for the selected provider.")]
        public string DefaultModel
        {
            get => defaultModel;
            set => defaultModel = value ?? string.Empty;
        }

        [Category("Credentials")]
        [DisplayName("OpenAI API key")]
        [Description("API key for OpenAI. Prefer environment variables for CI/security; this field stores a local key if provided.")]
        public string OpenAIApiKey
        {
            get => openAiApiKey;
            set => openAiApiKey = value ?? string.Empty;
        }
    }
}
