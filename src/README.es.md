# Agente IA Local (VSIX clásico)

Extensión **VSIX clásica** para Visual Studio que integra un agente de IA local dentro del IDE. Su foco es permitir ejecutar un flujo de **prompt → ejecución → resultado** desde una ToolWindow, usando el contexto básico disponible (solución/proyectos) y configuración local (Options + `settings.json`).

> Documento funcional canónico (ES). Para UX detallada y decisiones de arquitectura, ver los enlaces al final.

## Qué es

**Agente IA Local** agrega un punto de entrada dentro de Visual Studio (menú `Tools`) que abre una ToolWindow (`AgenteIALocalToolWindow`) con una UI WPF (`AgenteIALocalControl`). Desde ahí el usuario puede:

- Crear/seleccionar/eliminar chats (persistidos localmente)
- Escribir un prompt y ejecutarlo
- Ver la salida renderizada en un área de conversación
- Gestionar una lista de cambios (actualmente simulada)
- Consultar logs de ejecución
- Ajustar configuración desde Options y desde un panel inline

**Qué problema resuelve**
- Ejecutar un agente local dentro de Visual Studio con configuración persistente y trazas reproducibles en disco.

**Qué NO intenta resolver**
- No implementa (en el estado actual del código) transmisión en flujo, multiagente, ni aplicación real de cambios al workspace.
- No reemplaza documentación de UX ni documentación de arquitectura: este archivo describe **cómo se usa lo que existe hoy**.

**Por qué VSIX clásico (no SDK-style)**
- El proceso anfitrión se basa en `AsyncPackage`, VSCT y ToolWindow clásica para integrarse con el ciclo de vida de Visual Studio y su sistema de comandos.

## Estado actual del producto

✅ **VSIX / carga del Package**
- Package: `AgenteIALocalVSIXPackage`.
- Autoload: `ProvideAutoLoad(UIContextGuids80.NoSolution)` y `ProvideAutoLoad(UIContextGuids80.SolutionExists)`.
- Registro de ToolWindow: `ProvideToolWindow(typeof(AgenteIALocalToolWindow))`.

✅ **ToolWindow y ejecución**
- ToolWindow: `ToolWindows/AgenteIALocalToolWindow.cs`.
- UI principal: `ToolWindows/AgenteIALocalControl.xaml` + `AgenteIALocalControl.xaml.cs`.
- Manejo de estados de ejecución: enum `ExecutionState` (Idle/Running/Completed/Error) y propiedades bindables (`StateIconKind`, `StateColor`, `StateLabel`).

⚠️ **Backend LLM**
- Existe composición con alternativa:
  - Default: `MockCopilotExecutor` vía `AgentComposition.MockAgentService`.
  - Backend real (solo LM Studio en la composición VSIX actual): `AgentComposition.TryComposeRealBackend()` crea `LmStudioClient` + `Application.AgentService` y expone un adaptador síncrono.
- JAN en Infrastructure está como implementación simulada: `AgenteIALocal.Infrastructure/Agents/JanServerClient.cs` devuelve respuesta simulada.

✅ **Sprint activo**
- Sprint activo: **009.7** (documentación integral). Este sprint no agrega features; consolida documentación.

## Requisitos y compatibilidad (VS / .NET / limitaciones VSIX clásico)

- Visual Studio: diseñado para ejecutar en instancia experimental (debug) y como VSIX instalable.
- Target del VSIX: definido por los `.csproj` de cada proyecto dentro de la solución.
- Limitaciones típicas de VSIX clásico (en este repo):
  - Composición manual (sin contenedor DI en el host VSIX).
  - UI debe ser fail-safe: excepciones en UI se capturan/ignoran para no romper la ToolWindow.

## Cómo instalar y ejecutar (pasos reales)

1) Abrir la solución en Visual Studio.
2) Establecer el proyecto VSIX como startup (depuración estándar de extensiones).
3) Ejecutar con **Start Experimental Instance**.
4) En la instancia experimental: menú `Tools → Agente IA Local`.

Evidencia en código:
- Comando: `Commands/OpenAgenteIALocalCommand.cs`.
- Apertura de ToolWindow: uso de `IVsUIShell.FindToolWindow(...).Show()`.

## Cómo se usa (flujo de usuario real)

### 1) Abrir la ToolWindow
- Acción: `Tools → Agente IA Local`.
- Resultado: se crea/activa `AgenteIALocalToolWindow` y se carga `AgenteIALocalControl`.

### 2) Ver estado y disponibilidad
- La UI muestra contadores “Solution/Projects” (por defecto `0` hasta que se actualicen desde el proceso anfitrión).
- El estado de ejecución se muestra con icono/color y texto (`Idle`, `Running`, `Completed`, `Error`).

### 3) Trabajar con chats
- Selector de chat: `ChatComboBox`.
- Crear chat: `NewChatButton_Click` (con confirmación).
- Eliminar chat: `DeleteChatButton_Click` (con confirmación).

Persistencia:
- La UI usa `ChatStore.LoadAll()`, `ChatStore.CreateNew()`, `ChatStore.Delete()` (namespace `AgenteIALocalVSIX.Chats`).

### 4) Ejecutar un prompt
- El usuario escribe en `PromptTextBox`.
- Ejecuta con el botón (icono enviar) o con Enter (Enter envía, Shift+Enter mantiene salto): `PromptTextBox_KeyDown`.

Ejecución real:
- `RunButton_Click` arma un `CopilotRequest` usando:
  - `Action`: texto del usuario
  - `SolutionName` y `ProjectCount`: valores de UI
- Luego ejecuta en background:
  - Si `AgentComposition.AgentService != null`: `AgentService.Execute(req)`
  - Si no: alternativa `MockCopilotExecutor.Execute(req)`

### 5) Revisar resultados y “cambios”
- La respuesta se muestra en `ResponseJsonText` (solo lectura) con preprocesamiento `ChatRenderPreprocessor.Preprocess(...)`.
- Sección Changes:
  - La lista `ModifiedFiles` está inicializada con valores mock.
  - Botones Apply/Revert/Clear muestran confirmaciones y, en el caso de Clear, vacían la lista.

## UX/UI actual (resumen + link)

- La ToolWindow implementa:
  - Header con contadores (solución/proyectos), estado y accesos a configuración/ayuda.
  - Toolbar de chat (historial + acciones).
  - Área principal de conversación.
  - “Changes accordion” con acciones.
  - Barra inferior con combos de modo/modelo/servidor y botón de ejecución.

📎 Especificación UX completa: [Readme.UX.md](../Readme.UX.md)

## Configuración (Tools > Options + settings.json + inline si existe)

### 1) Tools → Options
- Página: `Options/AgenteOptionsPage.cs`.
- Persistencia: `ShellSettingsManager` + `WritableSettingsStore` en la colección `AgenteIALocal`.
- Campos (por código): `BaseUrl`, `Model`, `ApiKeyValue`.

> Nota: los atributos de la Options Page y algunas descripciones están en inglés en el código, pero el comportamiento es el indicado arriba.

### 2) `settings.json` (archivo por usuario)
- Store: `AgentSettingsStore` (`src/AgenteIALocalVSIX/AgentSettingsStore.cs`).
- Ubicación: `%LOCALAPPDATA%\AgenteIALocal\settings.json`.
- Esquema: `version: v1`, `servers[]`, `globalSettings`, `taskProfiles`, `activeServerId`.
- Comportamiento clave:
  - Si no existe el archivo, se crea con un server default `lmstudio-local`.
  - `Save` preserva campos desconocidos usando `_raw` (`JObject`).

### 3) Configuración inline (ToolWindow)
- En `AgenteIALocalControl.xaml.cs`:
  - Se carga `AgentSettingsStore.Load()` y se puebla el panel inline (`PopulateSettingsPanel`).
  - Se persisten cambios con `SaveSettingsButton_Click` → `AgentSettingsStore.Save(settings)`.

## Proveedores LLM soportados (LM Studio, JAN) y cómo se seleccionan

### LM Studio (soportado en ejecución real)
- Cliente HTTP: `AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs`.
- Endpoint base: `LmStudioEndpointResolver` (Infrastructure).
- Endpoint usado por default en composición VSIX: `ChatCompletionsPath = "/v1/chat/completions"`.

Selección en ejecución:
- `AgentComposition.TryComposeRealBackend()` lee `settings.json`.
- Solo activa backend real si `srv.Provider == "lmstudio"` y `BaseUrl` tiene valor.

### JAN (estado actual)
- Existe `JanServerClient` pero actualmente es una **implementación simulada** (no realiza HTTP real), devuelve un texto fijo.
- En la ToolWindow existe UI que muestra “JAN” como opción en un ComboBox, pero esa selección no está conectada a composición real en `AgentComposition`.

## Observabilidad y logging (dónde ver logs, qué se registra)

### Log en archivo
- Ubicación: `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.
- El Package registra un logger simple al inicializar: `AgenteIALocalVSIXPackage.InitializeAsync`.
- La ToolWindow también escribe en ese archivo (cuando `AgentComposition.Logger` no está disponible, usa alternativa local).

### ActivityLog de Visual Studio
- Helper: `Logging/ActivityLogHelper.cs`.
- Uso: el comando registra eventos y errores en ActivityLog cuando puede.

### Qué se registra (mínimo verificable)
- Eventos de inicialización del Package.
- Registro/ejecución del comando.
- Apertura de ToolWindow.
- Click de Run y transición de estados.

## Estructura de la solución (proyectos reales y responsabilidad)

- `AgenteIALocalVSIX`
  - Host VSIX (Package, comandos, ToolWindow, Options, settings.json y logging).
- `AgenteIALocal.Core`
  - Modelos y settings de proveedores (por ejemplo `AgentProviderType`, `LmStudioSettings`, `JanServerSettings`).
- `AgenteIALocal.Application`
  - Servicios de agente y contratos de logging (por ejemplo `Application.Agents.AgentService`, `IAgentLogger`).
- `AgenteIALocal.Infrastructure`
  - Clientes de proveedores (por ejemplo `LmStudioClient`, `JanServerClient` y resolvers de endpoint).
- `AgenteIALocal.UI`
  - Componentes UI reutilizables (si aplica; la ToolWindow principal está en el VSIX).
- `AgenteIALocal.Tests`
  - Pruebas (si existen en el proyecto; no se describen aquí).

## Troubleshooting (errores típicos y qué verificar)

### El comando aparece pero al click no abre la ToolWindow
- Verificar el log en ActivityLog y en `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.
- Confirmar que el Package cargó (autoload) y que `OpenAgenteIALocalCommand.InitializeAsync` registró el comando.

### Run deshabilitado / configuración incompleta
- Revisar `%LOCALAPPDATA%\AgenteIALocal\settings.json`:
  - `activeServerId` debe apuntar a un server existente.
  - El server activo debe tener `baseUrl` y `model` no vacíos para que la UI habilite Run.

### Respuesta vacía o error HTTP con LM Studio
- Verificar `BaseUrl` y que el endpoint `/v1/chat/completions` exista.
- Revisar errores registrados por `LmStudioClient`:
  - “Endpoint not configured”
  - “Non-JSON response from LM Studio”
  - Errores WebException con body si aplica.

### Se selecciona “JAN” en UI pero no cambia el proveedor real
- Comportamiento actual: la selección de servidor en UI no está conectada a `AgentComposition`.
- Con `settings.json`, solo se compone backend real si el proveedor es `lmstudio`.

## Documentación relacionada (links)

- [README.en.md](../README.en.md)
- [Readme.UX.md](../Readme.UX.md)
- [README.architecture.es.md](../README.architecture.es.md)
- [README.architecture.en.md](../README.architecture.en.md)
