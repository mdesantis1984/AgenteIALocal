# Arquitectura — Agente IA Local (VSIX clásico)

> Documento de arquitectura canónico (ES). Describe decisiones y composición técnica verificable en el código. No describe UX pixel-perfect.

## 🧭 Propósito y alcance

Este documento describe, de forma técnica y verificable en el repositorio:

- La arquitectura de la extensión **VSIX clásica** (host, composición, configuración, logging).
- La separación por proyectos/capas en la solución.
- El punto único de composición del agente y el estado real de los proveedores LLM.
- Dónde y cómo se persiste configuración (Options Page y `settings.json`).

Este documento **NO** cubre:

- UX/UI detallada (layout, estilos, interacción pixel-perfect). Solo se menciona cuando impacta arquitectura.
- Guía de usuario completa.
- Historial de sprints.

## 🧱 Restricciones del entorno (VSIX clásico)

### AsyncPackage y autoload (con y sin solución)

- El paquete principal es un `AsyncPackage`.
- Se configura autoload en background en dos contextos:
  - Sin solución: `UIContextGuids80.NoSolution`
  - Con solución: `UIContextGuids80.SolutionExists`

Referencia:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### ToolWindow clásica (WPF)

- La ToolWindow se registra desde el Package con `ProvideToolWindow(...)`.
- La UI se implementa con WPF (XAML) y code-behind.

Referencia:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### Registro de comandos (VSCT) y consideraciones de hilo

- La inicialización del Package ejecuta la preparación de comandos en `InitializeAsync` y protege el acceso a servicios que requieren UI thread.
- Las escrituras a ActivityLog usan helpers fail-safe y validación de UI thread.

Referencias:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`
- `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs`

## 🧩 Estructura por capas / proyectos

Según el workspace, la solución contiene estos proyectos (capas) y responsabilidades:

- `AgenteIALocalVSIX`
  - Host VSIX: `AsyncPackage`, ToolWindow, Options Page, composición del agente, logging y acceso a configuración.
- `AgenteIALocal.Core`
  - Contratos y tipos compartidos (núcleo) usados por Application/Infrastructure.
- `AgenteIALocal.Application`
  - Orquestación/casos de uso del agente (sin dependencias de VS SDK).
- `AgenteIALocal.Infrastructure`
  - Implementaciones concretas de proveedores/IO (por ejemplo, cliente LM Studio y stub JAN).
- `AgenteIALocal.UI`
  - Componentes de UI reutilizables (si aplica), sin ser el host VSIX.
- `AgenteIALocal.Tests`
  - Pruebas automatizadas.

Nota importante:
- Este documento **no afirma** Target Framework. Debe **verificarse en `*.csproj`** (ver sección “Ambigüedades / pendientes”).

## 🧬 Composición y dependencias (punto único)

### AgentComposition (fail-safe)

La composición del runtime se centraliza en `AgentComposition` con una estrategia **fail-safe**:

- Se expone un método idempotente (`EnsureComposition()`) que evita estados parciales.
- Se arranca con un agente **mock por defecto** para asegurar que la ToolWindow puede operar aun sin backend real.
- Se intenta componer un backend real en background; si la composición falla, se mantiene el mock.

Referencias:
- `src/AgenteIALocalVSIX/AgentComposition.cs`

#### Mock por defecto

- El host VSIX usa una implementación mock interna (`MockAgentService`) que delega en un executor mock.

Referencia:
- `src/AgenteIALocalVSIX/AgentComposition.cs`

#### Condiciones para componer backend real

El backend real se intenta componer leyendo `settings.json` desde `AgentSettingsStore`:

- Se carga configuración con `AgentSettingsStore.Load()`.
- Se elige el servidor activo por `activeServerId` y se busca su entrada en `servers[]`.
- Se compone backend real **solo** si se cumple:
  - `Provider == "lmstudio"` (comparación sin sensibilidad a mayúsculas/minúsculas)
  - `BaseUrl` no vacío

Referencia:
- `src/AgenteIALocalVSIX/AgentComposition.cs` (`TryComposeRealBackend()`)

#### Qué queda fuera Actualmente

- **JAN no está cableado por la composición actual del VSIX.**
- Puede existir código en Infrastructure, pero `AgentComposition` no lo selecciona como backend real.

Referencias:
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs`

## ⚙️ Configuración

### Tools → Options (VS Settings Store)

El VSIX registra una Options Page y persiste valores en el store de Visual Studio (User Settings):

- Colección: `AgenteIALocal`
- Keys:
  - `BaseUrl`
  - `Model`
  - `ApiKey`

Referencias:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs` (registro de Options Page)
- `src/AgenteIALocalVSIX/Options/AgenteOptionsPage.cs`

### `settings.json` v1 (archivo)

Existe un mecanismo adicional de configuración por archivo con esquema versionado:

- Ubicación: `%LOCALAPPDATA%\AgenteIALocal\settings.json`
- Versión de esquema: `v1` (`SchemaVersion = "v1"`)
- Comportamiento clave: preserva campos desconocidos (mantiene el JSON original y lo reaplica al guardar).

Campos esperables (verificados por carga/uso en el código; no implica que sean los únicos):

- Raíz:
  - `version`
  - `activeServerId`
  - `servers[]`
  - `globalSettings`
  - `taskProfiles`
- En cada elemento de `servers[]` (según uso/modelos):
  - `id`, `name`, `provider`, `baseUrl`, `apiKey`, `model`, `isDefault`, `createdAt`

Referencia:
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

### Configuración inline en ToolWindow (estado actual)

Hecho verificable:

- Existe edición inline de configuración desde el code-behind de la ToolWindow:
  - Carga vía `AgentSettingsStore.Load()`.
  - Guardado vía `AgentSettingsStore.Save(settings)`.
  - Alterna visibilidad de un elemento `SettingsPanel` desde un handler `SettingsButton_Click`.

Limitación (sin inventar):

- El layout exacto y campos concretos del panel (`SettingsPanel`) deben verificarse en XAML; este documento no detalla su estructura visual.

Referencia:
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

## 🤖 Proveedores LLM (estado real)

### LM Studio (real)

Estado verificable:

- Cliente HTTP real: `LmStudioClient`.
- Resolver/normalización de endpoint: `LmStudioEndpointResolver`.
- Path de chat completions usado por el host: `"/v1/chat/completions"`.
- Parsing defensivo de respuesta.

Referencias:
- `src/AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs`
- `src/AgenteIALocal.Infrastructure/Agents/LmStudioEndpointResolver.cs`
- `src/AgenteIALocalVSIX/AgentComposition.cs`

### JAN (stub)

Estado verificable:

- Existe `JanServerClient`, pero:
  - Se declara como stub/simulado.
  - No realiza HTTP real.
  - No está conectado a la composición real del VSIX (`AgentComposition`).

Referencias:
- `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs`
- `src/AgenteIALocalVSIX/AgentComposition.cs`

## 🧾 Observabilidad y logging

### Log a archivo

- El runtime escribe logs a:
  - `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`
- El Package inicializa el logger temprano y lo expone a la composición.

Referencia:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### ActivityLogHelper

- `ActivityLogHelper` encapsula escrituras al ActivityLog de Visual Studio de forma defensiva (fail-safe).

Referencia:
- `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs`

## ✅ Hechos verificables (tabla)

| Componente | Archivo/Clase | Descripción | Estado |
|---|---|---|---|
| Package (autoload, ToolWindow, Options) | `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs` (`AgenteIALocalVSIXPackage`) | Autoload (con/sin solución), ToolWindow y Options Page | ✅ real |
| Composición del agente | `src/AgenteIALocalVSIX/AgentComposition.cs` (`AgentComposition`) | Mock por defecto + intento de backend real (LM Studio) por `settings.json` | ✅ real |
| Config por archivo | `src/AgenteIALocalVSIX/AgentSettingsStore.cs` (`AgentSettingsStore`) | `settings.json` v1 en `%LOCALAPPDATA%\AgenteIALocal` y preservación de campos desconocidos | ✅ real |
| Options Page | `src/AgenteIALocalVSIX/Options/AgenteOptionsPage.cs` (`AgenteOptionsPage`) | Persistencia en VS Settings Store (colección `AgenteIALocal`) | ✅ real |
| Proveedor LM Studio | `src/AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs` (`LmStudioClient`) | Cliente HTTP real a `"/v1/chat/completions"` con parsing defensivo | ✅ real |
| Resolver LM Studio | `src/AgenteIALocal.Infrastructure/Agents/LmStudioEndpointResolver.cs` (`LmStudioEndpointResolver`) | Normalización/resolución de endpoints para LM Studio | ✅ real |
| Proveedor JAN | `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs` (`JanServerClient`) | Implementación simulada (stub), sin HTTP real y no cableada en el VSIX | ⚠️ stub |
| ActivityLog | `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs` (`ActivityLogHelper`) | Escritura defensiva en ActivityLog | ✅ real |

## ⚠️ Ambigüedades / pendientes (sin inventar)

- Target Framework / TargetFrameworkVersion:
  - Este documento no afirma una versión concreta.
  - **Verificar en los `*.csproj`** de cada proyecto cuál es el target real.
  - Importa porque condiciona compatibilidad (VS SDK, WPF, dependencias y APIs disponibles).

## 🔗 Documentación relacionada

- Documento funcional (ES): `src/README.es.md`
- Documento funcional (EN): `src/README.en.md`
- UX/UI (referencia, sin detalle aquí): `src/Readme.UX.md`
