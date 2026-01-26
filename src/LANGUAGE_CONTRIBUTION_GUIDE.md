# Language Contribution Guide — Agente IA Local VSIX

**Version:** 1.0  
**Last Updated:** 2026-01-26  
**Target Audience:** External contributors

---

## 📖 Introduction

Thank you for your interest in contributing a language translation to **Agente IA Local**!

This extension supports **dynamic internationalization (i18n)**, allowing users to switch the UI language without restarting Visual Studio. Language files are loaded from external JSON files at runtime, making it easy for the community to contribute new translations.

**Current supported languages:**
- 🇦🇷 **Spanish (Argentina)** — `es-AR` (embedded fallback)
- 🇺🇸 **English (United States)** — `en-US` (external)
- 🇫🇷 **Français (France)** — `fr-FR` (external)

**You can add:** Any language with ISO 639-1 code + region (e.g., `pt-BR`, `zh-CN`, `de-DE`, `ja-JP`, etc.)

---

## 🗂️ File Structure

All language files are stored in the VSIX package under:

```
src/AgenteIALocalVSIX/Languages/
├── en-US/
│   └── strings.json          ← Translation file (UTF-8 with BOM)
├── fr-FR/
│   └── strings.json
├── pt-BR/                    ← Example future contribution
│   └── strings.json
└── flags/
    └── img/
        ├── en-US.png         ← Flag image (h40 - height 40px, proportional width)
        ├── fr-FR.png
        └── pt-BR.png
```

**At runtime**, these files are copied to:
```
%LOCALAPPDATA%/AgenteIALocal/languages/
```

**Users can also manually add/edit translations** in the `%LOCALAPPDATA%` folder for local customization.

---

## 📝 Required Files

### 1. Translation File: `strings.json`

- **Location:** `src/AgenteIALocalVSIX/Languages/{code}/strings.json`
- **Format:** JSON (nested structure)
- **Encoding:** **UTF-8 with BOM** (required for Visual Studio resource loading)
- **Size:** Typically 2-4 KB (53+ translation keys)

### 2. Flag Image: `{code}.png` (optional but recommended)

- **Location:** `src/AgenteIALocalVSIX/Languages/flags/img/{code}.png`
- **Dimensions:** **h40** (height 40px, proportional width — typically 60×40px for 3:2 ratio)
- **Format:** PNG (optimized, ~500-1100 bytes)
- **Source:** [flagcdn.com](https://flagcdn.com/h40/{countryCode}.png) (public domain)

**Example download:**
```bash
# Portuguese (Brazil) — pt-BR
https://flagcdn.com/h40/br.png → save as pt-BR.png

# German (Germany) — de-DE
https://flagcdn.com/h40/de.png → save as de-DE.png
```

---

## 🌍 Language Code Format

Use **ISO 639-1** (2-letter language) + **ISO 3166-1** (2-letter region):

| Language | Code | Example Flag |
|----------|------|--------------|
| Portuguese (Brazil) | `pt-BR` | 🇧🇷 br.png |
| German (Germany) | `de-DE` | 🇩🇪 de.png |
| Japanese (Japan) | `ja-JP` | 🇯🇵 jp.png |
| Chinese (Simplified) | `zh-CN` | 🇨🇳 cn.png |
| Arabic (Saudi Arabia) | `ar-SA` | 🇸🇦 sa.png |

**Important:** Use the **region code** for the flag (e.g., `pt-BR` → `br.png`, `en-US` → `us.png`).

---

## 📐 JSON Schema (Complete)

### Required Structure

```json
{
  "metadata": {
    "code": "pt-BR",
    "name": "Portuguese (Brazil)",
    "nativeName": "Português (Brasil)",
    "flag": "pt-BR.png",
    "version": "1.0.0",
    "author": "Your Name or GitHub Username"
  },
  "ui": {
    "config": {
      "window": {
        "title": "Translation of: Local AI Agent Chat - Configuration"
      },
      "sidebar": {
        "idioma": "Translation of: Language",
        "llm": "Translation of: Local LLM's",
        "logging": "Translation of: Logging"
      },
      "buttons": {
        "save": "Translation of: Save",
        "cancel": "Translation of: Cancel"
      },
      "idioma": {
        "page": {
          "title": "Translation of: Language Configuration",
          "description": "Translation of: Select interface language"
        },
        "languages": {
          "es-AR": "Translation of: Spanish (Argentina)",
          "en-US": "Translation of: English (United States)",
          "fr-FR": "Translation of: French (France)",
          "pt-BR": "Translation of: Portuguese (Brazil)",
          "de-DE": "Translation of: German (Germany)"
        }
      },
      "llm": {
        "page": {
          "title": "Translation of: Local LLM Configuration",
          "description": "Translation of: Configure your local language model provider"
        },
        "provider": {
          "label": "Translation of: LLM Provider"
        },
        "server": {
          "label": "Translation of: Active Server",
          "activeServerId": "Translation of: Server ID",
          "baseUrl": "Translation of: Base URL",
          "model": "Translation of: Model",
          "apiKey": "Translation of: API Key"
        },
        "runmode": {
          "label": "Translation of: Run Mode",
          "agente": "Translation of: Agent",
          "preguntar": "Translation of: Ask"
        },
        "advanced": {
          "title": "Translation of: Advanced Settings",
          "temperature": "Translation of: Temperature",
          "maxTokens": "Translation of: Max Tokens",
          "includeUsage": "Translation of: Include usage statistics",
          "ideIntegration": "Translation of: IDE Integration",
          "applyChanges": "Translation of: Apply changes automatically",
          "maxSteps": "Translation of: Max steps"
        }
      },
      "logging": {
        "page": {
          "title": "Translation of: Logging Configuration",
          "description": "Translation of: Configure logging levels and output"
        },
        "enabled": "Translation of: Logging enabled",
        "basic": "Translation of: Basic levels",
        "advanced": "Translation of: Advanced levels",
        "levels": {
          "verbose": "Translation of: Verbose",
          "debug": "Translation of: Debug",
          "information": "Translation of: Information",
          "warning": "Translation of: Warning",
          "error": "Translation of: Error",
          "critical": "Translation of: Critical"
        },
        "tooltips": {
          "enabled": "Translation of: Enable/disable all logging",
          "verbose": "Translation of: Detailed trace information",
          "debug": "Translation of: Debug-level diagnostic information",
          "information": "Translation of: General informational messages",
          "warning": "Translation of: Warning messages (non-critical issues)",
          "error": "Translation of: Error messages (failures)",
          "critical": "Translation of: Critical failures (requires immediate attention)"
        }
      },
      "tooltips": {
        "sidebar": {
          "idioma": "Translation of: Change interface language",
          "llm": "Translation of: Configure local LLM providers",
          "logging": "Translation of: Configure logging levels"
        },
        "llm": {
          "provider": "Translation of: Select your local LLM provider (LM Studio, JAN, etc.)",
          "activeServerId": "Translation of: Unique identifier for this server configuration",
          "baseUrl": "Translation of: HTTP endpoint URL (e.g., http://127.0.0.1:1234)",
          "model": "Translation of: Model name or identifier",
          "apiKey": "Translation of: API key (if required by provider)",
          "runmode": "Translation of: Agent (autonomous) or Ask (single response)",
          "temperature": "Translation of: Randomness in responses (0.0 = deterministic, 1.0 = creative)",
          "maxTokens": "Translation of: Maximum response length (0 = unlimited)",
          "includeUsage": "Translation of: Include token usage in response",
          "ideIntegration": "Translation of: Enable Visual Studio integration features",
          "applyChanges": "Translation of: Automatically apply suggested code changes",
          "maxSteps": "Translation of: Maximum agent reasoning steps"
        },
        "buttons": {
          "save": "Translation of: Save all configuration changes",
          "cancel": "Translation of: Cancel and discard changes"
        },
        "idioma": {
          "select": "Translation of: Click to select this language"
        }
      }
    },
    "chat": {
      "window": {
        "title": "Translation of: Local AI Agent Chat"
      },
      "placeholder": "Translation of: Type your message here, you can press # to reference files",
      "labels": {
        "mode": "Translation of: Mode"
      },
      "tooltips": {
        "solution": "Translation of: Current solution name",
        "projects": "Translation of: Number of projects in solution",
        "config": "Translation of: Current configuration status",
        "settings": "Translation of: Open configuration window",
        "newChat": "Translation of: Create new chat session",
        "acceptChanges": "Translation of: Accept and apply suggested changes",
        "rejectChanges": "Translation of: Reject and discard suggested changes",
        "refresh": "Translation of: Refresh log from file",
        "copyAll": "Translation of: Copy all log content to clipboard",
        "openLog": "Translation of: Open log file in external editor",
        "deleteLog": "Translation of: Delete log file from disk"
      }
    },
    "errors": {
      "llm": {
        "connectionFailed": "Translation of: Could not connect to LLM provider",
        "invalidUrl": "Translation of: Invalid endpoint URL",
        "timeout": "Translation of: Request timeout"
      },
      "settings": {
        "loadFailed": "Translation of: Failed to load configuration",
        "saveFailed": "Translation of: Failed to save configuration"
      }
    }
  }
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `metadata.code` | string | ✅ Yes | ISO 639-1 + region (e.g., `pt-BR`) |
| `metadata.name` | string | ✅ Yes | English name (e.g., "Portuguese (Brazil)") |
| `metadata.nativeName` | string | ✅ Yes | Native name (e.g., "Português (Brasil)") |
| `metadata.flag` | string | ✅ Yes | Flag filename (e.g., "pt-BR.png") |
| `metadata.version` | string | ✅ Yes | Translation version (e.g., "1.0.0") |
| `metadata.author` | string | ✅ Yes | Your name or GitHub username |
| `ui.*` | object | ✅ Yes | All UI translation keys (nested structure) |

**Note:** All keys under `ui.*` are **required**. Missing keys will display the raw key name (e.g., `ui.missing.key`).

---

## 📋 Translation Rules

### 1. File Format
- **Encoding:** UTF-8 **with BOM** (Visual Studio requirement)
- **Line Endings:** CRLF (Windows) or LF (Unix) — both accepted
- **Indentation:** 2 spaces (consistent with existing files)
- **Validation:** Use [jsonlint.com](https://jsonlint.com/) to verify valid JSON

### 2. Metadata Requirements
- `code`: Must match folder name (e.g., folder `pt-BR/` → `"code": "pt-BR"`)
- `nativeName`: **Must be in the target language** (e.g., "Português (Brasil)", NOT "Portuguese (Brazil)")
- `flag`: Must match PNG filename (e.g., `"flag": "pt-BR.png"`)

### 3. Translation Keys
- **Do NOT modify key names** (e.g., keep `ui.config.sidebar.idioma`, do NOT change to `ui.config.sidebar.language`)
- **Translate only values** (right side of `:`)
- **Preserve placeholders:** If a key contains `{0}`, keep it in the translation
  ```json
  "modifiedFilesCount": "Cambios ({0})"  // Spanish
  "modifiedFilesCount": "Changes ({0})"  // English
  ```

### 4. Context-Aware Translation
- **ui.config.llm.runmode.agente** = Autonomous agent mode (executes multiple steps)
- **ui.config.llm.runmode.preguntar** = Single-response mode (ask and receive answer)
- **ui.config.logging.levels.*** = Standard log levels (Verbose < Debug < Information < Warning < Error < Critical)

### 5. Flag Image
- **Required dimensions:** h40 (height 40px, width proportional — typically 60×40px for 3:2 ratio)
- **Format:** PNG (optimized for web)
- **Source:** [flagcdn.com](https://flagcdn.com/) (public domain)
- **Download URL pattern:** `https://flagcdn.com/h40/{countryCode}.png`
  - Example: `https://flagcdn.com/h40/br.png` for Brazil
  - Save as: `{languageCode}.png` (e.g., `pt-BR.png`)

---

## 🛠️ Step-by-Step Guide

### Step 1: Create Translation File

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create folder** for your language:
   ```
   src/AgenteIALocalVSIX/Languages/{code}/
   ```
   Example: `src/AgenteIALocalVSIX/Languages/pt-BR/`

4. **Create `strings.json`** inside the folder:
   ```
   src/AgenteIALocalVSIX/Languages/pt-BR/strings.json
   ```

5. **Copy template** from `en-US/strings.json` (see section "Complete Examples" below)

6. **Translate all values** (keep keys unchanged)

### Step 2: Add Flag Image (Optional but Recommended)

1. **Download flag** from flagcdn.com:
   ```
   https://flagcdn.com/h40/{countryCode}.png
   ```
   Example: `https://flagcdn.com/h40/br.png` for Brazil

2. **Save as** `{code}.png`:
   ```
   src/AgenteIALocalVSIX/Languages/flags/img/pt-BR.png
   ```

3. **Verify dimensions:** 60×40px (or similar proportional h40)

### Step 3: Test Locally

1. **Build the VSIX project** in Visual Studio
2. **Install the VSIX** (double-click `.vsix` file or F5 debug)
3. **Open Config Window** (gear icon in toolbar)
4. **Navigate to "Idioma" tab** (Language)
5. **Verify your language appears** in the grid with flag + native name
6. **Click RadioButton** → UI should update immediately to your language
7. **Verify all labels/buttons/tooltips** are translated correctly

**Testing tips:**
- If your language does NOT appear → check `%LOCALAPPDATA%/AgenteIALocal/languages/` folder (VSIX should auto-copy on first run)
- If flag is missing → verify PNG file is in `flags/img/` and filename matches `metadata.flag`
- If UI does NOT update → check Output window (View → Output → Show output from: Debug) for errors

### Step 4: Submit Pull Request

1. **Commit changes:**
   ```bash
   git add src/AgenteIALocalVSIX/Languages/pt-BR/strings.json
   git add src/AgenteIALocalVSIX/Languages/flags/img/pt-BR.png
   git commit -m "Add Portuguese (Brazil) translation (pt-BR)"
   ```

2. **Push to your fork:**
   ```bash
   git push origin feature/add-pt-BR-translation
   ```

3. **Open Pull Request** on GitHub with the following:
   - **Title:** `Add [Language Name] translation ([code])`
   - **Description:** Use template below
   - **Screenshot:** Include a screenshot of the UI with your translation active

---

## 📸 Pull Request Template

```markdown
## Translation Contribution: [Language Name] ([code])

**Language:** [Native Name] ([code])  
**Author:** [@YourGitHubUsername]  
**Translation Coverage:** [X/53 keys] (100% if complete)

### Checklist
- [ ] `strings.json` created with all 53+ keys translated
- [ ] Encoding is UTF-8 with BOM
- [ ] Flag image `{code}.png` added (h40 dimensions)
- [ ] Metadata fields completed (`code`, `name`, `nativeName`, `flag`, `version`, `author`)
- [ ] Tested locally (F5 debug, language appears in grid, UI updates correctly)
- [ ] Screenshot attached showing UI with new language active

### Screenshot
![UI in {Language}](screenshot.png)

### Notes
(Optional: Add any context-specific translation notes, e.g., regional variations, formal vs. informal tone, etc.)
```

---

## 📚 Complete Examples

### Example 1: Spanish (Argentina) — `es-AR`

**File:** `src/AgenteIALocalVSIX/Languages/es-AR/strings.json`  
**Note:** This language is **embedded** in code as fallback (readonly), but external file can override it.

```json
{
  "metadata": {
    "code": "es-AR",
    "name": "Spanish (Argentina)",
    "nativeName": "Español (Argentina)",
    "flag": "es-AR.png",
    "version": "1.0.0",
    "author": "AgenteIALocal Team"
  },
  "ui": {
    "config": {
      "window": {
        "title": "Chat de Agente IA Local - Configuración"
      },
      "sidebar": {
        "idioma": "Idioma",
        "llm": "LLM's Local",
        "logging": "Logging"
      },
      "buttons": {
        "save": "Guardar",
        "cancel": "Cancelar"
      },
      "idioma": {
        "page": {
          "title": "Configuración de Idioma",
          "description": "Seleccione el idioma de la interfaz"
        },
        "languages": {
          "es-AR": "Español (Argentina)",
          "en-US": "Inglés (Estados Unidos)",
          "fr-FR": "Francés (Francia)",
          "pt-BR": "Portugués (Brasil)",
          "de-DE": "Alemán (Alemania)"
        }
      },
      "llm": {
        "page": {
          "title": "Configuración de LLM's Locales",
          "description": "Configure su proveedor de modelo de lenguaje local"
        },
        "provider": {
          "label": "Proveedor LLM"
        },
        "server": {
          "label": "Servidor Activo",
          "activeServerId": "ID del Servidor",
          "baseUrl": "URL Base",
          "model": "Modelo",
          "apiKey": "Clave API"
        },
        "runmode": {
          "label": "Modo de Ejecución",
          "agente": "Agente",
          "preguntar": "Preguntar"
        },
        "advanced": {
          "title": "Configuración Avanzada",
          "temperature": "Temperatura",
          "maxTokens": "Tokens Máximos",
          "includeUsage": "Incluir estadísticas de uso",
          "ideIntegration": "Integración IDE",
          "applyChanges": "Aplicar cambios automáticamente",
          "maxSteps": "Pasos máximos"
        }
      },
      "logging": {
        "page": {
          "title": "Configuración de Logging",
          "description": "Configure los niveles de registro y salida"
        },
        "enabled": "Logging habilitado",
        "basic": "Niveles básicos",
        "advanced": "Niveles avanzados",
        "levels": {
          "verbose": "Detallado",
          "debug": "Depuración",
          "information": "Información",
          "warning": "Advertencia",
          "error": "Error",
          "critical": "Crítico"
        },
        "tooltips": {
          "enabled": "Habilitar/deshabilitar todo el logging",
          "verbose": "Información de rastreo detallada",
          "debug": "Información de diagnóstico a nivel de depuración",
          "information": "Mensajes informativos generales",
          "warning": "Mensajes de advertencia (problemas no críticos)",
          "error": "Mensajes de error (fallos)",
          "critical": "Fallos críticos (requieren atención inmediata)"
        }
      },
      "tooltips": {
        "sidebar": {
          "idioma": "Cambiar idioma de la interfaz",
          "llm": "Configurar proveedores LLM locales",
          "logging": "Configurar niveles de registro"
        },
        "llm": {
          "provider": "Seleccione su proveedor LLM local (LM Studio, JAN, etc.)",
          "activeServerId": "Identificador único para esta configuración de servidor",
          "baseUrl": "URL del endpoint HTTP (ej: http://127.0.0.1:1234)",
          "model": "Nombre o identificador del modelo",
          "apiKey": "Clave API (si es requerida por el proveedor)",
          "runmode": "Agente (autónomo) o Preguntar (respuesta única)",
          "temperature": "Aleatoriedad en las respuestas (0.0 = determinístico, 1.0 = creativo)",
          "maxTokens": "Longitud máxima de respuesta (0 = ilimitado)",
          "includeUsage": "Incluir uso de tokens en la respuesta",
          "ideIntegration": "Habilitar funciones de integración con Visual Studio",
          "applyChanges": "Aplicar cambios de código sugeridos automáticamente",
          "maxSteps": "Pasos máximos de razonamiento del agente"
        },
        "buttons": {
          "save": "Guardar todos los cambios de configuración",
          "cancel": "Cancelar y descartar cambios"
        },
        "idioma": {
          "select": "Haga clic para seleccionar este idioma"
        }
      }
    },
    "chat": {
      "window": {
        "title": "Chat de Agente IA Local"
      },
      "placeholder": "Escribe tu consulta aquí, puedes presionar # para hacer referencia a un archivo de la solución / proyecto",
      "labels": {
        "mode": "Modo"
      },
      "tooltips": {
        "solution": "Nombre de la solución actual",
        "projects": "Número de proyectos en la solución",
        "config": "Estado de la configuración actual",
        "settings": "Abrir ventana de configuración",
        "newChat": "Crear nueva sesión de chat",
        "acceptChanges": "Aceptar y aplicar cambios sugeridos",
        "rejectChanges": "Rechazar y descartar cambios sugeridos",
        "refresh": "Actualizar registro desde archivo",
        "copyAll": "Copiar todo el contenido del registro al portapapeles",
        "openLog": "Abrir archivo de registro en editor externo",
        "deleteLog": "Eliminar archivo de registro del disco"
      }
    },
    "errors": {
      "llm": {
        "connectionFailed": "No se pudo conectar con el proveedor LLM",
        "invalidUrl": "URL de endpoint no válida",
        "timeout": "Tiempo de espera agotado"
      },
      "settings": {
        "loadFailed": "Error al cargar configuración",
        "saveFailed": "Error al guardar configuración"
      }
    }
  }
}
```

### Example 2: English (United States) — `en-US`

**File:** `src/AgenteIALocalVSIX/Languages/en-US/strings.json`

```json
{
  "metadata": {
    "code": "en-US",
    "name": "English (United States)",
    "nativeName": "English (United States)",
    "flag": "en-US.png",
    "version": "1.0.0",
    "author": "AgenteIALocal Team"
  },
  "ui": {
    "config": {
      "window": {
        "title": "Local AI Agent Chat - Configuration"
      },
      "sidebar": {
        "idioma": "Language",
        "llm": "Local LLM's",
        "logging": "Logging"
      },
      "buttons": {
        "save": "Save",
        "cancel": "Cancel"
      },
      "idioma": {
        "page": {
          "title": "Language Configuration",
          "description": "Select interface language"
        },
        "languages": {
          "es-AR": "Spanish (Argentina)",
          "en-US": "English (United States)",
          "fr-FR": "French (France)",
          "pt-BR": "Portuguese (Brazil)",
          "de-DE": "German (Germany)"
        }
      },
      "llm": {
        "page": {
          "title": "Local LLM Configuration",
          "description": "Configure your local language model provider"
        },
        "provider": {
          "label": "LLM Provider"
        },
        "server": {
          "label": "Active Server",
          "activeServerId": "Server ID",
          "baseUrl": "Base URL",
          "model": "Model",
          "apiKey": "API Key"
        },
        "runmode": {
          "label": "Run Mode",
          "agente": "Agent",
          "preguntar": "Ask"
        },
        "advanced": {
          "title": "Advanced Settings",
          "temperature": "Temperature",
          "maxTokens": "Max Tokens",
          "includeUsage": "Include usage statistics",
          "ideIntegration": "IDE Integration",
          "applyChanges": "Apply changes automatically",
          "maxSteps": "Max steps"
        }
      },
      "logging": {
        "page": {
          "title": "Logging Configuration",
          "description": "Configure logging levels and output"
        },
        "enabled": "Logging enabled",
        "basic": "Basic levels",
        "advanced": "Advanced levels",
        "levels": {
          "verbose": "Verbose",
          "debug": "Debug",
          "information": "Information",
          "warning": "Warning",
          "error": "Error",
          "critical": "Critical"
        },
        "tooltips": {
          "enabled": "Enable/disable all logging",
          "verbose": "Detailed trace information",
          "debug": "Debug-level diagnostic information",
          "information": "General informational messages",
          "warning": "Warning messages (non-critical issues)",
          "error": "Error messages (failures)",
          "critical": "Critical failures (requires immediate attention)"
        }
      },
      "tooltips": {
        "sidebar": {
          "idioma": "Change interface language",
          "llm": "Configure local LLM providers",
          "logging": "Configure logging levels"
        },
        "llm": {
          "provider": "Select your local LLM provider (LM Studio, JAN, etc.)",
          "activeServerId": "Unique identifier for this server configuration",
          "baseUrl": "HTTP endpoint URL (e.g., http://127.0.0.1:1234)",
          "model": "Model name or identifier",
          "apiKey": "API key (if required by provider)",
          "runmode": "Agent (autonomous) or Ask (single response)",
          "temperature": "Randomness in responses (0.0 = deterministic, 1.0 = creative)",
          "maxTokens": "Maximum response length (0 = unlimited)",
          "includeUsage": "Include token usage in response",
          "ideIntegration": "Enable Visual Studio integration features",
          "applyChanges": "Automatically apply suggested code changes",
          "maxSteps": "Maximum agent reasoning steps"
        },
        "buttons": {
          "save": "Save all configuration changes",
          "cancel": "Cancel and discard changes"
        },
        "idioma": {
          "select": "Click to select this language"
        }
      }
    },
    "chat": {
      "window": {
        "title": "Local AI Agent Chat"
      },
      "placeholder": "Type your message here, you can press # to reference files",
      "labels": {
        "mode": "Mode"
      },
      "tooltips": {
        "solution": "Current solution name",
        "projects": "Number of projects in solution",
        "config": "Current configuration status",
        "settings": "Open configuration window",
        "newChat": "Create new chat session",
        "acceptChanges": "Accept and apply suggested changes",
        "rejectChanges": "Reject and discard suggested changes",
        "refresh": "Refresh log from file",
        "copyAll": "Copy all log content to clipboard",
        "openLog": "Open log file in external editor",
        "deleteLog": "Delete log file from disk"
      }
    },
    "errors": {
      "llm": {
        "connectionFailed": "Could not connect to LLM provider",
        "invalidUrl": "Invalid endpoint URL",
        "timeout": "Request timeout"
      },
      "settings": {
        "loadFailed": "Failed to load configuration",
        "saveFailed": "Failed to save configuration"
      }
    }
  }
}
```

---

## 🔍 Validation Checklist

Before submitting your PR, verify:

- [ ] **File location:** `src/AgenteIALocalVSIX/Languages/{code}/strings.json`
- [ ] **Encoding:** UTF-8 with BOM (open in Notepad++ → Encoding → UTF-8-BOM)
- [ ] **Valid JSON:** Paste into [jsonlint.com](https://jsonlint.com/) → should show "Valid JSON"
- [ ] **Metadata complete:** All 6 fields (`code`, `name`, `nativeName`, `flag`, `version`, `author`)
- [ ] **All keys present:** 53+ keys under `ui.*` (compare with `en-US/strings.json`)
- [ ] **No key modifications:** Only values translated, keys unchanged
- [ ] **Flag image:** `flags/img/{code}.png` present (h40 dimensions)
- [ ] **Local testing:** Built + installed VSIX → language appears in grid → UI updates correctly
- [ ] **Screenshot:** Captured with new language active

---

## 🌐 Current Translation Keys (Reference)

Total keys: **53+**

### Metadata (6 keys)
```
metadata.code
metadata.name
metadata.nativeName
metadata.flag
metadata.version
metadata.author
```

### Config Window (30+ keys)
```
ui.config.window.title
ui.config.sidebar.idioma
ui.config.sidebar.llm
ui.config.sidebar.logging
ui.config.buttons.save
ui.config.buttons.cancel
ui.config.idioma.page.title
ui.config.idioma.page.description
ui.config.idioma.languages.*
ui.config.llm.page.title
ui.config.llm.page.description
ui.config.llm.provider.label
ui.config.llm.server.*
ui.config.llm.runmode.*
ui.config.llm.advanced.*
ui.config.logging.page.*
ui.config.logging.levels.*
ui.config.logging.tooltips.*
ui.config.tooltips.*
```

### Chat Window (15+ keys)
```
ui.chat.window.title
ui.chat.placeholder
ui.chat.labels.mode
ui.chat.tooltips.*
```

### Errors (6 keys)
```
ui.errors.llm.*
ui.errors.settings.*
```

**Total:** 53+ keys (may increase with future UI additions)

---

## ❓ FAQ

### Q: Can I add a regional variant of an existing language?

**A:** Yes! Examples:
- `en-GB` (English UK) vs `en-US` (English US)
- `es-ES` (Spain Spanish) vs `es-AR` (Argentina Spanish)
- `pt-PT` (Portugal Portuguese) vs `pt-BR` (Brazil Portuguese)

Each variant needs its own folder + `strings.json` + flag.

### Q: What if I only translate 50% of the keys?

**A:** The extension will display:
- Translated keys → your translation
- Missing keys → raw key name (e.g., `ui.config.llm.provider.label`)

We recommend **100% translation** for best user experience.

### Q: Can I test without building the entire VSIX?

**A:** Yes! Manual testing:
1. Copy your `strings.json` to:
   ```
   %LOCALAPPDATA%/AgenteIALocal/languages/{code}/strings.json
   ```
2. Copy flag PNG to:
   ```
   %LOCALAPPDATA%/AgenteIALocal/languages/flags/img/{code}.png
   ```
3. **Restart Visual Studio** (or F5 debug existing VSIX)
4. Open Config → Idioma tab → your language should appear

### Q: What happens if my JSON is invalid?

**A:** The extension will:
1. Log error to Output window
2. Skip loading your language (it won't appear in grid)
3. Continue with other languages (no crash)

**Fix:** Validate JSON at [jsonlint.com](https://jsonlint.com/) before submitting.

### Q: Can I change the translation later?

**A:** Yes! Submit a new PR with the same filename:
```
src/AgenteIALocalVSIX/Languages/{code}/strings.json
```

The extension supports **hot reload** — editing the file in `%LOCALAPPDATA%` reflects changes immediately (no restart needed).

### Q: Do I need to modify `.csproj` or C# code?

**A:** **NO**! The system is fully dynamic:
- `.csproj` has wildcard includes (`Languages/**/*.json`, `Languages/flags/img/*.png`)
- C# code scans filesystem at runtime
- Just add your files → build → works automatically

---

## 🚀 Quick Start (TL;DR)

1. **Copy** `en-US/strings.json` to `{yourCode}/strings.json`
2. **Translate** all values (keep keys)
3. **Download** flag PNG (h40) to `flags/img/{yourCode}.png`
4. **Build** VSIX + **Test** (F5)
5. **Submit PR** with screenshot

**That's it!** 🎉

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/mdesantis1984/AgenteIALocal/issues)
- **Discussions:** [GitHub Discussions](https://github.com/mdesantis1984/AgenteIALocal/discussions)
- **Email:** (see `PRIVACY.md` for contact)

---

## 📜 License

All contributed translations will be licensed under the same license as the main repository (see `LICENSE.md`).

By submitting a translation, you agree to license it under the project's terms.

---

**Thank you for contributing to Agente IA Local!** 🌍✨
