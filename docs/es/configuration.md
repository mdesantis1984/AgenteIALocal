# Configuración del Sistema

**[🇪🇸 Español](#) | [🇬🇧 English](../en/configuration.md)**

**Estado:** ✅ Completado  
**Planes:** [PLAN_Provider_Configuracion_2.5.md](../plans/PLAN_Provider_Configuracion_2.5.md), [PLAN_IDIOMA_1.0.md](../plans/PLAN_IDIOMA_1.0.md)

---

## 📋 Descripción General

El sistema de configuración de **Agente IA Local** utiliza dos archivos JSON separados para persistir settings del usuario:

1. **`settings.json`** — Configuración global (LLM, logging, comportamiento del agente)
2. **`language.json`** — Configuración de idioma (separada para evitar conflictos)

Ambos archivos soportan **hot reload** (cambios aplicados automáticamente sin reiniciar Visual Studio).

---

## 📁 Ubicación de Archivos

```
%LOCALAPPDATA%\AgenteIALocal\
├── settings.json            # Configuración global
├── language.json            # Configuración de idioma
├── logs/                    # Archivos de log
│   └── AgenteIALocal.log
└── languages/               # Traducciones (copiadas desde VSIX)
    ├── en-US/strings.json
    ├── fr-FR/strings.json
    └── flags/img/*.png
```

**Ejemplo ruta completa:**
```
C:\Users\{Usuario}\AppData\Local\AgenteIALocal\settings.json
```

---

## ⚙️ settings.json

### Schema Completo

```json
{
  "globalSettings": {
    "provider": "lmstudio",
    "runMode": "preguntar",
    "servers": [
      {
        "id": "default-lmstudio",
        "provider": "lmstudio",
        "baseUrl": "http://127.0.0.1:1234",
        "model": "llama-3.2-3b-instruct",
        "apiKey": ""
      }
    ],
    "activeServerId": "default-lmstudio",
    "requestDefaults": {
      "stream": true,
      "temperature": 0.7,
      "maxTokens": 0,
      "topP": 1.0,
      "stop": [],
      "presencePenalty": 0.0,
      "frequencyPenalty": 0.0,
      "streamOptions": {
        "includeUsage": true
      }
    },
    "agent": {
      "ideIntegration": true,
      "applyChanges": false,
      "maxSteps": 10
    },
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

### Sección: globalSettings

#### provider

| Campo | Tipo | Valores | Descripción |
|-------|------|---------|-------------|
| `provider` | string | `"lmstudio"`, `"jan"`, `"llamacpp"`, `"ollama"` | Proveedor LLM activo |

#### runMode

| Campo | Tipo | Valores | Descripción |
|-------|------|---------|-------------|
| `runMode` | string | `"agente"`, `"preguntar"` | Modo de ejecución del agente |

**Modos:**
- `"agente"`: Modo autónomo (ejecuta múltiples pasos, razonamiento, puede llamar tools)
- `"preguntar"`: Modo single-response (recibe pregunta → retorna respuesta única)

#### servers

Array de configuraciones de servidores LLM.

```json
{
  "id": "default-lmstudio",
  "provider": "lmstudio",
  "baseUrl": "http://127.0.0.1:1234",
  "model": "llama-3.2-3b-instruct",
  "apiKey": ""
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `id` | string | Identificador único del servidor |
| `provider` | string | Tipo de proveedor (`lmstudio`, `jan`, `llamacpp`, `ollama`) |
| `baseUrl` | string | URL del endpoint HTTP (ej: `http://127.0.0.1:1234`) |
| `model` | string | Nombre o ID del modelo |
| `apiKey` | string | API key (si es requerida por el proveedor; generalmente vacía para locales) |

#### activeServerId

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `activeServerId` | string | ID del servidor actualmente seleccionado (debe existir en `servers`) |

---

### Sección: requestDefaults

Parámetros por defecto para requests a LLM.

```json
{
  "requestDefaults": {
    "stream": true,
    "temperature": 0.7,
    "maxTokens": 0,
    "topP": 1.0,
    "stop": [],
    "presencePenalty": 0.0,
    "frequencyPenalty": 0.0,
    "streamOptions": {
      "includeUsage": true
    }
  }
}
```

| Campo | Tipo | Rango | Default | Descripción |
|-------|------|-------|---------|-------------|
| `stream` | boolean | - | `true` | Habilita streaming de respuestas (siempre `true` en modo "preguntar") |
| `temperature` | number | 0.0 - 2.0 | `0.7` | Aleatoriedad en respuestas (0.0 = determinístico, 1.0 = creativo) |
| `maxTokens` | number | >= 0 | `0` | Longitud máxima de respuesta (`0` = ilimitado) |
| `topP` | number | 0.0 - 1.0 | `1.0` | Nucleus sampling (alternativa a temperature) |
| `stop` | string[] | - | `[]` | Secuencias de parada (detiene generación cuando aparecen) |
| `presencePenalty` | number | -2.0 - 2.0 | `0.0` | Penalización por repetición de tokens (positivo = menos repetición) |
| `frequencyPenalty` | number | -2.0 - 2.0 | `0.0` | Penalización por frecuencia de tokens (positivo = menos palabras frecuentes) |

#### streamOptions.includeUsage

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `includeUsage` | boolean | Incluye estadísticas de uso (tokens prompt/completion/total) en la respuesta |

**IMPORTANTE:** Solo soportado por **LM Studio**. Si `provider != "lmstudio"`, este campo se ignora (no se envía al proveedor).

---

### Sección: agent

Configuración específica para modo **"agente"**.

```json
{
  "agent": {
    "ideIntegration": true,
    "applyChanges": false,
    "maxSteps": 10
  }
}
```

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `ideIntegration` | boolean | `true` | Habilita integración con Visual Studio (inyecta contexto del IDE en el prompt) |
| `applyChanges` | boolean | `false` | Aplica automáticamente cambios sugeridos por el agente (sin confirmación) |
| `maxSteps` | number | `10` | Máximo número de pasos de razonamiento del agente (previene loops infinitos) |

**Flujo con `ideIntegration: true`:**
1. Agente recibe contexto: solución abierta, proyectos, archivos seleccionados
2. Puede referenciar archivos automáticamente
3. Puede sugerir cambios con contexto del workspace

**Flujo con `applyChanges: true`:**
1. Agente sugiere cambio → se aplica automáticamente
2. ⚠️ **PELIGRO:** Puede modificar código sin confirmación

---

### Sección: logging

Configuración de niveles de logging.

```json
{
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
```

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

**Ver más:** [Sistema Logging](logging.md)

---

## 🌍 language.json

### Schema

```json
{
  "current": "es-AR",
  "autoDetect": true
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `current` | string | Código de idioma actual (ej: `"es-AR"`, `"en-US"`, `"fr-FR"`) |
| `autoDetect` | boolean | Habilita detección automática de idioma en próximo inicio (VS → OS → default) |

### Valores de `current`

| Valor | Idioma |
|-------|--------|
| `"es-AR"` | Español (Argentina) |
| `"en-US"` | English (United States) |
| `"fr-FR"` | Français (France) |
| `""` | (vacío) = Fuerza detección automática |

**Detección automática:**
1. Si `current == ""` o `autoDetect == true`:
   - Detecta locale de Visual Studio (DTE.LocaleID - deshabilitado actualmente)
   - Detecta locale del Sistema Operativo (`CultureInfo.CurrentUICulture`)
   - Fallback a idioma base (ej: `es-ES` → `es-AR`)
   - Si no hay coincidencia → `"en-US"`

**Ver más:** [Sistema i18n](i18n.md)

---

## 🔄 Persistencia

### Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│  UI Layer (ConfigWindow)                                │
│  - Usuario cambia settings en modal                     │
│  - SaveButton_Click()                                   │
└─────────────────────────────────────────────────────────┘
                           ↓ Llama
┌─────────────────────────────────────────────────────────┐
│  Stores (Persistence)                                   │
│  ┌───────────────────────────────────────────────────┐  │
│  │ AgentSettingsStore (settings.json)                │  │
│  │ - Load() → GlobalSettings                         │  │
│  │ - Save(GlobalSettings)                            │  │
│  │ - Round-trip preservation (unknown fields)        │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ LanguageSettingsStore (language.json)             │  │
│  │ - Load() → LanguageSettings                       │  │
│  │ - Save(LanguageSettings)                          │  │
│  │ - Atomic write (temp file → move)                 │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           ↓ Escribe
┌─────────────────────────────────────────────────────────┐
│  Archivos JSON                                          │
│  %LOCALAPPDATA%\AgenteIALocal\settings.json             │
│  %LOCALAPPDATA%\AgenteIALocal\language.json             │
└─────────────────────────────────────────────────────────┘
```

### AgentSettingsStore

**Ubicación:** `src/AgenteIALocal.Application/Settings/AgentSettingsStore.cs`

**Características:**
- ✅ **Round-trip preservation**: Preserva campos desconocidos (forward compatibility)
- ✅ **Atomic write**: Escribe a temp file → rename (evita corrupción)
- ✅ **Thread-safe**: Lock interno para reads/writes concurrentes

**Métodos:**

```csharp
public class AgentSettingsStore
{
    public GlobalSettings Load()
    {
        if (!File.Exists(_filePath))
            return CreateDefaults();

        string json = File.ReadAllText(_filePath);
        return JsonConvert.DeserializeObject<GlobalSettings>(json);
    }

    public void Save(GlobalSettings settings)
    {
        string tempPath = _filePath + ".tmp";
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);  // Atomic
    }

    private GlobalSettings CreateDefaults()
    {
        return new GlobalSettings
        {
            Provider = "lmstudio",
            RunMode = "preguntar",
            Servers = new[]
            {
                new ServerConfig
                {
                    Id = "default-lmstudio",
                    Provider = "lmstudio",
                    BaseUrl = "http://127.0.0.1:1234",
                    Model = "",
                    ApiKey = ""
                }
            },
            ActiveServerId = "default-lmstudio",
            RequestDefaults = new RequestDefaults
            {
                Stream = true,
                Temperature = 0.7,
                MaxTokens = 0
            },
            Agent = new AgentBehavior
            {
                IdeIntegration = true,
                ApplyChanges = false,
                MaxSteps = 10
            },
            Logging = new LogSettings
            {
                Enabled = true,
                All = "on"
            }
        };
    }
}
```

### LanguageSettingsStore

**Ubicación:** `src/AgenteIALocal.Localization/LanguageSettingsStore.cs`

**Archivo separado:** Evita conflictos con `settings.json` (round-trip preservation independiente).

**Métodos:**

```csharp
public class LanguageSettingsStore
{
    public LanguageSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new LanguageSettings
            {
                Current = "",  // Fuerza detección automática
                AutoDetect = true
            };
        }

        string json = File.ReadAllText(_filePath);
        return JsonConvert.DeserializeObject<LanguageSettings>(json);
    }

    public void Save(LanguageSettings settings)
    {
        string tempPath = _filePath + ".tmp";
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
```

---

## 🔄 Hot Reload

### FileSystemWatcher

Ambos archivos (`settings.json` + `language.json`) tienen **FileSystemWatcher** que detecta cambios externos.

**Flujo:**
1. Usuario edita `settings.json` en Notepad
2. FileSystemWatcher detecta evento `Changed`
3. Debounce 500ms (evita múltiples eventos)
4. `Reload()` deserializa JSON actualizado
5. `SettingsChanged` event notifica a componentes
6. UI actualiza automáticamente

**Ejemplo (language.json):**

```csharp
// En LocalizationService
_watcher = new FileSystemWatcher(_languagesRoot)
{
    Filter = "language.json",
    NotifyFilter = NotifyFilters.LastWrite,
    EnableRaisingEvents = true
};

_watcher.Changed += (s, e) =>
{
    _debounceTimer?.Stop();
    _debounceTimer = new Timer(500);
    _debounceTimer.Elapsed += (_, __) =>
    {
        var settings = new LanguageSettingsStore(_languagesRoot).Load();
        SetLanguage(settings.Current);
    };
    _debounceTimer.Start();
};
```

---

## ⚙️ UI de Configuración

### Modal de Configuración

**Abrir:** Gear icon (engranaje) en toolbar de chat window

**Pestañas:**
1. **Idioma** — Grid de idiomas con banderas (RadioButtons)
2. **LLM's Local** — Provider, servidor activo, requestDefaults, agent behavior
3. **Logging** — Niveles de logging (Verbose/Debug/Information/Warning/Error/Critical)

**Botones:**
- **Guardar** — Persiste cambios a `settings.json` + `language.json`
- **Cancelar** — Descarta cambios (restaura estado anterior)

### Live Updates

Algunos campos se persisten **automáticamente al cambiar** (sin esperar a Guardar):

- ✅ `baseUrl` (al refrescar modelos)
- ✅ `model` (al seleccionar en combo)
- ❌ `temperature`, `maxTokens`, etc. (requieren Guardar)

---

## 🛠️ Validación

### Reglas de Validación

| Campo | Regla |
|-------|-------|
| `baseUrl` | Debe ser URL válida (`http://` o `https://`), se normaliza automáticamente |
| `temperature` | Debe estar entre 0.0 y 2.0 (permite coma o punto como separador decimal) |
| `maxTokens` | Debe ser >= 0 (entero) |
| `topP` | Debe estar entre 0.0 y 1.0 |
| `presencePenalty` | Debe estar entre -2.0 y 2.0 |
| `frequencyPenalty` | Debe estar entre -2.0 y 2.0 |
| `maxSteps` | Debe ser >= 1 (entero) |

### Normalización Automática

**baseUrl:**
```csharp
// Input: "http://http//127.0.0.1:1234" (doble esquema)
// Output: "http://127.0.0.1:1234"

// Input: "127.0.0.1:1234" (sin esquema)
// Output: "http://127.0.0.1:1234"

// Input: "http://127.0.0.1:1234/v1/" (trailing slash)
// Output: "http://127.0.0.1:1234"  (normalizado)
```

**temperature:**
```csharp
// Input: "0,7" (coma europea)
// Output: 0.7 (double)

// Input: "0.7" (punto decimal)
// Output: 0.7 (double)
```

---

## 📚 Referencias

- **[PLAN_Provider_Configuracion_2.5.md](../plans/PLAN_Provider_Configuracion_2.5.md)** — Plan configuración providers
- **[PLAN_IDIOMA_1.0.md](../plans/PLAN_IDIOMA_1.0.md)** — Plan configuración idioma
- **[Sistema i18n](i18n.md)** — Detalle de `language.json`
- **[Sistema Logging](logging.md)** — Detalle de `logging.*` en `settings.json`

---

**🏠 [Volver al índice](README.md)**
