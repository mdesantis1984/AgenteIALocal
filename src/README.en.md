Agente IA Local for Visual Studio

---

Project overview

What it is

Agente IA Local is a classic Visual Studio extension (VSIX, non SDK-style) that hosts AI agent capabilities over the IDE context (solution, projects, and documents). The goal is to validate a decoupled architecture between UI, orchestration, and AI provider, with an MVP demonstrating VSIX packaging, command registration, and a base ToolWindow shell.

Why classic VSIX

Classic VSIX was chosen to maintain compatibility with legacy Visual Studio SDK APIs and to explicitly control packaging (classic csproj, VSCT compiled to resource) and menu/ToolWindow registration.

MVP scope

- Validate build and VSIX packaging.
- Expose a command under Tools that should open a sample ToolWindow.
- Provide contracts and a mock executor to simulate JSON request/response.

Current project status

What works today

- Build: OK with MSBuild.
- VSIX: generated at `src/AgenteIALocalVSIX/bin/Debug/AgenteIALocalVSIX.vsix` and installable in the Experimental Instance.
- Menu: a command is visible under Tools when the extension is deployed.

What does not work today / incomplete

- ToolWindow: clicking the menu does not open the ToolWindow in the current environment (observed limitation). The ToolWindow registration in the Package must be verified.

What is visible in Visual Studio

- Tools menu with the "Agente IA Local" entry.
- The ToolWindow does not appear when executing the command (current state).

Solution structure

- `AgenteIALocal.Core` — Neutral contracts and models (DTOs and interfaces).
- `AgenteIALocal.Application` — Pipeline orchestration and high-level logic (mock in MVP).
- `AgenteIALocal.Infrastructure` — Concrete adapters for IDE/filesystem and utilities.
- `AgenteIALocal.UI` — WPF controls and shared helpers.
- `AgenteIALocal.Tests` — Unit and light integration tests.
- `AgenteIALocalVSIX` — Classic VSIX host: Package, commands, ToolWindow, and packaging.

Layer dependencies

- VSIX (host) composes UI and integrates `Infrastructure` adapters that consume `Core` contracts.
- `Application` coordinates flows over `Core`; in MVP it may call mocks.
- `UI` and `VSIX` should not be coupled to concrete AI providers.

VSIX architecture

Role of the Package

- The `AsyncPackage` registers menus, commands, and ToolWindows, and exposes resources produced by VSCT.

Initialization and autoload

- The package can autoload in contexts such as `SolutionExists` (depending on attributes). Initialization calls `OpenAgenteIALocalCommand.InitializeAsync(this)` to register the command.

Command registration

- The command is created with `CommandID(CommandSet, CommandId)` and registered via `OleMenuCommandService`.

VSCT / Package / CommandSet relationship

- The VSCT defines `GuidSymbol` for the package and the `CommandSet`.
- The package GUID must exactly match the `[Guid(...)]`/`PackageGuidString` on the `AsyncPackage` and the Id in `.vsixmanifest`.
- The VSCT `CommandSet` GUID must match the GUID used in code.

`Menus.ctmenu` is not a physical file

- It is the resource name generated when compiling `.vsct`, registered with `[ProvideMenuResource("Menus.ctmenu", 1)]`. It does not exist in the repository as a file.

Common VSCT errors and their real causes

- VSCT1102: referenced GUID symbol not defined (missing `GuidSymbol` or name/value mismatch).
- VSCT1103: invalid definition or duplicate IDs (IDs do not match code or are duplicated).

Commands, menus, and UI

What commands exist

- `OpenAgenteIALocalCommand` (id `0x0100`) under the VSCT-defined `CommandSet`.

Where they should appear

- Under the Tools menu (current VSCT placement for visibility testing).

What code registers them

- `OpenAgenteIALocalCommand.InitializeAsync(this)` from `Package.InitializeAsync` adds the `OleMenuCommand`.

Why the menu is visible but the ToolWindow does not open

- Missing or incorrect ToolWindow registration in the Package: without `[ProvideToolWindow(typeof(AgenteIALocalToolWindow))]`, `ShowToolWindowAsync` may not create the window and can fail silently.

What is missing to make the ToolWindow work

- Confirm and apply ToolWindow registration and validate its creation when invoking the command.

Agent design

What the Agent is here

- A set of contracts and flows operating over the solution context, with mock execution in the MVP.

Existing contracts

- `CopilotRequest`, `CopilotResponse` (DTOs) and contracts in `Core`.

Role of `MockCopilotExecutor`

- Emulates deterministic JSON responses to validate the UI and pipeline without a real AI provider.

What parts are mock

- Orchestration and AI provider: no real integration, only simulation.

Build and debugging

Environment requirements

- Visual Studio 2022 + extension development workload.
- .NET Framework Developer Pack 4.7.2.
- MSBuild available.

Real MSBuild command

msbuild src/AgenteIALocalVSIX/AgenteIALocalVSIX.csproj /t:Build /p:Configuration=Debug

Experimental instance

- F5 launches Visual Studio with `/rootsuffix Exp`.

Diagnosing VSIX problems

- Verify installation under `Extensions -> Manage Extensions`.
- If the menu does not appear or the ToolWindow does not open, check `ActivityLog.xml` at:
  `%LOCALAPPDATA%\\Microsoft\\VisualStudio\\<version>_Exp\\ActivityLog.xml` or `%APPDATA%\\Microsoft\\VisualStudio\\<version>_Exp\\ActivityLog.xml`.

Lessons learned and common errors

- Insufficient package autoload: commands not visible.
- Not initializing commands from the Package: the menu is not registered.
- Misaligned GUIDs: invisible menus without errors.
- Assuming non-existent physical files (`Menus.ctmenu`).
- Mixing SDK-style patterns with classic without adjusting build/registration.

Technical roadmap

- Properly register the ToolWindow and validate its opening.
- Resolve VSTHRD warnings (consistent `JoinableTaskFactory` usage).
- Integrate a real LLM provider with secure configuration.
- Harden threading and lifecycle in the VSIX.
- Prepare for release (signing, versioning, notes, multi-VS testing).


