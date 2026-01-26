# PLAN CONSOLIDADO — AgenteIALocalVSIX (Provider/RunMode + Serilog)

- Rama: `feature/provider-runmode-sync`
- Versión: **2.4-serilog.4**
- Fecha: **2026-01-20**

## Alcance
- Completar requestDefaults + agent end-to-end (persistencia + payload).
- Migrar logging a **Serilog** (nuevo proyecto `AgenteIALocal.Logging` netstandard2.0) y **eliminar** la estructura actual de logging para evitar residuos.
- Mantener UI estable (cambios de UI al final), C# 7.3.

## Fases
### Fase 1 — Persistencia DTO completo + requestDefaults/agent end-to-end
- Guardar **todo el DTO** de `settings.json` (round-trip; preservar propiedades desconocidas) con **1 Load + 1 Save**.
- Modal: persistencia real **live + Save** de:
  - `globalSettings.runMode`
  - `globalSettings.requestDefaults.stream` (forzado `true`)
  - `globalSettings.requestDefaults.temperature`
  - `globalSettings.requestDefaults.maxTokens`
  - `globalSettings.requestDefaults.streamOptions.includeUsage` (solo `provider=lmstudio`; si no, `false`)
  - `globalSettings.requestDefaults.topP`, `stop`, `presencePenalty`, `frequencyPenalty` (comunes LM Studio + Jan; solo enviar si no-default/no vacío)
  - `globalSettings.agent.ideIntegration`, `applyChanges`, `maxSteps`
- Runtime:
  - Preguntar (streaming): mapear `temperature`, `max_tokens` (solo si >0), `top_p`, `stop`, `presence_penalty`, `frequency_penalty` (solo si no-default/no vacío), y `stream_options.include_usage` solo LM Studio.
  - Agente (non-stream): aplicar `temperature/max_tokens/top_p/stop/presence_penalty/frequency_penalty` (solo si no-default/no vacío) en el client.
  - Agente prompt: `ideIntegration=false` no inyecta contexto IDE; `applyChanges/maxSteps` viajan como instrucciones.

### Fase 2 — Serilog (pipeline único) + UI Log (250) + rolling (3MB) + niveles por settings
- Migrar logging a Serilog usando el proyecto ya agregado:
  - `src/AgenteIALocal.Logging/AgenteIALocal.Logging.csproj` (netstandard2.0) con paquetes: Serilog 4.3.0, Serilog.Sinks.Async 2.1.0, Serilog.Sinks.File 7.0.0.
- Requisitos de logging:
  - Un único API público para loggear “desde cualquier parte”.
  - Archivo con rolling automático a **3MB**.
  - Panel Log con formato **usuario final** (solo panel) y truncado a **últimos 250**.
  - Preparar `settings.json` para habilitar/deshabilitar niveles:
    - `globalSettings.logging.enabled` (bool)
    - `globalSettings.logging.all` (`on`|`off` opcional)
    - `globalSettings.logging.levels.verbose|debug|info|warning|error|critical` (bool)
  - Sin spam: no logs por chunk/retry/keypress.

### Fase 3 — Limpieza (delete) + Reglas + .github + smoke tests
- Eliminar estructura actual de logging (sin dejar “rasgos/basura”).
- **Legacy a eliminar (referencias + archivos):**
  - `src/AgenteIALocal.Core/Logging/IAgentLoggerV2.cs`
  - `src/AgenteIALocal.Core/Logging/ILogSink.cs`
  - `src/AgenteIALocal.Core/Logging/LogEntryTextFormatter.cs`
  - `src/AgenteIALocal.Infrastructure/LoggingV2/AgentLoggerV2.cs`
  - `src/AgenteIALocal.Infrastructure/LoggingV2/*` (CompositeLogSink, FileLogSink, NullLogSink, LogEventHub, UiLogBuffer, RollingFileWriter, VsixFileLogSink, formatter duplicado)
  - `src/AgenteIALocalVSIX/LoggingV2/VsActivityLogSink.cs`
  - `src/AgenteIALocalVSIX/AgentComposition.cs` (reemplazar por fachada Serilog en Logging)
  - Cualquier `AppendLog()`/`AppendLogFileLine()`/lectura directa del archivo para poblar el panel (migrar a sink UI)
- Nota: se elimina solo cuando toda la solución compila y corre con Serilog (tarea C1).
- Actualizar `Reglas.IA.md`: Copilot debe usar el logging predeterminado (nuevo API Serilog) y prohibir logs directos a UI/archivo.
- Agregar `.github/copilot-instructions.md` reforzando lo anterior.
- Smoke tests LM Studio + Jan.

## Tabla de progreso (por tarea)

| ID | Fase | Tarea | % | Estado |
|---:|:---:|---|---:|---|
| A1 | 1 | Persistencia: Save round-trip del DTO completo (1 Load + 1 Save; preserva unknown fields) | 100% | Completada |
| A1.1 | 1 | Hotfix: NormalizeBaseUri tolerante (evita host "http" por doble esquema al refrescar modelos) | 100% | Completada |
| A1.2 | 1 | Hotfix: NormalizeBaseUri repara forma canonicalizada (http://http//...) y evita host "http" en GET modelos | 100% | Completada |
| A1.3 | 1 | Hotfix: BaseUrl robusto (sanitiza doble esquema + endpoints v1) para ModelsFetch y LM Studio | 100% | Completada |
| A2 | 1 | Modal: requestDefaults temperature/maxTokens/includeUsage (live + Save) + parse robusto (coma/punto; int>=0) | 0% | Pendiente |
| A3 | 1 | Modal: requestDefaults comunes (topP/stop/presencePenalty/frequencyPenalty) live + Save (sin romper; no enviar si default/vacío) | 0% | Pendiente |
| A4 | 1 | Modal: agent ideIntegration/applyChanges/maxSteps live + Save (DTO completo) | 0% | Pendiente |
| A5 | 1 | Fix: PersistBaseUrlIfChanged persiste solo BaseUrl (sin tocar globals) | 0% | Pendiente |
| A6 | 1 | Runtime Preguntar: payload streaming aplica requestDefaults (temp/max_tokens + comunes) + stream_options solo LM Studio | 0% | Pendiente |
| A7 | 1 | Runtime Agente: AgentRequest + clients aplican requestDefaults (temp/max_tokens + comunes) y AgentConfig en prompt | 0% | Pendiente |
| A8 | 1 | Routing log: 1 línea por envío incluye provider/runMode + requestDefaults + agent flags | 0% | Pendiente |
| B1 | 2 | Nuevo API logging global en `AgenteIALocal.Logging`: `Log.Configure(...)` + `Log.V/D/I/W/E/C(...)` | 0% | Pendiente |
| B2 | 2 | Serilog File sink: rolling 3MB + naming estable + Async sink (si aplica) | 0% | Pendiente |
| B3 | 2 | Serilog UI sink: buffer 250 + formato usuario final (solo panel) | 0% | Pendiente |
| B4 | 2 | Config niveles por `settings.json`: enabled/all/levels.* (sin UI nueva por ahora) | 0% | Pendiente |
| B5 | 2 | Migrar llamadas: reemplazar AgentComposition/LoggerV2/AppendLog por nuevo API Serilog | 0% | Pendiente |
| B6 | 2 | Asegurar “no spam”: eliminar logs dentro de retry/chunk/keypress; mantener trazabilidad 1 línea por evento relevante | 0% | Pendiente |
| C1 | 3 | Eliminar estructura de logging actual (archivos y referencias) sin romper build | 0% | Pendiente |
| C2 | 3 | Reglas: actualizar `Reglas.IA.md` (uso obligatorio nuevo logging; prohibir logs directos) | 0% | Pendiente |
| C3 | 3 | `.github/copilot-instructions.md`: enforcement del nuevo logging y checklist (niveles/no-spam/1-línea) | 0% | Pendiente |
| C4 | 3 | Smoke tests: LM Studio + Jan (preguntar/agente) + verificación persistencia + verificación logs (panel 250 + rolling 3MB) | 0% | Pendiente |

## Criterios de aceptación (global)
- Persistencia: cambios en modal se guardan (live + Save) y **no se pierden**; `settings.json` preserva propiedades desconocidas.
- Payloads: Preguntar/Agente aplican requestDefaults (incluye comunes) con compatibilidad por provider; Jan nunca recibe `stream_options`.
- Logging Serilog: se puede loggear desde cualquier proyecto usando el nuevo API; archivo rota a 3MB; panel muestra últimas 250 líneas (formato usuario final).
- Limpieza: la estructura anterior de logging queda eliminada (sin rutas paralelas ni clases huérfanas).

