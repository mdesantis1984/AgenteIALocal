// REESCRITO TranslateExtension - ID: 20260126_022000
// FIX CRÍTICO: ProvideValue debe retornar Binding, NO string estático
// Razón: WPF NO re-evalúa MarkupExtensions - necesita Binding dinámico
using System;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace AgenteIALocal.Localization
{
    // NUEVA ARQUITECTURA - Retorna Binding a LocalizationProvider singleton
    // Cuando idioma cambia, LocalizationProvider notifica PropertyChanged → Binding actualiza UI
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslateExtension : MarkupExtension
    {
        public string Key { get; set; }
        public string FallbackValue { get; set; }

        public TranslateExtension() { }
        
        public TranslateExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Crear Binding al LocalizationProvider
            // Path: Item[{Key}] usa el indexer de LocalizationProvider
            var binding = new Binding
            {
                Source = LocalizationProvider.Instance,
                Path = new System.Windows.PropertyPath($"[{Key}]"),
                Mode = BindingMode.OneWay,
                Converter = new TranslateConverter { FallbackValue = FallbackValue }
            };

            // Retornar Binding (NO string) para que WPF actualice automáticamente
            return binding.ProvideValue(serviceProvider);
        }
    }

    // Converter para fallback si traducción falla
    internal class TranslateConverter : IValueConverter
    {
        public string FallbackValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return FallbackValue ?? parameter?.ToString() ?? string.Empty;
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

