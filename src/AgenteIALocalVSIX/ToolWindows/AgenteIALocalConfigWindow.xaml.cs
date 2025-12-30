using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;

namespace AgenteIALocalVSIX.ToolWindows
{
    public partial class AgenteIALocalConfigWindow : Window
    {
        private readonly string initialServerId;

        public AgenteIALocalConfigWindow(string serverId = null)
        {
            InitializeComponent();
            // Attach drag handler for custom header
            try
            {
                HeaderDragArea.MouseLeftButtonDown += HeaderDragArea_MouseLeftButtonDown;
            }
            catch { }

            try
            {
                //if (CloseButton != null)
                //{
                //    CloseButton.PreviewMouseLeftButtonDown += CloseButton_PreviewMouseLeftButtonDown;
                //}
            }
            catch { }

            this.Loaded += AgenteIALocalConfigWindow_Loaded;
            this.initialServerId = serverId ?? string.Empty;
            if (!string.IsNullOrEmpty(serverId))
            {
                this.Title = $"Configuración — {serverId}";
            }
        }

        private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // prevent header drag from stealing the click
            e.Handled = true;
        }

        private void HeaderDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Left) return;

                // If click originated within CloseButton visual tree, ignore
                try
                {
                    //if (CloseButton != null)
                    //{
                    //    var src = e.OriginalSource as DependencyObject;
                    //    while (src != null)
                    //    {
                    //        if (ReferenceEquals(src, CloseButton)) return;
                    //        src = VisualTreeHelper.GetParent(src);
                    //    }
                    //}
                }
                catch { }

                // Log attempt
                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: DragMove start"); } catch { }
                this.DragMove();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: DragMove failed: {ex.Message}", ex); } catch { }
            }
        }

        private async void AgenteIALocalConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = AgentSettingsStore.Load();
                if (settings != null)
                {
                    // choose server to display: prefer initialServerId, else settings.ActiveServerId
                    var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : settings.ActiveServerId;

                    var srv = (settings.Servers ?? new List<ServerConfig>())
                        .Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));

                    ActiveServerIdTextBox_Modal.Text = targetId ?? string.Empty;
                    ServerBaseUrlTextBox_Modal.Text = srv?.BaseUrl ?? string.Empty;
                    ServerApiKeyTextBox_Modal.Text = srv?.ApiKey ?? string.Empty;

                    // Wire BaseUrl change to re-fetch models
                    try
                    {
                        ServerBaseUrlTextBox_Modal.LostFocus -= ServerBaseUrl_LostFocus;
                        ServerBaseUrlTextBox_Modal.LostFocus += ServerBaseUrl_LostFocus;
                    }
                    catch { }

                    var baseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        try
                        {
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels start for {baseUrl}"); } catch { }
                            var models = await FetchModelsAsync(baseUrl);
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels end for {baseUrl} (count={models?.Count ?? 0})"); } catch { }

                            if (models != null && models.Count > 0)
                            {
                                ServerModelCombo_Modal.Items.Clear();
                                foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);

                                var activeModel = srv?.Model;
                                if (!string.IsNullOrEmpty(activeModel) && ServerModelCombo_Modal.Items.Contains(activeModel))
                                {
                                    ServerModelCombo_Modal.SelectedItem = activeModel;
                                }
                                else
                                {
                                    ServerModelCombo_Modal.SelectedIndex = 0;
                                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Defaulted model selection to '{ServerModelCombo_Modal.SelectedItem}'"); } catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModels error on load: {ex.Message}", ex); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Load error: {ex.Message}", ex); } catch { }
            }
        }

        private async void ServerBaseUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var newBase = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(newBase))
                {
                    ServerModelCombo_Modal.Items.Clear();
                    return;
                }

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: BaseUrl changed in modal to {newBase}; fetching models..."); } catch { }
                var models = await FetchModelsAsync(newBase);
                ServerModelCombo_Modal.Items.Clear();
                if (models != null && models.Count > 0)
                {
                    foreach (var m in models) ServerModelCombo_Modal.Items.Add(m);
                    ServerModelCombo_Modal.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Error re-fetching models on BaseUrl change: {ex.Message}", ex); } catch { }
            }
        }

        private async Task<List<string>> FetchModelsAsync(string baseUrl)
        {
            var result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl)) return result;

                var url = baseUrl.TrimEnd('/') + "/v1/models";
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync GET {url}"); } catch { }

                    var resp = await client.GetAsync(url);
                    if (!resp.IsSuccessStatusCode)
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync non-success status: {resp.StatusCode}"); } catch { }
                        return result;
                    }

                    var txt = await resp.Content.ReadAsStringAsync();
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
                                catch { }
                            }

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
                                catch { }
                            }

                            return result;
                        }

                        if (root is Newtonsoft.Json.Linq.JArray arr)
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
                        try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync parse error: {ex.Message}", ex); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: FetchModelsAsync error: {ex.Message}", ex); } catch { }
            }

            return result;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save start"); } catch { }
            try
            {
                var settings = AgentSettingsStore.Load() ?? new AgentSettings();
                if (settings.Servers == null) settings.Servers = new List<ServerConfig>();

                // Determine target server id: prefer initialServerId (derived from ToolWindow selection), else textbox value
                var activeIdInput = ActiveServerIdTextBox_Modal.Text ?? string.Empty;
                var targetId = !string.IsNullOrEmpty(initialServerId) ? initialServerId : activeIdInput;
                if (string.IsNullOrEmpty(targetId))
                {
                    // fallback to existing active or first server
                    targetId = settings.ActiveServerId ?? (settings.Servers.Count > 0 ? settings.Servers[0].Id : string.Empty);
                    if (string.IsNullOrEmpty(targetId)) targetId = activeIdInput;
                }

                var srv = settings.Servers.Find(s => string.Equals(s.Id, targetId, StringComparison.OrdinalIgnoreCase));
                if (srv == null)
                {
                    srv = new ServerConfig { Id = targetId, Name = targetId, Provider = settings.Servers.FirstOrDefault()?.Provider ?? "lmstudio", CreatedAt = DateTime.UtcNow };
                    settings.Servers.Add(srv);
                }

                srv.BaseUrl = ServerBaseUrlTextBox_Modal.Text ?? string.Empty;
                // do not log apiKey
                srv.ApiKey = ServerApiKeyTextBox_Modal.Text ?? string.Empty;

                // Robust extraction of selected model value
                string selectedModel = null;
                try
                {
                    selectedModel = (ServerModelCombo_Modal?.SelectedItem as string) 
                                    ?? (ServerModelCombo_Modal?.SelectedValue as string) 
                                    ?? ServerModelCombo_Modal?.Text;
                }
                catch { }
                selectedModel = (selectedModel ?? string.Empty).Trim();

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save activeId='{targetId}', baseUrlPresent={(!string.IsNullOrWhiteSpace(srv.BaseUrl)).ToString()}, modelPresent={(string.IsNullOrEmpty(selectedModel) ? "false" : "true")} "); } catch { }

                try { if (!string.IsNullOrEmpty(selectedModel)) srv.Model = selectedModel; } catch { }

                settings.ActiveServerId = targetId;
                AgentSettingsStore.Save(settings);

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save persisted ActiveServerId={settings.ActiveServerId}, BaseUrl={(srv.BaseUrl ?? "(empty)" )}, ModelLength={(srv.Model != null ? srv.Model.Length : 0)}"); } catch { }

                // Recompose and refresh
                try
                {
                    AgentComposition.RecomposeFromSettings("ConfigModal.Save");
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: RecomposeFromSettings error: {ex.Message}", ex); } catch { }
                }

                // Refresh owner control UI state using public API (no reflection)
                try
                {
                    AgenteIALocalControl found = null;
                    var ownerWindow = this.Owner as Window;
                    if (ownerWindow != null)
                    {
                        var content = ownerWindow.Content as FrameworkElement;
                        if (content is AgenteIALocalControl ac) found = ac;
                        else if (content != null) found = FindControlOfType(content, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                    }

                    if (found == null && System.Windows.Application.Current != null)
                    {
                        var mainContent = System.Windows.Application.Current.MainWindow?.Content as FrameworkElement;
                        if (mainContent is AgenteIALocalControl ac2) found = ac2;
                        else if (mainContent != null) found = FindControlOfType(mainContent, typeof(AgenteIALocalControl)) as AgenteIALocalControl;
                    }

                    if (found != null)
                    {
                        try
                        {
                            found.RefreshFromSettings();
                            try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: RefreshFromSettings called on owner control."); } catch { }
                        }
                        catch (Exception ex)
                        {
                            try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Error calling RefreshFromSettings: {ex.Message}", ex); } catch { }
                        }
                    }
                    else
                    {
                        try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Owner AgenteIALocalControl not found to refresh UI after save."); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Error refreshing owner UI: {ex.Message}", ex); } catch { }
                }

                try { AgentComposition.Info("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, "ConfigModal: Save end"); } catch { }
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Save error: {ex.Message}", ex); } catch { }
            }
            finally
            {
                try { this.Close(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                try { AgentComposition.Error("-", AgenteIALocal.Core.Logging.LogEvents.Vsix_UI, $"ConfigModal: Close error: {ex.Message}", ex); } catch { }
            }
        }

        private static FrameworkElement FindControlOfType(FrameworkElement root, Type t)
        {
            if (root == null) return null;
            if (root.GetType() == t) return root;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i) as FrameworkElement;
                var res = FindControlOfType(child, t);
                if (res != null) return res;
            }
            return null;
        }
    }
}