# Agente IA Local (VSIX) — Architecture, Scope and Roadmap

> Stable baseline tag: `vsix-stable-baseline`

---

## 🇪🇸 Español

### 1. Propósito del documento

Este documento define de forma **exhaustiva y vinculante** la arquitectura, alcance, fases y decisiones técnicas del proyecto **Agente IA Local (VSIX)**.

Objetivos:
- Retomar el proyecto en cualquier momento sin pérdida de contexto.
- Evitar la reapertura de decisiones ya validadas.
- Servir como referencia de arquitectura para desarrollo, mantenimiento y coordinación con herramientas (Copilot).

---

### 2. Estado actual y baseline estable

Existe un baseline estable, marcado y publicado como:
- Tag: **`vsix-stable-baseline`**

Este hito garantiza:
- VSIX clásico operativo.
- `AsyncPackage` cargando correctamente mediante `ProvideAutoLoad`.
- Comandos registrados y ejecutándose.
- ToolWindow abriendo correctamente.
- Código sin hacks temporales de diagnóstico.
- Repo limpio (sin cambios pendientes).

Regla: **este baseline no debe romperse**. Cualquier feature se desarrolla desde una rama creada a partir de este punto.

---

### 3. Decisiones técnicas cerradas (NO reabrir)

1. Modelo de proyecto: **VSIX clásico** (NO SDK-style moderno).
2. Target framework: **.NET Framework 4.8**.
3. Build/Debug: **Visual Studio Stable** (VS 2022 o VS 2026 Stable).
4. Visual Studio Insiders: solo para instalar/probar `.vsix` ya generado, no para build/debug.
5. El Package debe autoload: uso obligatorio de `ProvideAutoLoad`.
6. Arquitectura: **Clean Architecture** por capas.
7. Integración IA: **HTTP** usando API **OpenAI-compatible**.
8. Proveedor principal en Fase 1: **LM Studio (local)**.
9. Endpoint remoto futuro previsto: `https://ia.thiscloud.com.ar` (sin implementarlo en Fase 1).
10. Configuración en **Tools → Options**, no dentro de la ToolWindow.

---

### 4. Alcance del producto

El producto se conduce con un alcance combinado:
- **C (primero):** Prototipo técnico controlado para validar arquitectura, integración IA y flujo.
- **B (después):** Evolución hacia una extensión publicable (Marketplace) con hardening y estándares.

---

### 5. Plan por fases

#### Fase 1 — Prototipo IA funcional (objetivo principal: IA local)

Objetivo:
- Integración robusta con un LLM por HTTP, usando LM Studio como proveedor principal.

Incluye:
- Cliente HTTP OpenAI-compatible (chat completions como base).
- Configuración persistente (Base URL, Model, API Key).
- Options Page (Tools → Options → Agente IA Local).
- ToolWindow mínima funcional (prompt → respuesta visible).

Excluye (Fase 1):
- Timeout configurable (lo maneja el origen).
- Configuración dentro de ToolWindow.
- Historial avanzado.
- Tools/function calling avanzado y structured outputs (solo previstos).

#### Fase 2 — UX

Objetivo:
- Mejorar experiencia: estados, streaming UI, errores más claros, layout.

#### Fase 3 — Publicable

Objetivo:
- Hardening, versionado, compatibilidad, documentación final, criterios de publicación.

---

### 6. Arquitectura por capas (Clean Architecture)

Capas esperadas:
- **AgenteIALocalVSIX**: VSIX Package, Commands, ToolWindow, Options.
- **Core**: contratos, entidades, value objects.
- **Application**: casos de uso, orquestación.
- **Infrastructure**: HTTP clients, adaptadores, persistencia concreta.
- **UI**: XAML/WPF para ToolWindow (si está separada, o dentro del VSIX).

Regla:
- UI depende de Application.
- Application depende de Core.
- Infrastructure implementa interfaces definidas por Core/Application.
- UI **no** accede directamente a Infrastructure.

---

### 7. Componentes VSIX (clásicos)

- `AsyncPackage`:
  - Inicializa y registra comandos.
  - Registra ToolWindow.
  - Debe autoload en contextos comunes.
- VSCT:
  - Define grupos, comandos y placements.
  - Puede mostrar menú aunque el Package no esté cargado.
- Command handler:
  - Se registra en `OleMenuCommandService`.
  - Ejecuta `ShowToolWindowAsync`.
- ToolWindowPane:
  - Host de control WPF (XAML).

---

### 8. Autoload del Package (punto crítico)

Hecho operativo (aprendido y validado):
- El menú puede aparecer por VSCT.
- Pero si el Package no carga, el comando no se registra y **no se ejecuta**.

Por lo tanto el Package debe tener autoload para contextos típicos:
- `UIContextGuids80.NoSolution`
- `UIContextGuids80.SolutionExists`

Este punto forma parte del baseline estable.

---

### 9. Integración IA (LM Studio first)

Decisión:
- Protocolo: **HTTP REST**.
- API: **OpenAI-compatible**.

Endpoints previstos (LM Studio):
- `chat/completions` (principal en Fase 1)
- Streaming `chat/completions` (habilitación incremental)
- `responses`, `tools`, `structured output`, `embeddings` (futuro)

Base URL:
- Local: `http://localhost:<port>` (LM Studio)
- Futuro remoto: `https://ia.thiscloud.com.ar`

Headers:
- `Content-Type: application/json`
- `Authorization: Bearer <ApiKey>` (aunque local lo ignore, debe existir para remoto futuro)

---

### 10. Configuración (Tools → Options)

Alcance confirmado:
1. URL base configurable: **Sí**
2. Modelo configurable (string): **Sí**
3. API Key / token: **Sí**
4. Timeout configurable: **No** (lo maneja el origen)
5. Persistencia: **Sí** (WritableSettingsStore)
6. UI en Tools → Options: **Sí**
7. UI de config en ToolWindow: **No**

Persistencia:
- `WritableSettingsStore` (UserSettings)
- Sección/clave estable (por ejemplo: `AgenteIALocal`)

---

### 11. UI/UX — Fase 1

#### ToolWindow (Agente IA Local)

Objetivo:
- Ejecutar requests al LLM y mostrar resultados.

Estructura mínima:
- Header con estado: `Sin configurar / Listo / Error`
- TextBox multilinea de prompt
- Botones: `Enviar`, `Test conexión`, `Limpiar`
- Área de respuesta (texto con scroll)

Reglas:
- Si falta Base URL o Model → deshabilitar Enviar y mostrar instrucción: “Configura en Tools → Options”.

#### Options

Objetivo:
- Configurar de manera persistente el endpoint y credenciales.

---

### 12. Manejo de errores y logging

Errores a cubrir (Fase 1):
- URL vacía / inválida
- Model vacío
- 401/403 (API Key inválida)
- 404 (endpoint incompatible)
- 5xx
- JSON no compatible
- Sin conexión

Reglas:
- Mostrar error corto en la ToolWindow.
- Log interno en ActivityLog.
- No dejar MessageBox de diagnóstico permanente.

---

### 13. Plan de tareas — Fase 1 (orden)

**Fase 1.1 — Infraestructura settings/persistencia**
- `AgentSettings` (BaseUrl, Model, ApiKey)
- Provider con `WritableSettingsStore`

**Fase 1.2 — Options Page**
- Tools → Options
- Bindings simples

**Fase 1.3 — Cliente HTTP OpenAI-compatible**
- Implementación `HttpAgentClient`
- Request a `chat/completions`

**Fase 1.4 — Integración mínima en ToolWindow**
- Botón Test conexión
- Enviar prompt y mostrar respuesta

---

### 14. Versioning, Git and releases

- No trabajar sobre tags.
- Crear ramas desde `vsix-stable-baseline`.
- Incrementar versión en `source.extension.vsixmanifest` por hito.

---

### 15. Non-regression checklist

Antes de cada commit relevante:
- ToolWindow abre.
- Command ejecuta.
- Package autoload activo.
- Build OK (VS Stable).
- Sin MessageBoxes temporales.

---

## 🇺🇸 English

### 1. Document purpose

This document defines, in a **binding and exhaustive** way, the architecture, scope, phases, and technical decisions of the **Agente IA Local (VSIX)** project.

Goals:
- Resume the project at any time without losing context.
- Prevent reopening validated decisions.
- Provide an architectural reference for development, maintenance, and tooling coordination (Copilot).

---

### 2. Current status and stable baseline

There is a stable baseline, tagged and published as:
- Tag: **`vsix-stable-baseline`**

This milestone guarantees:
- A working classic VSIX.
- `AsyncPackage` loads properly via `ProvideAutoLoad`.
- Commands are registered and executed.
- The ToolWindow opens correctly.
- No temporary diagnostic hacks remain.
- A clean repository state.

Rule: **do not break this baseline**. All features must be developed on branches created from this point.

---

### 3. Closed technical decisions (DO NOT reopen)

1. Project model: **classic VSIX** (NO modern SDK-style VSIX).
2. Target framework: **.NET Framework 4.8**.
3. Build/Debug: **Visual Studio Stable** only (VS 2022 or VS 2026 Stable).
4. Visual Studio Insiders: only for installing/testing a built `.vsix`, not for build/debug.
5. Package autoload is mandatory: `ProvideAutoLoad` must be present.
6. Architecture: **Clean Architecture** layering.
7. IA integration: **HTTP** using an **OpenAI-compatible** API.
8. Phase 1 primary provider: **LM Studio (local)**.
9. Future remote endpoint planned: `https://ia.thiscloud.com.ar` (not implemented in Phase 1).
10. Configuration lives in **Tools → Options**, not inside the ToolWindow.

---

### 4. Product scope

The product follows a combined scope:
- **C (first):** a controlled technical prototype to validate architecture, IA integration, and flow.
- **B (later):** evolve into a publishable Marketplace-ready extension with hardening and standards.

---

### 5. Phase plan

#### Phase 1 — Functional IA prototype (primary goal: local IA)

Goal:
- Robust IA integration over HTTP, with LM Studio as the primary provider.

Includes:
- OpenAI-compatible HTTP client (chat completions as the base).
- Persistent configuration (Base URL, Model, API Key).
- Options Page (Tools → Options → Agente IA Local).
- Minimal functional ToolWindow (prompt → visible response).

Excludes (Phase 1):
- Configurable timeouts (handled by the origin).
- Configuration UI inside the ToolWindow.
- Advanced conversation history.
- Advanced tools/function calling and structured outputs (planned only).

#### Phase 2 — UX

Goal:
- Improve user experience: states, UI streaming, clearer errors, layout.

#### Phase 3 — Publishable

Goal:
- Hardening, versioning, compatibility, final docs, release criteria.

---

### 6. Layered architecture (Clean Architecture)

Expected layers:
- **AgenteIALocalVSIX**: VSIX Package, Commands, ToolWindow, Options.
- **Core**: contracts, entities, value objects.
- **Application**: use cases, orchestration.
- **Infrastructure**: HTTP clients, adapters, concrete persistence.
- **UI**: XAML/WPF for the ToolWindow.

Rule:
- UI depends on Application.
- Application depends on Core.
- Infrastructure implements interfaces defined by Core/Application.
- UI must not access Infrastructure directly.

---

### 7. Classic VSIX components

- `AsyncPackage`:
  - Initializes and registers commands.
  - Registers the ToolWindow.
  - Must autoload in common contexts.
- VSCT:
  - Defines groups, commands, and placements.
  - Can show menu entries even when the Package is not loaded.
- Command handler:
  - Registered via `OleMenuCommandService`.
  - Executes `ShowToolWindowAsync`.
- ToolWindowPane:
  - Hosts a WPF control (XAML).

---

### 8. Package autoload (critical)

Validated operational fact:
- VSCT can show a menu entry.
- But if the Package does not load, the command is not registered and **will not execute**.

Therefore, the Package must autoload in typical contexts:
- `UIContextGuids80.NoSolution`
- `UIContextGuids80.SolutionExists`

This requirement is part of the stable baseline.

---

### 9. IA integration (LM Studio first)

Decision:
- Protocol: **HTTP REST**.
- API: **OpenAI-compatible**.

Planned endpoints (LM Studio):
- `chat/completions` (Phase 1 primary)
- Streaming `chat/completions` (incremental enablement)
- `responses`, `tools`, `structured output`, `embeddings` (future)

Base URL:
- Local: `http://localhost:<port>` (LM Studio)
- Future remote: `https://ia.thiscloud.com.ar`

Headers:
- `Content-Type: application/json`
- `Authorization: Bearer <ApiKey>` (even if local ignores it, required for future remote)

---

### 10. Configuration (Tools → Options)

Confirmed scope:
1. Configurable base URL: **Yes**
2. Configurable model (free string): **Yes**
3. API Key / token: **Yes**
4. Configurable timeout: **No** (origin-managed)
5. Persistence: **Yes** (`WritableSettingsStore`)
6. UI in Tools → Options: **Yes**
7. Config UI in ToolWindow: **No**

Persistence:
- `WritableSettingsStore` (UserSettings)
- Stable section/key (e.g., `AgenteIALocal`)

---

### 11. UI/UX — Phase 1

#### ToolWindow (Agente IA Local)

Goal:
- Send requests to the LLM and display results.

Minimal structure:
- Header status: `Not configured / Ready / Error`
- Multiline prompt TextBox
- Buttons: `Send`, `Test connection`, `Clear`
- Response area (scrollable text)

Rules:
- If Base URL or Model is missing, disable Send and show: “Configure in Tools → Options”.

---

### 12. Error handling and logging

Errors to cover (Phase 1):
- Empty/invalid URL
- Empty model
- 401/403 (invalid API key)
- 404 (incompatible endpoint)
- 5xx
- Non-compatible JSON
- No connection

Rules:
- Show a short actionable error in the ToolWindow.
- Log internally via ActivityLog.
- Do not keep permanent diagnostic MessageBoxes.

---

### 13. Phase 1 task plan (order)

**Phase 1.1 — Settings & persistence**
- `AgentSettings` (BaseUrl, Model, ApiKey)
- Provider using `WritableSettingsStore`

**Phase 1.2 — Options Page**
- Tools → Options
- Simple bindings

**Phase 1.3 — OpenAI-compatible HTTP client**
- `HttpAgentClient`
- `chat/completions` request

**Phase 1.4 — Minimal ToolWindow end-to-end**
- Test connection button
- Send prompt and show response

---

### 14. Versioning, Git and releases

- Do not work on tags.
- Create branches from `vsix-stable-baseline`.
- Bump version in `source.extension.vsixmanifest` per milestone.

---

### 15. Non-regression checklist

Before each relevant commit:
- ToolWindow opens.
- Command executes.
- Package autoload active.
- Build passes (VS Stable).
- No temporary MessageBoxes.
