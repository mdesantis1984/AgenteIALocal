# Arquitectura del Sistema

**[🇪🇸 Español](#) | [🇬🇧 English](../en/architecture.md)**

**Patrón:** Clean Architecture / Onion Architecture  
**Estado:** ✅ Implementado

---

## 📋 Descripción General

**Agente IA Local** implementa **Clean Architecture** (también conocida como Onion Architecture) con 6 proyectos organizados en capas concéntricas. Las dependencias fluyen **hacia adentro** (desde capas externas hacia el Core), garantizando bajo acoplamiento y alta testeabilidad.

### ✨ Principios Aplicados

- ✅ **Dependency Inversion Principle (DIP)**: Capas externas dependen de abstracciones del Core
- ✅ **Single Responsibility Principle (SRP)**: Cada proyecto tiene una responsabilidad clara
- ✅ **Separation of Concerns**: UI, lógica de negocio, infraestructura separadas
- ✅ **Testabilidad**: Interfaces permiten mocking de dependencias
- ✅ **Mantenibilidad**: Cambios en una capa no afectan otras (bajo acoplamiento)

---

## 🏛️ Capas de la Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│  PRESENTATION LAYER                                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ AgenteIALocalVSIX (.NET Framework 4.7.2)             │  │
│  │ - UI/XAML (WPF + Material Design)                    │  │
│  │ - ViewModels (MVVM)                                  │  │
│  │ - ToolWindows (Chat, Config)                         │  │
│  │ - Event Handlers (Code-behind mínimo)                │  │
│  │ - Consume: ILocalizationService, ILlmClient, etc.    │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ Depende
┌─────────────────────────────────────────────────────────────┐
│  CROSS-CUTTING CONCERNS                                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ AgenteIALocal.Logging (.NET Standard 2.0)          │    │
│  │ - Serilog pipeline                                  │    │
│  │ - File sink (rolling 3 MB)                          │    │
│  │ - UI sink (buffer 250)                              │    │
│  │ - API: Log.Information/Error/etc.                   │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ AgenteIALocal.Localization (.NET Framework 4.7.2)  │    │
│  │ - LocalizationService                               │    │
│  │ - TranslateExtension (XAML MarkupExtension)         │    │
│  │ - LanguageSettingsStore                             │    │
│  │ - FileSystemWatcher (hot reload)                    │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                           ↓ Depende
┌─────────────────────────────────────────────────────────────┐
│  INFRASTRUCTURE LAYER                                       │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ AgenteIALocal.Infrastructure (.NET Standard 2.0)     │  │
│  │ - LmStudioClient (ILlmClient)                        │  │
│  │ - JanClient (ILlmClient)                             │  │
│  │ - LlamaCppClient (ILlmClient)                        │  │
│  │ - OllamaClient (ILlmClient)                          │  │
│  │ - HTTP communication (HttpClient)                    │  │
│  │ - File system (settings, logs, cache)                │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ Depende
┌─────────────────────────────────────────────────────────────┐
│  APPLICATION LAYER                                          │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ AgenteIALocal.Application (.NET Standard 2.0)        │  │
│  │ - AgentSettingsStore (persistencia settings.json)    │  │
│  │ - JSON ↔ DTOs conversion                             │  │
│  │ - Validation logic                                   │  │
│  │ - Business rules                                     │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ Depende
┌─────────────────────────────────────────────────────────────┐
│  CORE LAYER (Centro - Sin dependencias externas)           │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ AgenteIALocal.Core (.NET Standard 2.0)               │  │
│  │ - Interfaces: ILlmClient, ILocalizationService       │  │
│  │ - DTOs: GlobalSettings, RequestDefaults, etc.        │  │
│  │ - Domain entities (puros, sin lógica infraestructura)│  │
│  │ - Enums: LogLevel, RunMode, Provider                 │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Proyectos del Sistema

### 1. AgenteIALocal.Core

**Ubicación:** `src/AgenteIALocal.Core/`  
**Target Framework:** .NET Standard 2.0  
**Dependencias:** Ninguna (solo .NET Standard)

**Responsabilidades:**
- ✅ Definir interfaces (contratos) del sistema
- ✅ Definir DTOs (Data Transfer Objects) sin lógica
- ✅ Definir entidades de dominio puras
- ✅ Definir enums y constantes

**Interfaces clave:**

```csharp
// LLM Communication
public interface ILlmClient
{
    Task<LlmResponse> SendAsync(LlmRequest request);
    IAsyncEnumerable<string> StreamAsync(LlmRequest request);
    Task<IEnumerable<string>> GetModelsAsync();
}

// Localization
public interface ILocalizationService
{
    string GetString(string key);
    void SetLanguage(string code);
    event EventHandler LanguageChanged;
}
```

**DTOs clave:**

```csharp
// Settings
public class GlobalSettings
{
    public string Provider { get; set; }
    public string RunMode { get; set; }
    public ServerConfig[] Servers { get; set; }
    public RequestDefaults RequestDefaults { get; set; }
    public AgentBehavior Agent { get; set; }
    public LogSettings Logging { get; set; }
}

// LLM Request/Response
public class LlmRequest
{
    public string Model { get; set; }
    public Message[] Messages { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; }
}
```

**Regla de Oro:** ❌ **NUNCA** debe referenciar otros proyectos (solo .NET Standard)

---

### 2. AgenteIALocal.Application

**Ubicación:** `src/AgenteIALocal.Application/`  
**Target Framework:** .NET Standard 2.0  
**Dependencias:** Core + Newtonsoft.Json 13.0.3

**Responsabilidades:**
- ✅ Lógica de negocio (validaciones, transformaciones)
- ✅ Conversión JSON ↔ DTOs (deserialización/serialización)
- ✅ Persistencia de settings (`AgentSettingsStore`)
- ✅ Round-trip preservation (preservar campos desconocidos en JSON)

**Clases clave:**

```csharp
// Persistencia
public class AgentSettingsStore
{
    public GlobalSettings Load();
    public void Save(GlobalSettings settings);
    private GlobalSettings CreateDefaults();
}

// Validación
public class SettingsValidator
{
    public ValidationResult Validate(GlobalSettings settings);
}
```

**Características:**
- ✅ Atomic write (temp file → move) para evitar corrupción
- ✅ Thread-safe (lock interno)
- ✅ Preserva campos desconocidos (forward compatibility)

**Regla de Oro:** ❌ **NO** debe tener dependencias UI (WPF, XAML, etc.)

---

### 3. AgenteIALocal.Infrastructure

**Ubicación:** `src/AgenteIALocal.Infrastructure/`  
**Target Framework:** .NET Standard 2.0  
**Dependencias:** Core + Newtonsoft.Json 13.0.3

**Responsabilidades:**
- ✅ Implementar `ILlmClient` para cada proveedor (LM Studio, JAN, llama.cpp, Ollama)
- ✅ Comunicación HTTP (HttpClient)
- ✅ File system (lectura/escritura archivos, cache)
- ✅ Mapeo de formatos específicos de cada proveedor

**Implementaciones:**

```csharp
// LM Studio
public class LmStudioClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<LlmResponse> SendAsync(LlmRequest request)
    {
        var payload = MapToLmStudioFormat(request);
        var response = await _httpClient.PostAsync("/v1/chat/completions", payload);
        return MapFromLmStudioFormat(response);
    }
    
    public async IAsyncEnumerable<string> StreamAsync(LlmRequest request)
    {
        // SSE (Server-Sent Events) streaming
        await foreach (var chunk in StreamSseAsync(request))
        {
            yield return chunk;
        }
    }
}

// JAN
public class JanClient : ILlmClient { /* Similar */ }

// llama.cpp
public class LlamaCppClient : ILlmClient { /* Similar */ }

// Ollama
public class OllamaClient : ILlmClient { /* Similar */ }
```

**Diferencias por proveedor:**

| Proveedor | Endpoint | Soporta `stream_options.include_usage` | Formato Messages |
|-----------|----------|----------------------------------------|------------------|
| LM Studio | `/v1/chat/completions` | ✅ Sí | OpenAI-compatible |
| JAN | `/v1/chat/completions` | ❌ No (se omite) | OpenAI-compatible |
| llama.cpp | `/completion` | ❌ No | Formato propio |
| Ollama | `/api/chat` | ❌ No | Formato propio |

**Regla de Oro:** ❌ **NO** debe tener lógica de negocio (solo comunicación e I/O)

---

### 4. AgenteIALocal.Logging

**Ubicación:** `src/AgenteIALocal.Logging/`  
**Target Framework:** .NET Standard 2.0  
**Dependencias:** Serilog 4.2.0 + Serilog.Sinks.File 7.0.0

**Responsabilidades:**
- ✅ Pipeline centralizado de logging (Serilog)
- ✅ File sink con rolling automático (3 MB)
- ✅ UI sink (buffer 250 líneas)
- ✅ API público `Log.*` para uso desde cualquier proyecto

**API:**

```csharp
public static class Log
{
    public static void Configure(LogConfiguration config);
    public static void Verbose(string message, params object[] args);
    public static void Debug(string message, params object[] args);
    public static void Information(string message, params object[] args);
    public static void Warning(string message, params object[] args);
    public static void Error(Exception ex, string message, params object[] args);
    public static void Fatal(Exception ex, string message, params object[] args);
    public static void CloseAndFlush();
}
```

**Sinks configurados:**

1. **File Sink** → `%LOCALAPPDATA%/AgenteIALocal/logs/AgenteIALocal.log`
2. **UI Sink** → `ObservableCollection<string>` (data binding WPF)
3. **VS Activity Log** (opcional) → Output window de Visual Studio

**Ver más:** [Sistema Logging](logging.md)

---

### 5. AgenteIALocal.Localization

**Ubicación:** `src/AgenteIALocal.Localization/`  
**Target Framework:** .NET Framework 4.7.2  
**Dependencias:** Newtonsoft.Json 13.0.3

**Responsabilidades:**
- ✅ Implementar `ILocalizationService` (sistema i18n)
- ✅ Cargar idiomas desde archivos JSON externos
- ✅ Fallback embebido (es-AR readonly)
- ✅ FileSystemWatcher para hot reload
- ✅ `TranslateExtension` (MarkupExtension XAML)
- ✅ `LocalizationProvider` (INotifyPropertyChanged)

**Componentes:**

```csharp
// Service
public class LocalizationService : ILocalizationService
{
    public string GetString(string key);  // Nested lookup: "ui.config.window.title"
    public void SetLanguage(string code);
    public event EventHandler LanguageChanged;
    
    private void LoadExternalLanguages();  // Escanea Languages/*.json
    private void WatchFiles();  // FileSystemWatcher con debounce 500ms
}

// XAML Extension
public class TranslateExtension : MarkupExtension
{
    public string Key { get; set; }
    public string FallbackValue { get; set; }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = LocalizationProvider.Instance,
            Path = new PropertyPath($"[{Key}]"),
            FallbackValue = FallbackValue ?? Key
        };
    }
}

// Provider (INotifyPropertyChanged)
public class LocalizationProvider : INotifyPropertyChanged
{
    public string this[string key] => _service?.GetString(key) ?? key;
    
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
```

**Flujo reactivo:**
```
SetLanguage("en-US")
  → LanguageChanged event
  → PropertyChanged("Item[]")
  → WPF re-evalúa TODOS los {loc:Translate}
  → UI actualizada ✅
```

**Ver más:** [Sistema i18n](i18n.md)

---

### 6. AgenteIALocalVSIX

**Ubicación:** `src/AgenteIALocalVSIX/`  
**Target Framework:** .NET Framework 4.7.2  
**Dependencias:** Core + Application + Logging + Localization + MaterialDesignThemes 5.1.0

**Responsabilidades:**
- ✅ UI/XAML (WPF + Material Design)
- ✅ ViewModels (MVVM pattern)
- ✅ ToolWindows (Chat, Config)
- ✅ Event handlers (code-behind mínimo)
- ✅ Data binding a DTOs del Core

**Estructura:**

```
AgenteIALocalVSIX/
├── ToolWindows/
│   ├── MainToolWindow.cs           # Chat window
│   ├── AgenteIALocalControl.xaml   # Chat UI
│   ├── AgenteIALocalConfigWindow.xaml  # Config modal
│   └── AgenteIALocalConfigWindow.xaml.cs
├── AgenteIALocalVSIXPackage.cs     # Entry point
├── Commands/
│   └── AgenteIALocalCommand.cs     # Menu command
└── Properties/
    └── AssemblyInfo.cs
```

**REGLAS ARQUITECTÓNICAS (CRÍTICAS):**

### ⛔ PROHIBIDO en UI:

```csharp
// ❌ NO lógica de negocio
if (temperature < 0 || temperature > 2)
{
    MessageBox.Show("Invalid temperature");  // ❌ Validación en UI
}

// ❌ NO parsing JSON
var settings = JObject.Parse(json);  // ❌ Usar Newtonsoft en UI

// ❌ NO conversión de tipos
var dto = new GlobalSettings
{
    Temperature = double.Parse(textBox.Text)  // ❌ Conversión en UI
};

// ❌ NO persistencia directa
File.WriteAllText("settings.json", json);  // ❌ File I/O en UI
```

### ✅ PERMITIDO en UI:

```csharp
// ✅ Binding a propiedades de DTOs
<TextBlock Text="{Binding GlobalSettings.Provider}" />

// ✅ Llamar interfaces desde Core/Application
var settings = _settingsStore.Load();  // ISettingsStore
_localizationService.SetLanguage("en-US");  // ILocalizationService

// ✅ Event handlers mínimos (delegación a servicios)
private void SaveButton_Click(object sender, RoutedEventArgs e)
{
    var settings = BuildGlobalSettingsFromUi();  // Mínimo mapping
    _settingsStore.Save(settings);  // Delega a Application layer
}
```

**Patrón de inyección de dependencias:**

```csharp
// En AgenteIALocalVSIXPackage.cs
public static ILocalizationService LocalizationService { get; private set; }
public static ILlmClient CurrentLlmClient { get; private set; }

protected override async Task InitializeAsync(...)
{
    // Inicializar servicios
    LocalizationService = new LocalizationService(...);
    CurrentLlmClient = CreateLlmClient(provider);  // Factory pattern
    
    // Registrar ToolWindows
    await this.RegisterToolWindowAsync<MainToolWindow>(...);
}
```

---

## 🔄 Flujo de Dependencias

### ✅ CORRECTO (Dependency Inversion)

```
VSIX → Interfaces (Core) ← Implementaciones (Application, Infrastructure)
```

**Ejemplo:**

```csharp
// En VSIX
private readonly ILlmClient _llmClient;  // Depende de interface (Core)

// En InitializeAsync
_llmClient = new LmStudioClient(...);  // Implementación (Infrastructure)
```

### ❌ INCORRECTO (Acoplamiento directo)

```
VSIX → LmStudioClient directamente (sin interface)
```

**Anti-patrón:**

```csharp
// ❌ NO HACER
private readonly LmStudioClient _llmClient;  // Acoplamiento directo
```

---

## 📊 Matriz de Dependencias

| Proyecto | Core | Application | Infrastructure | Logging | Localization | VSIX |
|----------|------|-------------|----------------|---------|--------------|------|
| **Core** | - | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Application** | ✅ | - | ❌ | ❌ | ❌ | ❌ |
| **Infrastructure** | ✅ | ❌ | - | ❌ | ❌ | ❌ |
| **Logging** | ❌ | ❌ | ❌ | - | ❌ | ❌ |
| **Localization** | ✅ | ❌ | ❌ | ❌ | - | ❌ |
| **VSIX** | ✅ | ✅ | ✅ | ✅ | ✅ | - |

**Leyenda:**
- ✅ = Puede referenciar
- ❌ = NO debe referenciar

---

## 🎯 Patrones de Diseño Aplicados

### 1. Dependency Injection (DI)

**Uso:** Inyectar dependencias en constructores (interfaces)

```csharp
public class ChatViewModel
{
    private readonly ILlmClient _llmClient;
    private readonly ILocalizationService _localization;
    
    public ChatViewModel(ILlmClient llmClient, ILocalizationService localization)
    {
        _llmClient = llmClient;
        _localization = localization;
    }
}
```

### 2. Factory Pattern

**Uso:** Crear instancias de `ILlmClient` según provider

```csharp
public static ILlmClient CreateLlmClient(string provider, ServerConfig config)
{
    return provider switch
    {
        "lmstudio" => new LmStudioClient(config.BaseUrl),
        "jan" => new JanClient(config.BaseUrl),
        "llamacpp" => new LlamaCppClient(config.BaseUrl),
        "ollama" => new OllamaClient(config.BaseUrl),
        _ => throw new NotSupportedException($"Provider '{provider}' not supported")
    };
}
```

### 3. Repository Pattern

**Uso:** `AgentSettingsStore` como repositorio de settings

```csharp
public interface ISettingsStore
{
    GlobalSettings Load();
    void Save(GlobalSettings settings);
}

public class AgentSettingsStore : ISettingsStore
{
    // Implementación con atomic write, round-trip preservation
}
```

### 4. Observer Pattern

**Uso:** `LanguageChanged` event para reactividad UI

```csharp
public interface ILocalizationService
{
    event EventHandler LanguageChanged;
}

// En UI
_localizationService.LanguageChanged += OnLanguageChanged;

private void OnLanguageChanged(object sender, EventArgs e)
{
    // Actualizar UI automáticamente
}
```

### 5. Singleton Pattern

**Uso:** `LocalizationProvider.Instance` (thread-safe)

```csharp
public class LocalizationProvider
{
    private static readonly LocalizationProvider _instance = new LocalizationProvider();
    public static LocalizationProvider Instance => _instance;
    
    private LocalizationProvider() { }
}
```

---

## 📚 Referencias

- **[Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)**
- **[Onion Architecture (Jeffrey Palermo)](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)**
- **[SOLID Principles](https://en.wikipedia.org/wiki/SOLID)**
- **[Sistema i18n](i18n.md)** — Implementación Localization layer
- **[Sistema Logging](logging.md)** — Implementación Logging layer
- **[Configuración](configuration.md)** — Persistencia Application layer

---

**🏠 [Volver al índice](README.md)**
