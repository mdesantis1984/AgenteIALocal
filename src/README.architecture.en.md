# Agente IA Local (VSIX) — Architecture, Scope and Roadmap

> Stable baseline tag: `vsix-stable-baseline`

---

## 🇪🇸 Español

### 1. Purpose of the document

This document defines in a **exhaustive and binding** way the architecture, scope, phases and technical decisions of the **Agente IA Local (VSIX)** project.

Objectives:
- Resume the project at any time without loss of context.
- Avoid reopening decisions that have already been validated.
- Serve as an architectural reference for development, maintenance and coordination with tools (Copilot).

---

### 2. Current status and stable baseline

There is a stable baseline, tagged and published as:
- Tag: **`vsix-stable-baseline`**

This milestone guarantees:
- Classic VSIX operational.
- `AsyncPackage` loading correctly via `ProvideAutoLoad`.
- Commands registered and executing.
- ToolWindow opening correctly.
- Code without temporary diagnostic hacks.
- Clean repo (no pending changes).

Rule: **this baseline must not be broken**. Any feature is developed from a branch created from this point.

---

### 3. Closed technical decisions (DO NOT reopen)

1. Project model: **classic VSIX** (NO modern SDK-style).
2. Target framework: **.NET Framework 4.8**.

[OBSOLETO]
- Note: The value "Target framework: .NET Framework 4.8" was left as a historical decision. In practice some projects in the workspace (especially the VSIX project of this iteration) target **.NET Framework 4.7.2** to keep compatibility with the current build environment. Keep this entry for historical traceability; when a single stable target is decided, the baseline will be updated.

3. Build/Debug: **Visual Studio Stable** (VS 2022 or VS 2026 Stable).
4. Visual Studio Insiders: only to install/test an already generated `.vsix`, not for build/debug.
5. The Package must autoload: mandatory use of `ProvideAutoLoad`.
6. Architecture: **Clean Architecture** by layers.
7. AI integration: **HTTP** using an **OpenAI-compatible** API.
8. Primary provider in Phase 1: **LM Studio (local)**.
9. Future remote endpoint planned: `https://ia.thiscloud.com.ar` (not implemented in Phase 1).
10. Configuration in **Tools → Options**, not inside the ToolWindow.

---

### 4. Product scope

The product is driven with a combined scope:
- **C (first):** Controlled technical prototype to validate architecture, AI integration and flow.
- **B (later):** Evolution towards a publishable extension (Marketplace) with hardening and standards.

---

### 5. Phase plan

#### Phase 1 — Functional AI prototype (main objective: local AI)

Objective:
- Robust integration with an LLM over HTTP, using LM Studio as the primary provider.

Includes:
- OpenAI-compatible HTTP client (chat completions as base).
- Persistent configuration (Base URL, Model, API Key).
- Options Page (Tools → Options → Agente IA Local).
- Minimal functional ToolWindow (prompt → visible response).

Excludes (Phase 1):
- Configurable timeout (origin handles it).
- Configuration inside the ToolWindow.
- Advanced history.
- Advanced tools/function calling and structured outputs (only planned).

#### Phase 2 — UX

Objective:
- Improve experience: states, streaming UI, clearer errors, layout.

#### Phase 3 — Publishable

Objective:
- Hardening, versioning, compatibility, final documentation, publication criteria.

---

### 6. Layered architecture (Clean Architecture)

Expected layers:
- **AgenteIALocalVSIX**: VSIX Package, Commands, ToolWindow, Options.
- **Core**: contracts, entities, value objects.
- **Application**: use cases, orchestration.
- **Infrastructure**: HTTP clients, adapters, concrete persistence.
- **UI**: XAML/WPF for the ToolWindow (if separated, or within the VSIX).

Rule:
- UI depends on Application.
- Application depends on Core.
- Infrastructure implements interfaces defined by Core/Application.
- UI **must not** access Infrastructure directly.

[Architecture note]
- In recent practice the separation was reinforced: `Core` defines `IAgentService` and DTOs (`CopilotRequest/Response`), `Application` orchestrates calls and `Infrastructure` contains `HttpAgentClient`, `LmStudioClient` and `JanServerClient` as adapters. This separation enables testing the UI with mocks and allows manual composition from the `AsyncPackage`.

---

### 7. VSIX components (classic)

- `AsyncPackage`:
  - Initializes and registers commands.
  - Registers ToolWindow.
  - Must autoload in common contexts.
- VSCT:
  - Defines groups, commands and placements.
  - Can show menu items even if the Package is not loaded.
- Command handler:
  - Registered in `OleMenuCommandService`.
  - Executes `ShowToolWindowAsync`.
- ToolWindowPane:
  - Host of WPF control (XAML).

[Practical note]
- The proven pattern for command registration was to replicate a working example: `Instance` property, `InitializeAsync` that runs `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync`, obtaining `OleMenuCommandService` and `new MenuCommand(...)/AddCommand`. Strict adherence to this pattern ensures that clicking the menu invokes the handler's `Execute`.

---

### 8. Package autoload (critical)

Operational fact (learned and validated):
- The menu can appear via VSCT.
- But if the Package does not load, the command is not registered and **does not execute**.

Therefore the Package must autoload for typical contexts:
- `UIContextGuids80.NoSolution`
- `UIContextGuids80.SolutionExists`

This point is part of the stable baseline.

---

### 9. AI integration (LM Studio first)

Decision:
- Protocol: **HTTP REST**.
- API: **OpenAI-compatible**.

Planned endpoints (LM Studio):
- `chat/completions` (primary in Phase 1)
- Streaming `chat/completions` (incrementally enabled)
- `responses`, `tools`, `structured output`, `embeddings` (future)

Base URL:
- Local: `http://localhost:<port>` (LM Studio)
- Future remote: `https://ia.thiscloud.com.ar`

Headers:
- `Content-Type: application/json`
- `Authorization: Bearer <ApiKey>` (although local may ignore it, it must exist for future remote)

[Multiple providers]
- The current design contemplates **multiple providers**. Besides LM Studio, there is support by configuration for `JanServer` (remote alternative) and selection is made at initialization time using `AgentSettings` (Provider type). The `AsyncPackage` performs manual composition: reads `AgentSettings`, resolves types by reflection when applicable and assigns the concrete implementation to `AgentComposition.AgentService`.

---

### 10. Configuration (Tools → Options)

Confirmed scope:
1. Base URL configurable: **Yes**
2. Model configurable (string): **Yes**
3. API Key / token: **Yes**
4. Timeout configurable: **No** (handled by the origin)
5. Persistence: **Yes** (WritableSettingsStore)
6. UI in Tools → Options: **Yes**
7. Config UI in ToolWindow: **No**

Persistence:
- `WritableSettingsStore` (UserSettings)
- Stable section/key (for example: `AgenteIALocal`)

---

### 11. UI/UX — Phase 1

#### ToolWindow (Agente IA Local)

Objective:
- Execute requests to the LLM and show results.

Minimum structure:
- Header with status: `Not configured / Ready / Error`
- Multiline prompt TextBox
- Buttons: `Send`, `Test connection`, `Clear`
- Response area (scrollable text)

Rules:
- If Base URL or Model is missing → disable Send and show instruction: “Configure in Tools → Options”.

#### Options

Objective:
- Persistently configure the endpoint and credentials.

---

### 12. Error handling and logging

Errors to cover (Phase 1):
- Empty / invalid URL
- Empty Model
- 401/403 (invalid API Key)
- 404 (incompatible endpoint)
- 5xx
- Non-compatible JSON
- No connection

Rules:
- Show a short error in the ToolWindow.
- Internal log in ActivityLog.
- Do not leave permanent diagnostic MessageBoxes.

[Logging and abstractions]
- An abstraction of logging used by the extension was introduced (`IAgentLogger` / `AgentComposition.Logger`) and a concrete file implementation (`FileAgentLogger`) that writes traces to:
  `%LOCALAPPDATA%\\AgenteIALocal\\logs\\AgenteIALocal.log`.
- Current logging practice includes traces in:
  - `AsyncPackage.InitializeAsync` (startup, switch to UI thread, commands initialization)
  - Command registration in `OpenAgenteIALocalCommand.InitializeAsync` (start and registration)
  - Handler execution `Execute` (first mandatory log)
  - ToolWindow events (ctor, Loaded, Run click, controlled errors)
- Operational rules: do not silence errors without log; any caught exception must record `Logger.Error` and, when applicable, `ActivityLogHelper.TryLogError`.

---

### 13. Phase 1 task plan (order)

**Phase 1.1 — Infrastructure settings/persistence**
- `AgentSettings` (BaseUrl, Model, ApiKey)
- Provider with `WritableSettingsStore`

**Phase 1.2 — Options Page**
- Tools → Options
- Simple bindings

**Phase 1.3 — OpenAI-compatible HTTP client**
- Implementation `HttpAgentClient`
- Request to `chat/completions`

**Phase 1.4 — Minimal integration in ToolWindow**
- Test connection button
- Send prompt and show response

---

### 14. Versioning, Git and releases

- Do not work on tags.
- Create branches from `vsix-stable-baseline`.
- Increment version in `source.extension.vsixmanifest` per milestone.

---

### 15. Non-regression checklist

Before each relevant commit:
- ToolWindow opens.
- Command executes.
- Package autoload active.
- Build OK (VS Stable).
- No temporary MessageBoxes.

---

## 🇺🇸 English

### 1. Document purpose

This document defines, in a **binding and exhaustive** way, the architecture, scope, phases, and technical decisions of the **Agente IA Local (VSIX)** project.

Goals:
- Resume the project at any time without losing context.
- Prevent reopening validated decisions.
- Provide an architectural reference for development, maintenance, and tooling coordination (Copilot).

---

### 2. Current status and stable baseline

There is a stable baseline, tagged and published as:
- Tag: **`vsix-stable-baseline`**

This milestone guarantees:
- A working classic VSIX.
- `AsyncPackage` loads properly via `ProvideAutoLoad`.
- Commands are registered and executed.
- The ToolWindow opens correctly.
- No temporary diagnostic hacks remain.
- A clean repository state.

Rule: **do not break this baseline**. All features must be developed on branches created from this point.

---

### 3. Closed technical decisions (DO NOT reopen)

1. Project model: **classic VSIX** (NO modern SDK-style VSIX).
2. Target framework: **.NET Framework 4.8**.

[OBSOLETO]
- Note: The item "Target framework: .NET Framework 4.8" remains documented as a historical decision. In practice, some projects in the workspace (notably the VSIX project in the current iteration) target **.NET Framework 4.7.2** to maintain compatibility with the current build environment. Keep this entry for historical traceability; update the baseline when a single stable target is selected.

3. Build/Debug: **Visual Studio Stable** only (VS 2022 or VS 2026 Stable).
4. Visual Studio Insiders: only for installing/testing a built `.vsix`, not for build/debug.
5. Package autoload is mandatory: `ProvideAutoLoad` must be present.
6. Architecture: **Clean Architecture** layering.
7. IA integration: **HTTP** using an **OpenAI-compatible** API.
8. Phase 1 primary provider: **LM Studio (local)**.
9. Future remote endpoint planned: `https://ia.thiscloud.com.ar` (not implemented in Phase 1).
10. Configuration lives in **Tools → Options**, not inside the ToolWindow.

---

### 4. Product scope

The product follows a combined scope:
- **C (first):** a controlled technical prototype to validate architecture, IA integration, and flow.
- **B (later):** evolve into a publishable Marketplace-ready extension with hardening and standards.

---

### 5. Phase plan

#### Phase 1 — Functional IA prototype (primary goal: local IA)

Goal:
- Robust IA integration over HTTP, with LM Studio as the primary provider.

Includes:
- OpenAI-compatible HTTP client (chat completions as the base).
- Persistent configuration (Base URL, Model, API Key).
- Options Page (Tools → Options → Agente IA Local).
- Minimal functional ToolWindow (prompt → visible response).

Excludes (Phase 1):
- Configurable timeouts (handled by the origin).
- Configuration UI inside the ToolWindow.
- Advanced conversation history.
- Advanced tools/function calling and structured outputs (planned only).

#### Phase 2 — UX

Goal:
- Improve user experience: states, UI streaming, clearer errors, layout.

#### Phase 3 — Publishable

Goal:
- Hardening, versioning, compatibility, final docs, release criteria.

---

### 6. Layered architecture (Clean Architecture)

Expected layers:
- **AgenteIALocalVSIX**: VSIX Package, Commands, ToolWindow, Options.
- **Core**: contracts, entities, value objects.
- **Application**: use cases, orchestration.
- **Infrastructure**: HTTP clients, adapters, concrete persistence.
- **UI**: XAML/WPF for the ToolWindow.

Rule:
- UI depends on Application.
- Application depends on Core.
- Infrastructure implements interfaces defined by Core/Application.
- UI must not access Infrastructure directly.

---

### 7. Classic VSIX components

- `AsyncPackage`:
  - Initializes and registers commands.
  - Registers the ToolWindow.
  - Must autoload in common contexts.
- VSCT:
  - Defines groups, commands, and placements.
  - Can show menu entries even when the Package is not loaded.
- Command handler:
  - Registered via `OleMenuCommandService`.
  - Executes `ShowToolWindowAsync`.
- ToolWindowPane:
  - Hosts a WPF control (XAML).

[Practical note]
- The proven registration pattern for commands mirrors a functional example: `Instance` property, `InitializeAsync` that runs `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync`, obtain `OleMenuCommandService` and create `new MenuCommand(...)/AddCommand`. Strictly following this pattern ensures that clicking the menu invokes the handler's `Execute`.

---

### 8. Package autoload (critical)

Validated operational fact:
- VSCT can show a menu entry.
- But if the Package does not load, the command is not registered and **will not execute**.

Therefore, the Package must autoload in typical contexts:
- `UIContextGuids80.NoSolution`
- `UIContextGuids80.SolutionExists`

This requirement is part of the stable baseline.

---

### 9. IA integration (LM Studio first)

Decision:
- Protocol: **HTTP REST**.
- API: **OpenAI-compatible**.

Planned endpoints (LM Studio):
- `chat/completions` (Phase 1 primary)
- Streaming `chat/completions` (incremental enablement)
- `responses`, `tools`, `structured output`, `embeddings` (future)

Base URL:
- Local: `http://localhost:<port>` (LM Studio)
- Future remote: `https://ia.thiscloud.com.ar`

Headers:
- `Content-Type: application/json`
- `Authorization: Bearer <ApiKey>` (even if local ignores it, required for future remote)

---

### 10. Configuration (Tools → Options)

Confirmed scope:
1. Configurable base URL: **Yes**
2. Configurable model (free string): **Yes**
3. API Key / token: **Yes**
4. Configurable timeout: **No** (origin-managed)
5. Persistence: **Yes** (`WritableSettingsStore`)
6. UI in Tools → Options: **Yes**
7. Config UI in ToolWindow: **No**

Persistence:
- `WritableSettingsStore` (UserSettings)
- Stable section/key (e.g., `AgenteIALocal`)

---

### 11. UI/UX — Phase 1

#### ToolWindow (Agente IA Local)

Goal:
- Send requests to the LLM and display results.

Minimal structure:
- Header status: `Not configured / Ready / Error`
- Multiline prompt TextBox
- Buttons: `Send`, `Test connection`, `Clear`
- Response area (scrollable text)

Rules:
- If Base URL or Model is missing, disable Send and show: “Configure in Tools → Options”.

---

### 12. Error handling and logging

Errors to cover (Phase 1):
- Empty/invalid URL
- Empty model
- 401/403 (invalid API key)
- 404 (incompatible endpoint)
- 5xx
- Non-compatible JSON
- No connection

Rules:
- Show a short actionable error in the ToolWindow.
- Log internally via ActivityLog.
- Do not keep permanent diagnostic MessageBoxes.

[Logging and abstractions]
- An `IAgentLogger` abstraction (`AgentComposition.Logger`) and a concrete file logger implementation (`FileAgentLogger`) were introduced. The file logger writes to:
  `%LOCALAPPDATA%\\AgenteIALocal\\logs\\AgenteIALocal.log`.
- Logging is used extensively across package initialization, command registration, and ToolWindow operations to provide traceability and to simplify debugging in Experimental Instance.
- Operational rule: capture exceptions, log error details, and avoid rethrowing from Package initialization to keep VS stable.

---

### 13. Phase 1 task plan (order)

**Phase 1.1 — Settings & persistence**
- `AgentSettings` (BaseUrl, Model, ApiKey)
- Provider using `WritableSettingsStore`

**Phase 1.2 — Options Page**
- Tools → Options
- Simple bindings

**Phase 1.3 — OpenAI-compatible HTTP client**
- `HttpAgentClient`
- `chat/completions` request

**Phase 1.4 — Minimal ToolWindow end-to-end**
- Test connection button
- Send prompt and show response

---

### 14. Versioning, Git and releases

- Do not work on tags.
- Create branches from `vsix-stable-baseline`.
- Bump version in `source.extension.vsixmanifest` per milestone.

---

### 15. Non-regression checklist

Before each relevant commit:
- ToolWindow opens.
- Command executes.
- Package autoload active.
- Build passes (VS Stable).
- No temporary MessageBoxes.

---


---

### Recent decisions and architectural notes (incremental additions)

- VSCT ↔ Package wiring: GUID mismatches that prevented handlers' `Execute` from being invoked were corrected. Consistency validation and logging were added during `Package.InitializeAsync`.
- Manual composition of `AgentService`: due to classic VSIX constraints a DI container is not used. The `AsyncPackage` reads `AgentSettings` at initialization time and composes `AgentClient`/`AgentService` manually (reflection conditioned by Provider) and assigns the instance to `AgentComposition.AgentService`.
- Multi-provider support: `LmStudio` and `JanServer` are conceptually supported. Selection is based on `AgentSettings.Provider` and type resolution during Package initialization.
- Persistent logging: `FileAgentLogger` was added for local traces and integrated with `ActivityLogHelper` when necessary.
- Fail-safe in initialization: `Package.InitializeAsync` avoids rethrowing fatal exceptions, logs errors and continues in a safe state.

[Operational note]
- Do not use DI containers inside the classic VSIX: the accepted practice in this project is manual composition in the Package and exposure through `AgentComposition` to minimize footprint and avoid host lifecycle issues.

---

If there are additional architecture items that need clarifying (diagrams, sequence flows or decision records), add them as incremental PRs referencing this document and linking to the `vsix-stable-baseline` tag.

---

## Sprint 2 — Architectural Status and Decisions

Confirmed runtime diagnosis

- `AgenteIALocal.Infrastructure.dll` was observed to be not loaded/packaged at VSIX runtime during Experimental Instance testing.
- Reflection-based resolution of infrastructure types failed (Type.GetType(...) returned null for infrastructure types expected to be present).
- As a consequence, the `AgentService` composition did not complete and remained `null` at runtime.

Impact

- The backend cannot be composed, preventing end-to-end agent execution.
- The UI correctly reports the backend as unavailable (ToolWindow displays the appropriate status messages and disables actions that require the service).

Non-goals for Sprint 2

- Packaging the backend or changing VSIX packaging rules was intentionally out of scope for Sprint 2. The sprint focused on UI/UX and observability.

Decision

- Create a focused Sprint 3 to address VSIX packaging and Infrastructure loading issues.
- Treat the packaging and infrastructure loading task as technical debt to be resolved at the packaging/composition level (not via UI workarounds).

Architectural note

- Avoid applying UI-side workarounds to mask missing infrastructure assemblies; the correct fix must be applied at the VSIX packaging and composition level so that `AgenteIALocal.Infrastructure` is present and loadable at runtime.
- The fix will likely involve ensuring the assembly is included in the VSIX container, verifying project references and MSBuild packaging properties, and validating runtime probing paths or assembly resolution strategies if necessary.
