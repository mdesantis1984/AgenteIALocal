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

## Arquitectura

- [Arquitectura (ES)](src/README.architecture.es.md)
- [Architecture (EN)](src/README.architecture.en.md)

## Documentación

- [Documentación general (ES)](src/README.es.md)
- [General documentation (EN)](src/README.en.md)

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
