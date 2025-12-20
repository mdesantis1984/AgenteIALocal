using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgenteIALocal.Application.Agents;
using AgenteIALocal.Core.Agents;
using AgenteIALocal.Core.Settings;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AgenteIALocalVSIX.Settings;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalControl : UserControl
    {
        private enum UiState { Idle, Running, Completed, Error }
        private UiState state = UiState.Idle;

        private IAgentService agentService;
        private readonly IAgentSettingsProvider settingsProvider;

        public AgenteIALocalControl(IAgentSettingsProvider settingsProvider)
        {
            this.settingsProvider = settingsProvider;

            InitializeComponent();
            UpdateUiState(UiState.Idle);

            // Centralized decision: evaluate and display exactly one clear message for current configuration state
            EvaluateAndDisplayStatus();
        }

        public AgenteIALocalControl() : this(null)
        {
        }

        private void RootTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var rootTabControl = sender as TabControl ?? (this.FindName("RootTabControl") as TabControl);
                var configTab = this.FindName("ConfigTab") as TabItem;

                if (rootTabControl != null && configTab != null && rootTabControl.SelectedItem == configTab)
                {
                    // no-op: config UI maneja el DataContext internamente
                }
            }
            catch { }
        }

        private void AgenteIALocalControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: Loaded");
            }
            catch { }

            // Re-evaluate status when control is loaded in case options changed
            EvaluateAndDisplayStatus();
        }

        // Tab selection changed - when Log tab is selected, load the log file content
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MainTabControl.SelectedItem is TabItem ti && ti.Header != null && ti.Header.ToString() == "Log")
                {
                    LoadLogFile();
                }
            }
            catch { }
        }

        private void LoadLogFile()
        {
            var path = GetLogFilePath();
            LogPathText.Text = path ?? "(sin ruta)";

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                LogViewerTextBox.Text = "Log vacío / no encontrado";
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
                return;
            }

            try
            {
                var text = File.ReadAllText(path);
                LogViewerTextBox.Text = string.IsNullOrEmpty(text) ? "Log vacío / no encontrado" : text;

                var fi = new FileInfo(path);
                LogSizeText.Text = FormatSize(fi.Length);

                CopyLogButton.IsEnabled = true;
                DeleteLogButton.IsEnabled = true;
            }
            catch
            {
                LogViewerTextBox.Text = "No se pudo leer el archivo de log.";
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
            }
        }

        private string GetLogFilePath()
        {
            try
            {
                // Replicate the same path logic used by FileAgentLogger to avoid hardcoding a different path
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? string.Empty;
                var dir = Path.Combine(baseDir, "AgenteIALocal", "logs");
                var logFilePath = Path.Combine(dir, "AgenteIALocal.log");
                return logFilePath;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatSize(long bytes)
        {
            try
            {
                if (bytes < 1024) return bytes + " B";
                double kb = bytes / 1024.0;
                if (kb < 1024) return Math.Round(kb, 1) + " KB";
                double mb = kb / 1024.0;
                return Math.Round(mb, 2) + " MB";
            }
            catch
            {
                return "0 KB";
            }
        }

        private void CopyLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var text = LogViewerTextBox.Text ?? string.Empty;
                if (string.IsNullOrEmpty(text)) return;
                Clipboard.SetText(text);
                try { AgentComposition.Logger?.Info("ToolWindowControl: Log copied to clipboard"); } catch { }
            }
            catch
            {
                // fail-safe: do not show raw exceptions
            }
        }

        private void DeleteLogButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var path = GetLogFilePath();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    try { AgentComposition.Logger?.Info("ToolWindowControl: Log file deleted: " + path); } catch { }
                }

                // Update UI
                LogViewerTextBox.Text = string.Empty;
                LogSizeText.Text = "0 KB";
                CopyLogButton.IsEnabled = false;
                DeleteLogButton.IsEnabled = false;
            }
            catch
            {
                // If deletion failed, keep UI consistent but do not show exception details
                LogViewerTextBox.Text = "No se pudo borrar el archivo de log.";
            }
        }

        public void EvaluateAndDisplayStatus()
        {
            // Default reset
            ErrorText.Text = string.Empty;
            ErrorText.Foreground = Brushes.Red;

            // State 3 — Backend no disponible
            if (agentService == null)
            {
                StateText.Text = "Backend: n/a";
                ErrorText.Text = "Backend no disponible. Verificá la configuración o el proveedor.";
                RunButton.IsEnabled = false;
                return;
            }

            AgentSettings settings = null;
            try
            {
                var pkg = GetGlobalVsixPackage();
                if (pkg != null)
                {
                    var provider = new AgenteIALocalVSIX.Settings.VsWritableSettingsStoreAgentSettingsProvider(pkg);
                    settings = provider.Load();
                }
            }
            catch
            {
                settings = null;
            }

            bool hasProvider;
            bool hasBaseUrl;
            bool hasModel;
            ValidateSettings(settings, out hasProvider, out hasBaseUrl, out hasModel);

            if (hasProvider && hasBaseUrl && hasModel)
            {
                // Estado 1 — Configurado
                StateText.Text = "Configuración: OK";
                ErrorText.Foreground = Brushes.Green;
                ErrorText.Text = "Configuración OK. Listo para enviar mensajes.";
                RunButton.IsEnabled = true;
                return;
            }

            // Estado 2 — Configuración incompleta
            StateText.Text = "Configuración: incompleta";
            ErrorText.Foreground = Brushes.Orange;
            ErrorText.Text = "Configuración incompleta. Revisá las Opciones.";
            RunButton.IsEnabled = false;
        }

        private static void ValidateSettings(AgentSettings settings, out bool hasProvider, out bool hasBaseUrl, out bool hasModel)
        {
            hasProvider = false;
            hasBaseUrl = false;
            hasModel = false;

            if (settings == null)
            {
                return;
            }

            hasProvider = true;

            try
            {
                if (settings.Provider == AgentProviderType.JanServer)
                {
                    // JanServer: validate ONLY JanServer.BaseUrl; do NOT require model
                    hasBaseUrl = !string.IsNullOrWhiteSpace(settings.JanServer?.BaseUrl);
                    hasModel = true;
                    return;
                }

                // LmStudio: validate ONLY LM Studio settings; completely ignore JanServer
                hasBaseUrl = !string.IsNullOrWhiteSpace(settings.LmStudio?.BaseUrl);
                hasModel = !string.IsNullOrWhiteSpace(settings.LmStudio?.Model);
            }
            catch
            {
                hasBaseUrl = false;
                hasModel = false;
            }
        }

        private static Package GetGlobalVsixPackage()
        {
            try
            {
                // Save/Load uses the same VS settings store, which requires a sited package.
                // We get our AsyncPackage instance from the global service provider.
                return Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(AgenteIALocalVSIXPackage)) as Package;
            }
            catch
            {
                return null;
            }
        }

        public void SetSolutionInfo(string solutionName, int projectCount)
        {
            SolutionNameText.Text = solutionName;
            ProjectCountText.Text = projectCount.ToString();

            // Keep existing request generation (not modifying contracts)
            var req = BuildRequest(solutionName, projectCount);
            PromptTextBox.Text = req.Action + " - " + req.SolutionName;
            ResponseTextBox.Text = string.Empty;
            LogText.Text = "";

            try
            {
                AgentComposition.Logger?.Info("ToolWindowControl: SetSolutionInfo solution='" + (solutionName ?? string.Empty) + "' projects=" + projectCount);
            }
            catch { }
        }

        private Contracts.CopilotRequest BuildRequest(string solutionName, int projectCount)
        {
            return new Contracts.CopilotRequest
            {
                RequestId = System.Guid.NewGuid().ToString(),
                Action = "mock-execute",
                Timestamp = System.DateTime.UtcNow.ToString("o"),
                SolutionName = solutionName,
                ProjectCount = projectCount
            };
        }

        private string SerializeToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        private void UpdateUiState(UiState newState)
        {
            state = newState;
            StateText.Text = state.ToString();
            RunButton.IsEnabled = state == UiState.Idle || state == UiState.Completed || state == UiState.Error;
            ClearButton.IsEnabled = state != UiState.Running;
        }

        private async void RunButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try { AgentComposition.Logger?.Info("ToolWindowControl: RunButton clicked"); } catch { }

            if (state == UiState.Running) return;

            UpdateUiState(UiState.Running);
            ErrorText.Text = string.Empty;
            Log("Execution started.");

            try
            {
                var prompt = PromptTextBox.Text ?? string.Empty;

                try
                {
                    AgentComposition.Logger?.Info($"ToolWindowControl: Prompt length = {prompt.Length}");
                }
                catch { }

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "Prompt vacío.";
                    Log("Execution error: Prompt is empty.");
                    return;
                }

                // 1) Obtener IAgentSettingsProvider (mismo usado por ToolWindow) y leer settings activos
                var provider = settingsProvider;
                if (provider == null)
                {
                    try
                    {
                        var pkg = GetGlobalVsixPackage();
                        if (pkg != null)
                        {
                            provider = new VsWritableSettingsStoreAgentSettingsProvider(pkg);
                        }
                    }
                    catch
                    {
                        provider = null;
                    }
                }

                var settings = provider?.Load();
                if (settings == null)
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "No se pudieron leer las opciones (settings).";
                    Log("Execution error: settingsProvider.Load() returned null.");
                    return;
                }

                // Validar Provider == LmStudio
                if (settings.Provider != AgentProviderType.LmStudio)
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "El proveedor activo no es LM Studio. Revisá Tools → Options → Agente IA Local.";
                    Log("Execution error: Active provider is not LmStudio.");
                    return;
                }

                var lm = settings.LmStudio ?? new LmStudioSettings();
                var baseUrl = (lm.BaseUrl ?? string.Empty).TrimEnd('/');
                var chatPath = lm.ChatCompletionsPath ?? "/v1/chat/completions";
                var model = lm.Model ?? string.Empty;

                if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(chatPath) || string.IsNullOrWhiteSpace(model))
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "Configuración LM Studio incompleta (BaseUrl/ChatPath/Model).";
                    Log("Execution error: incomplete LM Studio settings.");
                    return;
                }

                var url = baseUrl + (chatPath.StartsWith("/") ? string.Empty : "/") + chatPath;

                // 2) Construir payload
                var payload = new JObject
                {
                    ["model"] = model,
                    ["messages"] = new JArray
                    {
                        new JObject
                        {
                            ["role"] = "user",
                            ["content"] = prompt
                        }
                    }
                };

                // Mostrar mensaje del usuario en el chat (UI existente: Prompt/Response)
                // Mantener cambio local: reutilizamos ResponseTextBox como historial simple.
                AppendChatLine("user", prompt);

                // 3) POST a LM Studio (API OpenAI compatible)
                string responseText;
                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(120);

                    var apiKey = lm.ApiKey ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        // OpenAI-compatible header; LM Studio suele ignorarlo si no hace falta.
                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    var content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    var resp = await http.PostAsync(url, content).ConfigureAwait(true);
                    responseText = await resp.Content.ReadAsStringAsync().ConfigureAwait(true);

                    if (!resp.IsSuccessStatusCode)
                    {
                        UpdateUiState(UiState.Error);
                        ErrorText.Text = $"Error HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}";
                        Log("Execution error: HTTP error " + (int)resp.StatusCode + " " + (resp.ReasonPhrase ?? string.Empty));
                        try { AgentComposition.Logger?.Warn("ToolWindowControl: LM Studio HTTP error body=" + (responseText ?? string.Empty)); } catch { }
                        return;
                    }
                }

                // 4) Parsear respuesta JSON: choices[0].message.content
                string assistantText = null;
                try
                {
                    var json = JObject.Parse(responseText ?? string.Empty);
                    assistantText = json.SelectToken("choices[0].message.content")?.ToString();
                }
                catch
                {
                    assistantText = null;
                }

                if (string.IsNullOrWhiteSpace(assistantText))
                {
                    UpdateUiState(UiState.Error);
                    ErrorText.Text = "Respuesta inválida de LM Studio (no se encontró choices[0].message.content).";
                    Log("Execution error: invalid JSON response.");
                    return;
                }

                // 5) Agregar texto al chat UI
                AppendChatLine("assistant", assistantText);

                // Mantener compatibilidad con UI existente
                ResponseTextBox.Text = assistantText;

                UpdateUiState(UiState.Completed);
                Log("Execution completed successfully.");
            }
            catch (Exception ex)
            {
                // 6) Manejo de errores básicos
                UpdateUiState(UiState.Error);
                ErrorText.Text = ex.Message;
                Log("Execution error: " + ex.Message);

                try
                {
                    AgentComposition.Logger?.Error("ToolWindowControl: Error during LM Studio request", ex);
                }
                catch { }
            }
            finally
            {
                // Re-evaluate status to ensure UI stays consistent after an execution attempt
                EvaluateAndDisplayStatus();
            }
        }

        private void AppendChatLine(string role, string text)
        {
            try
            {
                var r = role ?? string.Empty;
                var t = text ?? string.Empty;
                var line = $"{r}: {t}";

                // Simple historial en LogText (ya visible en la pestaña Main)
                LogText.Text = (LogText.Text ?? string.Empty) + "\n" + line;
            }
            catch
            {
                // ignore
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PromptTextBox.Text = string.Empty;
            ResponseTextBox.Text = string.Empty;
            LogText.Text = string.Empty;
            UpdateUiState(UiState.Idle);

            try { AgentComposition.Logger?.Info("ToolWindowControl: Clear click"); } catch { }

            EvaluateAndDisplayStatus();
        }

        private void OptionsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Package)) as Package;
                if (pkg != null)
                {
                    pkg.ShowOptionPage(typeof(AgenteIALocalVSIX.Options.AgenteIALocalOptionsPage));
                }
            }
            catch { }
        }

        private void Log(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            LogText.Text = ts + " - " + message + "\n" + LogText.Text;
        }
    }
}
