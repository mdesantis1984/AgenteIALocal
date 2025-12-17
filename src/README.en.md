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


