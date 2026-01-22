using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AgenteIALocal.Core.Settings;
// MODIFICADO - ID: 20260122_030200 - Cambiado de AgenteIALocal.Core.Logging a AgenteIALocal.Logging (Serilog)

namespace AgenteIALocalVSIX.Options
{
    public sealed class AgentOptionsViewModel : INotifyPropertyChanged
    {
        private AgentProviderType provider;
        private LmStudioSettings lmStudio;
        private JanServerSettings janServer;

        public event PropertyChangedEventHandler PropertyChanged;

        public AgentProviderType Provider
        {
            get => provider;
            set
            {
                if (provider == value) return;
                var old = provider;
                provider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLmStudio));
                OnPropertyChanged(nameof(IsJanServer));

                try
                {
                    AgenteIALocal.Logging.Log.Information("-", 9100, "Options.ViewModel", "Options: Provider changed " + old + " -> " + provider, null);
                }
                catch { }
            }
        }

        public LmStudioSettings LmStudio
        {
            get => lmStudio;
            set
            {
                if (ReferenceEquals(lmStudio, value)) return;
                lmStudio = value;
                OnPropertyChanged();
            }
        }

        public JanServerSettings JanServer
        {
            get => janServer;
            set
            {
                if (ReferenceEquals(janServer, value)) return;
                janServer = value;
                OnPropertyChanged();
            }
        }

        public bool IsLmStudio => Provider == AgentProviderType.LmStudio;

        public bool IsJanServer => Provider == AgentProviderType.JanServer;

        public AgentOptionsViewModel()
        {
            provider = AgentProviderType.LmStudio;
            lmStudio = new LmStudioSettings();
            janServer = new JanServerSettings();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
