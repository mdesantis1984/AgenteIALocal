using System.ComponentModel;

namespace AgenteIALocal.ViewModels
{
    public class AgenteIALocalToolWindowViewModel : INotifyPropertyChanged
    {
        private string statusText = "Agente IA Local – Ready";

        public string StatusText
        {
            get => statusText;
            set
            {
                if (statusText != value)
                {
                    statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
