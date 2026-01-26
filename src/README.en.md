# Agente IA Local (classic VSIX)

A **classic VSIX** extension for Visual Studio that integrates a local AI agent inside the IDE. The goal is to enable a **prompt → execution → result** flow from a ToolWindow, using the basic available context (solution/projects) and local configuration (Options + `settings.json`).

> Canonical functional document (EN). For detailed UX and architecture decisions, see the links at the end.

## What it is

**Agente IA Local** adds an entry point inside Visual Studio (menu `Tools`) that opens a ToolWindow (`AgenteIALocalToolWindow`) with a WPF UI (`AgenteIALocalControl`). From there the user can:

- Create/select/delete chats (persisted locally)
- Write a prompt and execute it
- View the rendered output in a conversation area
- Manage a list of changes (currently simulated)
- View execution logs
- Adjust configuration from Options and from an inline panel

**What problem it solves**
- Run a local agent inside Visual Studio with persistent configuration and reproducible traces on disk.

**What it does NOT try to solve**
- It does not implement (in the current state of the code) streaming, multi-agent, or real application of changes to the workspace.
- It does not replace UX documentation or architecture documentation: this file describes **how to use what exists today**.

**Why classic VSIX (not SDK-style)**
- The host process is based on `AsyncPackage`, VSCT and a classic ToolWindow to integrate with Visual Studio’s lifecycle and command system.

## Current product state

✅ **VSIX / Package load**
- Package: `AgenteIALocalVSIXPackage`.
- Autoload: `ProvideAutoLoad(UIContextGuids80.NoSolution)` and `ProvideAutoLoad(UIContextGuids80.SolutionExists)`.
- ToolWindow registration: `ProvideToolWindow(typeof(AgenteIALocalToolWindow))`.

✅ **ToolWindow and execution**
- ToolWindow: `ToolWindows/AgenteIALocalToolWindow.cs`.
- Main UI: `ToolWindows/AgenteIALocalControl.xaml` + `AgenteIALocalControl.xaml.cs`.
- Execution state handling: `ExecutionState` enum (Idle/Running/Completed/Error) and bindable properties (`StateIconKind`, `StateColor`, `StateLabel`).

⚠️ **LLM backend**
- There is composition with an alternative:
  - Default: `MockAgentExecutor` via `AgentComposition.MockAgentService`.
  - Real backend (LM Studio only in the current VSIX composition): `AgentComposition.TryComposeRealBackend()` creates `LmStudioClient` + `Application.AgentService` and exposes a synchronous adapter.
- JAN in Infrastructure is implemented as a simulation: `AgenteIALocal.Infrastructure/Agents/JanServerClient.cs` returns a simulated response.

✅ **Active sprint**
- Active sprint: **009.7** (documentation consolidation). This sprint does not add features; it consolidates documentation.

## Requirements and compatibility (VS / .NET / classic VSIX limitations)

- Visual Studio: designed to run in an experimental instance (debug) and as an installable VSIX.
- VSIX target: defined by the `.csproj` files of each project in the solution.
- Typical classic VSIX limitations (in this repo):
  - Manual composition (no DI container in the VSIX host).
  - UI must be fail-safe: UI exceptions are caught/ignored to avoid breaking the ToolWindow.

## How to install and run (real steps)

1) Open the solution in Visual Studio.
2) Set the VSIX project as startup (standard extension debugging).
3) Run with **Start Experimental Instance**.
4) In the experimental instance: menu `Tools → Agente IA Local`.

Code evidence:
- Command: `Commands/OpenAgenteIALocalCommand.cs`.
- ToolWindow opening: use of `IVsUIShell.FindToolWindow(...).Show()`.


## How to use it (real user flow)

### 1) Open the ToolWindow
- Action: `Tools → Agente IA Local`.
- Result: `AgenteIALocalToolWindow` is created/activated and `AgenteIALocalControl` is loaded.

### 2) View status and availability
- The UI shows “Solution/Projects” counters (default `0` until updated by the host process).
- The execution state is shown with icon/color and text (`Idle`, `Running`, `Completed`, `Error`).

### 3) Work with chats
- Chat selector: `ChatComboBox`.
- Create chat: `NewChatButton_Click` (with confirmation).
- Delete chat: `DeleteChatButton_Click` (with confirmation).

Persistence:
- The UI uses `ChatStore.LoadAll()`, `ChatStore.CreateNew()`, `ChatStore.Delete()` (namespace `AgenteIALocalVSIX.Chats`).

### 4) Execute a prompt
- The user types into `PromptTextBox`.
- Execute with the button (send icon) or with Enter (Enter sends, Shift+Enter keeps a newline): `PromptTextBox_KeyDown`.

Real execution:
- `RunButton_Click` builds an `AgentHostRequest` using:
  - `Action`: user text
  - `SolutionName` and `ProjectCount`: UI values
- Then it executes in background:
  - If `AgentComposition.AgentService != null`: `AgentService.Execute(req)`
  - Otherwise: alternative `MockAgentExecutor.Execute(req)`

### 5) Review results and “changes”
- The response is shown in `ResponseJsonText` (read-only) with pre-processing `ChatRenderPreprocessor.Preprocess(...)`.
- Changes section:
  - The `ModifiedFiles` list is initialized with mock values.
  - Apply/Revert/Clear buttons show confirmations and, in the case of Clear, empty the list.

## Current UX/UI (summary + link)

- The ToolWindow includes:
  - Header with counters (solution/projects), status and configuration/help access.
  - Chat toolbar (history + actions).
  - Main conversation area.
  - “Changes accordion” with actions.
  - Bottom bar with mode/model/server combos and the run button.

📎 Full UX specification: [Readme.UX.md](Readme.UX.md)

## Configuration (Tools > Options + settings.json + inline if present)

### 1) Tools → Options
- Page: `Options/AgenteOptionsPage.cs`.
- Persistence: `ShellSettingsManager` + `WritableSettingsStore` in the `AgenteIALocal` collection.
- Fields (per code): `BaseUrl`, `Model`, `ApiKeyValue`.

> Note: some Options Page attributes and descriptions are in English in the code, but the behavior is as described above.

### 2) `settings.json` (per-user file)
- Store: `AgentSettingsStore` (`src/AgenteIALocalVSIX/AgentSettingsStore.cs`).
- Location: `%LOCALAPPDATA%\AgenteIALocal\settings.json`.
- Schema: `version: v1`, `servers[]`, `globalSettings`, `taskProfiles`, `activeServerId`.
- Key behavior:
  - If the file does not exist, it is created with a default server `lmstudio-local`.
  - `Save` preserves unknown fields using `_raw` (`JObject`).

### 3) Inline configuration (ToolWindow)
- In `AgenteIALocalControl.xaml.cs`:
  - `AgentSettingsStore.Load()` is loaded and the inline panel is populated (`PopulateSettingsPanel`).
  - Changes are persisted with `SaveSettingsButton_Click` → `AgentSettingsStore.Save(settings)`.

## Supported LLM providers (LM Studio, JAN) and how they are selected

### LM Studio (supported for real execution)
- HTTP client: `AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs`.
- Base endpoint: `LmStudioEndpointResolver` (Infrastructure).
- Default endpoint used by the VSIX composition: `ChatCompletionsPath = "/v1/chat/completions"`.

Selection at runtime:
- `AgentComposition.TryComposeRealBackend()` reads `settings.json`.
- It only activates the real backend if `srv.Provider == "lmstudio"` and `BaseUrl` has a value.

### JAN (current state)
- `JanServerClient` exists but is currently a **simulated implementation** (it does not perform real HTTP), returning a fixed string.
- In the ToolWindow there is UI that shows “JAN” as an option in a ComboBox, but that selection is not connected to real composition in `AgentComposition`.

## Observability and logging (where to see logs, what is recorded)

### File log
- Location: `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.
- The Package registers a V2 logging pipeline on initialization: `AgenteIALocalVSIXPackage.InitializeAsync` wires `AgentComposition.LoggerV2`.
- The ToolWindow also writes to that file via the V2 file sink when available, and falls back to a local append-to-file helper when necessary.

### Visual Studio ActivityLog (V2 sink)
- The VSIX uses a V2 sink that writes to the Visual Studio activity/diagnostic log: `src/AgenteIALocalVSIX/LoggingV2/VsActivityLogSink.cs`.

### Logging implementation components
- VSIX sink: `LoggingV2/VsActivityLogSink.cs` (writes to Visual Studio ActivityLog defensively).
- Infra sink: `LoggingV2/VsixFileLogSink.cs` (writes formatted entries to local files under `%LOCALAPPDATA%\AgenteIALocal\logs`).
- Core formatter: `Logging/LogEntryTextFormatter.cs` (text formatting of log entries).
- Core V2 contract: `Logging/IAgentLoggerV2` (V2 logger interface used by composition).

### What is recorded (minimum verifiable)
- Package initialization events.
- Command registration/execution.
- ToolWindow opening.
- Run click and state transitions.

## Solution structure (real projects and responsibility)

- `AgenteIALocalVSIX`
  - VSIX host (Package, commands, ToolWindow, Options, settings.json and logging).
- `AgenteIALocal.Core`
  - Provider models and settings (e.g. `AgentProviderType`, `LmStudioSettings`, `JanServerSettings`).
- `AgenteIALocal.Application`
  - Agent services and logging contracts (V2 logger abstractions).
- `AgenteIALocal.Infrastructure`
  - Provider clients (e.g. `LmStudioClient`, `JanServerClient` and endpoint resolvers).
- `AgenteIALocal.UI`
  - Reusable UI components (if applicable; the main ToolWindow lives in the VSIX).
- `AgenteIALocal.Tests`
  - Tests (if present; not described here).

## Troubleshooting (common errors and what to check)

### The command appears but clicking it does not open the ToolWindow
- Check the ActivityLog and `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.
- Confirm that the Package loaded (autoload) and that `OpenAgenteIALocalCommand.InitializeAsync` registered the command.

### Run disabled / incomplete configuration
- Check `%LOCALAPPDATA%\AgenteIALocal\settings.json`:
  - `activeServerId` must point to an existing server.
  - The active server must have non-empty `baseUrl` and `model` for the UI to enable Run.

### Empty response or HTTP error with LM Studio
- Verify `BaseUrl` and that the `/v1/chat/completions` endpoint exists.
- Review errors reported by `LmStudioClient`:
  - “Endpoint not configured”
  - “Non-JSON response from LM Studio”
  - WebException errors with body, if applicable.

### “JAN” is selected in the UI but the real provider does not change
- Current behavior: server selection in the UI is not connected to `AgentComposition`.
- With `settings.json`, the real backend is only composed when the provider is `lmstudio`.

## Related documentation (links)

- [README.en.md](../README.en.md)
- [Readme.UX.md](Readme.UX.md)
- [README.architecture.es.md](../README.architecture.es.md)
- [README.architecture.en.md](../README.architecture.en.md)


