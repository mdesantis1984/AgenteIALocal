# Agente IA Local (VSIX) — Architecture, Scope and Roadmap

> Stable baseline tag: `vsix-stable-baseline`

---

## 🇪🇸 Español

### 1. Purpose of the document

This document defines, in a **exhaustive and binding** way, the architecture, scope, phases, and technical decisions of the **Agente IA Local (VSIX)** project.

Objectives:
- Resume the project at any time without loss of context.
- Avoid reopening decisions that have already been validated.
- Serve as an architectural reference for development, maintenance, and coordination with tools (Copilot).

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

[DEPRECATED]
- Note: The value "Target framework: .NET Framework 4.8" was left as a historical decision. In practice some projects in the workspace (especially the VSIX project of this iteration) target **.NET Framework 4.7.2** to keep compatibility with the current build environment. Keep this entry for historical traceability; when a single stable target is decided, the baseline will be updated.

3. Build/Debug: **Visual Studio Stable** (VS 2022 or VS 2026 Stable).
4. Visual Studio Insiders: only to install/test an already generated `.vsix`, not for build/debug.
5. The Package must autoload: mandatory use of `ProvideAutoLoad`.
6. Architecture: **Clean Architecture** by layers.
7. AI integration: **HTTP** using an **OpenAI-compatible** API.
8. Primary provider in Phase 1: **LM Studio (local)**.
9. Future remote endpoint planned: `https://ia.thiscloud.com.ar` (not implemented in Phase 1).
10. Configuration in **Tools → Options**, not inside the ToolWindow (except for an inline edit panel introduced in Sprint 3.3).

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
- Configuration inside of ToolWindow (except inline editing panel in Sprint 3.3).
- Advanced conversation history.
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

### 9. IA integration (LM Studio first)

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
- `Authorization: Bearer <ApiKey>` (even if local ignores it, required for future remote)

[Multiple providers]
- The current design contemplates **multiple providers**. Besides LM Studio, there is support by configuration for `JanServer` (remote alternative) and selection is made at initialization time using the `AgentSettings` (Provider type). The `AsyncPackage` performs manual composition: reads `AgentSettings`, resolves types by reflection when applicable and assigns the concrete implementation to `AgentComposition.AgentService`.

---

### 10. Configuration (Tools → Options)

Confirmed scope:
1. Base URL configurable: **Yes**
2. Model configurable (string): **Yes**
3. API Key / token: **Yes**
4. Timeout configurable: **No** (handled by the origin)
5. Persistence: **Yes** (WritableSettingsStore)
6. UI in Tools → Options: **Yes**
7. Config UI in ToolWindow: **No (except inline editing panel in Sprint 3.3)**

Persistence:
- `WritableSettingsStore` (UserSettings)
- Stable section/key (e.g., `AgenteIALocal`)

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

### Sprint 3.3 — Settings persistence and inline UI

In Sprint 3.3 we implemented a versioned `settings.json` (schema `v1`) stored at `%LOCALAPPDATA%\\AgenteIALocal\\settings.json`. The file contains an array of `servers`, optional `globalSettings`, optional `taskProfiles`, and a top-level `activeServerId` field. A default server entry targeting LM Studio (`lmstudio-local`) is created when the file is missing. The defaults aim for LM Studio local usage with `baseUrl: http://127.0.0.1:8080` and empty `apiKey`.

A resilient `AgentSettingsStore` was added to the VSIX to load and save the JSON file. Key properties:
- Preserves unknown fields by keeping a raw `JObject` and writing back unchanged fields when saving.
- Never throws to the UI; errors result in safe defaults being returned and attempted persistence.
- Allows reading and updating `activeServerId` and per-server `baseUrl`, `model`, and `apiKey`.

An inline settings panel was added to the ToolWindow to allow limited editing of the active server configuration (activeServerId, baseUrl, model, apiKey) and explicitly save them back to `settings.json`. The full Options page (`Tools → Options → Agente IA Local`) remains available for simpler per-user settings persisted via `WritableSettingsStore`.

Architectural rationale:
- Keep the baseline stable: composition remains manual and backend changes are out of scope for Sprint 3.3.
- Provide a simple UI path to edit critical connection properties without exposing the entire configuration model in the ToolWindow.

---

### 13. Non-regression checklist (updated)

Before merging changes from Sprint 3.3:
- ToolWindow opens.
- Command executes.
- Package autoload active.
- Build OK (VS Stable).
- No temporary MessageBoxes.

---

If there are additional architecture items that need clarifying (diagrams, sequence flows or decision records), add them as incremental PRs referencing this document and linking to the `vsix-stable-baseline` tag.

---

### Sprint 4 — Closed (tag: sprint-004-closed)

- Status: CLOSED
- Tag: `sprint-004-closed`
- Branch reference: `sprint-004-backend-lmstudio`

Short status note:
- Sprint 4 is closed (tag: `sprint-004-closed`). Work completed focused on backend mock executor validation and LM Studio adapter prototypes. Some experimental details were kept for further revision.

---

### Sprint 5 — In progress (UX)

- Status: IN PROGRESS
- Branch: `sprint-005-ux`
- Focus: ToolWindow UX improvements, visible runtime states, scrollable output, copy response, and explicit error visibility.

Short status note:
- Sprint 5 is currently in progress with focus on UX polish and ensuring no silent failures in the ToolWindow. No architecture decisions changed in this update.

---

### Sprint 007 — MaterialDesign foundation (documentation)

- No code commits: the architecture already met the MaterialDesign requirements, so this sprint only produced documentation.
- Validated items: `App.xaml` still merges `MaterialDesignTheme.Dark` and `MaterialDesignTheme.Defaults` exactly once, keeps the Blue primary / Lime accent palette, and contains no duplicate dictionaries; the ban on scripts (Python/PowerShell) was enforced and no external tooling was executed.
- Rejected items: adding `MaterialDesignTheme.Fonts.xaml` or similar dictionaries was rejected because it would have duplicated resources without delivering visual value.
- Recorded decisions: `md:PackIcon` and `md:ColorZoneAssist.Mode` remain in the ToolWindow as controlled dependencies until the UX replacement strategy is approved.
- Exit condition: architecture documentation updated, runtime unchanged, and Sprint 008 (UX pixel-perfect) unblocked.
