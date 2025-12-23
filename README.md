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
- Sprint 4: Closed (tag: sprint-004-closed)
- Sprint 5: In progress (UX) — branch: sprint-005-ux

Aviso importante

Este documento es solo índice y branding. La documentación técnica y funcional completa está en los README por idioma indicados arriba.

---

### Sprint 2 — UI / UX, Observabilidad y Diagnóstico

Cierre de Sprint 2: foco en experiencia de usuario, observabilidad y diagnóstico. Entregables y decisiones principales:

- Mejora integral de la ToolWindow (UX y navegación)
- Mensajería de estado clara (configurado / incompleto / backend no disponible)
- Acceso directo a Options desde UI
- Tab **Log**:
  - Visualización del log
  - Copiar contenido
  - Borrado del archivo
  - Información de tamaño y ruta
- Diagnóstico confirmado:
  - `AgenteIALocal.Infrastructure` **no se carga en runtime VSIX**
  - El backend no queda compuesto (`AgentService == null`)
- Decisión:
  - El backend y el empaquetado VSIX pasan al **Sprint 3 (Technical Debt)**

**Documentación relacionada:**

- [Documentación general (ES)](src/README.es.md)
- [General documentation (EN)](src/README.en.md)
- [Arquitectura (ES)](src/README.architecture.es.md)
- [Architecture (EN)](src/README.architecture.en.md)

## Sprint 2.5 — UX Foundations (Closed)

### Objective
- **Primary:** Consolidate Visual Studio–oriented UX foundations for the ToolWindow and the agent experience, ensuring clear states and navigation/observability components ready for the next iteration.

### Checklist (status)
- [x] Definition of UX principles for VSIX (non-blocking, IDE-integrated)
- [x] Base layout design for the ToolWindow (zones: input, context, actions, output)
- [x] Experience states defined (Idle, Running, Success, Error)
- [x] Visual conventions of Visual Studio applied (iconography, spacing, focus)
- [x] Validation of real flows (file reading, agent execution in mock mode)

### Notes
- This section mirrors the Sprint 2.5 closure documented in localized READMEs.
- It is added to the index to reflect the latest completed sprint without changing the overall structure of this file.

## Sprint 3.3 — Settings persistence y UI inline

En este sprint se completaron las siguientes tareas orientadas a persistencia y configuración:

- Se introdujo `settings.json` (esquema `v1`) en `%LOCALAPPDATA%\\AgenteIALocal\\settings.json` con una estructura versionada y soporte para múltiples servidores. El archivo se crea automáticamente con valores por defecto si no existe.
- Se implementó `AgentSettingsStore` (cargado/guardado seguro) que preserva campos desconocidos al reescribir el fichero y nunca lanza excepciones hacia la UI.
- Se añadieron valores por defecto centrados en LM Studio (servidor local `lmstudio-local`, `BaseUrl` por defecto `http://127.0.0.1:8080`).
- Se reintrodujo una página de Options en `Tools → Options → Agente IA Local` para configuración básica (BaseUrl, Model, ApiKey) usando `WritableSettingsStore`.
- Se incorporó un panel de configuración inline en la ToolWindow que permite editar campos seleccionados del `settings.json` (activeServerId, baseUrl, model, apiKey) y guardarlos explícitamente sin afectar otros campos.

Notas importantes

- No se modificó la lógica de ejecución o composición del backend en este sprint: la composición sigue siendo manual y orientada a mantener el baseline estable.
- Para más detalles arquitectónicos consultar `README.architecture.es.md` y `README.architecture.en.md`.

---

### Sprint 4 — Cerrado

(Notas y enlaces de interés pueden añadirse aquí si es necesario)

---

### Sprint 5 — En progreso

Objetivo principal: 

- Avanzar en las mejoras de UX relacionadas con la ToolWindow y la experiencia general del agente, basadas en los aprendizajes y feedback recibido hasta la fecha.

Branch actual:

- `sprint-005-ux`

---

### Sprint 007 — MaterialDesign foundation (documentación)

- Estado: cerrado sin commits de código; todas las acciones se limitaron a verificación y documentación.
- Validaciones: `App.xaml` ya tenía una sola carga de `MaterialDesignTheme.Dark` y `MaterialDesignTheme.Defaults`, paleta primaria Azul y acento Lima, por lo que no se requerían cambios; se confirmó la prohibición de scripts (Python/PowerShell) y no se ejecutó ninguna herramienta externa.
- Rechazos: se descartó incorporar `MaterialDesignTheme.Fonts.xaml` u otros recursos adicionales para evitar duplicidad y mantener el baseline estable.
- Decisiones explícitas: los controles `md:PackIcon` y el uso de `md:ColorZoneAssist.Mode` permanecen activos y documentados hasta que exista una alternativa equivalente.
- Exit condition: documentación actualizada, dependencias MaterialDesign auditadas y Sprint 008 (UX pixel-perfect) desbloqueado sin impactos visuales ni de runtime.

---
