# Agente IA Local for Visual Studio

Agente IA Local is a classic Visual Studio extension (VSIX, classic format) that provides a ToolWindow and a set of commands to experiment with AI agent flows over the IDE context (solution, projects, documents). The project's objective is to validate a decoupled architecture between UI, orchestration, and AI providers, facilitating testing, traceability and deployment as an extension installed in Visual Studio's Experimental Instance.

---

## 1. Introduction

This extension offers an entry point inside Visual Studio (menu `Tools → Agente IA Local`) and a ToolWindow that allows generating prompts, running the agent (mock or real) and viewing responses. It is designed as an evolutionary prototype (MVP → hardening) to facilitate later integration with LLM providers.

The technical approach is explicitly "classic VSIX" to maintain compatibility and control over packaging, command registration (VSCT) and ToolWindows.

---

## 2. Working methodology

We work following agile practices (Scrum) with short iterations and concrete deliverables. Some adopted working rules:

- Scrum per iteration: short sprints with clear objectives per branch.
- Atomic commits: every functional or fix change must map to a small commit with a meaningful message.
- Definition of Done includes: buildable code, minimal tests (when applicable), associated documentation and updated prompts/artifacts.
- Documentation as part of the DoD: prompts in `.md` format (see "Use of prompts") are formal artifacts and must be versioned in the task branch.
- Branches per iteration and per task: e.g. `iter-002/task-01-options-access`.

---

## 3. Roles

Clear roles are defined in the workflow:

- Human (developer/maintainer): makes final decisions, reviews and approves changes, runs tests in the Experimental Instance and performs merges to main branches.
- AI (ChatGPT): acts as architect and planner. It is used to analyze problems, generate technical prompts and propose fixes and work plans. It does not edit the repository code directly; its output is validated by a human.
- Copilot (VS/editor assistant): the only automated agent authorized to apply changes in the workspace (requested, targeted edits). It is used to perform the minimal changes approved by the human and follow exact implementation instructions.

This separation ensures responsibility and traceability between proposal (AI), execution (Copilot) and verification/approval (human).

---

## 4. Iterations (chronological)

Below is a summary of the main iterations and milestones achieved to date.

### Iteration: post-mvp-readme-and-hardening

- Real initial problems:
  - Mismatch between GUIDs in VSCT and the `Package` preventing correct wiring of menus/commands.
  - Initial logging insufficient for runtime diagnosis.
  - Fragile behavior in the ToolWindow when the backend (`AgentService`) was not composed.

- Fixes applied:
  - Fixed VSCT ↔ Package wiring: aligned GUIDs and CommandId in VSCT and code.
  - Hardened the `AsyncPackage` `InitializeAsync` with explicit logging and error handling without rethrowing (fail-safe).
  - Consistent command registration using `OleMenuCommandService` following the functional example pattern (MenuCommand/Instance/InitializeAsync pattern).
  - Improvements in the ToolWindow: WPF controls adjusted for legibility (using SystemColors) and safe UX flow handling when `AgentService` is null.
  - File logging enabled and extended (initialization traces, command registration and button execution).

- Result:
  - ToolWindow operable from the `Tools` menu (opening verified in Experimental Instance).
  - Menu and command correctly registered and executable.
  - Verifiable logs in `%LOCALAPPDATA%\\AgenteIALocal\\logs\\AgenteIALocal.log`.

- Commits and applied order:
  - Minimal changes grouped by objective: (1) logging/package init, (2) command registration, (3) VSCT fix, (4) ToolWindow UX hardening.

### Iteration: iter-002 (start)

- Branch created: `iter-002/task-01-options-access`.
- Sprint objective:
  - Expose and persist options from `Tools → Options` (Options Page) and allow `AgentService` to read configuration at initialization.
  - Improve documentation and add prompts as artifacts.
- Current state:
  - Options Page functional and persistent (configuration saved/reusable).
  - Documentation and prompt work in progress (README and `.md` prompts updated as part of DoD).

---

## 5. Architecture (high level)

Reference: see `README.architecture` (dedicated file in the repository) for diagrams and details.

Minimal summary:

- `AgenteIALocal.Core` — contracts, DTOs and interfaces (layer neutrality).
- `AgenteIALocal.Application` — orchestration of flows and business logic (implementations without direct UI dependency).
- `AgenteIALocal.Infrastructure` — concrete adapters (HTTP clients, resolvers, specific integrations).
- `AgenteIALocal.UI` — reusable WPF controls.
- `AgenteIALocalVSIX` — classic VSIX host: `AsyncPackage`, commands, ToolWindow, VSCT and packaging.

The architecture promotes injection/composition of `AgentService` by the Package so the UI only consumes the interface and does not depend on concrete implementations.

---

## 6. Use of prompts

`.md` prompts are used as formal artifacts in the development process. Reasons and practices:

- Why: they allow reproducibly defining the instructions given to the AI (ChatGPT) for design, diagnosis and change generation.
- What they contain: task description, workspace context, steps to execute, constraints and acceptance criteria.
- How they integrate into the flow: each relevant task/issue includes one or more versioned prompts in the task branch; prompts are part of DoD and attached to the PR as evidence of decision and execution.

Examples of use:
- Diagnosis of VSCT ↔ Package wiring.
- Hardening plan for `InitializeAsync` of the Package.
- UX guide for error handling in the ToolWindow.

---

## 7. Current project status

Honest and verifiable summary at the time of this document:

What works
- Build: `msbuild` of the solution and the VSIX project build successfully (VSIX target .NET Framework 4.7.2).
- VSIX: package buildable and installable in Experimental Instance.
- Menu and command: `Tools → Agente IA Local` appears and the command is properly registered (command execution flow verified).
- ToolWindow: opens and displays controls; UX improved for legibility and error handling.
- Options Page: persistent configurations available and reachable from `Tools → Options`.
- Logging: traces written to file at `%LOCALAPPDATA%\\AgenteIALocal\\logs\\AgenteIALocal.log` with entries for initialization, command registration and action execution.
- Backend in code: `Core`, `Application` and `Infrastructure` projects present (structure and mocks for flow validation).

What is in progress
- Final integration of LLM providers (real `AgentService` composition with LM Studio or JanServer depending on configuration).
- Further hardening on threading and VSTHRD patterns to remove residual warnings.
- Integration tests on the VSIX flow in Experimental Instance (partial automation on the roadmap).

What is NOT done yet
- Final validated support for a production LLM provider (adapters exist, but end-to-end validation and credential security are pending).
- Formal release packaging (signing, versioning for marketplace) — pending Release and QA process.

---

### Sprint 2 — UI / UX, Observability and Diagnostics

Scope of the sprint

- ToolWindow UX and navigation improvements.
- Direct access to Options from the UI (existing Options Page reachable from the ToolWindow).
- Clear status messaging for the user: "configured", "incomplete", and "backend not available".

Observability

- Implementation of a "Log" tab with the following capabilities:
  - Full log view in a read-only TextBox.
  - "Copy" button to copy the full log content to clipboard.
  - "Delete" button to remove the log file from disk and clear the view.
  - Visible file path and size (KB/MB).

Backend status (diagnosis)

- Confirmed diagnosis during Experimental Instance testing:
  - The `AgenteIALocal.Infrastructure` assembly was not loaded at VSIX runtime.
  - As a consequence, backend composition failed and `AgentService` remained `null`.

Decisions and outstanding scope

- Backend composition and VSIX packaging adjustments are moved to Sprint 3 as technical debt.
- The new "Config" tab remains experimental / partially implemented and will be validated in Sprint 3.

Outcome

- Sprint 2 closed with UI/UX improvements and an actionable diagnostics outcome. Backend stabilization is planned for Sprint 3.

Related documentation

- `src/README.es.md`
- `src/README.architecture.md`

## Documentation and traceability

- Each relevant task includes `.md` prompts and minimal changes applied via Copilot; commits reference the branch and task.
- This README is the base document at `src/README.es.md` and must be updated on every significant iteration.

---

If operational details are missing (e.g., build scripts, installation instructions by Visual Studio version, or additional architecture diagrams), those artifacts should be added via specific PRs and referenced from `README.architecture` or the `docs/` folder.

Thanks: this README reflects the current status and recent decisions (hardening, wiring and logging) at the time of the last update.

---

### Sprint 2.5 — UX Foundations (Closed)

Objective

- Primary: Consolidate Visual Studio–oriented UX foundations for the ToolWindow and the agent experience, ensuring clear states and navigation/observability components ready for the next iteration.

Checklist (status)

- [DONE] Definition of UX principles for VSIX (non-blocking, IDE-integrated)
- [DONE] Base layout design for the ToolWindow (zones: input, context, actions, output)
- [DONE] Experience states defined (Idle, Running, Success, Error)
- [DONE] Visual conventions of Visual Studio applied (iconography, spacing, focus)
- [DONE] Validation of real flows (file reading, agent execution in mock mode)

Notes

- This section is added as the formal closure of Sprint 2.5 — UX Foundations. Previous content has not been removed or rewritten; this documents the closed state and the minimal deliverables verified. Backend integration and final packaging tests remain planned for Sprint 3 as technical debt.

---

### Sprint 4 — Closed (tag: sprint-004-closed)

- Status: CLOSED
- Tag: `sprint-004-closed`
- Branch reference: `sprint-004-backend-lmstudio`

Checklist:
- [DONE] Mock executor validated and expanded for backend integration tests
- [DONE] Basic LM Studio adapter prototypes merged into feature branch
- [DONE] Package-level logging and diagnostics improved for backend flows
- [Deprecated/Outdated (kept for history)] Note: Some implementation details remain experimental and will be revisited in Sprint 5 if necessary.

---

### Sprint 5 — In progress (UX) — branch: sprint-005-ux

- Status: IN PROGRESS
- Branch: `sprint-005-ux`
- Focus areas:
  - ToolWindow UX polish and layout adjustments
  - Clear runtime states: Idle / Running / Success / Error
  - Scrollable output area for long responses
  - Copy response capability in the UI
  - Visible errors (no silent failures) and explicit logging

Immediate checklist:
- [ ] Implement scrollable response area in ToolWindow output
- [ ] Add "Copy response" button and clipboard behavior
- [ ] Ensure state labels update correctly for all flows
- [ ] Surface errors visibly and log them (no silent catches)
- [ ] Update UI styles to match Visual Studio theme conventions

Notes:
- Do not remove or alter prior sprint entries. If parts become outdated they are marked as Deprecated/Outdated and kept for history.

---

### Sprint 007 — MaterialDesign foundation (documentation)

- Status: closed with no code commits; the iteration focused solely on validation and reporting.
- Validations: `App.xaml` already contained a single merge of `MaterialDesignTheme.Dark` and `MaterialDesignTheme.Defaults` plus the Blue primary / Lime accent palettes, so no edits were required; the prohibition of running scripts (Python/PowerShell) was enforced and no external commands were executed.
- Rejections: adding `MaterialDesignTheme.Fonts.xaml` or any extra dictionaries was rejected because it would have introduced redundant resources and risked baseline drift.
- Explicit decisions: `md:PackIcon` controls and the use of `md:ColorZoneAssist.Mode` remain as-is until an approved equivalent replacement exists.
- Exit condition: documentation updated, MaterialDesign dependencies audited, and Sprint 008 (UX pixel-perfect) officially unblocked with zero visual/runtime impact.


