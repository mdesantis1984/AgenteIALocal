Agente IA Local for Visual Studio

---

Project status

MVP: Classic Visual Studio extension (VSIX) targeting .NET Framework 4.7.2 (non SDK-style project). It builds with MSBuild and produces a VSIX package. The package exposes a sample ToolWindow that shows a mock JSON pipeline (request/response) used to prototype the LLM integration UX.

Requirements

- Visual Studio 2022 (install the "Visual Studio extension development" workload).
- .NET Framework Developer Pack 4.7.2.
- Git (for repository operations).

How to build

Use MSBuild from the repository root:

msbuild src/AgenteIALocalVSIX/AgenteIALocalVSIX.csproj /t:Build /p:Configuration=Debug

How to run / debug

1. Open the solution in Visual Studio.
2. Set the `AgenteIALocalVSIX` project as startup project.
3. Press F5 to launch the Experimental Instance of Visual Studio (VSIX runs with `/rootsuffix Exp`).
4. In the experimental instance, open the ToolWindow from the corresponding menu.

Expected MVP behavior

- Simple UI with `Run` and `Clear` buttons.
- Text area that displays the request JSON sent by the UI and the simulated response JSON.
- The pipeline is mocked: execution is example-only to validate UI and JSON serialization/deserialization.

Solution structure

- `AgenteIALocal.Core` — Shared models and utilities (data types, contracts).
- `AgenteIALocal.Application` — Application logic and orchestration of the pipeline (mock in the MVP).
- `AgenteIALocal.Infrastructure` — Infrastructure implementations (adapters, serialization, filesystem helpers).
- `AgenteIALocal.UI` — Shared UI components and WPF helpers.
- `AgenteIALocal.Tests` — Unit and light integration tests.
- `AgenteIALocalVSIX` — Classic VSIX project that packages the extension and defines the ToolWindow and commands.

Important notes (classic VSIX)

- The VSIX project is classic (non SDK-style). Files must be explicitly included in the `csproj` to appear in Solution Explorer.
- Some legacy APIs (e.g. command services) require references to framework assemblies such as `System.Design` in non SDK projects.
- `Newtonsoft.Json` is used for JSON serialization/deserialization on this target framework.

Next steps

1. Harden VSTHRD warnings and concurrency using `JoinableTaskFactory`/`JoinableTaskContext` where appropriate.
2. Add configuration and secure storage for endpoints and models (e.g. LLM Studio) — move to secure settings or environment variables.
3. Implement the real execution pipeline with safe error handling and resource/time limits.
4. Packaging/release checklist: signing, versioning, release notes, testing on multiple VS versions.

Project working rules

- Operate one command at a time — run a single command and wait for its result before the next action.
- Always show the output of the executed command (logs/results) and decide the next step based on that output.
- Copilot (or automation scripts) must not modify `*.csproj`, `*.vsixmanifest` or source files unless explicitly instructed and authorized.

Non-goals / Scope

The repository currently contains only a UX/integration MVP. No real models or production pipelines are integrated.


