# Agente IA Local VSIX — Marketplace Listing (v1.0.46)

## Overview
Agente IA Local is a classic Visual Studio (VSIX) extension that provides a ToolWindow to run a local AI agent inside the IDE. The extension enables a prompt → execution → result flow and includes a small UI for composing prompts, viewing rendered results, and inspecting execution logs.

## Key features (implemented / verified)
- ToolWindow integrated via VSIX classic AsyncPackage and registered command (Tools menu).
- WPF-based UI (`AgenteIALocalControl`) with prompt input, response rendering area, and basic chat management.
- Mock and real backend composition: by default a MockAgentExecutor is used; LM Studio client is implemented and composed when configured.
- Local logging to `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log` via a V2 logging pipeline and file sink.
- ActivityLog integration: logs can also be written to Visual Studio ActivityLog via a dedicated sink.
- Options + inline settings panel and a `settings.json` persisted under `%LOCALAPPDATA%\AgenteIALocal` (if created).

## Requirements
- Visual Studio (classic VSIX host). See VSIX manifest and project `.csproj` targets for exact supported Visual Studio versions and .NET targets.
- The extension projects target .NET Framework 4.7.2 / .NET Framework 4.8 / .NET Standard 2.0; actual runtime compatibility depends on the packaged VSIX and host.

## Install
- Marketplace: Install directly from Visual Studio Marketplace (when published).
- Local VSIX: build the VSIX in Visual Studio and install the generated `.vsix` package in your experimental or real Visual Studio instance.

## Quick start
1. Install the extension.
2. Start Visual Studio (or Experimental Instance for debugging).
3. Open `Tools → Agente IA Local` to open the ToolWindow.
4. Write a prompt in the prompt box and press Enter or click the Run button to execute.
5. View the rendered response in the response pane and inspect logs on the Log tab.

## Configuration
- Options: the extension exposes an Options Page and an inline Settings Panel in the ToolWindow. The persistent `settings.json` is stored (by default) under `%LOCALAPPDATA%\AgenteIALocal\settings.json`.
- Provider selection: LM Studio is supported as a real backend; set `activeServerId` and server `baseUrl`/`model` in `settings.json` or via Options to enable real execution.

## Logging & troubleshooting
- Logs are written to `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log` (file sink) and may also be emitted to Visual Studio ActivityLog via the V2 sink.
- If the VSIX fails to compose a real backend, it falls back to the mock executor. Check the logs for `VSIX.Composition` events.

## Known limitations (verified)
- The extension is a classic VSIX and uses AsyncPackage + ToolWindow model (not SDK-style). Packaging and host compatibility must match the target Visual Studio version.
- JAN provider exists as a stub and may not perform real HTTP calls.
- No streaming AI responses; the current execution model is request/response.
- The ToolWindow aims to be fail-safe: many UI exceptions are swallowed to avoid breaking the host.

## Disclaimer
This extension is provided "AS IS". Use at your own risk. The author disclaims all liability.

## Support & Privacy
See `SUPPORT.md` and `PRIVACY.md` included in this repository.
