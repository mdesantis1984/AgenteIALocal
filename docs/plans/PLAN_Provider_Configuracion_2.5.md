# PLAN CONSOLIDADO — AgenteIALocalVSIX (Provider/RunMode)

- Rama: feature/provider-runmode-sync
- Versión del plan consolidado: 2.3 (incremento desde 2.2)
- Última actualización: 2026-01-18
- Nota: esta versión agrega evidencia adicional (executionPath) y el modo offline de modelos cuando /v1/models falla (servidor detenido), sin eliminar contenido previo.
- Nota v1.8: se descarta el modo no-stream. `requestDefaults.stream` queda como opcion unica (siempre `true`) en UI y payload. La seccion T5.1 anterior se conserva como historico (marcada OBSOLETA).
- Nota v1.9: se agrega filtrado de modelos no compatibles con chat (embeddings) para evitar seleccionar modelos que devuelven 400 “Model is not llm”. El filtrado aplica a listas de modelos y al modo offline (solo rehidrata modelos chat-validos).
- Nota v2.0: se detecta que el conteo de Tokens queda en 0 cuando el servidor no envía usage; se hará persistente el toggle "Include usage" (solo LM Studio) y se ajustará la extracción/representación de tokens (SSE) sin enviar stream_options a JAN.
- Nota v2.1: se implementa persistencia de `requestDefaults.streamOptions.includeUsage` (solo LM Studio), y se corrige la UI para mostrar Tokens reales cuando hay usage; cuando no hay usage, se muestra "-" (no 0).
- Nota v2.1: se implementa persistencia de includeUsage (solo LM Studio) y se ajusta el UI de tokens: no mostrar 0 cuando no hay usage (usar “-”), y persistir tokens reales cuando el servidor envía usage.
- Nota v2.2: se re-planifica requestDefaults y agent porque desde UI no persisten ni se aplican de punta a punta. Acciones: (1) ocultar Stream en UI (stream fijo true), (2) exponer Temperature y MaxTokens, (3) persistir includeUsage/temperature/maxTokens/agent en vivo (on-change) y en Guardar (Save), (4) aplicar requestDefaults en payload Preguntar (LM Studio/JAN) y en Agent clients (LmStudioClient/JanServerClient) con compatibilidad por proveedor.
- Nota v2.2: se detecta que requestDefaults (temperature/maxTokens/streamOptions.includeUsage) y agent (ideIntegration/applyChanges/maxSteps) no quedan funcionales desde el modal: faltan controles para temperature/maxTokens, el provider no se normaliza en los handlers, Save no persiste el DTO global, y la ruta Agente (IAgentClient) no aplica requestDefaults/agent. Se corrige: (a) Stream se oculta en UI pero se mantiene fijo stream=true; (b) requestDefaults y agent quedan configurables y persistidos on-change + on-save (DTO completo); (c) requestDefaults se aplica a payload en Preguntar y en Agente (LmStudioClient/JanServerClient) con compatibilidad por proveedor.
- Nota v2.3: se amplía `globalSettings.requestDefaults` con parámetros **comunes** (compatibles LM Studio + Jan): `topP`, `stop`, `presencePenalty`, `frequencyPenalty`. Persistencia **live + Save** y aplicación en payload (Preguntar streaming + Agente) **solo si están seteados** (no-default / no vacíos) para evitar breaking changes.

- Fuentes consolidadas (sin pérdida de contenido): plan.md, plan_v2.md, plan_v2.1.md, plan_v2.2.md, plan_v2.3.md, plan_v2.4.md

## Estado consolidado v2.3 (resumen operativo)

**Progreso consolidado:** 90%

**Completado:**
- Routing por runMode + executionPath (1 linea por envio).
- Filtrado de modelos no chat (embeddings).
- Offline model fallback (preserva modelo persistido cuando /v1/models falla).
- Stream fijo en true a nivel settings/payload (pendiente ocultar el control).
- Tokens: muestra '-' si no hay usage; muestra numero si hay usage.

**Pendiente inmediato:**
- requestDefaults: Temperature, MaxTokens, includeUsage, **topP, stop, presencePenalty, frequencyPenalty** -> UI + persistencia on-change + Save (DTO completo).
- agent: ideIntegration, applyChanges, maxSteps -> usar end-to-end en AgentService.
- payload: aplicar requestDefaults en Preguntar (chat completions) y Agente (LmStudioClient/JanServerClient); `stream_options.include_usage` solo LM Studio; **campos comunes** mapeados a OpenAI (`top_p`, `stop`, `presence_penalty`, `frequency_penalty`) solo si no-default/no-vacío.
- smoke tests LM Studio/JAN y documentacion final.

**Criterios de aceptación v2.3 (requestDefaults comunes, sin romper):**
- Al cambiar topP/stop/presencePenalty/frequencyPenalty en el modal, `settings.json` actualiza `globalSettings.requestDefaults.*` (live) y Guardar no revierte cambios.
- En Preguntar (streaming) y Agente (AgentClient), el payload incluye `top_p/stop/presence_penalty/frequency_penalty` **solo** cuando el usuario setea valores no-default (topP!=1.0, penalties!=0, stop no vacío).

### Sprint de cierre v2.3 (max 5 tareas)
- T1 (UI/Settings): Hacer configurables requestDefaults (temperature, maxTokens, streamOptions.includeUsage, **topP, stop, presencePenalty, frequencyPenalty**) y agent (ideIntegration, applyChanges, maxSteps) en el modal; persistir en vivo (event change) y tambien en Guardar (Save), guardando el DTO completo.
- T2 (UI): Ocultar visualmente Stream en el modal (sin eliminar control; solo Visibility=Collapsed). Mantener stream fijo true.
- T3 (Preguntar): Aplicar requestDefaults al payload /v1/chat/completions: `temperature`, `max_tokens` (si > 0), y campos comunes `top_p`, `stop`, `presence_penalty`, `frequency_penalty` (solo si no-default/no-vacío); `stream_options.include_usage` solo LM Studio cuando includeUsage=true.
- T4 (Agente): Propagar y aplicar requestDefaults y agent al pipeline de AgentService (Infrastructure clients) sin romper Clean Architecture; incluir mapeo de campos comunes (top_p/stop/presence_penalty/frequency_penalty) solo si no-default/no-vacío.
- T5 (Cierre): Smoke tests (LM Studio y JAN), actualizar documentacion y cerrar la rama.
### Sprint de cierre v2.3 (duplicado - deprecado; usar el bloque superior)
- T1 (UI/Settings): Hacer configurables requestDefaults (temperature, maxTokens, streamOptions.includeUsage, **topP, stop, presencePenalty, frequencyPenalty**) y agent (ideIntegration, applyChanges, maxSteps) en el modal; persistir en vivo (event change) y tambien en Guardar (Save), guardando el DTO completo.
- T2 (UI): Ocultar visualmente Stream en el modal (sin eliminar control; solo Visibility=Collapsed). Mantener stream fijo true.
- T3 (Preguntar): Aplicar requestDefaults al request /v1/chat/completions: `temperature`, `max_tokens` (si > 0), y campos comunes `top_p`, `stop`, `presence_penalty`, `frequency_penalty` (solo si no-default/no-vacío); `stream_options.include_usage` solo LM Studio cuando includeUsage=true.
- T4 (Agente): Propagar requestDefaults y agent options al pipeline de agente (AgentService + LmStudioClient/JanServerClient); incluir mapeo de campos comunes (top_p/stop/presence_penalty/frequency_penalty) solo si no-default/no-vacío.
- T5 (Cierre): Smoke tests (LM Studio y JAN), actualizar documentacion y cerrar rama (commit/push/PR segun reglas).


## Definiciones (Source of Truth)
### Provider (persistente)
- Ubicación: `settings.json` → `Servers[ActiveServerId].Provider`
- Valores: `"lmstudio"` | `"jan"`
- Default real: `"lmstudio"`

### RunMode (persistente)
- Ubicación: `settings.json` → `GlobalSettings["runMode"]`
- Valores: `"preguntar"` | `"agente"`
- Default real: `"preguntar"`

---

## Matriz de impacto (qué cambia según selección)
### Provider
- BaseUrl/ApiKey/Model se leen del `ServerConfig` activo (mismo mecanismo actual).
- Runtime: request a `POST {BaseUrl}/v1/chat/completions`
- Authorization: si hay ApiKey → `Authorization: Bearer {ApiKey}`.

### RunMode
- `preguntar`: payload normal (sin system prompt de agente).
- `agente`: agrega system prompt de “agente” (string constante) antes del user message.
- No tool-calling en esta iteración (para mantener compatibilidad).

---

## Tareas (máx. 5, cada una con commit)
### T1 — Defaults y persistencia robusta en AgentSettingsStore
**Archivos:**
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

**Cambios:**
- Asegurar `GlobalSettings["runMode"]="preguntar"` en `CreateDefaultSettings` si falta.
- Asegurar `Provider="lmstudio"` y baseUrl default LM Studio (ya existe; verificar consistencia).
- Helpers mínimos: getters/setters seguros para runMode (sin NRE).

**Criterio de aceptación:**
- Si `settings.json` no existe, se crea con provider=lmstudio y runMode=preguntar.
- Si `runMode` falta, al cargar se asume `preguntar` y al guardar queda persistido.

---

### T2 — Modal: selector Provider persistido en settings
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`

**Cambios:**
- Añadir ComboBox Provider (LM Studio / Jan).
- OnLoad: precargar desde `Servers[ActiveServerId].Provider`.
- OnSave: persistir provider en el server activo y guardar con AgentSettingsStore.

**Criterio de aceptación:**
- Abrir modal muestra Provider actual.
- Cambiar Provider + Save → `settings.json` actualizado.

---

### T3 — Control principal: sincronización UI ↔ settings (provider + runmode)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.Helpers.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- `ServerLLM_SelectionChanged`: guardar Provider en settings + refrescar/recompose + refrescar UI (modelos y ConfigLabel si aplica).
- Agregar `TypeActivitie_SelectionChanged` en XAML + handler: persistir `runMode` en GlobalSettings + refrescar runtime.
- OnInit/Loaded: hidratar combos desde settings con flag `_isInitializingUi` para evitar loops.

**Criterio de aceptación:**
- Cambiar provider/mode en control actualiza `settings.json`.
- Reabrir modal refleja lo seleccionado.
- Reabrir toolwindow refleja lo guardado.

---

### T4 — Runtime: dejar de estar hardcodeado a lmstudio
**Archivos:**
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- Generalizar checks `Provider == lmstudio` a `Provider in {lmstudio, jan}`.
- Recompose: no asignar mock AgentService solo por no ser lmstudio.
- Request: reutilizar el mismo pipeline OpenAI-compatible con baseUrl seleccionado.

**Criterio de aceptación:**
- Provider=jan no cae en mock; ejecuta request contra su baseUrl.
- Provider=lmstudio sigue funcionando sin regressions (streaming SSE intacto).

---

### T5 — RunMode afecta el payload (Preguntar/Agente)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- Al construir `messages`, inyectar system prompt solo en modo `agente`.
- Logging on-change de RunMode (una sola línea por cambio).

**Criterio de aceptación:**
- Alternar Preguntar/Agente cambia el payload del siguiente envío.
- No rompe streaming ni el append incremental ya implementado.

---

## No objetivos (para evitar romper)
- No introducir tool-calling/`tools` en esta iteración.
- No refactor grande de arquitectura ni nuevos proyectos.
- No tocar archivos prohibidos.

---

## Checklist de verificación manual
1) Provider LM Studio:
   - Ping OK → modelos poblados → Run streaming OK.
2) Provider Jan:
   - Ping (si /v1/models soporta) → modelos OK; si no, degradación controlada (ConfigLabel “Not Config” y sin crash).
   - Run contra `POST /v1/chat/completions` con Authorization si hay ApiKey.
3) RunMode:
   - `preguntar` vs `agente` modifica system prompt (observable por logs/payload) sin romper respuesta.

---

## Secuencia “paso a paso”
- Paso 1: Discovery (grep + localizar puntos exactos).
- Paso 2: Implementar T1 + commit.
- Paso 3: Implementar T2 + commit.
- Paso 4: Implementar T3 + commit.
- Paso 5: Implementar T4 + commit.
- Paso 6: Implementar T5 + commit.
- Paso 7: Pruebas VS 2026 Experimental + PR.


<!-- END plan.md -->

---

## Fuente: plan_v2.md

<!-- BEGIN plan_v2.md -->

# Plan de implementación (v2): Provider (LM Studio / Jan) + RunMode + Streaming + Agent flags (sync UI ↔ settings.json)

**Proyecto:** AgenteIALocalVSIX  
**Rama:** `feature/provider-runmode-sync`  
**Última actualización:** 2026-01-15  
**Estado:** En progreso (0%)

## Objetivo
1) El usuario elige **Provider** (LM Studio / Jan) y queda guardado en `settings.json` (AgentSettingsStore).  
2) UI del **control principal** y del **modal** quedan **sincronizados** (bidireccional) con `settings.json`.  
3) El **runtime** envía al LLM usando la configuración del **server activo** y el **modo** seleccionado, sin romper streaming SSE actual.

## Reglas operativas (Reglas.IA.md)
- Paso a paso: 1 tarea a la vez, con confirmación humana antes de modificar.
- Cambios mínimos en Presentation; no tocar archivos prohibidos (`*.vsix`, `*.vsct`, `*.vsixmanifest`, `*.csproj`, `*.sln`).
- SRP/SOLID: responsabilidades separadas por métodos/clases parciales existentes (sin añadir nuevos proyectos; evitar nuevos archivos si obliga a tocar csproj).
- Logging: solo transiciones (on-change), sin spam.

---

## Esquema objetivo de `settings.json` (v1 extendido, backwards-compatible)
> Nota: se agregan campos **opcionales**. El store debe **preservar** campos desconocidos (ya existe `_raw`).

```json
{
  "version": "v1",
  "servers": [
    {
      "id": "lmstudio-local",
      "name": "LM Studio (local)",
      "provider": "lmstudio",
      "baseUrl": "http://127.0.0.1:1234",
      "apiKey": "lm-studio",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": true,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": {
        "stream": true,
        "streamOptions": { "includeUsage": true }
      },
      "capabilities": {
        "supportsModelsEndpoint": true,
        "supportsChatCompletions": true,
        "supportsStreaming": true,
        "supportsStreamOptionsIncludeUsage": true,
        "supportsTools": true
      }
    },
    {
      "id": "jan-local",
      "name": "Jan (local)",
      "provider": "jan",
      "baseUrl": "http://127.0.0.1:1337",
      "apiKey": "jan-lm",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": false,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": { "stream": true },
      "capabilities": {
        "supportsModelsEndpoint": null,
        "supportsChatCompletions": true,
        "supportsStreaming": null,
        "supportsStreamOptionsIncludeUsage": null,
        "supportsTools": null
      }
    }
  ],
  "activeServerId": "lmstudio-local",
  "globalSettings": {
    "defaultTimeoutMs": 60000,
    "useProxy": false,
    "runMode": "preguntar",
    "requestDefaults": {
      "stream": true,
      "temperature": 0.2,
      "maxTokens": 0
    },
    "agent": {
      "ideIntegration": true,
      "applyChanges": false,
      "maxSteps": 5
    }
  },
  "taskProfiles": []
}
```

### Reglas de “source of truth”
- El selector **Provider** NO debe “mutar” un mismo server: debe **cambiar `activeServerId`** al server del provider seleccionado.
- El `model` se persiste **por server** (`servers[i].model`) cuando el usuario lo elige.
- `isDefault` solo cambia si el usuario lo marca explícitamente (no por seleccionar).

---

## Qué debe agregarse/ajustarse en la ventana de configuración (modal)
**Archivo(s):** `AgenteIALocalConfigWindow.xaml/.cs`  
**Objetivo:** editar el server activo y parámetros globales sin romper estilos existentes.

### Sección “Proveedor / Servidor”
- **ComboBox Provider**: `LM Studio` / `Jan`
  - Acción: al cambiar, seleccionar el server correspondiente (set `activeServerId`) y **recargar** campos (BaseUrl, ApiKey, Model, stream flags).
- **TextBox BaseUrl** (ya existe): mantiene validación “ping” /v1/models y populate de modelos.
- **TextBox ApiKey**: editable y persistente por server.
- **ComboBox Model**: se llena tras ping 200; al elegir, se persiste en el server activo.

### Sección “Request”
- **Toggle Stream** (CheckBox/ToggleButton con estilo existente):
  - Persiste en `server.requestOverrides.stream` (override por provider).
- **Toggle includeUsage**:
  - Persiste en `server.requestOverrides.streamOptions.includeUsage`.
  - UI: habilitado solo si provider es `lmstudio` (para Jan se deja disabled/oculto; no romper por envío de campos no soportados).

### Sección “Modo”
- **ComboBox RunMode**: `Preguntar` / `Agente`
  - Persiste en `globalSettings.runMode`.
  - Debe reflejarse también en el combo del control principal (sync bidireccional).

### Sección “Agente”
- **Toggle IDE Integration**: `globalSettings.agent.ideIntegration`
- **Toggle Apply changes**: `globalSettings.agent.applyChanges`
- **Numeric Max steps**: `globalSettings.agent.maxSteps`

> Nota: si hoy el modal no tiene espacio, se agrega un “Advanced expander” manteniendo el mismo esquema de colores/estilos.

---

## Tareas (Sprint ≤ 5 tareas). Progreso acumulado se actualiza aquí.
### T1 — SettingsStore: schema extendido + defaults + migración suave (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

**Cambios:**
- Asegurar que existan 2 servers (lmstudio-local y jan-local) sin pisar configs existentes.
- Asegurar `globalSettings.runMode` con default `preguntar`.
- Asegurar `globalSettings.requestDefaults` y `globalSettings.agent` con defaults.
- Asegurar `activeServerId` válido (si falta o apunta a inexistente).
- Persistir solo si se inyectaron defaults (defensivo, sin throw).

**DoD:**
- `settings.json` nuevo se crea con ambos servers + global settings extendido.
- `settings.json` existente no pierde campos ni valores; solo se completan faltantes.

---

### T2 — Modal: provider + stream + includeUsage + runMode + agent (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`

**Cambios:**
- Agregar controls (Provider, Stream, includeUsage, RunMode, Agent flags) usando estilos existentes.
- OnLoad: bind/precarga desde settings.
- OnChange: actualizar UI (habilitar includeUsage solo en lmstudio) y si corresponde persistir.
- OnSave: persistir todo (server activo + global settings).

**DoD:**
- Cambiar Provider cambia `activeServerId` y recarga campos.
- Guardar persiste y reabrir mantiene valores.

---

### T3 — Control principal: sync combos (Provider y RunMode) con settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.Helpers.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- `ServerLLM_SelectionChanged`: set `activeServerId` (no mutar Provider) + Save + recompose + refrescar UI.
- `TypeActivitie_SelectionChanged`: set `globalSettings.runMode` + Save.
- OnInit/Loaded: hidratar combos desde settings con flag anti-recursión.

**DoD:**
- Cambiar en el control persiste; abrir modal refleja lo mismo (y viceversa).

---

### T4 — Runtime: provider-agnóstico (lmstudio/jan) + aplicar stream settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- Quitar hardcode “lmstudio”: provider `jan` no debe caer en mock.
- `POST {BaseUrl}/v1/chat/completions` para ambos.
- Stream on/off según settings (server override o global defaults).
- `stream_options.include_usage` solo si provider=lmstudio y flag true.
- Authorization Bearer si ApiKey no vacío.

**DoD:**
- LM Studio sigue funcionando como hoy.
- Jan ejecuta request real (sin mock) con su baseUrl.

---

### T5 — RunMode afecta payload + verificación + actualización plan (% y checklist) (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`
- (opcional) README/ReleaseNotes si entra en reglas del sprint

**Cambios:**
- `preguntar` vs `agente`: `agente` agrega system prompt (constante) y aplica flags `agent.*` solo a lógica interna (no tool-calling aún).
- Logging on-change de Provider/RunMode/Stream/ConfigStatus.

**DoD:**
- Alternar RunMode cambia el siguiente payload (observable por logs) y no rompe streaming.

---

## Tracking de avance (manual)
- Total = promedio de T1..T5.
- Al cerrar cada tarea: actualizar aquí el % y anotar commit SHA.

**Progreso actual:** 0%  
- T1: 0% (pendiente)  
- T2: 0% (pendiente)  
- T3: 0% (pendiente)  
- T4: 0% (pendiente)  
- T5: 0% (pendiente)  

---

## Checklist de pruebas (VS 2026 Experimental)
1) Provider LM Studio: ping OK → modelos poblados → Run streaming OK.
2) Provider Jan: ping OK/Degradación controlada → Run OK contra /v1/chat/completions con Bearer.
3) Stream off: respuesta completa (no SSE).
4) RunMode Agente: system prompt aplicado; sin tool-calling; no regressions en UI.

<!-- END plan_v2.md -->

---

## Fuente: plan_v2.1.md

<!-- BEGIN plan_v2.1.md -->

# Plan de implementación (v2): Provider (LM Studio / Jan) + RunMode + Streaming + Agent flags (sync UI ↔ settings.json)

**Proyecto:** AgenteIALocalVSIX  
**Rama:** `feature/provider-runmode-sync`  
**Última actualización:** 2026-01-15  
**Estado:** En progreso (0%)

## Objetivo
1) El usuario elige **Provider** (LM Studio / Jan) y queda guardado en `settings.json` (AgentSettingsStore).  
2) UI del **control principal** y del **modal** quedan **sincronizados** (bidireccional) con `settings.json`.  
3) El **runtime** envía al LLM usando la configuración del **server activo** y el **modo** seleccionado, sin romper streaming SSE actual.

## Reglas operativas (Reglas.IA.md)
- Paso a paso: 1 tarea a la vez, con confirmación humana antes de modificar.
- Cambios mínimos en Presentation; no tocar archivos prohibidos (`*.vsix`, `*.vsct`, `*.vsixmanifest`, `*.csproj`, `*.sln`).
- SRP/SOLID: responsabilidades separadas por métodos/clases parciales existentes (sin añadir nuevos proyectos; evitar nuevos archivos si obliga a tocar csproj).
- Logging: solo transiciones (on-change), sin spam.

---

## Esquema objetivo de `settings.json` (v1 extendido, backwards-compatible)
> Nota: se agregan campos **opcionales**. El store debe **preservar** campos desconocidos (ya existe `_raw`).

```json
{
  "version": "v1",
  "servers": [
    {
      "id": "lmstudio-local",
      "name": "LM Studio (local)",
      "provider": "lmstudio",
      "baseUrl": "http://127.0.0.1:1234",
      "apiKey": "lm-studio",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": true,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": {
        "stream": true,
        "streamOptions": { "includeUsage": true }
      },
      "capabilities": {
        "supportsModelsEndpoint": true,
        "supportsChatCompletions": true,
        "supportsStreaming": true,
        "supportsStreamOptionsIncludeUsage": true,
        "supportsTools": true
      }
    },
    {
      "id": "jan-local",
      "name": "Jan (local)",
      "provider": "jan",
      "baseUrl": "http://127.0.0.1:1337",
      "apiKey": "jan-lm",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": false,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": { "stream": true },
      "capabilities": {
        "supportsModelsEndpoint": null,
        "supportsChatCompletions": true,
        "supportsStreaming": null,
        "supportsStreamOptionsIncludeUsage": null,
        "supportsTools": null
      }
    }
  ],
  "activeServerId": "lmstudio-local",
  "globalSettings": {
    "defaultTimeoutMs": 60000,
    "useProxy": false,
    "runMode": "preguntar",
    "requestDefaults": {
      "stream": true,
      "temperature": 0.2,
      "maxTokens": 0
    },
    "agent": {
      "ideIntegration": true,
      "applyChanges": false,
      "maxSteps": 5
    }
  },
  "taskProfiles": []
}
```

### Reglas de “source of truth”
- El selector **Provider** NO debe “mutar” un mismo server: debe **cambiar `activeServerId`** al server del provider seleccionado.
- El `model` se persiste **por server** (`servers[i].model`) cuando el usuario lo elige.
- `isDefault` solo cambia si el usuario lo marca explícitamente (no por seleccionar).

---

## Qué debe agregarse/ajustarse en la ventana de configuración (modal)
**Archivo(s):** `AgenteIALocalConfigWindow.xaml/.cs`  
**Objetivo:** editar el server activo y parámetros globales sin romper estilos existentes.

### Sección “Proveedor / Servidor”
- **ComboBox Provider**: `LM Studio` / `Jan`
  - Acción: al cambiar, seleccionar el server correspondiente (set `activeServerId`) y **recargar** campos (BaseUrl, ApiKey, Model, stream flags).
- **TextBox BaseUrl** (ya existe): mantiene validación “ping” /v1/models y populate de modelos.
- **TextBox ApiKey**: editable y persistente por server.
- **ComboBox Model**: se llena tras ping 200; al elegir, se persiste en el server activo.

### Sección “Request”
- **Toggle Stream** (CheckBox/ToggleButton con estilo existente):
  - Persiste en `server.requestOverrides.stream` (override por provider).
- **Toggle includeUsage**:
  - Persiste en `server.requestOverrides.streamOptions.includeUsage`.
  - UI: habilitado solo si provider es `lmstudio` (para Jan se deja disabled/oculto; no romper por envío de campos no soportados).

### Sección “Modo”
- **ComboBox RunMode**: `Preguntar` / `Agente`
  - Persiste en `globalSettings.runMode`.
  - Debe reflejarse también en el combo del control principal (sync bidireccional).

### Sección “Agente”
- **Toggle IDE Integration**: `globalSettings.agent.ideIntegration`
- **Toggle Apply changes**: `globalSettings.agent.applyChanges`
- **Numeric Max steps**: `globalSettings.agent.maxSteps`

> Nota: si hoy el modal no tiene espacio, se agrega un “Advanced expander” manteniendo el mismo esquema de colores/estilos.

---

## Tareas (Sprint ≤ 5 tareas). Progreso acumulado se actualiza aquí.
### T1 — SettingsStore: schema extendido + defaults + migración suave (100%)

**Commit:** f7111e2
**Archivos:**
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

**Cambios:**
- Asegurar que existan 2 servers (lmstudio-local y jan-local) sin pisar configs existentes.
- Asegurar `globalSettings.runMode` con default `preguntar`.
- Asegurar `globalSettings.requestDefaults` y `globalSettings.agent` con defaults.
- Asegurar `activeServerId` válido (si falta o apunta a inexistente).
- Persistir solo si se inyectaron defaults (defensivo, sin throw).

**DoD:**
- `settings.json` nuevo se crea con ambos servers + global settings extendido.
- `settings.json` existente no pierde campos ni valores; solo se completan faltantes.

---

### T2 — Modal: provider + stream + includeUsage + runMode + agent (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`

**Cambios:**
- Agregar controls (Provider, Stream, includeUsage, RunMode, Agent flags) usando estilos existentes.
- OnLoad: bind/precarga desde settings.
- OnChange: actualizar UI (habilitar includeUsage solo en lmstudio) y si corresponde persistir.
- OnSave: persistir todo (server activo + global settings).

**DoD:**
- Cambiar Provider cambia `activeServerId` y recarga campos.
- Guardar persiste y reabrir mantiene valores.

---

### T3 — Control principal: sync combos (Provider y RunMode) con settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.Helpers.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- `ServerLLM_SelectionChanged`: set `activeServerId` (no mutar Provider) + Save + recompose + refrescar UI.
- `TypeActivitie_SelectionChanged`: set `globalSettings.runMode` + Save.
- OnInit/Loaded: hidratar combos desde settings con flag anti-recursión.

**DoD:**
- Cambiar en el control persiste; abrir modal refleja lo mismo (y viceversa).

---

### T4 — Runtime: provider-agnóstico (lmstudio/jan) + aplicar stream settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- Quitar hardcode “lmstudio”: provider `jan` no debe caer en mock.
- `POST {BaseUrl}/v1/chat/completions` para ambos.
- Stream on/off según settings (server override o global defaults).
- `stream_options.include_usage` solo si provider=lmstudio y flag true.
- Authorization Bearer si ApiKey no vacío.

**DoD:**
- LM Studio sigue funcionando como hoy.
- Jan ejecuta request real (sin mock) con su baseUrl.

---

### T5 — RunMode afecta payload + verificación + actualización plan (% y checklist) (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`
- (opcional) README/ReleaseNotes si entra en reglas del sprint

**Cambios:**
- `preguntar` vs `agente`: `agente` agrega system prompt (constante) y aplica flags `agent.*` solo a lógica interna (no tool-calling aún).
- Logging on-change de Provider/RunMode/Stream/ConfigStatus.

**DoD:**
- Alternar RunMode cambia el siguiente payload (observable por logs) y no rompe streaming.

---

## Tracking de avance (manual)
- Total = promedio de T1..T5.
- Al cerrar cada tarea: actualizar aquí el % y anotar commit SHA.

**Progreso actual:** 20%
- T1: 100% (done) — f7111e2
- T2: 0% (pendiente)
- T3: 0% (pendiente)
- T4: 0% (pendiente)
- T5: 0% (pendiente)

---

## Checklist de pruebas (VS 2026 Experimental)
1) Provider LM Studio: ping OK → modelos poblados → Run streaming OK.
2) Provider Jan: ping OK/Degradación controlada → Run OK contra /v1/chat/completions con Bearer.
3) Stream off: respuesta completa (no SSE).
4) RunMode Agente: system prompt aplicado; sin tool-calling; no regressions en UI.

<!-- END plan_v2.1.md -->

---

## Fuente: plan_v2.2.md

<!-- BEGIN plan_v2.2.md -->

# Plan de implementación (v2): Provider (LM Studio / Jan) + RunMode + Streaming + Agent flags (sync UI ↔ settings.json)

**Proyecto:** AgenteIALocalVSIX  
**Rama:** `feature/provider-runmode-sync`  
**Última actualización:** 2026-01-15  
**Estado:** En progreso (0%)

## Objetivo
1) El usuario elige **Provider** (LM Studio / Jan) y queda guardado en `settings.json` (AgentSettingsStore).  
2) UI del **control principal** y del **modal** quedan **sincronizados** (bidireccional) con `settings.json`.  
3) El **runtime** envía al LLM usando la configuración del **server activo** y el **modo** seleccionado, sin romper streaming SSE actual.

## Reglas operativas (Reglas.IA.md)
- Paso a paso: 1 tarea a la vez, con confirmación humana antes de modificar.
- Cambios mínimos en Presentation; no tocar archivos prohibidos (`*.vsix`, `*.vsct`, `*.vsixmanifest`, `*.csproj`, `*.sln`).
- SRP/SOLID: responsabilidades separadas por métodos/clases parciales existentes (sin añadir nuevos proyectos; evitar nuevos archivos si obliga a tocar csproj).
- Logging: solo transiciones (on-change), sin spam.

---

## Esquema objetivo de `settings.json` (v1 extendido, backwards-compatible)
> Nota: se agregan campos **opcionales**. El store debe **preservar** campos desconocidos (ya existe `_raw`).

```json
{
  "version": "v1",
  "servers": [
    {
      "id": "lmstudio-local",
      "name": "LM Studio (local)",
      "provider": "lmstudio",
      "baseUrl": "http://127.0.0.1:1234",
      "apiKey": "lm-studio",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": true,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": {
        "stream": true,
        "streamOptions": { "includeUsage": true }
      },
      "capabilities": {
        "supportsModelsEndpoint": true,
        "supportsChatCompletions": true,
        "supportsStreaming": true,
        "supportsStreamOptionsIncludeUsage": true,
        "supportsTools": true
      }
    },
    {
      "id": "jan-local",
      "name": "Jan (local)",
      "provider": "jan",
      "baseUrl": "http://127.0.0.1:1337",
      "apiKey": "jan-lm",
      "model": "qwen3-4b-dotnet-specialist",
      "isDefault": false,
      "createdAt": "2025-12-27T03:46:38.4943506Z",
      "requestOverrides": { "stream": true },
      "capabilities": {
        "supportsModelsEndpoint": null,
        "supportsChatCompletions": true,
        "supportsStreaming": null,
        "supportsStreamOptionsIncludeUsage": null,
        "supportsTools": null
      }
    }
  ],
  "activeServerId": "lmstudio-local",
  "globalSettings": {
    "defaultTimeoutMs": 60000,
    "useProxy": false,
    "runMode": "preguntar",
    "requestDefaults": {
      "stream": true,
      "temperature": 0.2,
      "maxTokens": 0
    },
    "agent": {
      "ideIntegration": true,
      "applyChanges": false,
      "maxSteps": 5
    }
  },
  "taskProfiles": []
}
```

### Reglas de “source of truth”
- El selector **Provider** NO debe “mutar” un mismo server: debe **cambiar `activeServerId`** al server del provider seleccionado.
- El `model` se persiste **por server** (`servers[i].model`) cuando el usuario lo elige.
- `isDefault` solo cambia si el usuario lo marca explícitamente (no por seleccionar).

---

## Qué debe agregarse/ajustarse en la ventana de configuración (modal)
**Archivo(s):** `AgenteIALocalConfigWindow.xaml/.cs`  
**Objetivo:** editar el server activo y parámetros globales sin romper estilos existentes.

### Sección “Proveedor / Servidor”
- **ComboBox Provider**: `LM Studio` / `Jan`
  - Acción: al cambiar, seleccionar el server correspondiente (set `activeServerId`) y **recargar** campos (BaseUrl, ApiKey, Model, stream flags).
- **TextBox BaseUrl** (ya existe): mantiene validación “ping” /v1/models y populate de modelos.
- **TextBox ApiKey**: editable y persistente por server.
- **ComboBox Model**: se llena tras ping 200; al elegir, se persiste en el server activo.

### Sección “Request”
- **Toggle Stream** (CheckBox/ToggleButton con estilo existente):
  - Persiste en `server.requestOverrides.stream` (override por provider).
- **Toggle includeUsage**:
  - Persiste en `server.requestOverrides.streamOptions.includeUsage`.
  - UI: habilitado solo si provider es `lmstudio` (para Jan se deja disabled/oculto; no romper por envío de campos no soportados).

### Sección “Modo”
- **ComboBox RunMode**: `Preguntar` / `Agente`
  - Persiste en `globalSettings.runMode`.
  - Debe reflejarse también en el combo del control principal (sync bidireccional).

### Sección “Agente”
- **Toggle IDE Integration**: `globalSettings.agent.ideIntegration`
- **Toggle Apply changes**: `globalSettings.agent.applyChanges`
- **Numeric Max steps**: `globalSettings.agent.maxSteps`

> Nota: si hoy el modal no tiene espacio, se agrega un “Advanced expander” manteniendo el mismo esquema de colores/estilos.

---

## Tareas (Sprint ≤ 5 tareas). Progreso acumulado se actualiza aquí.
### T1 — SettingsStore: schema extendido + defaults + migración suave (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/AgentSettingsStore.cs`

**Cambios:**
- Asegurar que existan 2 servers (lmstudio-local y jan-local) sin pisar configs existentes.
- Asegurar `globalSettings.runMode` con default `preguntar`.
- Asegurar `globalSettings.requestDefaults` y `globalSettings.agent` con defaults.
- Asegurar `activeServerId` válido (si falta o apunta a inexistente).
- Persistir solo si se inyectaron defaults (defensivo, sin throw).

**DoD:**
- `settings.json` nuevo se crea con ambos servers + global settings extendido.
- `settings.json` existente no pierde campos ni valores; solo se completan faltantes.

---

### T2 — Modal: provider + stream + includeUsage + runMode + agent (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`

**Cambios:**
- Agregar controls (Provider, Stream, includeUsage, RunMode, Agent flags) usando estilos existentes.
- OnLoad: bind/precarga desde settings.
- OnChange: actualizar UI (habilitar includeUsage solo en lmstudio) y si corresponde persistir.
- OnSave: persistir todo (server activo + global settings).

**DoD:**
- Cambiar Provider cambia `activeServerId` y recarga campos.
- Guardar persiste y reabrir mantiene valores.

---

### T3 — Control principal: sync combos (Provider y RunMode) con settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.Helpers.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- `ServerLLM_SelectionChanged`: set `activeServerId` (no mutar Provider) + Save + recompose + refrescar UI.
- `TypeActivitie_SelectionChanged`: set `globalSettings.runMode` + Save.
- OnInit/Loaded: hidratar combos desde settings con flag anti-recursión.

**DoD:**
- Cambiar en el control persiste; abrir modal refleja lo mismo (y viceversa).

---

### T4 — Runtime: provider-agnóstico (lmstudio/jan) + aplicar stream settings (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/AgentComposition.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

**Cambios:**
- Quitar hardcode “lmstudio”: provider `jan` no debe caer en mock.
- `POST {BaseUrl}/v1/chat/completions` para ambos.
- Stream on/off según settings (server override o global defaults).
- `stream_options.include_usage` solo si provider=lmstudio y flag true.
- Authorization Bearer si ApiKey no vacío.

**DoD:**
- LM Studio sigue funcionando como hoy.
- Jan ejecuta request real (sin mock) con su baseUrl.

---

### T5 — RunMode afecta payload + verificación + actualización plan (% y checklist) (0%)
**Archivos:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`
- (opcional) README/ReleaseNotes si entra en reglas del sprint

**Cambios:**
- `preguntar` vs `agente`: `agente` agrega system prompt (constante) y aplica flags `agent.*` solo a lógica interna (no tool-calling aún).
- Logging on-change de Provider/RunMode/Stream/ConfigStatus.

**DoD:**
- Alternar RunMode cambia el siguiente payload (observable por logs) y no rompe streaming.

---

## Tracking de avance (manual)
- Total = promedio de T1..T5.
- Al cerrar cada tarea: actualizar aquí el % y anotar commit SHA.

**Progreso actual:** 0%  
- T1: 0% (pendiente)  
- T2: 0% (pendiente)  
- T3: 0% (pendiente)  
- T4: 0% (pendiente)  
- T5: 0% (pendiente)  

---

## Checklist de pruebas (VS 2026 Experimental)
1) Provider LM Studio: ping OK → modelos poblados → Run streaming OK.
2) Provider Jan: ping OK/Degradación controlada → Run OK contra /v1/chat/completions con Bearer.
3) Stream off: respuesta completa (no SSE).
4) RunMode Agente: system prompt aplicado; sin tool-calling; no regressions en UI.

<!-- END plan_v2.2.md -->

---

## Fuente: plan_v2.3.md

<!-- BEGIN plan_v2.3.md -->

# Plan v2.3 — Provider + RunMode Sync (LM Studio + Jan)

**Branch:** `feature/provider-runmode-sync`  
**Objetivo:** que el usuario final pueda alternar **Provider** (LM Studio / Jan) y **RunMode** (Preguntar / Agente) y que el envío al LLM, el UI y `settings.json` queden **sincronizados**, **persistidos** y **sin romper** lo existente.

---

## Restricciones operativas
- Cambios mínimos en Presentation (VSIX / ToolWindows).
- No tocar archivos prohibidos del VSIX (manifest/vsct/csproj/sln/vsix, etc.).
- C# **7.3** (evitar top-level statements, static local functions, ref/unsafe en async, etc.).
- Definición de terminado: **Build OK + 0 errores + 0 warnings** en los archivos tocados (incl. analyzers VSTHRD) y smoke test manual.

---

## Estado actual (confirmado por discovery)
- En el control principal:
  - Provider UI: **ComboBox `ServerLLM`** con `SelectionChanged="ServerLLM_SelectionChanged"` pero **no hay implementación**.
  - RunMode UI: **ComboBox `TypeActivitie`** (Agente/Preguntar) **sin handler**.
  - Hidratación UI: constructor + `PopulateSettingsPanel` + `ComputeIsLlmConfigured` + `RefreshFromSettings`.
- Backends:
  - `AgentComposition` compone backend real **solo** si `srv.Provider == "lmstudio"`, caso contrario usa `MockAgentService`.
  - Chat streaming en UI usa endpoint `.../v1/chat/completions` (LM Studio).
- Persistencia:
  - `AgentSettingsStore` ya maneja `servers`, `activeServerId`, `globalSettings` y migraciones suaves.
  - Defaults extendidos agregados: `servers` incluye `lmstudio-local` y `jan-local`; `globalSettings.runMode`, `globalSettings.requestDefaults`, `globalSettings.agent`.

---

## Esquema JSON (target)
- `servers[]`: `{ id, name, provider, baseUrl, apiKey, model, isDefault, createdAt }`
- `activeServerId`: server activo real
- `globalSettings.runMode`: `"preguntar"` | `"agente"`
- `globalSettings.requestDefaults`: `{ stream: bool, includeUsage?: bool, temperature: number, maxTokens: number }`
- `globalSettings.agent`: `{ ideIntegration: bool, applyChanges: bool, maxSteps: int }`
- `taskProfiles`: reservado (perfiles de ejecución/plantillas por tipo de tarea; se mantiene sin romper aunque todavía no se use)

---

## Progreso / tareas
> Nota: los porcentajes se actualizan por commit.

### T1 — Defaults/migración en AgentSettingsStore (DONE 100%)
- **Commit:** `f7111e2`  
- Resultado: defaults extendidos + ensures (servers/globalSettings/activeServerId) + persistencia suave.

### T2 — Modal Config: bloque Advanced + wiring (DONE 100%)
- **Commit:** `f5d3bef`  
- Resultado: Expander Advanced + controles (Provider/RunMode/Stream/IncludeUsage/Agent flags/MaxSteps) + persistencia.

### T2.1 — Hardening C#7.3 + Analyzers (EN CURSO 0–80% según estado local)
**Motivo:** quedaron warnings de threading/analyzers; esto bloquea seguir.  
**Objetivo:** eliminar los warnings reportados:
- `VSTHRD100` / `VSTHRD101` en `AgenteIALocalControl.Helpers.cs`
- `VSTHRD110` / `VSTHRD001` en `AgenteIALocalControl.xaml.cs`
**Criterio de aceptación**
- Rebuild solución: **0 warnings** (al menos los 4 anteriores eliminados; ideal 0 total).
- Smoke: abrir ToolWindow, no cuelga, log refresh y UI funcionan.

### T3 — Control principal: Provider (ServerLLM) → settings + UI sync (PENDIENTE)
**Qué**
- Implementar `ServerLLM_SelectionChanged` en `AgenteIALocalControl.xaml.cs`.
- Traducir selección UI → `provider`:
  - `"LM Studio"` → `activeServerId = "lmstudio-local"`
  - `"JAN"` → `activeServerId = "jan-local"`
- Persistir en `AgentSettingsStore.Save(settings)` y disparar:
  - `RefreshFromSettings()` para re-hidratar combos, ConfigLabel y modelos.
  - `AgentComposition.RecomposeFromSettings("ui:provider-changed")`
**Criterio de aceptación**
- Cambiar Provider en footer:
  - actualiza `ConfigLabel` (OK/Not Config) de acuerdo a health.
  - el combo de modelos y estado reflejan el server activo.
  - persiste en `settings.json` y al reabrir VS se mantiene.

### T4 — Control principal: RunMode (TypeActivitie) → settings + comportamiento (PENDIENTE)
**Qué**
- Agregar `SelectionChanged` al XAML (o wiring en code-behind) para `TypeActivitie`.
- Persistir `globalSettings.runMode` (`"agente"`/`"preguntar"`).
- Al ejecutar:
  - si `"preguntar"`: usar flujo actual de chat/streaming (con provider-aware payload).
  - si `"agente"`: usar `AgentComposition.AgentService.RunAsync(...)` (flujo agente).
**Criterio de aceptación**
- Alternar RunMode en UI:
  - persiste y restaura al reabrir.
  - afecta el flujo de ejecución (se observa en logs + UX).

### T5 — Provider-aware request (LM Studio + Jan) para Preguntar (PENDIENTE)
**Qué**
- En el flujo de `"preguntar"`:
  - usar `settings.activeServerId` para elegir `BaseUrl/Model/ApiKey`.
  - respetar `globalSettings.requestDefaults.stream`.
  - si provider != lmstudio, **no enviar** `stream_options.include_usage` (ya hay toggle disabled en modal).
- Normalizar endpoints:
  - ping/model list: `/v1/models`
  - completions: `/v1/chat/completions`
  - **Validar** compatibilidad Jan (discovery web + prueba real).
**Criterio de aceptación**
- Con Provider=LM Studio y Provider=Jan:
  - ping OK llena modelos; ping FAIL limpia modelos y muestra Not Config.
  - enviar pregunta funciona (stream o no) sin errores.

### T6 — Agent provider (Jan) en composición (PENDIENTE, opcional según T4)
**Qué**
- Si RunMode = `"agente"` y Provider = `jan`:
  - componer un `JanAgentService` real o fallback controlado (con logs) sin romper LM Studio.
- Mantener SOLID: interfaz de servicio en Application, implementación en Infrastructure, composición en VSIX.
**Criterio de aceptación**
- RunMode=Agente con LM Studio sigue funcionando.
- RunMode=Agente con Jan: o funciona, o hace fallback explícito con mensaje/log consistente (sin crash).

---

## Riesgos / decisiones pendientes
- Confirmar por documentación y pruebas:
  - Jan expone API OpenAI-compatible (paths/streaming) y qué features soporta (`include_usage`, SSE, etc.).
- Definir si `includeUsage` debe ser global (requestDefaults) o por-provider (hoy es global; en Jan se ignora).

---

## Próximo paso recomendado (bloqueante)
**Ejecutar T2.1 (warnings) antes de seguir con T3/T4.**  
Una vez que pases el output de Copilot y el build output, fijamos exactamente los cambios mínimos para eliminar los 4 warnings restantes y cerramos con commit atómico.

<!-- END plan_v2.3.md -->

---

## Fuente: plan_v2.4.md

<!-- BEGIN plan_v2.4.md -->

# Plan v2.4 — Provider + RunMode Sync (LM Studio + Jan)

**Branch:** `feature/provider-runmode-sync`  
**Objetivo:** que el usuario final pueda alternar **Provider** (LM Studio / Jan) y **RunMode** (Preguntar / Agente) y que el envío al LLM, el UI y `settings.json` queden **sincronizados**, **persistidos** y **sin romper** lo existente.

## Restricciones operativas
- Cambios mínimos en Presentation (VSIX / ToolWindows).
- No tocar archivos prohibidos del VSIX (manifest/vsct/csproj/sln/vsix, etc.).
- C# **7.3**.
- Definición de terminado por tarea: **Build OK + 0 errores + 0 warnings** (incl. analyzers VSTHRD) en archivos tocados + smoke test manual.

## Estado actual
- Control principal:
  - Provider UI: **ComboBox `ServerLLM`** tiene `SelectionChanged="ServerLLM_SelectionChanged"` (pendiente implementar en code-behind).
  - RunMode UI: **ComboBox `TypeActivitie`** (Agente/Preguntar) sin handler (pendiente).
  - Hidratación UI: constructor + `PopulateSettingsPanel` + `ComputeIsLlmConfigured` + `RefreshFromSettings`.
- Backends:
  - `AgentComposition` compone backend real solo cuando `srv.Provider == "lmstudio"`; si no, usa `MockAgentService` (pendiente extender para Jan/Agente).
  - Preguntar (chat) usa `/v1/chat/completions` + streaming (LM Studio).
- Persistencia:
  - `AgentSettingsStore` maneja `servers`, `activeServerId`, `globalSettings` y migraciones suaves.
  - Defaults ya incluyen `lmstudio-local` + `jan-local` y `globalSettings.runMode/requestDefaults/agent`.

## Esquema JSON (target)
- `servers[]`: `{ id, name, provider, baseUrl, apiKey, model, isDefault, createdAt }`
- `activeServerId`: server activo real
- `globalSettings.runMode`: `"preguntar"` | `"agente"`
- `globalSettings.requestDefaults`: `{ stream: bool, includeUsage?: bool, temperature: number, maxTokens: number }`
- `globalSettings.agent`: `{ ideIntegration: bool, applyChanges: bool, maxSteps: int }`
- `taskProfiles`: reservado (perfiles/plantillas por tipo de tarea; se mantiene sin romper aunque todavía no se use)

## Progreso / tareas
> Nota: los porcentajes se actualizan por commit. Si una tarea está “READY TO COMMIT”, es que está aplicada localmente pero falta commit.

### T1 — Defaults/migración en AgentSettingsStore (DONE 100%)
- **Commit:** `f7111e2`  
- Resultado: defaults extendidos + ensures (servers/globalSettings/activeServerId) + persistencia suave.

### T2 — Modal Config: bloque Advanced + wiring (DONE 100%)
- **Commit:** `f5d3bef`  
- Resultado: Expander Advanced + controles (Provider/RunMode/Stream/IncludeUsage/Agent flags/MaxSteps) + persistencia.

### T2.1 — Hardening: eliminar warnings VSTHRD* (READY TO COMMIT 100%)
**Motivo:** analyzers bloquearon avance.  
**Alcance:** cambios defensivos mínimos para eliminar `async void`, `async lambdas` en void delegates, tareas sin observar, y reemplazar `Dispatcher.BeginInvoke` por `UiAsync + FireAndForget` donde el analyzer lo exige.  
**Archivos objetivo**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.Helpers.cs`
**Criterio de aceptación**
- Rebuild solución: **0 warnings VSTHRD001/110/100/101**.
- Smoke: ToolWindow abre, modal abre, ping BaseUrl sigue funcionando, autoscroll funciona.
**Commit sugerido**
- `chore(threading): remove VSTHRD* warnings (toolwindow)`

### T2.2 — UX: Placeholder “tipo payload” en PromptTextBox (PENDIENTE)
**Qué**
- Placeholder visible solo cuando **Text vacío** y **TextBox SIN foco**.
- Al hacer click/foco: placeholder se oculta aunque el TextBox siga vacío.
- Al perder foco con texto vacío: placeholder vuelve.
**Archivo**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml`
**Criterio de aceptación**
- TextBox vacío y sin foco => visible.
- Click/foco (vacío) => oculto.
- Escribir => oculto.
- Borrar (con foco) => oculto.
- Perder foco (vacío) => visible.
- Build OK.

### T3 — Control principal: Provider (ServerLLM) → settings + UI sync (PENDIENTE)
**Qué**
- Implementar `ServerLLM_SelectionChanged` en `AgenteIALocalControl.xaml.cs`.
- Mapear UI → `activeServerId`:
  - `"LM Studio"` → `lmstudio-local`
  - `"JAN"` → `jan-local`
- Persistir `AgentSettingsStore.Save(settings)` y disparar:
  - `RefreshFromSettings()`
  - `AgentComposition.RecomposeFromSettings("ui:provider-changed")`
**Criterio de aceptación**
- Cambiar Provider en footer actualiza `settings.json` (`activeServerId`) y se restaura al reabrir.
- UI (ConfigLabel/modelos) reacciona coherente al ping.

### T4 — Control principal: RunMode (TypeActivitie) → settings + comportamiento (PENDIENTE)
**Qué**
- Agregar `SelectionChanged` para `TypeActivitie` (XAML o wiring) y persistir `globalSettings.runMode`.
- Ejecutar:
  - `"preguntar"`: flujo chat/streaming provider-aware.
  - `"agente"`: usar `AgentComposition.AgentService.RunAsync(...)` (flujo agente).
**Criterio de aceptación**
- RunMode persiste, restaura, y cambia el flujo observable (logs + UX).

### T5 — Preguntar provider-aware (LM Studio + Jan) (PENDIENTE)
**Qué**
- Elegir server por `activeServerId` y respetar `globalSettings.requestDefaults`.
- Para `jan`: no enviar `stream_options.include_usage` (o feature-detect).
- Endpoints: ping `/v1/models`, chat `/v1/chat/completions`.
**Criterio de aceptación**
- Provider=LM Studio y Provider=Jan: ping llena/limpia modelos correctamente; preguntar funciona (stream o no).

### T6 — Agente con Provider=Jan (OPCIONAL / PENDIENTE)
**Qué**
- Componer servicio real para Jan en modo Agente o fallback explícito (sin crash) con logs consistentes.
- Mantener SOLID: interfaz en Application, implementación en Infrastructure, composición en VSIX.
**Criterio de aceptación**
- Agente con LM Studio no se rompe.
- Agente con Jan: funciona o fallback explícito con mensaje/log consistente.

## Próximo paso recomendado
1) Cerrar **T2.1** con commit atómico (si el rebuild ya está limpio).  
2) Aplicar **T2.2** (placeholder focus-aware) y commit.  
3) Recién después avanzar con **T3** y **T4**.

<!-- END plan_v2.4.md -->

---

# Plan real consolidado (estado actual)

Plan actualizado: 2026-01-16 (incluye cierre UX T7).

| Tarea | Alcance (DoD) | Evidencia actual | % |
|---|---|---|---:|
| T1 — Defaults/migración en AgentSettingsStore | Defaults extendidos: servers lmstudio+jan, globalSettings.runMode/requestDefaults/agent, activeServerId válido, preserva _raw | Commit f7111e2 aplicado | 100 |
| T2 — Modal Config: bloque Advanced + wiring | Expander Advanced + Provider/RunMode/Stream/IncludeUsage/Agent flags/MaxSteps con persistencia | Commit f5d3bef aplicado | 100 |
| T2.1 — Hardening analyzers/threading | Mitigar VSTHRD* con refactors mínimos, build OK sin warnings en archivos tocados | Cambios aplicados (según estado actual reportado: build OK) | 100 |
| T2.2 — UX placeholder “tipo payload” | Placeholder visible solo vacío+sin foco; oculto en foco aunque vacío | Validado por prueba manual (placeholder funciona) | 100 |
| T3 — Control principal: Provider sync (ServerLLM) | Persistir activeServerId, hidratar combo, recompose/refresh sin loops; UI de health coherente | Persistencia OK; queda inconsistencia BaseUrl/health (mensaje “no responde” aunque modelos cargan) | 85 |
| T4 — Control principal: RunMode sync (TypeActivitie) | Persistir globalSettings.runMode, hidratar combo, cambiar flujo Preguntar/Agente | No validado/pendiente en plan actual | 0 |
| T5 — Preguntar provider-aware (LM Studio + Jan) | Requests usan server activo; Authorization Bearer; includeUsage solo lmstudio; endpoints /v1/*; streaming compatible | Parcial: /v1/models mejorado (fallback host + auth), falta consolidar health+chat completions provider-aware | 35 |
| T6 — Agente con Provider=Jan (opcional) | Jan agent real o fallback explícito sin crash; mantiene SOLID | Pendiente | 0 |

> Nota operativa: la incidencia actual “Base URL / health en Jan y LM Studio” se trata como parte de T3/T5 (coherencia ping/modelos + normalización de URL + estado UI).

---

# Addendum UX — Pixel perfect (sin scroll) para paneles de Configuración

## Nuevo objetivo (prioridad UX)
Ajustar el **layout** (solo UI) para que sea **pixel perfect** respecto a las capturas adjuntas, sin alterar el comportamiento actual, manteniendo el esquema de colores/estilos existentes y **sin scroll**.

## Alcance
- Pantalla **Idioma** (NUEVA: actualmente no existe; crearla solo maquetada).
- Pantalla **LLM’s Local** (configuracion de proveedores locales).

## Decisiones confirmadas
- **Sin scroll**: el contenido debe entrar en la ventana sin ScrollViewer (por ahora).
- **Orden y jerarquia**: el orden de los controles debe ser el que se muestra en las capturas.
- **Espaciado**: margenes y espaciados **simetricos** y consistentes (mismo ritmo vertical/horizontal).
- **Footer fijo**: botones **Guardar/Cancelar** pegados abajo, sin empujar ni solapar el contenido.
- **Menu izquierdo**: ancho fijo como en la captura; contenido a la derecha alineado como en el mock.
- **Idioma (X en Spanish)**: solo maquetado (sin logica aun).
- **Styles**: reutilizar styles actuales (colores, tipografias, radios, sombras); la imagen es referencia solo de layout.

## Reglas
- No cambiar logica de negocio ni persistencia.
- No introducir scroll.
- Reutilizar styles/colores actuales (Dark + Material).

## DoD
- Layout identico al mock: alineaciones, anchos, espaciados, header, footer y jerarquia visual.
- Footer fijo abajo (Guardar/Cancelar) y contenido superior sin scroll.
- Menu izquierdo con ancho fijo consistente.
- Pantalla **Idioma** creada y navegable desde el menu (solo maquetado, sin funcionalidad).
- Build OK sin warnings nuevos.

## Archivos candidatos
- XAML de la ventana/panel de configuracion y/o UserControls que representen **Idioma** y **LLM’s Local**, y resources asociados si aplica.

## Criterio de aceptacion
- Comparacion visual lado a lado con el mock: layout coincide (pixel perfect) y no aparece scroll.
- No se rompe el funcionamiento actual (guardar/cancelar/selecciones existentes).
- La pantalla Idioma existe (solo layout) y puede abrirse desde el menu.

## Estado (addendum)

| Tarea | Alcance (DoD) | % |
|---|---|---:|
| T7 — UX Pixel perfect Configuracion (Idioma + LLM’s Local) | Maquetar LLM’s Local y crear pantalla Idioma (solo layout) con layout identico al mock, sin scroll, footer fijo, menu ancho fijo, sin cambios de logica | 100 |

### Evidencia (T7)
- Ventana de configuracion: header + sidebar + contenido + footer fijo sin scroll (pixel perfect segun mock).
- Sidebar: botones con icono + texto; seleccion exclusiva (comportamiento tipo RadioButton).
- ComboBoxes: eliminado overlay celeste (#BEE6FD) en zona de flecha/hover manteniendo comportamiento de desplegable.
- Window title/caption: incluye version VSIX (GetVsixVersionString) y sufijo 'Configuracion'.

> Nota: no se registran SHAs aqui porque deben tomarse del commit real al cerrar T7.

---

# Actualizacion 2026-01-16 — Sync bidireccional ToolWindow <-> ConfigWindow (Provider/RunMode/Model)

## Cambio implementado (resumen, sin eliminar contenido previo)

**Archivos modificados:**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`

**Miembros agregados (con marcadores):**
- `TrySelectComboByText` en ambos code-behind (ID: `20260116_180500`) para seleccionar items por texto/Content sin asignar `SelectedItem` a un `string` cuando el ComboBox contiene `ComboBoxItem`.
- `_lastActiveServerIdUi` (ID: `20260116_181200`) para detectar cambios reales de `activeServerId` y refrescar modelos solo cuando corresponde.
- `TypeActivitie_SelectionChanged` (ID: `20260116_181200`) para persistir `globalSettings.runMode` desde ToolWindow cuando el cambio es del usuario.

**Miembros modificados (alto nivel):**
- ToolWindow: `OnSettingsSaved`, `RefreshFromSettings`, `ServerLLM_SelectionChanged`, `ModelOfLLM_SelectionChanged`, `RefreshModelsForActiveServerAsync`.
- ConfigWindow: `LoadAdvancedControls`, `HandleLoadedAsync`, `ApplyServerToUi`.

**Build:** OK (0 errores, 0 warnings).

## Estado del problema (antes/despues)

- **Antes:** desincronizacion visible (Provider/Model/RunMode no coherentes entre ToolWindow y ConfigWindow), loops potenciales por hidratacion + handlers, y riesgo de requests repetidos a `/v1/models`.
- **Despues (objetivo de esta iteracion):**
  - Seleccion por texto para Provider/RunMode/Model en hidratacion.
  - Guards para evitar persistencia durante refresh programatico.
  - Refresco de modelos condicionado a cambio real de `activeServerId`.

> Nota: queda pendiente la validacion manual del comportamiento en VS 2026 Experimental (ver siguiente seccion). No se eliminaron secciones previas del plan; esta entrada se agrega como actualizacion.

## Progreso actualizado (estimacion basada en evidencia actual)

### Progreso core (T1..T5)
- T1: 100% (DONE)
- T2: 100% (DONE)
- T3: 90% (Sync provider/model entre ventanas: implementado; falta confirmar smoke sin loops en escenarios reales)
- T4: 60% (Sync/persistencia RunMode: implementado; **falta** que RunMode cambie el flujo/payload en runtime de forma verificable)
- T5: 35% (provider-aware en Preguntar: parcial; falta consolidar reglas finales de payload/flags segun provider y pruebas)

**Progreso core total (promedio T1..T5): 77%**

### Progreso extendido (incluye hardening/UX)
- T2.1: 100% (VSTHRD*/warnings)
- T2.2: 100% (placeholder)
- T7: 100% (pixel-perfect)

**Progreso extendido (T1, T2, T2.1, T2.2, T3, T4, T5, T7): 86%**

## Validacion manual pendiente (bloqueante para cerrar T3 y ajustar % real)

1) Abrir VS 2026 Experimental, abrir ToolWindow + ConfigWindow.
2) Cambiar Provider en ConfigWindow (p.ej. a JAN) y observar en <1s en ToolWindow:
   - Provider refleja JAN.
   - Model pertenece al server activo o se limpia.
   - Max 1 GET `/v1/models` por cambio real de provider.
3) Cambiar Provider en ToolWindow (a LM Studio) y observar en ConfigWindow:
   - Provider refleja LM Studio.
   - Campos (BaseUrl/Model/ApiKey) corresponden al server activo.
4) Cambiar RunMode (Agente/Preguntar) en cualquiera y verificar reflejo inmediato en el otro.
5) Ver logs: ausencia de loop/spam y ausencia de requests repetidos durante simple hidratacion.

## Proximo paso sugerido (una sola accion)

- Ejecutar la validacion manual anterior y, si aparece mismatch/loop, adjuntar 30-50 lineas del log alrededor del cambio + `activeServerId` y el objeto del server activo de `settings.json`.

---

# Actualizacion 2026-01-16 — Evidencia de traza routing RunMode en AgenteIALocal.log

## Contexto
- Se necesitaba auditar el dispatch por `globalSettings.runMode` en el MISMO log de archivo (`%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`).
- Se detecto que `AgentComposition.Info(...)` no siempre persistia en el archivo; la traza de routing se movio al pipeline `AppendLog(...)` (ToolWindow) dentro del punto central de dispatch.

## Cambio aplicado (resumen)
- Archivo: `src/AgenteIALocalVSIX/Execution/DefaultRunExecutor.cs`
- Metodo: `ExecuteSendAccordingToRunModeAsync`
- Accion: reemplazar el log directo por una unica llamada a `o.AppendLog("Routing send runMode=...")` (1 linea por envio), sin apiKey ni prompt completo.

## Evidencia (PASS)
- Se observaron lineas `Routing send runMode=preguntar ...` y `Routing send runMode=agente ...` en el archivo `AgenteIALocal.log`, exactamente 1 por envio.

## Impacto en el plan
- Se agrega criterio de aceptacion transversal (T4/T5): **por cada envio** debe existir **exactamente 1** linea grep-friendly con prefijo `Routing send runMode=` en `AgenteIALocal.log`.

## Ajuste de tracking (solo informativo; no elimina tracking previo)
- T4 (RunMode): +5% por evidencia de auditoria (traza en archivo).
- Resto de porcentajes: sin cambio hasta validar que RunMode modifica el flujo/payload (AgentService vs ChatStreaming).

# Actualizacion 2026-01-17 — Evidencia executionPath + RunMode dispatch (PASS)

## Contexto
- Se requirio confirmar en runtime que **RunMode** no solo se persiste/sincroniza, sino que **cambia el flujo de ejecucion**.

## Cambio aplicado (resumen)
- Archivo: `src/AgenteIALocalVSIX/Execution/DefaultRunExecutor.cs`
- Metodo: `ExecuteSendAccordingToRunModeAsync`
- Accion: la linea de routing se extendio para incluir `executionPath=...` y se mantuvo la regla **1 linea por envio**.

## Evidencia (PASS)
- Se validaron envios reales con:
  - `Routing send runMode=agente ... executionPath=AgentService`
  - `Routing send runMode=preguntar ... executionPath=ChatStreaming`

## Impacto en tracking
- T4 (RunMode): DONE a nivel de flujo (dispatch verificable en log de archivo).


# Actualizacion 2026-01-17 — Modo offline de Model cuando /v1/models falla (PASS)

## Problema
- Cuando el servidor local (JAN/LM Studio) esta detenido, la UI hacia `GET /v1/models`, fallaba, y terminaba dejando **Model vacio**, generando confusion (parecia falta de configuracion).

## Cambio aplicado (resumen)
- ConfigWindow: en fallos de `GET /v1/models`, se pasa una lista `offlineModels` con el **modelo persistido** del server activo (si existe) y se selecciona.
- ToolWindow: si la lista de modelos resultante es vacia, se preserva/rehidrata `srv.Model` persistido como unico item.
- `ConfigLabel` se mantiene basado en **config completeness** (BaseUrl+Model) y no en health del endpoint.

**Archivos tocados (nivel plan):**
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalConfigWindow.xaml.cs`
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs`

## Evidencia (PASS)
- Con JAN detenido: el modal muestra `Servidor no responde (/v1/models)` pero el campo **Model** muestra el valor persistido.
- En ToolWindow el footer mantiene el **Model** persistido y no queda en blanco.

## Impacto en tracking
- T3 (sync UI + modelos): cerrado en la parte de UX/rehidratacion offline.


# Progreso actualizado (estado vigente 2026-01-17)

> Nota: las tablas de progreso anteriores se conservan como historico. Esta seccion es la referencia vigente.

## Progreso core (T1..T5)
| Tarea | % | Estado | Nota breve |
|---|---:|---|---|
| T1 — Defaults/migracion AgentSettingsStore | 100 | DONE | Defaults extendidos + migracion suave |
| T2 — Modal Config Advanced + wiring | 100 | DONE | Provider/RunMode persistidos; Stream fijo (siempre ON); IncludeUsage/Agent flags persistidos |
| T3 — Control principal: sync Provider/Model/RunMode + no-loops | 100 | DONE | Sync bidireccional + rehidratacion robusta + modo offline Model |
| T4 — Runtime: RunMode cambia flujo (Preguntar/Agente) | 100 | DONE | Dispatch verificado por log con executionPath |
| T5 — Preguntar provider-aware (LM Studio + Jan) | 45 | EN CURSO | Falta aplicar requestDefaults (temperature/maxTokens) + includeUsage solo LM Studio; Stream fijo (siempre ON) |

**Progreso core total (promedio T1..T5): 89%**

## Progreso extendido (incluye hardening/UX)
| Item | % | Estado |
|---|---:|---|
| T2.1 — Hardening analyzers/threading | 100 | DONE |
| T2.2 — Placeholder PromptTextBox | 100 | DONE |
| T7 — UX Pixel perfect Configuracion | 100 | DONE |

**Progreso extendido (T1,T2,T2.1,T2.2,T3,T4,T5,T7): 93%**


# Proxima tarea propuesta (T5.1) — OBSOLETA (no-stream descartado)

## Objetivo
- En modo `preguntar`, el request a `/v1/chat/completions` debe respetar:
  - `globalSettings.requestDefaults.stream` (true/false)
  - `globalSettings.requestDefaults.temperature`
  - `globalSettings.requestDefaults.maxTokens` (si 0 o faltante, no enviar)
  - `includeUsage` **solo** si Provider=`lmstudio` y el toggle esta habilitado (no enviar a JAN)

## DoD
- Con `stream=false`: no se usa SSE; se parsea respuesta no-streaming y se renderiza de una vez.
- Con `stream=true`: streaming funciona como hoy.
- Para JAN: no se envia `stream_options.include_usage`.
- Build OK (0 errores, 0 warnings).

## Criterio de aceptacion manual
1) LM Studio: stream=true → streaming OK; stream=false → respuesta completa OK.
2) JAN: stream=true (si soporta) o degradacion controlada; stream=false → respuesta completa OK.
3) Log: 1 linea `Routing send ... executionPath=...` por envio (sin spam adicional).


---

# Proxima tarea propuesta (T5.1-R) — Aplicar requestDefaults en Preguntar (stream-only) + Stream fijo en UI

## Alcance
- En modo `preguntar`, el request a `/v1/chat/completions` debe respetar:
  - `globalSettings.requestDefaults.temperature`
  - `globalSettings.requestDefaults.maxTokens` (si 0 o faltante, NO enviar)
  - `globalSettings.requestDefaults.streamOptions.includeUsage` **solo** si Provider=`lmstudio` (NO enviar a JAN)
- `globalSettings.requestDefaults.stream` pasa a ser **opcion unica**: siempre `true`.
  - UI: el checkbox Stream queda **siempre marcado** y **deshabilitado** (no editable).
  - Persistencia: al guardar, se fuerza `requestDefaults.stream=true`.

## DoD
- Streaming SSE funciona como hoy (sin path no-stream).
- Payload incluye `temperature` y `max_tokens` segun defaults.
- `stream_options.include_usage` se envia solo a LM Studio cuando el flag esta habilitado.
- En JAN nunca se envia `stream_options`.
- Build OK (0 errores, 0 warnings).

## Criterio de aceptacion manual
1) Con LM Studio levantado: streaming OK; `includeUsage=true` agrega usage (si LM lo soporta) sin romper.
2) Con JAN levantado: streaming OK; nunca se envia `stream_options`.
3) En ConfigWindow: Stream aparece como unica opcion (checked + disabled) y al guardar queda `requestDefaults.stream=true`.
4) Log: se mantiene 1 linea `Routing send ... executionPath=...` por envio.
