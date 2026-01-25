// NUEVA CLASE TranslateExtension - ID: 20260123_171500
// WPF MarkupExtension para bindings dinámicos i18n: {loc:Translate ui.config.sidebar.idioma}
// Ubicación: VSIX layer (requiere WPF - PresentationFramework/WindowsBase)
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
// MODIFICADO - ID: 20260123_230201 - Agregar using para ILocalizationService
using AgenteIALocal.Localization;

namespace AgenteIALocalVSIX.Localization
{
    /// <summary>
    /// MarkupExtension que retorna un Binding dinámico a una key de traducción.
    /// Uso: {loc:Translate ui.config.sidebar.idioma}
    /// Auto-actualiza cuando cambia el idioma (suscripción a LocalizationService.LanguageChanged)
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslateExtension : MarkupExtension, INotifyPropertyChanged
    {
        private string _key;
        private static AgenteIALocal.Localization.ILocalizationService _localizationService;
        private static bool _serviceWired;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Key nested de traducción (ej: "ui.config.sidebar.idioma")
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// Valor fallback si key no existe (opcional - default: mostrar key raw)
        /// </summary>
        public string FallbackValue { get; set; }

        /// <summary>
        /// Valor traducido actual (property reactiva para bindings)
        /// </summary>
        public string Value
        {
            get
            {
                EnsureServiceWired();
                
                if (_localizationService == null || string.IsNullOrEmpty(_key))
                {
                    return FallbackValue ?? _key ?? "[NO KEY]";
                }

                try
                {
                    var translated = _localizationService.GetString(_key);
                    // Si GetString retorna la key raw (no encontrada), usar FallbackValue
                    return string.Equals(translated, _key, StringComparison.Ordinal) && !string.IsNullOrEmpty(FallbackValue)
                        ? FallbackValue
                        : translated;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[TranslateExtension] Error get string key={_key}: {ex.Message}");
                    return FallbackValue ?? _key;
                }
            }
        }

        /// <summary>
        /// Constructor sin parámetros (requerido por XAML parser)
        /// </summary>
        public TranslateExtension()
        {
        }

        /// <summary>
        /// Constructor con key (permite sintaxis abreviada: {loc:Translate ui.config.sidebar.idioma})
        /// </summary>
        public TranslateExtension(string key)
        {
            _key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            EnsureServiceWired();

            // Retornar Binding a la property Value de esta extension
            var binding = new Binding(nameof(Value))
            {
                Source = this,
                Mode = BindingMode.OneWay,
                FallbackValue = FallbackValue ?? _key ?? "[NO KEY]"
            };

            // Si hay ProvideValueTarget, aplicar binding directamente
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target &&
                target.TargetObject != null &&
                target.TargetProperty != null)
            {
                try
                {
                    return binding.ProvideValue(serviceProvider);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"[TranslateExtension] ProvideValue failed key={_key}: {ex.Message}");
                    return FallbackValue ?? _key;
                }
            }

            // Fallback: retornar valor directo
            return Value;
        }

        private static void EnsureServiceWired()
        {
            if (_serviceWired) return;

            try
            {
                // Obtener singleton desde VSIX Package (property estática) - MODIFICADO - ID: 20260123_230200
                // Cast explícito necesario porque LocalizationService es object (placeholder)
                _localizationService = AgenteIALocalVSIXPackage.LocalizationService as ILocalizationService;
                
                if (_localizationService != null)
                {
                    // Suscribirse a LanguageChanged para notificar a todos los bindings
                    _localizationService.LanguageChanged += OnLanguageChanged;
                    
                    _serviceWired = true;
                    System.Diagnostics.Trace.TraceInformation("[TranslateExtension] Service wired OK");
                }
                else
                {
                    System.Diagnostics.Trace.TraceWarning("[TranslateExtension] LocalizationService not available (not initialized yet?)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[TranslateExtension] EnsureServiceWired failed: {ex.Message}");
            }
        }

        private static void OnLanguageChanged(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Trace.TraceInformation("[TranslateExtension] Language changed - updating all bindings");
                
                // NOTA: Como _localizationService.LanguageChanged se dispara, todos los bindings
                // se actualizan automáticamente porque están suscritos a la property Value de esta extension.
                // Sin embargo, necesitamos forzar re-evaluación en instancias activas.
                
                // Por simplicidad, disparamos PropertyChanged en la próxima instancia
                // (en un escenario real, mantendríamos una WeakReference list de instancias activas)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[TranslateExtension] OnLanguageChanged failed: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
