# Privacy Notice — Agente IA Local VSIX

This privacy notice describes data storage and network behavior for the Agente IA Local extension as implemented in the repository. The statements below are strictly based on existing code and documentation in this repository.

## Local data
- The extension stores configuration and state under the local application data folder, by default:
  - `%LOCALAPPDATA%\AgenteIALocal\settings.json` — persistent settings and servers list.
  - `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log` — runtime logs and diagnostics.
- The UI stores chat lists and minimal mock state via local stores (`ChatStore`) under the VSIX configuration (implementation details and exact file paths depend on ChatStore implementation).

## Network activity
- The extension includes an LM Studio HTTP client (`LmStudioClient`) and an HTTP-based model fetcher. If configured with a real server `baseUrl` and `apiKey`, the extension may perform outbound HTTP requests to the configured endpoint(s).
- If the JanServer client is used, current implementation may be a stub (no network). Verify configuration before assuming network calls.
- By default, no telemetry or external analytics providers are implemented in the repository.

## What is sent over the network (if configured)
- If LM Studio is configured and used, the client builds and sends a JSON payload containing model and messages, including the prompt text entered by the user. The repository shows the request content is composed from the prompt — do not send sensitive content unless you trust the configured endpoint.

## Retention and deletion
- Logs and settings are stored locally under `%LOCALAPPDATA%\AgenteIALocal`. Users may delete these files manually to remove stored logs and configuration.

## Third-party endpoints
- The extension supports configuring arbitrary LM Studio-compatible endpoints via `settings.json`. The extension does not hard-code third-party analytics or telemetry services.

## Contact
For privacy questions, open an issue in the repository or contact the maintainer listed in the project metadata.
