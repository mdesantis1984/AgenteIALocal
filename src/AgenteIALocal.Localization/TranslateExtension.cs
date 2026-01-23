// NUEVA CLASE TranslateExtension - ID: 20260123_122620
using System;
using System.Windows.Markup;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;

namespace AgenteIALocal.Localization
{
    // Markup extension that returns a translated string and updates when language changes
    public class TranslateExtension : MarkupExtension, INotifyPropertyChanged
    {
        public string Key { get; set; }
        public string FallbackValue { get; set; }

        private string _value;

        public TranslateExtension() { }
        public TranslateExtension(string key)
        {
            Key = key;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Provide initial value
            UpdateValue();

            // Subscribe to language changes to update value
            try
            {
                var svc = LocalizationProvider.Instance;
                if (svc != null)
                {
                    svc.LanguageChanged += (s, e) => {
                        // update on UI thread
                        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                        dispatcher.BeginInvoke(new Action(() => {
                            UpdateValue();
                            OnPropertyChanged(nameof(Value));
                        }));
                    };
                }
            }
            catch
            {
                // ignore
            }

            return Value;
        }

        private void UpdateValue()
        {
            try
            {
                var svc = LocalizationProvider.Instance;
                if (svc != null && !string.IsNullOrEmpty(Key))
                {
                    _value = svc.GetString(Key) ?? (FallbackValue ?? Key);
                    return;
                }
            }
            catch { }
            _value = FallbackValue ?? Key;
        }

        public string Value
        {
            get => _value;
        }
    }
}
