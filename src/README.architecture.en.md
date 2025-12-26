# Architecture — Agente IA Local (classic VSIX)

> Canonical architecture document (EN). Describes decisions and technical composition verifiable in the code. Does not describe pixel-perfect UX.

## 🧭 Purpose and scope

This document describes, in a technical way and verifiable in the repository:

- The architecture of the **classic VSIX** extension (host, composition, configuration, logging).
- The separation by projects/layers in the solution.
- The single agent composition point and the real state of the LLM providers.
- Where and how configuration is persisted (Options Page and `settings.json`).

This document does **NOT** cover:

- Detailed UX/UI (layout, styles, pixel-perfect interaction). It is only mentioned when it impacts architecture.
- Full user guide.
- Sprint history.

## 🧱 Environment constraints (classic VSIX)

### AsyncPackage and autoload (with and without solution)

- The main package is an `AsyncPackage`.
- Background autoload is configured in two contexts:
  - Without solution: `UIContextGuids80.NoSolution`
  - With solution: `UIContextGuids80.SolutionExists`

Reference:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### Classic ToolWindow (WPF)

- The ToolWindow is registered from the Package with `ProvideToolWindow(...)`.
- The UI is implemented with WPF (XAML) and code-behind.

Reference:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### Command registration (VSCT) and threading considerations

- Package initialization runs command setup in `InitializeAsync` and protects access to services that require the UI thread.
- Writes to ActivityLog use fail-safe helpers and UI thread validation.

References:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`
- `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs`

## 🧩 Layer / project structure

According to the workspace, the solution contains these projects (layers) and responsibilities:

- `AgenteIALocalVSIX`
  - VSIX host: `AsyncPackage`, ToolWindow, Options Page, agent composition, logging, and access to configuration.
- `AgenteIALocal.Core`
  - Shared contracts and types (core) used by Application/Infrastructure.
- `AgenteIALocal.Application`
  - Agent orchestration/use cases (without VS SDK dependencies).
- `AgenteIALocal.Infrastructure`
  - Concrete provider/IO implementations (for example, LM Studio client and JAN stub).
- `AgenteIALocal.UI`
  - Reusable UI components (if applicable), without being the VSIX host.
- `AgenteIALocal.Tests`
  - Automated tests.

Important note:
- This document **does not state** a Target Framework. It must be **verified in `*.csproj`** (see section “Ambiguities / pending items”).

## 🧬 Composition and dependencies (single point)

### AgentComposition (fail-safe)

Runtime composition is centralized in `AgentComposition` with a **fail-safe** strategy:

- An idempotent method (`EnsureComposition()`) is exposed to avoid partial states.
- It starts with a **default mock** agent to ensure the ToolWindow can operate even without a real backend.
- It attempts to compose a real backend in the background; if composition fails, the mock is kept.

References:
- `src/AgenteIALocalVSIX/AgentComposition.cs`

#### Default mock

- The VSIX host uses an internal mock implementation (`MockAgentService`) that delegates to a mock executor.

Reference:
- `src/AgenteIALocalVSIX/AgentComposition.cs`

#### Conditions to compose the real backend

The real backend is attempted by reading `settings.json` from `AgentSettingsStore`:

- Configuration is loaded with `AgentSettingsStore.Load()`.
- The active server is selected via `activeServerId` and its entry is searched in `servers[]`.
- The real backend is composed **only** if:
  - `Provider == "lmstudio"` (case-insensitive comparison)
  - `BaseUrl` is not empty

Reference:
- `src/AgenteIALocalVSIX/AgentComposition.cs` (`TryComposeRealBackend()`)

#### What is currently out of scope

- **JAN is not wired by the current VSIX composition.**
- Infrastructure code may exist, but `AgentComposition` does not select it as a real backend.

References:
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs`

## ⚙️ Configuration

### Tools → Options (VS Settings Store)

The VSIX registers an Options Page and persists values in Visual Studio’s store (User Settings):

- Collection: `AgenteIALocal`
- Keys:
  - `BaseUrl`
  - `Model`
  - `ApiKey`

References:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs` (Options Page registration)
- `src/AgenteIALocalVSIX/Options/AgenteOptionsPage.cs`

### `settings.json` v1 (file)

There is an additional file-based configuration mechanism with a versioned schema:

- Location: `%LOCALAPPDATA%\AgenteIALocal\settings.json`
- Schema version: `v1` (`SchemaVersion = "v1"`)
- Key behavior: preserves unknown fields (keeps the original JSON and reapplies it when saving).

Expected fields (verified by load/usage in code; this does not imply they are the only ones):

- Root:
  - `version`
  - `activeServerId`
  - `servers[]`
  - `globalSettings`
  - `taskProfiles`
- In each `servers[]` element (according to usage/models):
  - `id`, `name`, `provider`, `baseUrl`, `apiKey`, `model`, `isDefault`, `createdAt`

Reference:
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

### Inline configuration in ToolWindow (current state)

Verifiable fact:

- There is inline configuration editing from the ToolWindow code-behind:
  - Loads via `AgentSettingsStore.Load()`.
  - Saves via `AgentSettingsStore.Save(settings)`.
  - Toggles visibility of an element named `SettingsPanel` from a `SettingsButton_Click` handler.

Limitation (without inventing):

- The exact layout and concrete fields of the panel (`SettingsPanel`) must be verified in XAML; this document does not detail its visual structure.

Reference:
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

## 🤖 LLM providers (real state)

### LM Studio (real)

Verifiable state:

- Real HTTP client: `LmStudioClient`.
- Endpoint resolver/normalization: `LmStudioEndpointResolver`.
- Chat completions path used by the host: `"/v1/chat/completions"`.
- Defensive response parsing.

References:
- `src/AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs`
- `src/AgenteIALocal.Infrastructure/Agents/LmStudioEndpointResolver.cs`
- `src/AgenteIALocalVSIX/AgentComposition.cs`

### JAN (stub)

Verifiable state:

- `JanServerClient` exists, but:
  - It is declared as a stub/simulated.
  - It does not perform real HTTP.
  - It is not connected to the real VSIX composition (`AgentComposition`).

References:
- `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs`
- `src/AgenteIALocalVSIX/AgentComposition.cs`

## 🧾 Observability and logging

### File logging

- Runtime writes logs to:
  - `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`
- The Package initializes the logger early and exposes it to composition.

Reference:
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs`

### ActivityLogHelper

- `ActivityLogHelper` encapsulates writes to the Visual Studio ActivityLog defensively (fail-safe).

Reference:
- `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs`

## ✅ Verifiable facts (table)

| Component | File/Class | Description | Status |
|---|---|---|---|
| Package (autoload, ToolWindow, Options) | `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs` (`AgenteIALocalVSIXPackage`) | Autoload (with/without solution), ToolWindow and Options Page | ✅ real |
| Agent composition | `src/AgenteIALocalVSIX/AgentComposition.cs` (`AgentComposition`) | Default mock + attempt of real backend (LM Studio) via `settings.json` | ✅ real |
| File-based config | `src/AgenteIALocalVSIX/AgentSettingsStore.cs` (`AgentSettingsStore`) | `settings.json` v1 in `%LOCALAPPDATA%\AgenteIALocal` and unknown fields preservation | ✅ real |
| Options Page | `src/AgenteIALocalVSIX/Options/AgenteOptionsPage.cs` (`AgenteOptionsPage`) | Persistence in VS Settings Store (collection `AgenteIALocal`) | ✅ real |
| LM Studio provider | `src/AgenteIALocal.Infrastructure/Agents/LmStudioClient.cs` (`LmStudioClient`) | Real HTTP client to `"/v1/chat/completions"` with defensive parsing | ✅ real |
| LM Studio resolver | `src/AgenteIALocal.Infrastructure/Agents/LmStudioEndpointResolver.cs` (`LmStudioEndpointResolver`) | Endpoint normalization/resolution for LM Studio | ✅ real |
| JAN provider | `src/AgenteIALocal.Infrastructure/Agents/JanServerClient.cs` (`JanServerClient`) | Simulated implementation (stub), no real HTTP and not wired in the VSIX | ⚠️ stub |
| ActivityLog | `src/AgenteIALocalVSIX/Logging/ActivityLogHelper.cs` (`ActivityLogHelper`) | Defensive writing to ActivityLog | ✅ real |

## ⚠️ Ambiguities / pending items (without inventing)

- Target Framework / TargetFrameworkVersion:
  - This document does not state a specific version.
  - **Verify in the `*.csproj`** of each project what the real target is.
  - It matters because it conditions compatibility (VS SDK, WPF, dependencies and available APIs).

## 🔗 Related documentation

- Functional document (ES): `src/README.es.md`
- Functional document (EN): `src/README.en.md`
- UX/UI (reference, no detail here): `src/Readme.UX.md`
