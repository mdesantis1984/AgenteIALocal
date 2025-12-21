Agente IA Local

---

Descripción breve

Extensión clásica de Visual Studio (VSIX, no SDK-style) para alojar un agente de IA local que opere sobre el contexto real de la solución (solution, projects, documents). El MVP valida empaquetado VSIX, registro de comandos y una ToolWindow base.

Estado actual (MVP, en desarrollo)

- Build: OK (MSBuild)
- VSIX: generado e instalable
- Menú: visible en `Tools`
- ToolWindow: no abre en el entorno actual (pendiente de registro/validación)

Estructura de la solución (proyectos)

- `AgenteIALocal.Core`
- `AgenteIALocal.Application`
- `AgenteIALocal.Infrastructure`
- `AgenteIALocal.UI`
- `AgenteIALocal.Tests`
- `AgenteIALocalVSIX`

Documentación completa por idioma

- Español (canónica): `./README.es.md`
- English (full translation): `./README.en.md`

Indice de sprint y roadmap

- Sprint 1: MVP inicial (ToolWindow básico, mock executor)
- Sprint 2: UX y observabilidad (ToolWindow improvements, logging)
- Sprint 3.3: Settings persistence, Options page, settings.json v1 and inline settings UI (ver detalle abajo)

Aviso importante

Este documento es solo índice y branding. La documentación técnica y funcional completa está en los README por idioma indicados arriba.

---

Sprint 3.3 — Settings persistence y UI inline

En este sprint se completaron las siguientes tareas orientadas a persistencia y configuración:

- Se introdujo `settings.json` (esquema `v1`) en `%LOCALAPPDATA%\\AgenteIALocal\\settings.json` con una estructura versionada y soporte para múltiples servidores. El archivo se crea automáticamente con valores por defecto si no existe.
- Se implementó `AgentSettingsStore` (cargado/guardado seguro) que preserva campos desconocidos al reescribir el fichero y nunca lanza excepciones hacia la UI.
- Se añadieron valores por defecto centrados en LM Studio (servidor local `lmstudio-local`, `BaseUrl` por defecto `http://127.0.0.1:8080`).
- Se reintrodujo una página de Options en `Tools → Options → Agente IA Local` para configuración básica (BaseUrl, Model, ApiKey) usando `WritableSettingsStore`.
- Se incorporó un panel de configuración inline en la ToolWindow que permite editar campos seleccionados del `settings.json` (activeServerId, baseUrl, model, apiKey) y guardarlos explícitamente sin afectar otros campos.

Notas importantes

- No se modificó la lógica de ejecución o composición del backend en este sprint: la composición sigue siendo manual y orientada a mantener el baseline estable.
- Para más detalles arquitectónicos consultar `README.architecture.es.md` y `README.architecture.en.md`.
