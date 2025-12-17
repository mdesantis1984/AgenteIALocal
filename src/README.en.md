Agente IA Local for Visual Studio — English

🔎 Overview

Agente IA Local is a local Visual Studio extension solution intended to host components that enable AI-assisted developer tooling executed locally. The repository is organized into multiple projects (core libraries, application layer, infrastructure, UI and tests) and is structured to produce a VSIX extension and supporting libraries for local development and testing.

📋 Current Status

- Baseline solution and project structure are consolidated.
- VSIX scaffolding and package project exist for local development and testing.
- A non-functional command has been added and registered in the package; the command appears under the View menu when the VSIX is run in an experimental instance.
- No ToolWindow or visible UI beyond the menu item is implemented in this phase.

🏗 Solution Architecture

The solution is organized into separate projects by responsibility:
- `AgenteIALocal` — Package and integration for the VSIX
- `AgenteIALocal.Application` — Application layer
- `AgenteIALocal.Core` — Domain and shared logic
- `AgenteIALocal.Infrastructure` — Platform-specific implementations
- `AgenteIALocal.UI` — Visual Studio extension / VSIX project
- `AgenteIALocal.Tests` — Unit and integration tests

Projects target .NET Framework 4.8 and .NET Standard 2.0 where appropriate. The expected build artifact for the extension is a VSIX package produced from the UI project.

🛠 Build Requirements

1. Visual Studio 2019 or 2022 with the "Visual Studio extension development" and ".NET desktop development" workloads installed.
2. Targeting packs for .NET Framework 4.8 and .NET Standard 2.0 available on the system.
3. (Optional) Visual Studio SDK components for VSIX development.

📦 Build & Run (manual steps)

1. Open the solution in Visual Studio.
2. Restore NuGet packages if prompted.
3. Select `Debug` configuration.
4. Build the solution (`Build -> Build Solution` or `Ctrl+Shift+B`).
5. Set `AgenteIALocal.UI` as startup project and run the VSIX in the Experimental Instance (`F5`).

🐞 Debugging

- Place breakpoints in UI or other projects before launching the experimental instance.
- Logs and diagnostic output are emitted by the projects themselves; there is no centralized telemetry configured.
- If the extension does not load, confirm the VSIX project built successfully and that the experimental instance is being launched by Visual Studio.

🚫 Non-Goals

- Shipping a production-ready, published VSIX to the Visual Studio Marketplace at this stage.
- Providing cloud-hosted AI inference or managed external AI services.
- Changing project structure, `.csproj` files, solution files, or VSCT resources as part of these early phases.

---

## Phase 1 – VSIX Shell (Technical Notes)

Scope of Phase 1

- Establish a minimal, local-only VSIX shell suitable for iterative development and testing.
- Provide an entry point (menu command) and a dockable ToolWindow with a very small UI surface to build upon.

What is implemented

- A command class (`OpenAgenteIALocalCommand`) registered in the package and exposed under the `View` menu via the existing VSCT file.
- A minimal ToolWindow implementation (`AgenteIALocalToolWindow`) and a corresponding user control (`AgenteIALocalToolWindowControl`).
- A lightweight ViewModel (`AgenteIALocalToolWindowViewModel`) implementing `INotifyPropertyChanged` with a `StatusText` property.
- Binding from the control to the ViewModel (currently performed from code-behind to keep the initial implementation simple).
- Removal of WPF `App.xaml` from the VSIX project to keep the project as a class library (resolves ApplicationDefinition build issues).

What is intentionally not implemented

- No business logic, services, or dependency injection are present.
- No workspace scanning, document model, or integration with external AI services.
- No persistent settings, telemetry, or CI/CD deployment configuration.
- The VSIX is not published or production-ready; it is intended for local development and testing only.

Key technical decisions

- The VSIX project is implemented as a class library (no `ApplicationDefinition` / `App.xaml`). This avoids WPF application semantics in the package.
- The primary UI surface is a ToolWindow; this keeps the extension surface minimal and compatible with VS UI patterns.
- Command activation is done via the existing VSCT command table; the command class registers a menu command and opens the ToolWindow when invoked.
- A simple MVVM pattern is established without external frameworks: a plain `INotifyPropertyChanged` ViewModel and a binding target. Initial UI binding is performed from the control's code-behind for simplicity and to avoid premature XAML wiring.
- No services or DI frameworks are introduced at this phase to reduce complexity and keep the shell transferable.

Current functional state

- The solution builds successfully targeting .NET Framework 4.8 and .NET Standard 2.0 where applicable.
- The `View -> Abrir Agente IA Local` menu item is registered and visible when running the VSIX in the Visual Studio Experimental Instance.
- Invoking the menu opens the dockable ToolWindow with the placeholder content bound to `StatusText`.

How this phase prepares Phase 2

- Provides a stable, visible UI shell (command + ToolWindow) where workspace-awareness features can be added.
- Establishes a simple ViewModel contract (`INotifyPropertyChanged`) that will be extended with services and data providers in Phase 2.
- Keeps the codebase clean of infrastructure decisions (no DI or services) so different approaches can be evaluated in the next phase without heavy refactoring.

Explicit non-goals (repeated)

- Do not assume any cloud or external AI capabilities are available.
- Do not publish or release this VSIX as a production artifact in this state.

Notes

- All changes made during Phase 1 are limited to adding the command/ToolWindow and minimal ViewModel; no project files, solution files, or VSCT resources were modified beyond using the existing `AgenteIALocal.vsct` command identifiers.
- The project intentionally avoids introducing new packages or frameworks in this phase.

---

## Phase 2 – Workspace Awareness (Technical Notes)

Purpose of Workspace Awareness

- Provide read-only, testable contracts and implementations that expose solution, project and open-document metadata from the development environment.
- Enable future features that require contextual information about the workspace without coupling higher layers to Visual Studio SDK types.

Interfaces and contracts overview

- `IWorkspaceContext`, `ISolutionContext`, `IDocumentContext` are defined in `AgenteIALocal.Core` and provide read-only access to solution and document metadata.
- Contracts expose simple properties (e.g., solution name, solution path, project list, open documents, active document) and intentionally avoid lifecycle or mutation APIs.

Hybrid strategy: VS SDK adapters + fallback

- Infrastructure provides a hybrid strategy: prefer runtime VS SDK adapters when the extension runs inside Visual Studio, otherwise use conservative filesystem-based fallbacks.
- `VsSdkAvailability` detects presence of SDK types at runtime using reflection; adapter classes use reflection to access `IVsSolution` and `EnvDTE.DTE` when available.
- The fallback implementations operate without the VS SDK: `VisualStudioSolutionContext` parses the nearest `.sln` file to extract project entries and `VisualStudioDocumentContextProvider` returns safe empty/default values when editor information is not available.

Available data (solution, projects, documents)

- Solution: name and path (null when no solution found or determinable).
- Projects: list of `IProjectInfo` with project name, absolute path and inferred language (based on project file extension).
- Open documents: provider returns zero or more `IDocumentContext` entries; active document is returned when determinable by the adapter.

What is intentionally not implemented

- No live events or change notifications for solution/project/document changes.
- No caches, no background scanning, and no mutation APIs (read-only only).
- No service registration or dependency injection in this phase; adapters and factories are simple and explicit.
- No attempt to normalize complex project types or workspace models beyond basic file-path extraction.

How this phase prepares Phase 3

- Provides stable, language-agnostic contracts and adapters so Phase 3 can implement IA provider abstractions without coupling to IDE-specific APIs.
- Ensures that higher-level services can request workspace metadata from a single abstraction layer and remain unit-testable by mocking core interfaces.
- The hybrid adapter pattern allows the next phase to implement richer integrations using VS SDK services while preserving testability via the fallback implementations.

Technical decisions (summary)

- Hybrid adapter pattern (SDK preferred, fallback otherwise).
- No compile-time dependency on VS SDK outside the `AgenteIALocal.Infrastructure` project.
- Reflection-based detection and invocation to avoid hard SDK references.
- Read-only access by design; no events nor caching introduced in Phase 2.


