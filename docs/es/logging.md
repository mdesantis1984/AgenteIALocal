# Sistema de Logging

**[🇪🇸 Español](#) | [🇬🇧 English](../en/logging.md)**

**Versión:** 2.4-serilog.4  
**Estado:** ✅ Completado  
**Plan:** [PLAN_SERILOG_1.3.md](../plans/PLAN_SERILOG_1.3.md)

---

## 📋 Descripción General

El sistema de logging de **Agente IA Local** utiliza **Serilog** como pipeline centralizado para registrar eventos de la aplicación. Soporta múltiples destinos (sinks) y ofrece configuración dinámica de niveles de logging sin necesidad de recompilar.

### ✨ Características Clave

- 📝 **Pipeline único**: API centralizado `Log.*` desde cualquier proyecto
- 🗄️ **Rolling automático**: Archivos de log rotan a 3 MB
- 🖥️ **UI integrada**: Panel de log con últimas 250 líneas (formato usuario final)
- ⚙️ **Configuración dinámica**: Niveles habilitables desde `settings.json`
- 🎯 **Sin spam**: Trazabilidad 1 línea por evento relevante (sin logs por chunk/retry/keypress)
- 🔧 **Multi-sink**: File + UI + VS Activity Log (extensible)

---

## 🏗️ Arquitectura

### Componentes Principales

```
┌─────────────────────────────────────────────────────────┐
│  Cualquier Proyecto (Core, Application, Infrastructure, │
│  VSIX)                                                   │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Log.Information("Message")                        │  │
│  │ Log.Warning("Warning message")                    │  │
│  │ Log.Error(ex, "Error context")                    │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           ↓ Llama
┌─────────────────────────────────────────────────────────┐
│  AgenteIALocal.Logging (netstandard2.0)                 │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Log (Static Facade)                               │  │
│  │ - Configure(config)                               │  │
│  │ - Verbose/Debug/Information/Warning/Error/Fatal   │  │
│  │ - Close()                                         │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Serilog Pipeline                                  │  │
│  │ - MinimumLevel (desde settings.json)              │  │
│  │ - Enrichers (timestamp, nivel, contexto)          │  │
│  │ - Sinks: File + UI + VsActivityLog                │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           ↓ Escribe
┌─────────────────────────────────────────────────────────┐
│  Destinos (Sinks)                                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │ File Sink (Rolling 3 MB)                          │  │
│  │ %LOCALAPPDATA%/AgenteIALocal/logs/                │  │
│  │ AgenteIALocal.log                                 │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ UI Sink (Buffer 250 líneas)                       │  │
│  │ - Formato usuario final                           │  │
│  │ - Observable para data binding                    │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ VS Activity Log (Opcional)                        │  │
│  │ - Output window VS                                │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 📦 Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| Serilog | 4.2.0 | Pipeline de logging core |
| Serilog.Sinks.File | 7.0.0 | Sink a archivo con rolling |
| Serilog.Sinks.Async | 2.1.0 | Escritura asíncrona (opcional) |

**Proyecto:** `AgenteIALocal.Logging` (netstandard2.0)

---

## 🔧 API Público

### Configuración

```csharp
// Inicialización (en AgenteIALocalVSIXPackage.InitializeAsync)
Log.Configure(new LogConfiguration
{
    MinimumLevel = LogEventLevel.Information,
    FilePath = Path.Combine(localAppData, "logs", "AgenteIALocal.log"),
    FileSizeLimitBytes = 3 * 1024 * 1024,  // 3 MB
    RetainedFileCountLimit = 5,
    UiBufferSize = 250,
    EnableVsActivityLog = true
});
```

### Uso Básico

```csharp
// Niveles de logging (orden: Verbose < Debug < Information < Warning < Error < Fatal)

// Verbose (diagnóstico detallado)
Log.Verbose("Iniciando escaneo de idiomas en {Path}", languagesPath);

// Debug (información de depuración)
Log.Debug("Modelo seleccionado: {ModelId}", modelId);

// Information (eventos generales)
Log.Information("Servicio de localización inicializado. Idioma: {Language}", currentLang);

// Warning (advertencias no críticas)
Log.Warning("Archivo strings.json no encontrado para {LanguageCode}", code);

// Error (errores con contexto)
try
{
    // código
}
catch (Exception ex)
{
    Log.Error(ex, "Error al cargar configuración desde {FilePath}", filePath);
}

// Fatal (errores críticos)
Log.Fatal(ex, "Error crítico en inicialización de VSIX");
```

### Cleanup

```csharp
// Al cerrar la aplicación (en Dispose)
Log.CloseAndFlush();
```

---

## ⚙️ Configuración Dinámica

### settings.json

```json
{
  "globalSettings": {
    "logging": {
      "enabled": true,
      "all": "on",
      "levels": {
        "verbose": false,
        "debug": true,
        "information": true,
        "warning": true,
        "error": true,
        "critical": true
      }
    }
  }
}
```

### Descripción de Campos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `enabled` | boolean | Habilita/deshabilita TODO el logging |
| `all` | string | `"on"` habilita todos los niveles (sobrescribe `levels.*`) |
| `levels.verbose` | boolean | Habilita nivel Verbose (diagnóstico detallado) |
| `levels.debug` | boolean | Habilita nivel Debug (depuración) |
| `levels.information` | boolean | Habilita nivel Information (eventos generales) |
| `levels.warning` | boolean | Habilita nivel Warning (advertencias) |
| `levels.error` | boolean | Habilita nivel Error (errores) |
| `levels.critical` | boolean | Habilita nivel Critical (fallos críticos) |

**Cambios en tiempo real:** Editar `settings.json` → guardar → cambios aplican automáticamente (FileSystemWatcher).

---

## 📁 Archivos de Log

### Ubicación

```
%LOCALAPPDATA%\AgenteIALocal\logs\
├── AgenteIALocal.log          # Archivo actual
├── AgenteIALocal20260126.log  # Archivado (rolling por fecha)
└── AgenteIALocal20260125.log  # Archivado
```

### Rolling Automático

- **Tamaño límite:** 3 MB por archivo
- **Naming:** `AgenteIALocal{Date}.log`
- **Retención:** 5 archivos (configurable)
- **Trigger:** Cuando `AgenteIALocal.log` alcanza 3 MB, se renombra a `AgenteIALocal{Date}.log` y se crea uno nuevo

### Formato de Línea

```
2026-01-26 10:30:45.123 [INF] Servicio de localización inicializado. Idioma: es-AR
2026-01-26 10:30:46.456 [WRN] Archivo strings.json no encontrado para pt-BR
2026-01-26 10:30:47.789 [ERR] Error al cargar configuración desde settings.json: FileNotFoundException
```

**Formato:** `{Timestamp} [{Level}] {Message} {Exception?}`

---

## 🖥️ Panel de Log (UI)

### Características

- **Buffer:** Últimas 250 líneas
- **Formato usuario final:** Simplificado (timestamp + mensaje)
- **Actualización automática:** Observable pattern (INotifyCollectionChanged)
- **Truncado inteligente:** FIFO (First In, First Out) cuando supera 250 líneas

### Implementación

```csharp
// UiLogSink (en AgenteIALocal.Logging)
public class UiLogSink : ILogEventSink
{
    private readonly ObservableCollection<string> _buffer = new ObservableCollection<string>();
    private const int MaxLines = 250;

    public IEnumerable<string> Lines => _buffer;

    public void Emit(LogEvent logEvent)
    {
        string formatted = FormatForUser(logEvent);

        // Thread-safe update en UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_buffer.Count >= MaxLines)
                _buffer.RemoveAt(0);  // FIFO

            _buffer.Add(formatted);
        });
    }

    private string FormatForUser(LogEvent logEvent)
    {
        // Formato simplificado: "10:30:45 Servicio inicializado"
        return $"{logEvent.Timestamp:HH:mm:ss} {logEvent.RenderMessage()}";
    }
}
```

### Binding XAML

```xaml
<!-- LogPanel en ConfigWindow -->
<ListBox ItemsSource="{Binding LogLines}"
         ScrollViewer.VerticalScrollBarVisibility="Auto"
         ScrollViewer.HorizontalScrollBarVisibility="Auto">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" FontFamily="Consolas" FontSize="11" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

## 🎯 Reglas de Logging (Sin Spam)

### ✅ PERMITIDO (1 línea por evento relevante)

```csharp
// Inicialización de servicios
Log.Information("LocalizationService inicializado. Idioma: {Code}", currentLanguageCode);

// Cambio de configuración
Log.Information("Idioma cambiado: {OldCode} → {NewCode}", oldCode, newCode);

// Envío de request a LLM
Log.Information("Request enviado a {Provider}: model={Model}, temp={Temp}", provider, model, temp);

// Recepción de respuesta completa
Log.Information("Response recibido: {Tokens} tokens, duration={Duration}ms", tokens, duration);

// Errores con contexto
Log.Error(ex, "Error al conectar con {Provider} en {BaseUrl}", provider, baseUrl);
```

### ❌ PROHIBIDO (spam)

```csharp
// NO loggear por chunk de streaming
foreach (var chunk in stream)
{
    Log.Debug("Chunk recibido: {Content}", chunk);  // ❌ SPAM
}

// NO loggear por retry
for (int i = 0; i < maxRetries; i++)
{
    Log.Information("Retry {Attempt}/{Max}", i, maxRetries);  // ❌ SPAM
}

// NO loggear por keypress
private void TextBox_KeyDown(object sender, KeyEventArgs e)
{
    Log.Verbose("Key pressed: {Key}", e.Key);  // ❌ SPAM
}

// NO loggear en loops frecuentes
foreach (var item in largeList)
{
    Log.Debug("Processing {Item}", item);  // ❌ SPAM si list > 10 items
}
```

### ✅ ALTERNATIVA CORRECTA

```csharp
// En lugar de logs por chunk:
Log.Information("Iniciando streaming desde {Provider}", provider);
// ... recibir chunks sin loggear ...
Log.Information("Streaming completado: {Chunks} chunks, {Tokens} tokens", chunkCount, tokenCount);

// En lugar de logs por retry:
Log.Warning("Request falló, reintentando ({Attempt}/{Max})", attemptCount, maxRetries);

// En lugar de logs por keypress:
// (NO loggear - usar solo para debugging con breakpoints)
```

---

## 📊 Niveles de Logging

### Jerarquía

```
Verbose (más detallado)
  ↓
Debug
  ↓
Information
  ↓
Warning
  ↓
Error
  ↓
Fatal (más crítico)
```

### Cuándo Usar Cada Nivel

| Nivel | Uso | Ejemplo |
|-------|-----|---------|
| **Verbose** | Diagnóstico muy detallado (debugging profundo) | Trazas de ejecución, valores de variables |
| **Debug** | Información de depuración | Valores de parámetros, estados intermedios |
| **Information** | Eventos normales de la aplicación | Servicio iniciado, request enviado, idioma cambiado |
| **Warning** | Situaciones anómalas pero no críticas | Archivo opcional no encontrado, timeout con retry exitoso |
| **Error** | Errores que requieren atención | Excepción capturada, operación fallida |
| **Fatal** | Errores críticos que impiden funcionamiento | VSIX no puede inicializar, dependencia faltante crítica |

### Configuración Recomendada

**Desarrollo:**
```json
{
  "logging": {
    "enabled": true,
    "levels": {
      "verbose": false,
      "debug": true,
      "information": true,
      "warning": true,
      "error": true,
      "critical": true
    }
  }
}
```

**Producción:**
```json
{
  "logging": {
    "enabled": true,
    "levels": {
      "verbose": false,
      "debug": false,
      "information": true,
      "warning": true,
      "error": true,
      "critical": true
    }
  }
}
```

---

## 🔄 Migración desde Sistema Anterior

### Sistema Legacy (ELIMINADO)

```csharp
// ❌ OBSOLETO - NO USAR
IAgentLoggerV2 logger = ...;
logger.AppendLog("Message", LogLevel.Info);
logger.AppendLogFileLine("File message");

// ❌ OBSOLETO - NO USAR
AgentComposition.Instance.Logger.AppendLog(...);
```

### Sistema Nuevo (Serilog)

```csharp
// ✅ CORRECTO - USAR SIEMPRE
Log.Information("Message");
Log.Error(ex, "Error context: {Detail}", detail);
```

### Archivos Eliminados

**Limpieza completada** — Los siguientes archivos fueron eliminados en la migración a Serilog:

- `src/AgenteIALocal.Core/Logging/IAgentLoggerV2.cs`
- `src/AgenteIALocal.Core/Logging/ILogSink.cs`
- `src/AgenteIALocal.Core/Logging/LogEntryTextFormatter.cs`
- `src/AgenteIALocal.Infrastructure/LoggingV2/*` (completo)
- `src/AgenteIALocalVSIX/LoggingV2/VsActivityLogSink.cs`
- `src/AgenteIALocalVSIX/AgentComposition.cs` (reemplazado por `Log.*`)

---

## 📚 Referencias

- **[PLAN_SERILOG_1.3.md](../plans/PLAN_SERILOG_1.3.md)** — Plan completo de migración
- **[Configuration](configuration.md)** — Configuración de `settings.json`
- **[Serilog Documentation](https://serilog.net/)** — Documentación oficial

---

**🏠 [Volver al índice](README.md)**
