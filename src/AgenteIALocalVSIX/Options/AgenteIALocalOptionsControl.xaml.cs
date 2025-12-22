using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AgenteIALocal.Core.Settings;

namespace AgenteIALocalVSIX.Options
{
    public partial class AgenteIALocalOptionsControl : UserControl
    {
        private AgentOptionsViewModel viewModel;

        public AgenteIALocalOptionsControl(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;

            try
            {
                this.Focusable = true;
            }
            catch { }

            try
            {
                this.Loaded += OptionsControl_Loaded;
                this.GotFocus += (s, e) => AgentComposition.Logger?.Invoke("OptionsControl: GotFocus");
            }
            catch { }

            try
            {
                this.AddHandler(Keyboard.GotKeyboardFocusEvent, new RoutedEventHandler(OnAnyGotKeyboardFocus), true);
            }
            catch { }
        }

        private void OptionsControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Logger?.Invoke("OptionsControl: Loaded Focusable=" + this.Focusable + " Focused=" + (Keyboard.FocusedElement == null ? "null" : Keyboard.FocusedElement.GetType().Name));
            }
            catch { }

            try
            {
                // Defer focus to allow VS Options host to finish layout.
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    try
                    {
                        FocusFirstTextBox();

                        AgentComposition.Logger?.Invoke("OptionsControl: Loaded deferred Focusable=" + this.Focusable + " Focused=" + (Keyboard.FocusedElement == null ? "null" : Keyboard.FocusedElement.GetType().Name));
                    }
                    catch (Exception ex)
                    {
                        try { AgentComposition.Logger?.Invoke("OptionsControl: deferred focus failed: " + ex.Message); } catch { }
                    }
                }));
            }
            catch { }
        }

        private void FocusFirstTextBox()
        {
            // Prefer LM Studio BaseUrl when visible
            var lm = this.FindName("LmBaseUrlBox") as TextBox;
            if (lm != null && lm.IsVisible)
            {
                try
                {
                    lm.Focus();
                    Keyboard.Focus(lm);
                    AgentComposition.Logger?.Invoke("OptionsControl: Focused LmBaseUrlBox");
                }
                catch { }
                return;
            }

            var jan = this.FindName("JanBaseUrlBox") as TextBox;
            if (jan != null && jan.IsVisible)
            {
                try
                {
                    jan.Focus();
                    Keyboard.Focus(jan);
                    AgentComposition.Logger?.Invoke("OptionsControl: Focused JanBaseUrlBox");
                }
                catch { }
            }
        }

        public void LoadSettings(AgenteIALocalOptionsPage page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            AgentComposition.Logger?.Invoke("OptionsControl: LoadSettings start");

            var loaded = page.SettingsProvider?.Load();
            var settings = loaded ?? new AgenteIALocal.Core.Settings.AgentSettings();

            viewModel = new AgentOptionsViewModel
            {
                Provider = settings.Provider,
                LmStudio = settings.LmStudio ?? new LmStudioSettings(),
                JanServer = settings.JanServer ?? new JanServerSettings()
            };

            DataContext = viewModel;

            try
            {
                AgentComposition.Logger?.Invoke("OptionsControl: LmStudio type=" + (viewModel.LmStudio == null ? "null" : viewModel.LmStudio.GetType().FullName) + " BaseUrl=" + (viewModel.LmStudio?.BaseUrl ?? string.Empty) + " ChatPath=" + (viewModel.LmStudio?.ChatCompletionsPath ?? string.Empty) + " Model=" + (viewModel.LmStudio?.Model ?? string.Empty) + " ApiKey=***masked***");
            }
            catch { }

            try
            {
                AgentComposition.Logger?.Invoke("OptionsControl: JanServer type=" + (viewModel.JanServer == null ? "null" : viewModel.JanServer.GetType().FullName) + " BaseUrl=" + (viewModel.JanServer?.BaseUrl ?? string.Empty) + " ChatPath=" + (viewModel.JanServer?.ChatCompletionsPath ?? string.Empty) + " ApiKey=***masked***");
            }
            catch { }

            AgentComposition.Logger?.Invoke("OptionsControl: DataContext set to " + viewModel.GetType().FullName + " Provider=" + viewModel.Provider);
            AgentComposition.Logger?.Invoke("OptionsControl: Active BaseUrl=" + (GetActiveBaseUrl(viewModel) ?? string.Empty));
            AgentComposition.Logger?.Invoke("OptionsControl: Active ChatPath=" + (GetActiveChatPath(viewModel) ?? string.Empty));

            try
            {
                if (LmApiKeyBox != null) LmApiKeyBox.Password = viewModel.LmStudio?.ApiKey ?? string.Empty;
            }
            catch { }

            try
            {
                if (JanApiKeyBox != null) JanApiKeyBox.Password = viewModel.JanServer?.ApiKey ?? string.Empty;
            }
            catch { }

            HookPasswordBoxes();

            AgentComposition.Logger?.Invoke("OptionsControl: LoadSettings end");
        }

        public void SaveSettings(AgenteIALocalOptionsPage page
)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            var vm = viewModel ?? (DataContext as AgentOptionsViewModel);
            if (vm == null) return;

            AgentComposition.Logger?.Invoke("OptionsControl: SaveSettings start Provider=" + vm.Provider + " BaseUrl=" + (GetActiveBaseUrl(vm) ?? string.Empty) + " ChatPath=" + (GetActiveChatPath(vm) ?? string.Empty));

            // Ensure ApiKeys are taken from PasswordBoxes (Password is not bindable in WPF)
            try
            {
                if (vm.LmStudio != null && LmApiKeyBox != null) vm.LmStudio.ApiKey = LmApiKeyBox.Password;
            }
            catch { }

            try
            {
                if (vm.JanServer != null && JanApiKeyBox != null) vm.JanServer.ApiKey = JanApiKeyBox.Password;
            }
            catch { }

            var loaded = page.SettingsProvider?.Load();
            var settings = loaded ?? new AgenteIALocal.Core.Settings.AgentSettings();
            settings.Provider = vm.Provider;
            settings.LmStudio = vm.LmStudio ?? new LmStudioSettings();
            settings.JanServer = vm.JanServer ?? new JanServerSettings();

            page.SettingsProvider?.Save(settings);

            AgentComposition.Logger?.Invoke("OptionsControl: SaveSettings end (ApiKey=***masked***)");
        }

        private void HookPasswordBoxes()
        {
            try
            {
                if (LmApiKeyBox != null)
                {
                    LmApiKeyBox.PasswordChanged -= LmApiKeyBox_PasswordChanged;
                    LmApiKeyBox.PasswordChanged += LmApiKeyBox_PasswordChanged;
                }
            }
            catch { }

            try
            {
                if (JanApiKeyBox != null)
                {
                    JanApiKeyBox.PasswordChanged -= JanApiKeyBox_PasswordChanged;
                    JanApiKeyBox.PasswordChanged += JanApiKeyBox_PasswordChanged;
                }
            }
            catch { }
        }

        private void LmApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (viewModel?.LmStudio == null) return;
            if (sender is PasswordBox pb) viewModel.LmStudio.ApiKey = pb.Password;
        }

        private void JanApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (viewModel?.JanServer == null) return;
            if (sender is PasswordBox pb) viewModel.JanServer.ApiKey = pb.Password;
        }

        private void OnAnyGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (e is KeyboardFocusChangedEventArgs kf && kf.OriginalSource is TextBox tb)
                {
                    AgentComposition.Logger?.Invoke("OptionsControl: TextBox GotFocus Name=" + (tb.Name ?? string.Empty));
                }
            }
            catch { }
        }

        private static string GetActiveBaseUrl(AgentOptionsViewModel vm)
        {
            try
            {
                if (vm == null) return null;
                return vm.IsJanServer ? vm.JanServer?.BaseUrl : vm.LmStudio?.BaseUrl;
            }
            catch
            {
                return null;
            }
        }

        private static string GetActiveChatPath(AgentOptionsViewModel vm)
        {
            try
            {
                if (vm == null) return null;
                return vm.IsJanServer ? vm.JanServer?.ChatCompletionsPath : vm.LmStudio?.ChatCompletionsPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
