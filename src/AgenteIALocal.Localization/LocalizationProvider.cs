// REESCRITO LocalizationProvider - ID: 20260126_022100
// NUEVA ARQUITECTURA: Clase instanciable con INotifyPropertyChanged + indexer
// Binding en TranslateExtension se suscribe automáticamente a cambios
using System;
using System.ComponentModel;

namespace AgenteIALocal.Localization
{
    // Singleton wrapper con PropertyChanged para Binding WPF
    public class LocalizationProvider : INotifyPropertyChanged
    {
        private static LocalizationProvider _instance;
        private ILocalizationService _service;

        public static LocalizationProvider Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LocalizationProvider();
                return _instance;
            }
        }

        private LocalizationProvider()
        {
        }

        // Inicializar con servicio real
        public void Initialize(ILocalizationService service)
        {
            if (_service != null)
            {
                // Desuscribir del servicio anterior
                _service.LanguageChanged -= OnLanguageChanged;
            }

            _service = service;

            if (_service != null)
            {
                // Suscribir al nuevo servicio
                _service.LanguageChanged += OnLanguageChanged;
            }

            // Notificar cambio inicial
            OnPropertyChanged("Item[]");
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Notificar a TODOS los bindings que TODAS las traducciones cambiaron
            // "Item[]" es convención WPF para indexers (fuerza re-evaluación de TODOS)
            OnPropertyChanged("Item[]");
        }

        // Indexer para Binding: {Binding Source=LocalizationProvider, Path=[ui.config.window.title]}
        public string this[string key]
        {
            get
            {
                try
                {
                    if (_service == null)
                        return key;

                    return _service.GetString(key) ?? key;
                }
                catch
                {
                    return key;
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

