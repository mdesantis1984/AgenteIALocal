using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocalVSIX.ToolWindows
{
    public sealed class AgenteIALocalConfigViewModel : INotifyPropertyChanged
    {
        private readonly IAgentSettingsProvider settingsProvider;
        private ProviderConfigViewModel lmStudio;
        private ProviderConfigViewModel janServer;

        public event PropertyChangedEventHandler PropertyChanged;

        public ProviderConfigViewModel LmStudio
        {
            get => lmStudio;
            set
            {
                if (ReferenceEquals(lmStudio, value)) return;
                lmStudio = value;
                OnPropertyChanged();
            }
        }

        public ProviderConfigViewModel JanServer
        {
            get => janServer;
            set
            {
                if (ReferenceEquals(janServer, value)) return;
                janServer = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }

        public AgenteIALocalConfigViewModel(IAgentSettingsProvider settingsProvider)
        {
            this.settingsProvider = settingsProvider;
            lmStudio = new ProviderConfigViewModel();
            janServer = new ProviderConfigViewModel();
            SaveCommand = new RelayCommand(Save);
        }

        public void Load()
        {
            var settings = settingsProvider?.Load() ?? new AgentSettings();

            var lm = settings.LmStudio ?? new LmStudioSettings();
            LmStudio.BaseUrl = lm.BaseUrl ?? string.Empty;
            LmStudio.ApiKey = lm.ApiKey ?? string.Empty;
            LmStudio.ChatCompletionsPath = lm.ChatCompletionsPath ?? "/v1/chat/completions";
            LmStudio.Model = lm.Model ?? string.Empty;

            var jan = settings.JanServer ?? new JanServerSettings();
            JanServer.BaseUrl = jan.BaseUrl ?? string.Empty;
            JanServer.ApiKey = jan.ApiKey ?? string.Empty;
            JanServer.ChatCompletionsPath = jan.ChatCompletionsPath ?? "/v1/chat/completions";
        }

        public void Save()
        {
            if (settingsProvider == null) return;

            var settings = settingsProvider.Load() ?? new AgentSettings();

            settings.LmStudio = new LmStudioSettings
            {
                BaseUrl = LmStudio.BaseUrl ?? string.Empty,
                ApiKey = LmStudio.ApiKey ?? string.Empty,
                ChatCompletionsPath = LmStudio.ChatCompletionsPath ?? "/v1/chat/completions",
                Model = LmStudio.Model ?? string.Empty
            };

            settings.JanServer = new JanServerSettings
            {
                BaseUrl = JanServer.BaseUrl ?? string.Empty,
                ApiKey = JanServer.ApiKey ?? string.Empty,
                ChatCompletionsPath = JanServer.ChatCompletionsPath ?? "/v1/chat/completions"
            };

            settingsProvider.Save(settings);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public sealed class ProviderConfigViewModel : INotifyPropertyChanged
        {
            private bool enabled;
            private string baseUrl;
            private string apiKey;
            private string modelsPath;
            private string chatCompletionsPath;
            private string completionsPath;
            private string embeddingsPath;
            private string user;
            private string password;
            private string model;
            private string modelInfo;

            public event PropertyChangedEventHandler PropertyChanged;

            public bool Enabled
            {
                get => enabled;
                set { if (enabled == value) return; enabled = value; OnPropertyChanged(); }
            }

            public string BaseUrl
            {
                get => baseUrl;
                set { if (baseUrl == value) return; baseUrl = value; OnPropertyChanged(); }
            }

            public string ApiKey
            {
                get => apiKey;
                set { if (apiKey == value) return; apiKey = value; OnPropertyChanged(); }
            }

            public string ModelsPath
            {
                get => modelsPath;
                set { if (modelsPath == value) return; modelsPath = value; OnPropertyChanged(); }
            }

            public string ChatCompletionsPath
            {
                get => chatCompletionsPath;
                set { if (chatCompletionsPath == value) return; chatCompletionsPath = value; OnPropertyChanged(); }
            }

            public string CompletionsPath
            {
                get => completionsPath;
                set { if (completionsPath == value) return; completionsPath = value; OnPropertyChanged(); }
            }

            public string EmbeddingsPath
            {
                get => embeddingsPath;
                set { if (embeddingsPath == value) return; embeddingsPath = value; OnPropertyChanged(); }
            }

            public string User
            {
                get => user;
                set { if (user == value) return; user = value; OnPropertyChanged(); }
            }

            public string Password
            {
                get => password;
                set { if (password == value) return; password = value; OnPropertyChanged(); }
            }

            public string Model
            {
                get => model;
                set { if (model == value) return; model = value; OnPropertyChanged(); }
            }

            public string ModelInfo
            {
                get => modelInfo;
                set { if (modelInfo == value) return; modelInfo = value; OnPropertyChanged(); }
            }

            public ProviderConfigViewModel()
            {
                enabled = true;
                baseUrl = string.Empty;
                apiKey = string.Empty;
                modelsPath = "/v1/models";
                chatCompletionsPath = "/v1/chat/completions";
                completionsPath = "/v1/completions";
                embeddingsPath = "/v1/embeddings";
                user = string.Empty;
                password = string.Empty;
                model = string.Empty;
                modelInfo = string.Empty;
            }

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
