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


