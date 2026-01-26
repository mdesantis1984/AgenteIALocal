using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AgenteIALocalVSIX;
using AgenteIALocalVSIX.Commons;
using Microsoft.VisualStudio.Shell;
// MODIFICADO - ID: 20260123_215600 - ROLLBACK System.Text.Json → Newtonsoft.Json + agregar Core.Configuration
using AgenteIALocal.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalConfigWindow : Window
    {
        // NUEVO - suppress BaseUrl TextChanged handler while programmatically setting text - ID: 20260115_223100
        private bool _suppressBaseUrlTextChanged_20260116;
        private readonly string initialServerId;
        // NUEVO CAMPO BaseUrlPingCts - ID: 20260114_000072
        private CancellationTokenSource _baseUrlPingCts;
        // NUEVO CAMPO LastPersistedBaseUrl - ID: 20260114_000073
        private string _lastPersistedBaseUrl;
        private bool _isInitializingAdvancedUi;
        // NUEVO EVENTO BaseUrlHealthChanged - ID: 20260114_000079
        public event Action<bool, string, IReadOnlyList<string>> BaseUrlHealthChanged;

        // NUEVO: in-memory models cache per serverId - ID: 20260115_220500
        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>> _modelsCacheByServerId = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>>();

        // NUEVO: versioning to avoid stale UI updates for models fetch - ID: 20260115_220500
        private int _modelsFetchVersion = 0;
        // NUEVO: guard to avoid recursion when toggling nav buttons - ID: 20260116_112500
        private bool _suppressNavToggleChecked_20260116;

        public AgenteIALocalConfigWindow(string serverId = null)
        {
            InitializeComponent();
            
            // NUEVO - ID: 20260125_003004 - Lazy init LocalizationService (garantiza ejecución)
            AgenteIALocalVSIXPackage.InitializeLocalizationServiceOnce();
            
            try { HeaderDragArea.MouseLeftButtonDown += HeaderDragArea_MouseLeftButtonDown; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "HeaderDragArea wire failed: " + ex.Message, ex); }
            Loaded += AgenteIALocalConfigWindow_Loaded;
            // Subscribe to settings saved notifications to refresh modal when settings change elsewhere
            try { AgentSettingsStore.SettingsSaved += OnSettingsSaved; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "SettingsSaved subscribe failed: " + ex.Message, ex); }
            initialServerId = serverId ?? string.Empty;
            if (!string.IsNullOrEmpty(serverId))
            {
                Title = "Configuración — " + serverId;
            }

            // MODIFICADO - ID: 20260116_110300
            // Set window and header caption to match ToolWindowPane caption including VSIX version
            try
            {
                var version = typeof(AgenteIALocalVSIXPackage).GetVsixVersionString();
                var caption = $"Chat de Agente IA Local {version} - Configuracion";
                try { this.Title = caption; } catch (Exception exTitle) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "Title set failed: " + exTitle.Message, exTitle); }
                try { HeaderTitleText.Text = caption; } catch (Exception exHeader) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "HeaderTitleText set failed: " + exHeader.Message, exHeader); }
            }
            catch (Exception exVer) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "Version caption failed: " + exVer.Message, exVer); }

            // NUEVO - ID: 20260124_001800 - E4: Suscribir a LanguageChanged para reload automático
            try
            {
                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                if (locService != null)
                {
                    locService.LanguageChanged += OnLanguageChanged;
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Ctor", "LanguageChanged subscribed", null);
                }
            }
            catch (Exception exLang) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Ctor", "LanguageChanged subscribe failed: " + exLang.Message, exLang); }

            // NUEVO: view switching is handled by XAML DataTriggers; no code-behind wiring required - ID: 20260116_094500
        }

        private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void HeaderDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left) return;
                // MODIFICADO - ID: 20260122_010900 - Migrado a Serilog
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.DragMove", "DragMove start", null);
                DragMove();
            }
            catch (Exception ex)
            {
                // MODIFICADO - ID: 20260122_010900 - Migrado a Serilog
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.DragMove", "DragMove failed: " + ex.Message, ex);
            }
        }

        private void AgenteIALocalConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FireAndForget(HandleLoadedAsync(), "ConfigWindow.Loaded");
        }

        private void OnSettingsSaved(string reason)
        {
            try
            {
                if (!IsLoaded) return;
                // Rehydrate advanced controls only, avoid triggering save
                try
                {
                    var jt = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        try
                        {
                            _isInitializingAdvancedUi = true;
                            var settings = AgentSettingsStore.Load();
                            var targetId = settings != null ? settings.ActiveServerId : null;
                            var srv = (settings != null && settings.Servers != null) ? settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase)) : null;
                            ApplyServerToUi(targetId ?? string.Empty, srv);
                            LoadAdvancedControls(settings, srv);
                        }
                        catch (Exception exRefresh)
                        {
                            AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.SettingsSaved", "RefreshFromSettings failed: " + exRefresh.Message, exRefresh);
                        }
                        finally { _isInitializingAdvancedUi = false; }
                    });

                    _ = jt.Task.ContinueWith(t => { var _e = t.Exception; }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, System.Threading.Tasks.TaskScheduler.Default);
                }
                catch (Exception exJt) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.SettingsSaved", "JoinableTask failed: " + exJt.Message, exJt); }
            }
            catch (Exception exOuter) { AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.SettingsSaved", "OnSettingsSaved failed: " + exOuter.Message, exOuter); }
        }

        // MODIFICADO METODO NavToggle_Checked - ID: 20260122_013400
        // Make sidebar ToggleButtons act mutually exclusive (Idioma, LLM, Logging)
        // FIX: null-checks para evitar NullReferenceException durante inicialización
        private void NavToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressNavToggleChecked_20260116) return;
            try
            {
                _suppressNavToggleChecked_20260116 = true;
                var tb = sender as ToggleButton;
                if (tb == null) return;

                // CRÍTICO: Null-checks - los controles pueden ser null durante inicialización XAML
                if (NavIdiomaToggle == null || NavLlmToggle == null || NavLoggingToggle == null)
                {
                    // Controles no inicializados todavía - skip mutual exclusion
                    return;
                }

                // Uncheck all OTHER toggles (mutual exclusion)
                if (tb == NavIdiomaToggle)
                {
                    try { NavLlmToggle.IsChecked = false; } catch (Exception exLlm) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavLlmToggle uncheck failed: " + exLlm.Message, exLlm); }
                    try { NavLoggingToggle.IsChecked = false; } catch (Exception exLog) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavLoggingToggle uncheck failed: " + exLog.Message, exLog); }
                }
                else if (tb == NavLlmToggle)
                {
                    try { NavIdiomaToggle.IsChecked = false; } catch (Exception exIdioma) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavIdiomaToggle uncheck failed: " + exIdioma.Message, exIdioma); }
                    try { NavLoggingToggle.IsChecked = false; } catch (Exception exLog) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavLoggingToggle uncheck failed: " + exLog.Message, exLog); }
                }
                else if (tb == NavLoggingToggle)
                {
                    try { NavIdiomaToggle.IsChecked = false; } catch (Exception exIdioma) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavIdiomaToggle uncheck failed: " + exIdioma.Message, exIdioma); }
                    try { NavLlmToggle.IsChecked = false; } catch (Exception exLlm) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavLlmToggle uncheck failed: " + exLlm.Message, exLlm); }
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NavToggle", "NavToggle_Checked failed: " + ex.Message, ex);
            }
            finally { _suppressNavToggleChecked_20260116 = false; }
        }

        // MODIFICADO METODO LoadAdvancedControls - ID: 20260126_031100
        // FIX: Usar i18n keys para RunMode (mapear valor persistido → texto traducido actual)
        private void LoadAdvancedControls(AgentSettings settings, ServerConfig activeServer)
        {
            if (settings == null) return;

            // Provider: nombres INVARIANTES ("LM Studio", "JAN" en todos los idiomas) - NO necesita i18n
            var provider = (activeServer != null ? activeServer.Provider : string.Empty).ToLowerInvariant();
            try { TrySelectComboByText(ProviderCombo_Modal, provider == "jan" ? "JAN" : "LM Studio"); } catch (Exception exProv) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "ProviderCombo select failed: " + exProv.Message, exProv); }

            // RunMode: nombres VARIABLES entre idiomas - usar LocalizationService para obtener texto traducido
            var runMode = settings.GlobalSettings?.RunMode ?? "preguntar";
            try
            {
                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                string runModeText;
                
                if (string.Equals(runMode, "agente", StringComparison.OrdinalIgnoreCase))
                {
                    // runMode persistido = "agente" → i18n key → texto actual ("Agente" en es-AR, "Agent" en en-US)
                    runModeText = locService != null ? locService.GetString("ui.config.llm.runmode.agente") : "Agente";
                }
                else
                {
                    // runMode persistido = "preguntar" → i18n key → texto actual ("Preguntar" en es-AR, "Ask" en en-US)
                    runModeText = locService != null ? locService.GetString("ui.config.llm.runmode.preguntar") : "Preguntar";
                }
                
                TrySelectComboByText(RunModeCombo_Modal, runModeText);
                AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.LoadAdvanced", $"RunMode selected: {runMode} → i18n: {runModeText}", null);
            }
            catch (Exception exMode) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "RunModeCombo select failed: " + exMode.Message, exMode); }

            var requestDefaults = settings.GlobalSettings?.RequestDefaults;
            // MODIFICADO METODO LoadAdvancedControls - ID: 20260123_225601
            // Force Stream as the only option in UI: checked + disabled (usar DTO)
            var streamValue = requestDefaults?.Stream ?? true;
            try { StreamToggle_Modal.IsChecked = true; StreamToggle_Modal.IsEnabled = false; } catch (Exception exStream) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "StreamToggle init failed: " + exStream.Message, exStream); }

            // MODIFICADO - ID: 20260126_031200 - FIX: IncludeUsage CheckBox se recarga SIEMPRE desde settings.json
            var includeUsage = requestDefaults?.StreamOptions?.IncludeUsage ?? false;
            try
            {
                IncludeUsageToggle_Modal.IsChecked = includeUsage;
                IncludeUsageToggle_Modal.IsEnabled = provider == "lmstudio";
                AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.LoadAdvanced", $"IncludeUsage loaded: {includeUsage} (enabled={provider == "lmstudio"})", null);
            }
            catch (Exception exUsage) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "IncludeUsage load failed: " + exUsage.Message, exUsage); }

            // hydrate temperature and maxTokens - MODIFICADO - ID: 20260123_225602 - Usar DTOs
            try
            {
                var temp = requestDefaults?.Temperature ?? 0.2;
                TemperatureTextBox_Modal.Text = temp.ToString("G");
            }
            catch (Exception exTemp) { TemperatureTextBox_Modal.Text = "0.2"; AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "Temperature load failed: " + exTemp.Message, exTemp); }

            try
            {
                var mt = requestDefaults?.MaxTokens ?? 0;
                MaxTokensTextBox_Modal.Text = mt.ToString();
            }
            catch (Exception exMax) { MaxTokensTextBox_Modal.Text = "0"; AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadAdvanced", "MaxTokens load failed: " + exMax.Message, exMax); }

            var agent = settings.GlobalSettings?.Agent;
            AgentIdeIntegrationToggle_Modal.IsChecked = agent?.IdeIntegration ?? true;
            AgentApplyChangesToggle_Modal.IsChecked = agent?.ApplyChanges ?? false;
            var maxSteps = agent?.MaxSteps ?? 5;
            AgentMaxStepsTextBox_Modal.Text = maxSteps.ToString();

            // NUEVO - ID: 20260122_013500 - E1: Cargar controles de logging
            LoadLoggingControls(settings);

            // NUEVO - ID: 20260124_001601 - E2: Cargar controles de idioma
            LoadIdiomaControls();
        }

        // MODIFICADO METODO LoadLoggingControls - ID: 20260123_225603
        // E1: Cargar estado de logging desde settings.json (usando DTOs)
        // DEFAULTS: enabled=false, critical=true, error=true, resto=false
        private void LoadLoggingControls(AgentSettings settings)
        {
            try
            {
                if (settings == null || settings.GlobalSettings == null)
                {
                    // Defaults: enabled=false (solo Critical + Error habilitados)
                    try { LoggingEnabledToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "LoggingEnabled default failed: " + ex.Message, ex); }
                    try { LoggingVerboseToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Verbose default failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Debug default failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Information default failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Warning default failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsChecked = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Error default failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsChecked = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Critical default failed: " + ex.Message, ex); }
                    return;
                }

                var logging = settings.GlobalSettings.Logging;
                if (logging == null)
                {
                    // Defaults si no existe sección logging: enabled=false, solo Critical + Error
                    try { LoggingEnabledToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "LoggingEnabled default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingVerboseToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Verbose default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Debug default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Information default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Warning default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsChecked = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Error default (null logging) failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsChecked = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Critical default (null logging) failed: " + ex.Message, ex); }
                    return;
                }

                // Cargar valores desde settings.json (defaults: enabled=false si null) - MODIFICADO - ID: 20260123_225604
                var enabled = logging.Enabled;
                var verbose = logging.Verbose;
                var debug = logging.Debug;
                var information = logging.Information;
                var warning = logging.Warning;
                var error = logging.Error;
                var critical = logging.Critical;

                // E5: Master override - si enabled=true, forzar todos los niveles a true
                if (enabled)
                {
                    try { LoggingEnabledToggle_Modal.IsChecked = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "LoggingEnabled set failed: " + ex.Message, ex); }
                    try { LoggingVerboseToggle_Modal.IsChecked = true; LoggingVerboseToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Verbose override failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsChecked = true; LoggingDebugToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Debug override failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsChecked = true; LoggingInformationToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Information override failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsChecked = true; LoggingWarningToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Warning override failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsChecked = true; LoggingErrorToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Error override failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsChecked = true; LoggingCriticalToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Critical override failed: " + ex.Message, ex); }
                }
                else
                {
                    // Cargar valores individuales
                    try { LoggingEnabledToggle_Modal.IsChecked = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "LoggingEnabled set false failed: " + ex.Message, ex); }
                    try { LoggingVerboseToggle_Modal.IsChecked = verbose; LoggingVerboseToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Verbose set failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsChecked = debug; LoggingDebugToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Debug set failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsChecked = information; LoggingInformationToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Information set failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsChecked = warning; LoggingWarningToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Warning set failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsChecked = error; LoggingErrorToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Error set failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsChecked = critical; LoggingCriticalToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadLogging", "Critical set failed: " + ex.Message, ex); }
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LoadLogging", $"Logging controls loaded: enabled={enabled}, v={verbose}, d={debug}, i={information}, w={warning}, e={error}, c={critical}", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoadLogging", "LoadLoggingControls failed: " + ex.Message, ex);
            }
        }

        // REESCRITO METODO LoadIdiomaControls - ID: 20260126_020500
        // NUEVA ARQUITECTURA: Generación dinámica de grid basada en banderas PNG existentes
        // Lógica: escanear Languages/flags/img/*.png → crear Border+Image+TextBlock+RadioButton → habilitar SI existe strings.json
        private void LoadIdiomaControls()
        {
            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LoadIdioma", "LoadIdiomaControls: INICIO (generación dinámica)", null);

                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                if (locService == null)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadIdioma", "LocalizationService is null - skip idioma load", null);
                    return;
                }

                // Limpiar grid existente (en caso de reload)
                try
                {
                    LanguageGrid.Children.Clear();
                }
                catch (Exception exClear)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadIdioma", "Grid.Clear failed: " + exClear.Message, exClear);
                }

                // Obtener idiomas disponibles desde LocalizationService
                var available = locService.GetAvailableLanguages();
                var currentLang = locService.CurrentLanguageCode ?? "es-AR";

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LoadIdioma", $"Current lang: {currentLang}, Languages: {System.Linq.Enumerable.Count(available)}", null);

                // Generar controles dinámicamente
                foreach (var lang in available)
                {
                    try
                    {
                        // Crear Border contenedor
                        var border = new System.Windows.Controls.Border
                        {
                            Margin = new Thickness(0, 0, 8, 8),
                            Padding = new Thickness(12),
                            Background = (System.Windows.Media.Brush)this.Resources["HeaderBackgroundBrush"],
                            CornerRadius = new CornerRadius(8),
                            Width = 120,
                            Height = 140,
                            Opacity = lang.IsAvailable ? 1.0 : 0.5
                        };

                        // StackPanel vertical interno
                        var stack = new System.Windows.Controls.StackPanel
                        {
                            Orientation = System.Windows.Controls.Orientation.Vertical,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        };

                        // Image (bandera PNG)
                        var img = new System.Windows.Controls.Image
                        {
                            Width = 48,
                            Height = 32,
                            Margin = new Thickness(0, 8, 0, 8),
                            Stretch = System.Windows.Media.Stretch.Uniform
                        };

                        // MODIFICADO - ID: 20260126_021500 - Cambio pack URI a FileSystem URI
                        // Razón: <Content> crea archivos físicos, NO recursos embebidos (pack URI solo con <Resource>)
                        // Solución: Cargar desde path absoluto del filesystem
                        try
                        {
                            if (!string.IsNullOrEmpty(lang.FlagPath) && System.IO.File.Exists(lang.FlagPath))
                            {
                                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(lang.FlagPath, UriKind.Absolute);
                                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                img.Source = bmp;
                            }
                            else
                            {
                                AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadIdioma", $"Flag not found: {lang.FlagPath}", null);
                            }
                        }
                        catch (Exception exImg)
                        {
                            AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadIdioma", $"Flag image failed for {lang.Code}: {exImg.Message}", exImg);
                        }


                        // TextBlock (nombre nativo del idioma)
                        var txt = new System.Windows.Controls.TextBlock
                        {
                            Style = (System.Windows.Style)this.Resources["Text.Body"],
                            FontSize = 12,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 8),
                            Text = lang.NativeName ?? lang.Name ?? lang.Code
                        };

                        // RadioButton (selección)
                        var radio = new System.Windows.Controls.RadioButton
                        {
                            GroupName = "Language",
                            IsEnabled = lang.IsAvailable,
                            IsChecked = string.Equals(lang.Code, currentLang, StringComparison.OrdinalIgnoreCase),
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            Tag = lang.Code // Guardar código para event handler
                        };

                        // MODIFICADO - ID: 20260126_021600 - Wire event handler con logs comprehensivos
                        radio.Checked += (s, e) =>
                        {
                            AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.IdiomaChange", $"RadioButton.Checked event fired (init flag={_isInitializingAdvancedUi})", null);
                            
                            if (_isInitializingAdvancedUi)
                            {
                                AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.IdiomaChange", "Skipped - initializing UI", null);
                                return;
                            }
                            
                            var langCode = (s as System.Windows.Controls.RadioButton)?.Tag as string;
                            if (string.IsNullOrEmpty(langCode))
                            {
                                AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.IdiomaChange", "RadioButton.Tag is null or empty", null);
                                return;
                            }

                            try
                            {
                                var svc = AgenteIALocalVSIXPackage.LocalizationService;
                                if (svc == null)
                                {
                                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.IdiomaChange", "LocalizationService null - skip language change", null);
                                    return;
                                }

                                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.IdiomaChange", $"Calling SetLanguage({langCode})...", null);
                                svc.SetLanguage(langCode);
                                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.IdiomaChange", $"✓ Language changed to {langCode}", null);
                            }
                            catch (Exception exSet)
                            {
                                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.IdiomaChange", $"{langCode} change failed: {exSet.Message}", exSet);
                            }
                        };


                        // Ensamblar jerarquía
                        stack.Children.Add(img);
                        stack.Children.Add(txt);
                        stack.Children.Add(radio);
                        border.Child = stack;

                        // Agregar al grid
                        LanguageGrid.Children.Add(border);

                        AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.LoadIdioma", $"✓ Created: {lang.Code} (enabled={lang.IsAvailable})", null);
                    }
                    catch (Exception exLang)
                    {
                        AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LoadIdioma", $"Error creating controls for {lang.Code}: {exLang.Message}", exLang);
                    }
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LoadIdioma", $"LoadIdiomaControls: OK - {LanguageGrid.Children.Count} idiomas generados", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoadIdioma", "LoadIdiomaControls failed: " + ex.Message, ex);
            }
        }


        // NUEVO METODO WireAdvancedHandlersOnce - ID: 20250304_170002
        // MODIFICADO - ID: 20260122_040100 - Agregado logging en catch (pauta obligatoria)
        private void WireAdvancedHandlersOnce()
        {
            try
            {
                ProviderCombo_Modal.SelectionChanged -= ProviderCombo_Modal_SelectionChanged;
                ProviderCombo_Modal.SelectionChanged += ProviderCombo_Modal_SelectionChanged;

                RunModeCombo_Modal.SelectionChanged -= RunModeCombo_Modal_SelectionChanged;
                RunModeCombo_Modal.SelectionChanged += RunModeCombo_Modal_SelectionChanged;

                StreamToggle_Modal.Checked -= StreamToggle_Modal_Checked;
                StreamToggle_Modal.Unchecked -= StreamToggle_Modal_Checked;
                StreamToggle_Modal.Checked += StreamToggle_Modal_Checked;
                StreamToggle_Modal.Unchecked += StreamToggle_Modal_Checked;

                IncludeUsageToggle_Modal.Checked -= IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Unchecked -= IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Checked += IncludeUsageToggle_Modal_Checked;
                IncludeUsageToggle_Modal.Unchecked += IncludeUsageToggle_Modal_Checked;

                // Temperature and MaxTokens handlers
                TemperatureTextBox_Modal.TextChanged -= TemperatureTextBox_Modal_TextChanged;
                TemperatureTextBox_Modal.LostFocus -= TemperatureTextBox_Modal_LostFocus;
                TemperatureTextBox_Modal.TextChanged += TemperatureTextBox_Modal_TextChanged;
                TemperatureTextBox_Modal.LostFocus += TemperatureTextBox_Modal_LostFocus;

                MaxTokensTextBox_Modal.TextChanged -= MaxTokensTextBox_Modal_TextChanged;
                MaxTokensTextBox_Modal.LostFocus -= MaxTokensTextBox_Modal_LostFocus;
                MaxTokensTextBox_Modal.TextChanged += MaxTokensTextBox_Modal_TextChanged;
                MaxTokensTextBox_Modal.LostFocus += MaxTokensTextBox_Modal_LostFocus;

                AgentIdeIntegrationToggle_Modal.Checked -= AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Unchecked -= AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Checked += AgentIdeIntegrationToggle_Modal_Checked;
                AgentIdeIntegrationToggle_Modal.Unchecked += AgentIdeIntegrationToggle_Modal_Checked;

                AgentApplyChangesToggle_Modal.Checked -= AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Unchecked -= AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Checked += AgentApplyChangesToggle_Modal_Checked;
                AgentApplyChangesToggle_Modal.Unchecked += AgentApplyChangesToggle_Modal_Checked;

                AgentMaxStepsTextBox_Modal.TextChanged -= AgentMaxStepsTextBox_Modal_TextChanged;
                AgentMaxStepsTextBox_Modal.LostFocus -= AgentMaxStepsTextBox_Modal_LostFocus;
                AgentMaxStepsTextBox_Modal.TextChanged += AgentMaxStepsTextBox_Modal_TextChanged;
                AgentMaxStepsTextBox_Modal.LostFocus += AgentMaxStepsTextBox_Modal_LostFocus;

                // NUEVO - ID: 20260122_013900 - Wire logging handlers
                LoggingEnabledToggle_Modal.Checked -= LoggingEnabledToggle_Modal_Checked;
                LoggingEnabledToggle_Modal.Unchecked -= LoggingEnabledToggle_Modal_Checked;
                LoggingEnabledToggle_Modal.Checked += LoggingEnabledToggle_Modal_Checked;
                LoggingEnabledToggle_Modal.Unchecked += LoggingEnabledToggle_Modal_Checked;

                LoggingVerboseToggle_Modal.Checked -= LoggingVerboseToggle_Modal_Checked;
                LoggingVerboseToggle_Modal.Unchecked -= LoggingVerboseToggle_Modal_Checked;
                LoggingVerboseToggle_Modal.Checked += LoggingVerboseToggle_Modal_Checked;
                LoggingVerboseToggle_Modal.Unchecked += LoggingVerboseToggle_Modal_Checked;

                LoggingDebugToggle_Modal.Checked -= LoggingDebugToggle_Modal_Checked;
                LoggingDebugToggle_Modal.Unchecked -= LoggingDebugToggle_Modal_Checked;
                LoggingDebugToggle_Modal.Checked += LoggingDebugToggle_Modal_Checked;
                LoggingDebugToggle_Modal.Unchecked += LoggingDebugToggle_Modal_Checked;

                LoggingInformationToggle_Modal.Checked -= LoggingInformationToggle_Modal_Checked;
                LoggingInformationToggle_Modal.Unchecked -= LoggingInformationToggle_Modal_Checked;
                LoggingInformationToggle_Modal.Checked += LoggingInformationToggle_Modal_Checked;
                LoggingInformationToggle_Modal.Unchecked += LoggingInformationToggle_Modal_Checked;

                LoggingWarningToggle_Modal.Checked -= LoggingWarningToggle_Modal_Checked;
                LoggingWarningToggle_Modal.Unchecked -= LoggingWarningToggle_Modal_Checked;
                LoggingWarningToggle_Modal.Checked += LoggingWarningToggle_Modal_Checked;
                LoggingWarningToggle_Modal.Unchecked += LoggingWarningToggle_Modal_Checked;

                LoggingErrorToggle_Modal.Checked -= LoggingErrorToggle_Modal_Checked;
                LoggingErrorToggle_Modal.Unchecked -= LoggingErrorToggle_Modal_Checked;
                LoggingErrorToggle_Modal.Checked += LoggingErrorToggle_Modal_Checked;
                LoggingErrorToggle_Modal.Unchecked += LoggingErrorToggle_Modal_Checked;

                LoggingCriticalToggle_Modal.Checked -= LoggingCriticalToggle_Modal_Checked;
                LoggingCriticalToggle_Modal.Unchecked -= LoggingCriticalToggle_Modal_Checked;
                LoggingCriticalToggle_Modal.Checked += LoggingCriticalToggle_Modal_Checked;
                LoggingCriticalToggle_Modal.Unchecked += LoggingCriticalToggle_Modal_Checked;

                // MODIFICADO - ID: 20260126_020700 - Eliminado TODO wiring hardcoded RadioButtons
                // Razón: Grid dinámico - TODOS los event handlers wired en LoadIdiomaControls() lambda
                // (Radio_esAR, Radio_enUS, etc. YA NO EXISTEN en XAML - generados dinámicamente)
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.WireHandlers", "WireAdvancedHandlersOnce failed: " + ex.Message, ex);
            }
        }

        // MODIFICADO METODO TrySelectComboByText - ID: 20260126_031000
        // FIX: Extraer texto desde TextBlock.Text cuando Content es TextBlock (soporte i18n {loc:Translate})
        // Selects an existing ComboBox item by comparing display text case-insensitively.
        private bool TrySelectComboByText(ComboBox cb, string text)
        {
            if (cb == null || string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                foreach (var item in cb.Items)
                {
                    try
                    {
                        string s = null;
                        var cbi = item as ComboBoxItem;
                        if (cbi != null)
                        {
                            // NUEVO - ID: 20260126_031000 - Extraer texto desde TextBlock.Text si Content es TextBlock
                            var textBlock = cbi.Content as TextBlock;
                            if (textBlock != null)
                            {
                                try { s = textBlock.Text ?? string.Empty; } catch (Exception exText) { s = null; AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.TrySelect", "TextBlock.Text failed: " + exText.Message, exText); }
                            }
                            else
                            {
                                // Fallback: Content directo como string o ToString()
                                try { s = cbi.Content?.ToString(); } catch (Exception exContent) { s = null; AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.TrySelect", "ComboBoxItem.Content failed: " + exContent.Message, exContent); }
                            }
                        }
                        if (string.IsNullOrEmpty(s))
                        {
                            try { s = item?.ToString(); } catch (Exception exToString) { s = null; AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.TrySelect", "item.ToString() failed: " + exToString.Message, exToString); }
                        }
                        if (string.IsNullOrEmpty(s)) continue;
                        if (string.Equals(s, text, StringComparison.OrdinalIgnoreCase))
                        {
                            cb.SelectedItem = item;
                            return true;
                        }
                    }
                    catch (Exception exItem) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.TrySelect", "Item processing failed: " + exItem.Message, exItem); }
                }
            }
            catch (Exception exLoop) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.TrySelect", "Foreach loop failed: " + exLoop.Message, exLoop); }
            return false;
        }

        // NUEVO METODO NormalizeProviderId - ID: 20260117_234000
        // Normaliza el texto visible del combo Provider a un identificador corto de proveedor.
        // Entrada: texto (ej. "LM Studio", "JAN") -> salida: "lmstudio" | "jan" | ""
        private static string NormalizeProviderId(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;
                var s = text.Trim().ToLowerInvariant();
                // remove spaces and hyphens for tolerant matching
                s = s.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (s == "lmstudio" || s == "lmstudio") return "lmstudio";
                if (s == "lmstudio") return "lmstudio"; // defensive
                if (s.StartsWith("lmstudio")) return "lmstudio";
                if (s.StartsWith("lmstudio")) return "lmstudio";
                if (s == "jan" || s.StartsWith("jan")) return "jan";
                return string.Empty;
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.NormalizeProvider", "NormalizeProviderId failed: " + ex.Message, ex); return string.Empty; }
        }

        // MODIFICADO METODO ProviderCombo_Modal_SelectionChanged - ID: 20260126_032200
        // FIX: Leer Tag invariante de ComboBoxItem (NO texto)
        private void ProviderCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;

            try
            {
                // NUEVO - ID: 20260126_032200 - Leer Tag invariante directamente
                var cbi = ProviderCombo_Modal?.SelectedItem as ComboBoxItem;
                var providerId = (cbi?.Tag as string ?? string.Empty).ToLowerInvariant();
                
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Provider", "Provider Tag is null - skipping", null);
                    return;
                }
                
                var targetServerId = string.Equals(providerId, "jan", StringComparison.OrdinalIgnoreCase) ? "jan-local" : "lmstudio-local";
                AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Provider", $"Provider from UI Tag: {providerId} → targetServerId: {targetServerId}", null);

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                var prev = settings.ActiveServerId ?? string.Empty;
                if (string.Equals(prev, targetServerId, StringComparison.OrdinalIgnoreCase))
                {
                    // no change
                }
                else
                {
                    settings.ActiveServerId = targetServerId;
                    AgentSettingsStore.Save(settings);
                }
                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetServerId, StringComparison.OrdinalIgnoreCase));
                ApplyServerToUi(targetServerId, srv);

                // Enable include-usage toggle only for LM Studio; disable and clear otherwise
                try
                {
                    IncludeUsageToggle_Modal.IsEnabled = string.Equals(providerId, "lmstudio", StringComparison.OrdinalIgnoreCase);
                    if (!string.Equals(providerId, "lmstudio", StringComparison.OrdinalIgnoreCase))
                    {
                        IncludeUsageToggle_Modal.IsChecked = false;
                    }
                }
                catch (Exception exToggle)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Provider", "IncludeUsageToggle update failed: " + exToggle.Message, exToggle);
                }

                try
                {
                    ServerBaseUrlTextBox_Modal_TextChanged(ServerBaseUrlTextBox_Modal, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
                }
                catch (Exception exBaseUrl)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Provider", "ServerBaseUrlTextBox_Modal_TextChanged trigger failed: " + exBaseUrl.Message, exBaseUrl);
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Provider", "Provider change error: " + ex.Message, ex);
            }
        }

        // MODIFICADO METODO RunModeCombo_Modal_SelectionChanged - ID: 20260123_225605
        private void RunModeCombo_Modal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var selected = GetSelectedComboContent(RunModeCombo_Modal);
                var runMode = string.Equals(selected, "Agente", StringComparison.OrdinalIgnoreCase) ? "agente" : "preguntar";
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();
                settings.GlobalSettings.RunMode = runMode;
                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch (Exception exFind) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.RunMode", "Provider lookup failed: " + exFind.Message, exFind); }
                // DESHABILITADO live update - ID: 20260122_030100 - Solo SaveButton persiste
                // AgentSettingsStore.Save(settings);
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.RunMode", $"RunMode UI changed to {runMode} (NOT saved yet - waiting for Save button)", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.RunMode", "RunMode change error: " + ex.Message, ex);
            }
        }

        // MODIFICADO METODO StreamToggle_Modal_Checked - ID: 20260123_225606
        private void StreamToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                // Ensure requestDefaults.stream is always true on save (usar DTO)
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();
                settings.GlobalSettings.RequestDefaults.Stream = true;
                // provider for tracing
                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch (Exception exFind) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Stream", "Provider lookup failed: " + exFind.Message, exFind); }
                // DESHABILITADO live update - ID: 20260122_030100 - Solo SaveButton persiste
                // AgentSettingsStore.Save(settings);
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Stream", "Stream toggle changed (NOT saved yet - waiting for Save button)", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Stream", "Stream toggle error: " + ex.Message, ex);
            }
        }

        // MODIFICADO METODO IncludeUsageToggle_Modal_Checked - ID: 20260126_032400
        // FIX: Leer Provider Tag invariante (NO NormalizeProviderId con texto)
        private void IncludeUsageToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                // Delegate to unified persister which sets includeUsage according to provider
                PersistRequestDefaultsFromUi();
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.IncludeUsage", "IncludeUsage toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO AgentIdeIntegrationToggle_Modal_Checked - ID: 20250304_170007
        private void AgentIdeIntegrationToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistAgentFlag("ideIntegration", AgentIdeIntegrationToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: Agent IDE toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO AgentApplyChangesToggle_Modal_Checked - ID: 20250304_170008
        private void AgentApplyChangesToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistAgentFlag("applyChanges", AgentApplyChangesToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.AgentApplyChanges", "Agent ApplyChanges toggle error: " + ex.Message, ex);
            }
        }

        // MODIFICADO METODO PersistAgentFlag - ID: 20260123_225607
        private void PersistAgentFlag(string key, bool value)
        {
            var settings = AgentSettingsStore.Load() ?? new AgentSettings();
            if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();
            
            // Mapear key a propiedad DTO
            if (key == "ideIntegration")
                settings.GlobalSettings.Agent.IdeIntegration = value;
            else if (key == "applyChanges")
                settings.GlobalSettings.Agent.ApplyChanges = value;
            
            string provider = null;
            try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch (Exception exFind) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Agent", "Provider lookup failed: " + exFind.Message, exFind); }
            // DESHABILITADO live update - ID: 20260122_030100 - Solo SaveButton persiste
            // AgentSettingsStore.Save(settings);
            AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Agent", $"Agent.{key}={value} UI changed - NOT saved yet", null);
        }

        // NUEVO METODO AgentMaxStepsTextBox_Modal_TextChanged - ID: 20250304_170010
        private void AgentMaxStepsTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var value = ParseMaxSteps(AgentMaxStepsTextBox_Modal.Text);
                PersistAgentMaxSteps(value);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.MaxSteps", "MaxSteps change error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO AgentMaxStepsTextBox_Modal_LostFocus - ID: 20250304_170011
        private void AgentMaxStepsTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var value = ParseMaxSteps(AgentMaxStepsTextBox_Modal.Text);
                AgentMaxStepsTextBox_Modal.Text = value.ToString();
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.MaxSteps", "MaxSteps LostFocus failed: " + ex.Message, ex); }
        }

        // MODIFICADO METODO PersistAgentMaxSteps - ID: 20260123_225608
        private void PersistAgentMaxSteps(int value)
        {
            var settings = AgentSettingsStore.Load() ?? new AgentSettings();
            if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();
            settings.GlobalSettings.Agent.MaxSteps = value;
            string provider = null;
            try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch (Exception exFind) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Agent", "Provider lookup failed: " + exFind.Message, exFind); }
            // DESHABILITADO live update - ID: 20260122_030100 - Solo SaveButton persiste
            // AgentSettingsStore.Save(settings);
            AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Agent", $"Agent.maxSteps={value} UI changed - NOT saved yet", null);
        }

        // NUEVO METODO PersistLoggingFlag - ID: 20260122_013600
        // E2: Guardar estado global logging.enabled (live update NO - solo al Save)
        // Master override: si enabled=true, habilitar todos los niveles UI
        private void PersistLoggingFlag(bool enabled)
        {
            try
            {
                // E5: Master override en UI - si enabled=true, forzar todos los niveles a true y deshabilitar checkboxes individuales
                if (enabled)
                {
                    try { LoggingVerboseToggle_Modal.IsChecked = true; LoggingVerboseToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Verbose override failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsChecked = true; LoggingDebugToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Debug override failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsChecked = true; LoggingInformationToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Information override failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsChecked = true; LoggingWarningToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Warning override failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsChecked = true; LoggingErrorToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Error override failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsChecked = true; LoggingCriticalToggle_Modal.IsEnabled = false; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Critical override failed: " + ex.Message, ex); }
                }
                else
                {
                    // Habilitar checkboxes individuales
                    try { LoggingVerboseToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Verbose enable failed: " + ex.Message, ex); }
                    try { LoggingDebugToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Debug enable failed: " + ex.Message, ex); }
                    try { LoggingInformationToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Information enable failed: " + ex.Message, ex); }
                    try { LoggingWarningToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Warning enable failed: " + ex.Message, ex); }
                    try { LoggingErrorToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Error enable failed: " + ex.Message, ex); }
                    try { LoggingCriticalToggle_Modal.IsEnabled = true; } catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.PersistLogging", "Critical enable failed: " + ex.Message, ex); }
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.PersistLogging", $"Logging.enabled={enabled} UI changed - NOT saved yet", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.PersistLogging", "PersistLoggingFlag failed: " + ex.Message, ex);
            }
        }

        // NUEVO METODO PersistLoggingLevel - ID: 20260122_013700
        // E3: Guardar estado de nivel individual (live update NO - solo al Save)
        private void PersistLoggingLevel(string level, bool enabled)
        {
            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.PersistLogging", $"Logging.{level}={enabled} UI changed - NOT saved yet", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.PersistLogging", "PersistLoggingLevel failed: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingEnabledToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingEnabledToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingFlag(LoggingEnabledToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingEnabled", "LoggingEnabled toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingVerboseToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingVerboseToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("verbose", LoggingVerboseToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingVerbose", "Verbose toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingDebugToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingDebugToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("debug", LoggingDebugToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingDebug", "Debug toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingInformationToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingInformationToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("information", LoggingInformationToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingInformation", "Information toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingWarningToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingWarningToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("warning", LoggingWarningToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingWarning", "Warning toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingErrorToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingErrorToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("error", LoggingErrorToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingError", "Error toggle error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO LoggingCriticalToggle_Modal_Checked - ID: 20260122_013800
        private void LoggingCriticalToggle_Modal_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                PersistLoggingLevel("critical", LoggingCriticalToggle_Modal.IsChecked == true);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LoggingCritical", "Critical toggle error: " + ex.Message, ex);
            }
        }

        // ELIMINADOS: Radio_esAR_Checked, Radio_enUS_Checked, Radio_ptBR_Checked, Radio_frFR_Checked, Radio_deDE_Checked - ID: 20260126_020800
        // Razón: Grid dinámico - TODOS los event handlers generados en LoadIdiomaControls() (lambda en línea ~405)
        // Ya NO existen RadioButtons individuales (Radio_esAR, Radio_enUS, etc.) - reemplazados por generación dinámica

        // MODIFICADO METODO OnLanguageChanged - ID: 20260126_020900
        // Grid dinámico: buscar RadioButton activo en LanguageGrid.Children
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            try
            {
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LanguageChanged", "Language changed event - updating grid selection", null);

                var locService = AgenteIALocalVSIXPackage.LocalizationService;
                if (locService == null) return;

                var currentLang = locService.CurrentLanguageCode ?? "es-AR";

                // Recorrer grid dinámico y actualizar IsChecked
                try
                {
                    foreach (var child in LanguageGrid.Children)
                    {
                        var border = child as System.Windows.Controls.Border;
                        if (border == null) continue;

                        var stack = border.Child as System.Windows.Controls.StackPanel;
                        if (stack == null) continue;

                        // Buscar RadioButton (último hijo del StackPanel)
                        System.Windows.Controls.RadioButton radio = null;
                        foreach (var item in stack.Children)
                        {
                            radio = item as System.Windows.Controls.RadioButton;
                            if (radio != null) break;
                        }

                        if (radio == null) continue;

                        var langCode = radio.Tag as string;
                        if (string.IsNullOrEmpty(langCode)) continue;

                        // Actualizar IsChecked según idioma actual
                        var shouldCheck = string.Equals(langCode, currentLang, StringComparison.OrdinalIgnoreCase);
                        if (radio.IsChecked != shouldCheck)
                        {
                            _isInitializingAdvancedUi = true; // Evitar recursión
                            radio.IsChecked = shouldCheck;
                            _isInitializingAdvancedUi = false;
                        }
                    }
                }
                catch (Exception exGrid)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.LanguageChanged", "Grid update failed: " + exGrid.Message, exGrid);
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.LanguageChanged", $"Language change handled OK - current: {currentLang}", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.LanguageChanged", "OnLanguageChanged failed: " + ex.Message, ex);
            }
        }


        // MODIFICADO METODO ApplyModalLoggingToSettings - ID: 20260123_225609
        // E4: Aplicar TODOS los cambios de logging al presionar "Guardar" (usar DTOs)
        private void ApplyModalLoggingToSettings(GlobalSettings globalSettings)
        {
            if (globalSettings == null) return;
            try
            {
                // Master checkbox
                var enabled = LoggingEnabledToggle_Modal?.IsChecked == true;
                globalSettings.Logging.Enabled = enabled;

                // Si master enabled=true, todos los niveles son true (override)
                if (enabled)
                {
                    globalSettings.Logging.Verbose = true;
                    globalSettings.Logging.Debug = true;
                    globalSettings.Logging.Information = true;
                    globalSettings.Logging.Warning = true;
                    globalSettings.Logging.Error = true;
                    globalSettings.Logging.Critical = true;
                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", "SET logging.enabled=true (all levels forced to true)", null);
                }
                else
                {
                    // Leer valores individuales de cada checkbox
                    globalSettings.Logging.Verbose = LoggingVerboseToggle_Modal?.IsChecked == true;
                    globalSettings.Logging.Debug = LoggingDebugToggle_Modal?.IsChecked == true;
                    globalSettings.Logging.Information = LoggingInformationToggle_Modal?.IsChecked == true;
                    globalSettings.Logging.Warning = LoggingWarningToggle_Modal?.IsChecked == true;
                    globalSettings.Logging.Error = LoggingErrorToggle_Modal?.IsChecked == true;
                    globalSettings.Logging.Critical = LoggingCriticalToggle_Modal?.IsChecked == true;
                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET logging.enabled=false, individual levels: v={globalSettings.Logging.Verbose}, d={globalSettings.Logging.Debug}, i={globalSettings.Logging.Information}, w={globalSettings.Logging.Warning}, e={globalSettings.Logging.Error}, c={globalSettings.Logging.Critical}", null);
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.ApplyLogging", "ApplyModalLoggingToSettings failed: " + ex.Message, ex);
            }
        }




        // MODIFICADO METODO PersistRequestDefaultsFromUi - ID: 20260123_225610
        // Parse robusto usando DTOs: temperature (coma/punto + InvariantCulture), maxTokens (int>=0), includeUsage (bool)
        private void PersistRequestDefaultsFromUi()
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.GlobalSettings == null) settings.GlobalSettings = new GlobalSettings();

                // Force stream true
                settings.GlobalSettings.RequestDefaults.Stream = true;

                // streamOptions.includeUsage depends on provider
                // MODIFICADO - ID: 20260126_032500 - Leer Provider Tag invariante
                var cbi = ProviderCombo_Modal?.SelectedItem as ComboBoxItem;
                var providerTag = (cbi?.Tag as string ?? string.Empty).ToLowerInvariant();
                
                if (string.Equals(providerTag, "lmstudio", StringComparison.OrdinalIgnoreCase))
                {
                    settings.GlobalSettings.RequestDefaults.StreamOptions.IncludeUsage = IncludeUsageToggle_Modal.IsChecked == true;
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.RequestDefaults", $"IncludeUsage={IncludeUsageToggle_Modal.IsChecked} (provider Tag: {providerTag})", null);
                }
                else
                {
                    settings.GlobalSettings.RequestDefaults.StreamOptions.IncludeUsage = false;
                    AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.RequestDefaults", $"IncludeUsage=false (provider Tag: {providerTag})", null);
                }

                // temperature - Parse robusto: acepta coma/punto, usa InvariantCulture
                double? tempValue = null;
                try
                {
                    var tempText = (TemperatureTextBox_Modal.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(tempText))
                    {
                        // Normalizar coma a punto para InvariantCulture
                        tempText = tempText.Replace(',', '.');
                        double parsed;
                        if (double.TryParse(tempText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                        {
                            tempValue = parsed;
                        }
                        else
                        {
                            // Parse failed - loguear Warning
                            AgenteIALocal.Logging.Log.Warning("-", 9200, "ConfigModal.Parse", $"Temperature parse failed: invalid value '{TemperatureTextBox_Modal.Text}'. Keeping previous value.", null);
                        }
                    }
                    else
                    {
                        // Vacío - usar default 0.2
                        tempValue = 0.2;
                    }
                }
                catch (Exception exTemp)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9201, "ConfigModal.Parse", $"Temperature parse exception: {exTemp.Message}", exTemp);
                    tempValue = 0.2; // fallback
                }

                // Solo guardar si el parse fue exitoso
                if (tempValue.HasValue)
                {
                    settings.GlobalSettings.RequestDefaults.Temperature = tempValue.Value;
                }

                // maxTokens - Parse robusto: int >= 0, null si vacío
                int? maxTokensValue = null;
                try
                {
                    var maxText = (MaxTokensTextBox_Modal.Text ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(maxText))
                    {
                        int parsed;
                        if (int.TryParse(maxText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsed))
                        {
                            if (parsed >= 0)
                            {
                                maxTokensValue = parsed;
                            }
                            else
                            {
                                // Negativo - loguear Warning y no guardar
                                AgenteIALocal.Logging.Log.Warning("-", 9202, "ConfigModal.Parse", $"MaxTokens negative value: {parsed}. Must be >= 0. Keeping previous value.", null);
                            }
                        }
                        else
                        {
                            // Parse failed - loguear Warning
                            AgenteIALocal.Logging.Log.Warning("-", 9203, "ConfigModal.Parse", $"MaxTokens parse failed: invalid value '{MaxTokensTextBox_Modal.Text}'. Keeping previous value.", null);
                        }
                    }
                    else
                    {
                        // Vacío - guardar null (sin límite)
                        maxTokensValue = 0; // 0 = sin límite según convención del backend
                    }
                }
                catch (Exception exMax)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9204, "ConfigModal.Parse", $"MaxTokens parse exception: {exMax.Message}", exMax);
                    maxTokensValue = 0; // fallback
                }

                // Solo guardar si el parse fue exitoso
                if (maxTokensValue.HasValue)
                {
                    settings.GlobalSettings.RequestDefaults.MaxTokens = maxTokensValue.Value;
                }

                string provider = null;
                try { provider = settings.Servers?.Find(s => string.Equals(s.Id, settings.ActiveServerId, StringComparison.OrdinalIgnoreCase))?.Provider; } catch (Exception exFind) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.RequestDefaults", "Provider lookup failed: " + exFind.Message, exFind); }
                // DESHABILITADO live update - ID: 20260122_030100 - Solo SaveButton persiste
                // AgentSettingsStore.Save(settings);
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.RequestDefaults", $"RequestDefaults UI changed (temp={settings.GlobalSettings.RequestDefaults.Temperature}, max={settings.GlobalSettings.RequestDefaults.MaxTokens}) - NOT saved yet", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Persist", "PersistRequestDefaultsFromUi error: " + ex.Message, ex);
            }
        }

        private void TemperatureTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            // no-op; persist on LostFocus
        }

        private void TemperatureTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            PersistRequestDefaultsFromUi();
        }

        private void MaxTokensTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializingAdvancedUi) return;
            // no-op; persist on LostFocus
        }

        private void MaxTokensTextBox_Modal_LostFocus(object sender, RoutedEventArgs e)
        {
            PersistRequestDefaultsFromUi();
        }

        // NUEVO METODO ParseMaxSteps - ID: 20250304_170013
        private static int ParseMaxSteps(string text)
        {
            int value;
            if (int.TryParse(text, out value) && value >= 1)
            {
                return value;
            }

            return 5;
        }

        // MODIFICADO METODO GetSelectedComboContent - ID: 20260126_031300
        // FIX CRÍTICO: Extraer texto desde TextBlock.Text cuando Content es TextBlock
        // Causa raíz bug: cbi.Content as string retornaba "" → SaveButton_Click guardaba valores vacíos
        private static string GetSelectedComboContent(ComboBox combo)
        {
            if (combo == null) return string.Empty;
            var item = combo.SelectedItem;
            var cbi = item as ComboBoxItem;
            
            if (cbi != null)
            {
                // NUEVO - ID: 20260126_031300 - Extraer texto desde TextBlock.Text si Content es TextBlock
                var textBlock = cbi.Content as TextBlock;
                if (textBlock != null)
                {
                    return textBlock.Text ?? string.Empty;
                }
                
                // Fallback: Content directo como string
                return cbi.Content as string ?? string.Empty;
            }
            
            var s = item as string;
            if (!string.IsNullOrEmpty(s)) return s;
            return combo.Text ?? string.Empty;
        }

        // NUEVO METODO FireAndForget - ID: 20250310_000001
        private void FireAndForget(Task task, string op)
        {
            if (task == null) return;

            _ = task.ContinueWith(t =>
            {
                var ex = t.Exception != null ? t.Exception.GetBaseException() : null;
                if (ex != null)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.FireAndForget", op + " failed: " + ex.Message, ex);
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        // NUEVO METODO HandleLoadedAsync - ID: 20250310_000002
        private async Task HandleLoadedAsync()
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings == null) return;

                // Suppress baseUrl change handler while we hydrate controls to avoid races
                _isInitializingAdvancedUi = true;
                _suppressBaseUrlTextChanged_20260116 = true;
                try
                {
                    var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : settings.ActiveServerId;
                    var srv = (settings.Servers ?? new List<ServerConfig>()).Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));

                    ActiveServerIdTextBox_Modal.Text = targetId ?? string.Empty;
                    ServerBaseUrlTextBox_Modal.Text = srv != null ? srv.BaseUrl : string.Empty;
                    _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                    ServerApiKeyTextBox_Modal.Text = srv != null ? srv.ApiKey : string.Empty;

                    LoadAdvancedControls(settings, srv);
                    WireAdvancedHandlersOnce();

                    try
                    {
                        ServerBaseUrlTextBox_Modal.LostFocus -= ServerBaseUrl_LostFocus;
                        ServerBaseUrlTextBox_Modal.LostFocus += ServerBaseUrl_LostFocus;
                    }
                    catch (Exception exWire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Loaded", "ServerBaseUrl LostFocus wire failed: " + exWire.Message, exWire); }
                }
                finally
                {
                    _suppressBaseUrlTextChanged_20260116 = false;
                    _isInitializingAdvancedUi = false;
                }

                // After init, if there's a baseUrl, trigger a single evaluation (do not run parallel fetches here)
                var baseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    try { FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged.OnLoaded"); } catch (Exception exFire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Loaded", "FireAndForget BaseUrlTextChanged failed: " + exFire.Message, exFire); }
                }
            }
            catch (Exception exOuter)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Loaded", "ConfigWindow Loaded: unexpected error: " + exOuter.Message, exOuter);
            }
        }

        // NUEVO METODO HandleServerBaseUrlLostFocusAsync - ID: 20250310_000003
        private Task HandleServerBaseUrlLostFocusAsync()
        {
            return Task.CompletedTask;
        }

        // NUEVO METODO HandleBaseUrlTextChangedAsync - ID: 20250310_000004
        // MODIFICADO METODO HandleBaseUrlTextChangedAsync - ID: 20260123_000006
        // Acepta modelo persistido para preservar selección después de fetch
        private async Task HandleBaseUrlTextChangedAsync(string persistedModel = null)
        {
            try
            {
                // Respect suppression: do not run when suppress flag set
                if (_suppressBaseUrlTextChanged_20260116 || _isInitializingAdvancedUi) return;

                if (_baseUrlPingCts != null)
                {
                    _baseUrlPingCts.Cancel();
                    _baseUrlPingCts.Dispose();
                }
                _baseUrlPingCts = new CancellationTokenSource();
                var ct = _baseUrlPingCts.Token;

                var baseUrl = (ServerBaseUrlTextBox_Modal.Text ?? string.Empty).Trim();
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Collapsed;
                ServerBaseUrlTextBox_Modal.ToolTip = null;

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    try
                    {
                        ServerModelCombo_Modal.Items.Clear();
                        ServerModelCombo_Modal.SelectedItem = null;
                    }
                    catch (Exception exClear)
                    {
                        AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo clear failed (empty baseUrl): " + exClear.Message, exClear);
                    }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged event failed: " + exEvent.Message, exEvent); }
                    return;
                }

                Uri uriResult;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uriResult))
                {
                    ShowBaseUrlError("URL inválida");
                    try
                    {
                        ServerModelCombo_Modal.Items.Clear();
                        ServerModelCombo_Modal.SelectedItem = null;
                    }
                    catch (Exception exClear)
                    {
                        AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo clear failed (invalid URL): " + exClear.Message, exClear);
                    }
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged event failed: " + exEvent.Message, exEvent); }
                    return;
                }

                // MODIFICADO HandleBaseUrlTextChangedAsync: use unified endpoint builder + auth header + fallback host - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
                var baseUri = NormalizeBaseUri(baseUrl);
                if (baseUri == null)
                {
                    ShowBaseUrlError("URL inválida");
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged event failed (null baseUri): " + exEvent.Message, exEvent); }
                    return;
                }

                var primary = BuildModelsUri(baseUri);
                var fallbackHost = TryGetFallbackHost(baseUri);

                // If models endpoint cannot be built (baseUrl not yet complete/valid), do not perform HTTP or log error.
                if (string.IsNullOrWhiteSpace(primary))
                {
                    try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged event failed (empty primary): " + exEvent.Message, exEvent); }
                    return;
                }

                var myFetchVersion = System.Threading.Interlocked.Increment(ref _modelsFetchVersion); // NUEVO: version tag for this fetch - ID: 20260115_220500
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    HttpResponseMessage resp = null;
                    Exception firstEx = null;

                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Get, primary))
                        {
                            req.Headers.Accept.Clear();
                            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            try
                            {
                                var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                apiKey = apiKey.Trim();
                                if (!string.IsNullOrWhiteSpace(apiKey))
                                {
                                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                }
                            }
                            catch { }

                            resp = await client.SendAsync(req, ct).ConfigureAwait(true);
                        }
                    }
                    catch (Exception exPrimary)
                    {
                        firstEx = exPrimary;
                    }

                    // If primary failed with connection refused and a fallback host is available, retry
                    if (firstEx != null && IsConnectionRefused(firstEx) && !string.IsNullOrEmpty(fallbackHost))
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.ModelsFetch", "primary GET failed, retrying with fallback host", null);
                        var altBuilder = new UriBuilder(baseUri) { Host = fallbackHost };
                        var alt = BuildModelsUri(altBuilder.Uri);
                        try
                        {
                            using (var req = new HttpRequestMessage(HttpMethod.Get, alt))
                            {
                                req.Headers.Accept.Clear();
                                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                try
                                {
                                    var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                    apiKey = apiKey.Trim();
                                    if (!string.IsNullOrWhiteSpace(apiKey))
                                    {
                                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                    }
                                }
                                catch { }

                                resp = await client.SendAsync(req, ct).ConfigureAwait(true);
                                firstEx = null;

                                // Update textbox to effective host so user sees working host (do not persist)
                                try { _suppressBaseUrlTextChanged_20260116 = true; ServerBaseUrlTextBox_Modal.Text = altBuilder.Uri.ToString().TrimEnd('/'); } catch (Exception exUpdate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Textbox update (fallback) failed: " + exUpdate.Message, exUpdate); } finally { _suppressBaseUrlTextChanged_20260116 = false; }
                            }
                        }
                        catch (Exception)
                        {
                            // both attempts failed -> try offline fallback using persisted model
                            try { ShowBaseUrlError("Servidor no responde (/v1/models)"); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError failed: " + exShow.Message, exShow); }
                            try
                            {
                                var offlineModels = new System.Collections.Generic.List<string>();
                                try
                                {
                                    var settings = AgentSettingsStore.Load();
                                    if (settings != null && settings.Servers != null)
                                    {
                                        var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                        if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                    }
                                }
                                catch (Exception exLoad) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.BaseUrlChanged", "Offline models load (both failed) failed: " + exLoad.Message, exLoad); }

                                if (offlineModels.Count > 0)
                                {
                                    try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch (Exception exPopulate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo populate (both failed) failed: " + exPopulate.Message, exPopulate); }
                                }

                                try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (both failed) failed: " + exEvent.Message, exEvent); }
                            }
                            catch (Exception) { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch (Exception exEvent2) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Fallback event (both failed) failed: " + exEvent2.Message, exEvent2); } }
                            return;
                        }
                    }

                    if (firstEx != null)
                    {
                        // If cancelled, do not touch UI
                        if (ct.IsCancellationRequested) return;
                        try { ShowBaseUrlError(firstEx.Message); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError (firstEx) failed: " + exShow.Message, exShow); }
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch (Exception exLoad) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.BaseUrlChanged", "Offline models load (firstEx) failed: " + exLoad.Message, exLoad); }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch (Exception exPopulate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo populate (firstEx) failed: " + exPopulate.Message, exPopulate); }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (firstEx) failed: " + exEvent.Message, exEvent); }
                        }
                        catch (Exception) { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch (Exception exEvent2) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Fallback event (firstEx) failed: " + exEvent2.Message, exEvent2); } }
                        return;
                    }

                    if (resp == null)
                    {
                        // stale check
                        if (myFetchVersion != _modelsFetchVersion) return; // discard
                        try { ShowBaseUrlError("Servidor no responde (/v1/models)"); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError (resp==null) failed: " + exShow.Message, exShow); }
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch { }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch (Exception exPopulate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo populate (resp==null) failed: " + exPopulate.Message, exPopulate); }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (resp==null) failed: " + exEvent.Message, exEvent); }
                        }
                        catch (Exception) { try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Fallback event (resp==null) failed: " + exEvent.Message, exEvent); } }
                        return;
                    }

                    // Handle auth vs other failures vs success
                    if (resp.IsSuccessStatusCode)
                    {
                        // stale check
                        if (myFetchVersion != _modelsFetchVersion) return; // discard if newer fetch started

                        HideBaseUrlError();

                        List<string> models = new List<string>();
                        try
                        {
                            models = await FetchModelsAsync(baseUrl).ConfigureAwait(true);
                        }
                        catch (Exception exFetch) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "FetchModelsAsync (success) failed: " + exFetch.Message, exFetch); }

                        // If fetch returned models, update cache and UI; if not, try using cache or persisted model
                        if (models != null && models.Count > 0)
                        {
                            try { _modelsCacheByServerId.AddOrUpdate(ActiveServerIdTextBox_Modal.Text ?? string.Empty, (k) => new System.Collections.Generic.List<string>(models), (k, v) => new System.Collections.Generic.List<string>(models)); } catch (Exception exCache) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Models cache update failed: " + exCache.Message, exCache); }
                            try
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);
                                
                                // MODIFICADO - ID: 20260123_000006 - Preservar modelo persistido
                                // Si hay un modelo persistido y está en la lista, seleccionarlo
                                // Si no, seleccionar el primero
                                if (!string.IsNullOrWhiteSpace(persistedModel) && models.Contains(persistedModel))
                                {
                                    ServerModelCombo_Modal.SelectedItem = persistedModel;
                                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.BaseUrlChanged", $"Reselected persisted model: {persistedModel}", null);
                                }
                                else
                                {
                                    ServerModelCombo_Modal.SelectedIndex = 0;
                                    if (!string.IsNullOrWhiteSpace(persistedModel))
                                    {
                                        AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", $"Persisted model '{persistedModel}' not found in server models - selected first available", null);
                                    }
                                }
                            }
                            catch (Exception exPopulate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo populate (success) failed: " + exPopulate.Message, exPopulate); }
                        }
                        else
                        {
                            // no models returned: fallback to cache or persisted model
                            var sid = (ActiveServerIdTextBox_Modal.Text ?? string.Empty);
                            System.Collections.Generic.List<string> cached = null;
                            _modelsCacheByServerId.TryGetValue(sid, out cached);
                            if (cached != null && cached.Count > 0)
                            {
                                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Using cached models for server " + sid, null);
                                try
                                {
                                    ServerModelCombo_Modal.Items.Clear();
                                    foreach (var m in cached) ServerModelCombo_Modal.Items.Add(m);
                                    
                                    // MODIFICADO - ID: 20260123_000006 - Preservar modelo persistido desde cache
                                    if (!string.IsNullOrWhiteSpace(persistedModel) && cached.Contains(persistedModel))
                                    {
                                        ServerModelCombo_Modal.SelectedItem = persistedModel;
                                    }
                                    else
                                    {
                                        ServerModelCombo_Modal.SelectedIndex = 0;
                                    }
                                }
                                catch (Exception exCached) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo from cache failed: " + exCached.Message, exCached); }
                            }
                            else
                            {
                                // fallback to persisted single model if exists
                                try
                                {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model))
                                    {
                                        try
                                        {
                                            if (AgenteIALocalControl.IsChatModelId(srv.Model))
                                            {
                                                ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.Items.Add(srv.Model); ServerModelCombo_Modal.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                // persisted model is not chat-capable -> do not inject, show error
                                                try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch (Exception exClear) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo clear (embedding) failed: " + exClear.Message, exClear); }
                                                try { ShowBaseUrlError("Modelo no compatible (embedding)"); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError (embedding) failed: " + exShow.Message, exShow); }
                                            }
                                        }
                                        catch (Exception exModel) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "IsChatModelId check failed: " + exModel.Message, exModel); }
                                    }
                                }
                                }
                                catch (Exception exPersisted) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.BaseUrlChanged", "Persisted model load (success) failed: " + exPersisted.Message, exPersisted); }
                            }
                        }

                        try { BaseUrlHealthChanged?.Invoke(true, baseUrl, models); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (success) failed: " + exEvent.Message, exEvent); }
                    }
                    else if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized || resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        // Auth issue: clear combo and show error
                        try { ShowBaseUrlError("No autorizado: revisa API Key"); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError (auth) failed: " + exShow.Message, exShow); }
                        try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch (Exception exClear) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo clear (auth) failed: " + exClear.Message, exClear); }
                        try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new List<string>()); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (auth) failed: " + exEvent.Message, exEvent); }
                    }
                    else
                    {
                        try { ShowBaseUrlError("Servidor responde " + resp.StatusCode); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ShowBaseUrlError (non-success) failed: " + exShow.Message, exShow); }
                        // non-success (other than auth): try offline fallback before clearing
                        try
                        {
                            var offlineModels = new System.Collections.Generic.List<string>();
                            try
                            {
                                var settings = AgentSettingsStore.Load();
                                if (settings != null && settings.Servers != null)
                                {
                                    var srv = settings.Servers.Find(s => string.Equals(s.Id, ActiveServerIdTextBox_Modal.Text, StringComparison.OrdinalIgnoreCase));
                                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model)) offlineModels.Add(srv.Model);
                                }
                            }
                            catch (Exception exLoad) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.BaseUrlChanged", "Offline models load (non-success) failed: " + exLoad.Message, exLoad); }

                            if (offlineModels.Count > 0)
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); foreach (var m in offlineModels) ServerModelCombo_Modal.Items.Add(m); ServerModelCombo_Modal.SelectedIndex = 0; } catch (Exception exPopulate) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo populate (non-success) failed: " + exPopulate.Message, exPopulate); }
                            }
                            else
                            {
                                try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch (Exception exClear) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "ModelCombo clear (non-success else) failed: " + exClear.Message, exClear); }
                            }
                            try { BaseUrlHealthChanged?.Invoke(false, baseUrl, offlineModels); } catch (Exception exEvent) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "BaseUrlHealthChanged (non-success) failed: " + exEvent.Message, exEvent); }
                        }
                        catch (Exception) { try { ServerModelCombo_Modal.Items.Clear(); ServerModelCombo_Modal.SelectedItem = null; } catch (Exception exClear2) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Final clear failed: " + exClear2.Message, exClear2); } try { BaseUrlHealthChanged?.Invoke(false, baseUrl, new System.Collections.Generic.List<string>()); } catch (Exception exEvent2) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BaseUrlChanged", "Final event failed: " + exEvent2.Message, exEvent2); } }
                    }
                }
            }
            catch (Exception exMain)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.BaseUrlChanged", "HandleBaseUrlTextChangedAsync failed: " + exMain.Message, exMain);
            }
        }

        // NUEVO METODO ApplyServerToUi - ID: 20250304_170015
        // MODIFICADO - ID: 20260123_000006 - Preservar modelo guardado después de fetch
        private void ApplyServerToUi(string serverId, ServerConfig srv)
        {
            // NUEVO - suppress BaseUrl change handler while hydrating UI - ID: 20260115_223100
            _suppressBaseUrlTextChanged_20260116 = true;
            try
            {
                ActiveServerIdTextBox_Modal.Text = serverId ?? string.Empty;
                ServerBaseUrlTextBox_Modal.Text = srv != null ? srv.BaseUrl : string.Empty;
                _lastPersistedBaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                ServerApiKeyTextBox_Modal.Text = srv != null ? srv.ApiKey : string.Empty;

                // CRÍTICO - ID: 20260123_000006
                // Guardar el modelo persistido ANTES del fetch asíncrono
                var persistedModel = srv != null ? srv.Model : string.Empty;

                try
                {
                    ServerModelCombo_Modal.Items.Clear();
                    if (srv != null && !string.IsNullOrWhiteSpace(srv.Model))
                    {
                        ServerModelCombo_Modal.Items.Add(srv.Model);
                        ServerModelCombo_Modal.SelectedItem = srv.Model;
                    }
                }
                catch (Exception exModel) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.ApplyServer", "ModelCombo init failed: " + exModel.Message, exModel); }
            }
            finally
            {
                _suppressBaseUrlTextChanged_20260116 = false;
            }

            // Fire a single evaluation of the base URL after hydration
            // MODIFICADO - ID: 20260123_000006 - Pasar modelo persistido para preservar selección
            try
            {
                var persistedModel = srv != null ? srv.Model : string.Empty;
                FireAndForget(HandleBaseUrlTextChangedAsync(persistedModel), "ConfigModal.BaseUrlTextChanged.ApplyServerToUi");
            }
            catch (Exception exFire) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.ApplyServer", "FireAndForget failed: " + exFire.Message, exFire); }
        }

        private void ServerBaseUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            FireAndForget(HandleServerBaseUrlLostFocusAsync(), "ConfigWindow.ServerBaseUrl.LostFocus");
        }

        // NUEVO METODO ServerBaseUrlTextBox_Modal_TextChanged - ID: 20260114_000074
        private void ServerBaseUrlTextBox_Modal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressBaseUrlTextChanged_20260116 || _isInitializingAdvancedUi) return;
            FireAndForget(HandleBaseUrlTextChangedAsync(), "ConfigModal.BaseUrlTextChanged");
        }

        // MODIFICADO METODO PersistBaseUrlIfChanged - ID: 20260122_000001
        // Fix A5: persiste SOLO BaseUrl (sin tocar globalSettings)
        private void PersistBaseUrlIfChanged(string baseUrl)
        {
            if (_isInitializingAdvancedUi) return;
            try
            {
                if (string.Equals(baseUrl, _lastPersistedBaseUrl, StringComparison.Ordinal)) return;

                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                var targetId = ActiveServerIdTextBox_Modal.Text;
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    var firstServer = settings.Servers.FirstOrDefault();
                    targetId = settings.ActiveServerId ?? (firstServer != null ? firstServer.Id : string.Empty);
                }
                if (string.IsNullOrWhiteSpace(targetId)) return;

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    var firstServer = settings.Servers.FirstOrDefault();
                    var provider = firstServer != null ? firstServer.Provider : "lmstudio";
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = provider, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = baseUrl;
                // FIX A5: NO llamar ApplyModalGlobalsToSettings aquí - solo persiste BaseUrl
                // ApplyModalGlobalsToSettings se ejecuta en SaveButton_Click para persistir todos los globals

                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);
                _lastPersistedBaseUrl = baseUrl;
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: BaseUrl persisted (live update) '" + baseUrl + "' for server '" + targetId + "'", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: PersistBaseUrlIfChanged error: " + ex.Message, ex);
            }
        }

        // NUEVO METODO ShowBaseUrlError - ID: 20260114_000076
        private void ShowBaseUrlError(string message)
        {
            try
            {
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(message)) ServerBaseUrlTextBox_Modal.ToolTip = message;
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.ShowError", "ShowBaseUrlError failed: " + ex.Message, ex); }
        }

        // NUEVO METODO HideBaseUrlError - ID: 20260114_000077
        private void HideBaseUrlError()
        {
            try
            {
                ServerBaseUrlPingErrorText_Modal.Visibility = Visibility.Collapsed;
                ServerBaseUrlTextBox_Modal.ToolTip = null;
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.HideError", "HideBaseUrlError failed: " + ex.Message, ex); }
        }

        // NUEVO METODO NormalizeBaseUri - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static Uri NormalizeBaseUri(string baseUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return null;
                Uri uri;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri))
                {
                    if (Uri.TryCreate("http://" + baseUrl, UriKind.Absolute, out uri)) return uri;
                    return null;
                }
                return uri;
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Normalize", "NormalizeBaseUri failed: " + ex.Message, ex); return null; }
        }

        // NUEVO METODO BuildModelsUri - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static string BuildModelsUri(Uri baseUri)
        {
            try
            {
                if (baseUri == null) return null;
                var s = baseUri.ToString().TrimEnd('/');
                var lower = s.ToLowerInvariant();
                if (lower.EndsWith("/v1/models")) return s; // already complete
                if (lower.EndsWith("/v1")) return s + "/models";
                return s + "/v1/models";
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.BuildUri", "BuildModelsUri failed: " + ex.Message, ex); return null; }
        }

        // NUEVO METODO TryGetFallbackHost - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static string TryGetFallbackHost(Uri baseUri)
        {
            try
            {
                if (baseUri == null) return null;
                var host = baseUri.Host ?? string.Empty;
                if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) return "localhost";
                if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return "127.0.0.1";
                return null;
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fallback", "TryGetFallbackHost failed: " + ex.Message, ex); return null; }
        }

        // NUEVO METODO IsConnectionRefused - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
        private static bool IsConnectionRefused(Exception ex)
        {
            try
            {
                if (ex == null) return false;
                var ae = ex as AggregateException;
                if (ae != null) ex = ae.GetBaseException();

                var httpEx = ex as HttpRequestException;
                if (httpEx != null)
                {
                    var inner = httpEx.InnerException;
                    if (inner != null) ex = inner;
                }

                var socketEx = ex as System.Net.Sockets.SocketException;
                if (socketEx != null)
                {
                    if (socketEx.ErrorCode == 10061) return true;
                }

                if (ex.InnerException != null) return IsConnectionRefused(ex.InnerException);

                var msg = ex.Message ?? string.Empty;
                if (msg.IndexOf("Connection refused", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (msg.IndexOf("ECONNREFUSED", StringComparison.OrdinalIgnoreCase) >= 0) return true;

                return false;
            }
            catch (Exception exOuter) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.ConnRefused", "IsConnectionRefused failed: " + exOuter.Message, exOuter); return false; }
        }

        private async Task<List<string>> FetchModelsAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;
                // MODIFICADO FetchModelsAsync - ID: GENERAR_1_ID_YYYYMMDD_HHMMSS_Y_REUTILIZAR
                var baseUri = NormalizeBaseUri(baseUrl);
                if (baseUri == null) return result;

                var primary = BuildModelsUri(baseUri);
                var fallbackHost = TryGetFallbackHost(baseUri);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync GET " + primary, null);

                    HttpResponseMessage resp = null;
                    Exception firstEx = null;
                    try
                    {
                        using (var req = new HttpRequestMessage(HttpMethod.Get, primary))
                        {
                            req.Headers.Accept.Clear();
                            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            try
                            {
                                var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                apiKey = apiKey.Trim();
                                if (!string.IsNullOrWhiteSpace(apiKey))
                                {
                                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                }
                            }
                            catch (Exception exAuth) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fetch", "Authorization header (fetch primary) failed: " + exAuth.Message, exAuth); }

                            resp = await client.SendAsync(req).ConfigureAwait(true);
                        }
                    }
                    catch (Exception exPrimary)
                    {
                        firstEx = exPrimary;
                    }

                    // If primary attempt threw connection refused and we have a fallback host, try alternate host
                    if (firstEx != null && IsConnectionRefused(firstEx) && !string.IsNullOrEmpty(fallbackHost))
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: primary GET failed with connection refused, will retry with fallback host", null);
                        var altBuilder = new UriBuilder(baseUri) { Host = fallbackHost };
                        var alt = BuildModelsUri(altBuilder.Uri);
                        try
                        {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync fallback GET " + alt, null);
                        try
                        {
                            using (var req = new HttpRequestMessage(HttpMethod.Get, alt))
                            {
                                req.Headers.Accept.Clear();
                                req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                                try
                                {
                                    var apiKey = (ServerApiKeyTextBox_Modal != null ? ServerApiKeyTextBox_Modal.Text : null) ?? string.Empty;
                                    apiKey = apiKey.Trim();
                                    if (!string.IsNullOrWhiteSpace(apiKey))
                                    {
                                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                                    }
                                }
                                catch (Exception exAuth) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fetch", "Authorization header (fallback) failed: " + exAuth.Message, exAuth); }

                                resp = await client.SendAsync(req).ConfigureAwait(true);
                                firstEx = null; // mark fallback attempted
                            }
                        }
                        catch (Exception exAlt)
                        {
                            // fallback also failed
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync both primary and fallback failed: " + exAlt.Message, exAlt);
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch (Exception exFilter) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fetch", "FilterChatModels failed: " + exFilter.Message, exFilter); }
                            return result;
                        }
                        }
                        catch (Exception exAlt)
                        {
                            // fallback also failed
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync both primary and fallback failed (outer): " + exAlt.Message, exAlt);
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch (Exception exFilter) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fetch", "FilterChatModels (both failed outer) failed: " + exFilter.Message, exFilter); }
                            return result;
                        }
                    }

                    if (firstEx != null)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync error: " + firstEx.Message, firstEx);
                        return result;
                    }

                    if (resp == null)
                    {
                        return result;
                    }

                    if (!resp.IsSuccessStatusCode)
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync non-success status: " + resp.StatusCode, null);
                        return result;
                    }

                    var txt = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);
                    if (string.IsNullOrWhiteSpace(txt)) return result;

                    try
                    {
                        var root = Newtonsoft.Json.Linq.JToken.Parse(txt);
                        var data = root["data"] as Newtonsoft.Json.Linq.JArray;
                        if (data != null)
                        {
                            foreach (var item in data)
                            {
                                try
                                {
                                    var id = item.Value<string>("id");
                                    if (!string.IsNullOrEmpty(id)) result.Add(id);
                                }
                                catch (Exception exItem) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Fetch", "Parse data item failed: " + exItem.Message, exItem); }
                            }
                            try { AgenteIALocalControl.FilterChatModelsInPlace(result); } catch (Exception exFilter) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Fetch", "FilterChatModels (data) failed: " + exFilter.Message, exFilter); }
                            return result;
                        }

                        var models = root["models"] as Newtonsoft.Json.Linq.JArray;
                        if (models != null)
                        {
                            foreach (var item in models)
                            {
                                try
                                {
                                    var id = item.Value<string>("id") ?? item.ToString();
                                    if (!string.IsNullOrEmpty(id)) result.Add(id);
                                }
                                catch (Exception exItem) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.Fetch", "Parse models item failed: " + exItem.Message, exItem); }
                            }
                            return result;
                        }

                        var arr = root as Newtonsoft.Json.Linq.JArray;
                        if (arr != null)
                        {
                            foreach (var item in arr)
                            {
                                var s = item.ToString();
                                if (!string.IsNullOrEmpty(s)) result.Add(s);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync parse error: " + ex.Message, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: FetchModelsAsync error: " + ex.Message, ex);
            }

            return result;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Save start", null);
            
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                var activeIdInput = ActiveServerIdTextBox_Modal.Text ?? string.Empty;
                var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : activeIdInput;
                if (string.IsNullOrEmpty(targetId))
                {
                    targetId = settings.ActiveServerId ?? (settings.Servers.Count > 0 ? settings.Servers[0].Id : string.Empty);
                    if (string.IsNullOrEmpty(targetId)) targetId = activeIdInput;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    var first = settings.Servers.FirstOrDefault();
                    var provider = first != null ? first.Provider : "lmstudio";
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = provider, CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                srv.ApiKey = ServerApiKeyTextBox_Modal.Text ?? string.Empty;

                string selectedModel = null;
                try
                {
                    selectedModel = (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.SelectedItem as string : null)
                                    ?? (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.SelectedValue as string : null)
                                    ?? (ServerModelCombo_Modal != null ? ServerModelCombo_Modal.Text : null);
                }
                catch (Exception exModel) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Save", "Model selection read failed: " + exModel.Message, exModel); }
                selectedModel = (selectedModel ?? string.Empty).Trim();

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Save activeId='" + targetId + "', baseUrlPresent=" + (!string.IsNullOrWhiteSpace(srv.BaseUrl)).ToString() + ", modelPresent=" + (string.IsNullOrEmpty(selectedModel) ? "false" : "true"), null);

                if (!string.IsNullOrEmpty(selectedModel))
                {
                    // MODIFICADO METODO SaveButton_Click - ID: 20260117_132400
                    // Do not persist embedding/non-chat models
                    if (AgenteIALocalControl.IsChatModelId(selectedModel))
                    {
                        srv.Model = selectedModel;
                    }
                    else
                    {
                        // do not persist; clear selection and inform user via UI error
                        srv.Model = string.Empty;
                        try { ShowBaseUrlError("Modelo no compatible (embedding) - no guardado"); } catch (Exception exShow) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Save", "ShowBaseUrlError failed: " + exShow.Message, exShow); }
                    }
                }

                settings.ActiveServerId = targetId;
                
                // MODIFICADO SaveButton_Click - ID: 20260123_225611
                // Usar DTOs tipados en lugar de JObject - UI sin lógica JSON
                try
                {
                    // Asegurar GlobalSettings existe
                    if (settings.GlobalSettings == null)
                        settings.GlobalSettings = new GlobalSettings();

                    // runMode desde RunModeCombo_Modal
                    try
                    {
                        // MODIFICADO - ID: 20260126_032000 - Leer Tag invariante (NO texto traducido)
                        var cbi = RunModeCombo_Modal?.SelectedItem as ComboBoxItem;
                        string runMode = "preguntar"; // default
                        
                        if (cbi != null)
                        {
                            var tag = cbi.Tag as string;
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                runMode = tag.ToLowerInvariant();
                            }
                        }
                        
                        settings.GlobalSettings.RunMode = runMode;
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET runMode = {runMode} (from UI Tag)", null);
                    }
                    catch (Exception exRunMode)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set runMode: " + exRunMode.Message, exRunMode);
                    }
                    
                    // requestDefaults desde controles de temperatura/maxTokens/includeUsage
                    try
                    {
                        settings.GlobalSettings.RequestDefaults.Stream = true; // siempre true
                        
                        // temperature
                        try
                        {
                            var tempText = TemperatureTextBox_Modal?.Text ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(tempText))
                            {
                                var normalized = tempText.Replace(',', '.');
                                double tempVal;
                                if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tempVal))
                                {
                                    settings.GlobalSettings.RequestDefaults.Temperature = tempVal;
                                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET temperature = {tempVal} (from UI: {tempText})", null);
                                }
                            }
                        }
                        catch (Exception exTemp)
                        {
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set temperature: " + exTemp.Message, exTemp);
                        }
                        
                        // maxTokens
                        try
                        {
                            var maxText = MaxTokensTextBox_Modal?.Text ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(maxText))
                            {
                                int maxVal;
                                if (int.TryParse(maxText, out maxVal) && maxVal >= 0)
                                {
                                    settings.GlobalSettings.RequestDefaults.MaxTokens = maxVal;
                                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET maxTokens = {maxVal} (from UI: {maxText})", null);
                                }
                            }
                        }
                        catch (Exception exMax)
                        {
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set maxTokens: " + exMax.Message, exMax);
                        }
                        
                        // includeUsage (solo LM Studio)
                        try
                        {
                            // MODIFICADO - ID: 20260126_032300 - Leer Provider Tag invariante
                            var cbi = ProviderCombo_Modal?.SelectedItem as ComboBoxItem;
                            var providerTag = (cbi?.Tag as string ?? string.Empty).ToLowerInvariant();
                            var isLmStudio = string.Equals(providerTag, "lmstudio", StringComparison.OrdinalIgnoreCase);
                            
                            if (isLmStudio && IncludeUsageToggle_Modal != null)
                            {
                                settings.GlobalSettings.RequestDefaults.StreamOptions.IncludeUsage = IncludeUsageToggle_Modal.IsChecked == true;
                                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET includeUsage = {IncludeUsageToggle_Modal.IsChecked} (provider Tag: {providerTag})", null);
                            }
                            else
                            {
                                settings.GlobalSettings.RequestDefaults.StreamOptions.IncludeUsage = false;
                                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET includeUsage = false (provider Tag: {providerTag})", null);
                            }
                        }
                        catch (Exception exUsage)
                        {
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set includeUsage: " + exUsage.Message, exUsage);
                        }
                    }
                    catch (Exception exReqDef)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set requestDefaults: " + exReqDef.Message, exReqDef);
                    }
                    
                    // agent desde controles ideIntegration/applyChanges/maxSteps
                    try
                    {
                        settings.GlobalSettings.Agent.IdeIntegration = AgentIdeIntegrationToggle_Modal?.IsChecked == true;
                        settings.GlobalSettings.Agent.ApplyChanges = AgentApplyChangesToggle_Modal?.IsChecked == true;
                        
                        try
                        {
                            var maxStepsText = AgentMaxStepsTextBox_Modal?.Text ?? "5";
                            var maxStepsVal = ParseMaxSteps(maxStepsText);
                            settings.GlobalSettings.Agent.MaxSteps = maxStepsVal;
                            AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"SET agent.ideIntegration={settings.GlobalSettings.Agent.IdeIntegration}, applyChanges={settings.GlobalSettings.Agent.ApplyChanges}, maxSteps={maxStepsVal}", null);
                        }
                        catch
                        {
                            settings.GlobalSettings.Agent.MaxSteps = 5;
                        }
                    }
                    catch (Exception exAgent)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to set agent: " + exAgent.Message, exAgent);
                    }
                    
                    // NUEVO - ID: 20260122_014100 - E4: Aplicar logging settings desde UI
                    try
                    {
                        ApplyModalLoggingToSettings(settings.GlobalSettings);
                    }
                    catch (Exception exLogging)
                    {
                        AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Failed to apply logging settings: " + exLogging.Message, exLogging);
                    }
                }
                catch (Exception exGlobals)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Save", "Failed to persist GlobalSettings from UI: " + exGlobals.Message, exGlobals);
                }
                
                // LOG CRÍTICO - Antes de Save
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"CALLING AgentSettingsStore.Save() - ActiveServerId={settings.ActiveServerId}", null);
                
                AgentSettingsStore.Save(settings);
                
                // LOG CRÍTICO - Después de Save
                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", "AgentSettingsStore.Save() COMPLETED", null);

                // MODIFICADO - ID: 20260123_225612 - F2: Reconfigurar Serilog pipeline (usar DTOs)
                try
                {
                    var loggingSettings = new AgenteIALocal.Logging.LogSettings
                    {
                        Enabled = settings.GlobalSettings.Logging.Enabled,
                        All = false, // No usamos "all" - usamos "enabled" como master
                        Verbose = settings.GlobalSettings.Logging.Verbose,
                        Debug = settings.GlobalSettings.Logging.Debug,
                        Information = settings.GlobalSettings.Logging.Information,
                        Warning = settings.GlobalSettings.Logging.Warning,
                        Error = settings.GlobalSettings.Logging.Error,
                        Critical = settings.GlobalSettings.Logging.Critical
                    };

                    // Si enabled=true (master), forzar todos los niveles a true
                    if (loggingSettings.Enabled && settings.GlobalSettings.Logging.Enabled)
                    {
                        loggingSettings.Verbose = true;
                        loggingSettings.Debug = true;
                        loggingSettings.Information = true;
                        loggingSettings.Warning = true;
                        loggingSettings.Error = true;
                        loggingSettings.Critical = true;
                    }

                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", $"CALLING Log.Reconfigure() - enabled={loggingSettings.Enabled}, levels: v={loggingSettings.Verbose}, d={loggingSettings.Debug}, i={loggingSettings.Information}, w={loggingSettings.Warning}, e={loggingSettings.Error}, c={loggingSettings.Critical}", null);
                    AgenteIALocal.Logging.Log.Reconfigure(loggingSettings);
                    AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigModal.Save", "Log.Reconfigure() COMPLETED - logging changes applied immediately", null);
                }
                catch (Exception exReconfigure)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigModal.Save", "Log.Reconfigure() failed: " + exReconfigure.Message, exReconfigure);
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Save persisted ActiveServerId=" + settings.ActiveServerId + ", BaseUrl=" + (srv.BaseUrl ?? "(empty)") + ", ModelLength=" + (srv.Model != null ? srv.Model.Length : 0), null);

                try
                {
                    AgentComposition.RecomposeFromSettings("ConfigModal.Save");
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: RecomposeFromSettings error: " + ex.Message, ex);
                }

                try
                {
                    AgenteIALocalControl found = null;
                    var ownerWindow = Owner as Window;
                    if (ownerWindow != null)
                    {
                        var content = ownerWindow.Content as FrameworkElement;
                        if (content is AgenteIALocalControl)
                        {
                            found = content as AgenteIALocalControl;
                        }
                        else if (content != null)
                        {
                            found = FindControlOfType(content, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                        }
                    }

                    if (found == null && System.Windows.Application.Current != null)
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        var mainContent = mainWindow != null ? mainWindow.Content as FrameworkElement : null;
                        if (mainContent is AgenteIALocalControl)
                        {
                            found = mainContent as AgenteIALocalControl;
                        }
                        else if (mainContent != null)
                        {
                            found = FindControlOfType(mainContent, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                        }
                    }

                    if (found != null)
                    {
                        try
                        {
                            found.RefreshFromSettings();
                            AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: RefreshFromSettings called on owner control.", null);
                        }
                        catch (Exception ex)
                        {
                            AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: Error calling RefreshFromSettings: " + ex.Message, ex);
                        }
                    }
                    else
                    {
                        AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Owner AgenteIALocalControl not found to refresh UI after save.", null);
                    }
                }
                catch (Exception ex)
                {
                    AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: Error refreshing owner UI: " + ex.Message, ex);
                }

                AgenteIALocal.Logging.Log.Information("-", 9100, "ConfigWindow", "ConfigModal: Save end", null);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: Save error: " + ex.Message, ex);
            }
            finally
            {
                try { DialogResult = true; } catch (Exception exDialog) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Save", "DialogResult set failed: " + exDialog.Message, exDialog); }
                try { Close(); } catch (Exception exClose) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.Save", "Close failed: " + exClose.Message, exClose); }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9100, "ConfigWindow", "ConfigModal: Close error: " + ex.Message, ex);
            }
        }

        // ELIMINADO METODO ApplyModalGlobalsToSettings - ID: 20260123_225950
        // Ya no necesario - SaveButton_Click maneja directamente GlobalSettings DTO

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                try { AgentSettingsStore.SettingsSaved -= OnSettingsSaved; } catch (Exception exUnsub) { AgenteIALocal.Logging.Log.Debug("-", 9100, "ConfigModal.OnClosed", "SettingsSaved unsubscribe failed: " + exUnsub.Message, exUnsub); }
            }
            catch (Exception ex) { AgenteIALocal.Logging.Log.Warning("-", 9100, "ConfigModal.OnClosed", "OnClosed failed: " + ex.Message, ex); }
            base.OnClosed(e);
        }

        private static FrameworkElement FindControlOfType(FrameworkElement root, Type t)
        {
            if (root == null) return null;
            if (root.GetType() == t) return root;
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i) as FrameworkElement;
                var res = FindControlOfType(child, t);
                if (res != null) return res;
            }
            return null;
        }
    }
}
