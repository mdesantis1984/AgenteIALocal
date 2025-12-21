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

[OBSOLETO]
- Nota: El valor "Target framework: .NET Framework 4.8" se dejó como decisión histórica. En la práctica algunos proyectos del workspace (especialmente el proyecto VSIX de esta iteración) apuntan a **.NET Framework 4.7.2** para mantener compatibilidad con el entorno de build actual. Mantener esta entrada para trazabilidad histórica; cuando se decida un único objetivo estable, se actualizará el baseline.

3. Build/Debug: **Visual Studio Stable** (VS 2022 o VS 2026 Stable).
4. Visual Studio Insiders: solo para instalar/probar `.vsix` ya generado, no para build/debug.
5. El Package debe autoload: uso obligatorio de `ProvideAutoLoad`.
6. Arquitectura: **Clean Architecture** por capas.
7. Integración IA: **HTTP** usando API **OpenAI-compatible**.
8. Proveedor principal en Fase 1: **LM Studio (local)**.
9. Endpoint remoto futuro previsto: `https://ia.thiscloud.com.ar` (sin implementarlo en Fase 1).
10. Configuración en **Tools → Options**, no dentro de la ToolWindow (salvo panel inline de edición controlada en Sprint 3.3).

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
- Configuración dentro de ToolWindow (salvo panel inline controlado en Sprint 3.3).
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

[Nota de arquitectura]
- En la práctica reciente se reforzó la separación: `Core` define `IAgentService` y DTOs (`CopilotRequest/Response`), `Application` orquesta llamadas y `Infrastructure` contiene `HttpAgentClient`, `LmStudioClient` y `JanServerClient` como adaptadores. Esta separación facilita probar la UI con mocks y permite composición manual por el `AsyncPackage`.

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

[Nota práctica]
- El patrón comprobado para registro de comandos fue replicar un ejemplo funcional: `Instance` property, `InitializeAsync` que ejecuta `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync`, obtención de `OleMenuCommandService` y `new MenuCommand(...)/AddCommand`. Seguir estricto este patrón asegura que el click en el menú invoque `Execute` del handler.

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

[Multiples proveedores]
- El diseño actual contempla **múltiples proveedores**. Además de LM Studio, existe soporte por configuración para `JanServer` (alternativa remota) y la selección se realiza en tiempo de inicialización usando los `AgentSettings` (Provider type). El `AsyncPackage` realiza composición manual: lee `AgentSettings`, resuelve tipos por reflexión cuando aplica y asigna la implementación concreta a `AgentComposition.AgentService`.

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

[Logging y abstracciones]
- Se introdujo una abstracción de logging utilizada por la extensión (`IAgentLogger` / `AgentComposition.Logger`) y una implementación concreta de archivo (`FileAgentLogger`) que escribe trazas a:
  `%LOCALAPPDATA%\\AgenteIALocal\\logs\\AgenteIALocal.log`.
- La práctica de logging actual incluye trazas en:
  - `AsyncPackage.InitializeAsync` (inicio, cambio a UI thread, inicialización de comandos)
  - Registro de comandos en `OpenAgenteIALocalCommand.InitializeAsync` (inicio y registro)
  - Ejecución del handler `Execute` (primer log obligatorio)
  - ToolWindow eventos (ctor, Loaded, Run click, errores controlados)
- Reglas operativas: no silenciar errores sin log; cualquier excepción capturada debe registrar `Logger.Error` y, cuando aplique, `ActivityLogHelper.TryLogError`.

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


---

### Decisiones recientes y notas de arquitectura (adiciones incrementales)

- Cableado VSCT ↔ Package: se corrigieron discrepancias de GUID que impedían que `Execute` de los handlers se invocara. Se agregó validación de consistencia y logging durante `Package.InitializeAsync`.
- Composición manual del `AgentService`: debido a las restricciones del VSIX clásico no se utiliza un contenedor DI. El `AsyncPackage` lee `AgentSettings` en tiempo de inicialización y compone `AgentClient`/`AgentService` de forma manual (reflexión condicionada por Provider) y asigna la instancia a `AgentComposition.AgentService`.
- Soporte multi-proveedor: actualmente soportados conceptualmente `LmStudio` y `JanServer`. La selección se basa en `AgentSettings.Provider` y en la resolución de tipos durante la inicialización del Package.
- Logging persistente: se añadió `FileAgentLogger` para trazas locales y se integró con `ActivityLogHelper` cuando es necesario.
- Fail-safe en inicialización: `Package.InitializeAsync` evita relanzar excepciones fatales, registra errores y continúa en estado seguro.

[Nota operacional]
- No usar contenedores DI dentro del VSIX clásico: la práctica aceptada en este proyecto es composición manual en el Package y exposición a través de `AgentComposition` para minimizar el footprint y evitar problemas de ciclo de vida del host.

---

If there are additional architecture items that need clarifying (diagrams, sequence flows or decision records), add them as incremental PRs referencing this document and linking to the `vsix-stable-baseline` tag.

---
