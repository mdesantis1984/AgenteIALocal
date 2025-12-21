# Agente IA Local para Visual Studio

Agente IA Local es una extensión clásica de Visual Studio (VSIX, formato clásico) que provee una ToolWindow y un conjunto de comandos para experimentar con flujos de agente de IA sobre el contexto del IDE (solución, proyectos, documentos). El objetivo del proyecto es validar una arquitectura desacoplada entre UI, orquestación y proveedores de IA, facilitando pruebas, trazabilidad y despliegue como extensión instalada en la Experimental Instance de Visual Studio.

---

## 1. Introducción

Esta extensión ofrece un punto de entrada dentro de Visual Studio (menú `Tools → Agente IA Local`) y una ToolWindow que permite generar prompts, ejecutar el agente (mock o real) y visualizar las respuestas. Está diseñada como un prototipo evolutivo (MVP → hardening) para facilitar la integración posterior con proveedores LLM.

El enfoque técnico es explícitamente del tipo "VSIX clásico" para mantener compatibilidad y control sobre el empaquetado, registro de comandos (VSCT) y ToolWindows.

---

## 2. Metodología de trabajo

Trabajamos siguiendo prácticas ágiles (Scrum) con iteraciones cortas y entregables concretos. Algunas reglas de trabajo adoptadas:

- Scrum por iteración: sprints cortos con objetivos claros por rama.
- Commits atómicos: cada cambio funcional o de corrección debe mapearse a un commit pequeño y con mensaje significativo.
- Definition of Done incluye: código compilable, tests mínimos (cuando aplican), documentación asociada y prompts/artifacts actualizados.
- Documentación como parte del DoD: los prompts en formato `.md` (ver sección "Uso de prompts") son artefactos formales y deben quedar versionados en la rama de la tarea.
- Ramas por iteración y por task: p. ej. `iter-002/task-01-options-access`.

---

## 3. Roles

Se definen roles claros en el flujo de trabajo:

- Humano (desarrollador/maintainer): toma decisiones finales, revisa y aprueba cambios, ejecuta pruebas en Experimental Instance y realiza merges a ramas principales.
- IA (ChatGPT): rol de arquitecto y planificador. Se usa para analizar problemas, generar prompts técnicos y proponer correcciones y planes de trabajo. No edita directamente el código en el repositorio; su output se valida por un humano.
- Copilot (VS/editor assistant): único agente automatizado autorizado a aplicar cambios en el workspace (ediciones puntuales solicitadas). Se usa para ejecutar los cambios mínimos aprobados por el humano y seguir instrucciones de implementación exactas.

Esta separación asegura responsabilidad y trazabilidad entre propuesta (IA), ejecución (Copilot) y verificación/aprobación (humano).

---

## 4. Iteraciones (cronológico)

A continuación se resumen las iteraciones principales y los hitos logrados hasta la fecha.

### Iteración: post-mvp-readme-and-hardening

- Problemas iniciales reales:
  - Mismatch entre GUIDs en el VSCT y el `Package` que impedía el cableado correcto de menús/commands.
  - Logging inicial insuficiente para diagnóstico en runtime.
  - Comportamiento frágil en la ToolWindow cuando el backend (AgentService) no estaba compuesto.

- Fixes aplicados:
  - Corrección del wiring VSCT ↔ Package: se alinearon GUIDs y CommandId en VSCT y código.
  - Hardened `InitializeAsync` del `AsyncPackage` con logging explícito y manejo de errores sin relanzar (fail-safe).
  - Registro consistente del comando mediante `OleMenuCommandService` siguiendo el patrón funcional de ejemplo (patrón MenuCommand/Instance/InitializeAsync).
  - Mejoras en la ToolWindow: controles WPF ajustados para legibilidad (uso de SystemColors) y manejo seguro de flujo UX cuando `AgentService` es null.
  - Logging a archivo habilitado y ampliado (trazas de inicialización, registro de comandos y ejecución de botones).

- Resultado:
  - ToolWindow operativa desde el menú `Tools` (apertura verificada en Experimental Instance).
  - Menú y comando correctamente registrados y ejecutables.
  - Logs verificables en `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.

- Commits y orden aplicado:
  - Cambios minimos, agrupados por objetivo: (1) logging/package init, (2) comando registro, (3) VSCT fix, (4) ToolWindow UX hardening.

### Iteración: iter-002 (inicio)

- Rama creada: `iter-002/task-01-options-access`.
- Objetivo del sprint:
  - Exponer y persistir opciones desde `Tools → Options` (Options Page) y permitir que `AgentService` lea configuración al inicializar.
  - Mejorar documentación y añadir prompts como artefactos.
- Estado actual:
  - Options Page funcional y persistente (configuración guardada/reutilizable).
  - Trabajo de documentación y prompts en curso (README y prompts `.md` actualizados como parte del DoD).

---

## 5. Arquitectura (alto nivel)

Referencia: ver `README.architecture` (archivo dedicado en el repositorio) para diagrama y detalles.

Resumen mínimo:

- `AgenteIALocal.Core` — contratos, DTOs e interfaces (neutralidad entre capas).
- `AgenteIALocal.Application` — orquestación de flujos y lógica de negocio (implementaciones sin dependencia directa de UI).
- `AgenteIALocal.Infrastructure` — adaptadores concretos (clientes HTTP, resolvers, integraciones específicas).
- `AgenteIALocal.UI` — controles WPF reutilizables.
- `AgenteIALocalVSIX` — host VSIX clásico: `AsyncPackage`, comandos, ToolWindow, VSCT y empaquetado.

La arquitectura promueve la inyección/composición del `AgentService` por el Package, de modo que la UI sólo consuma la interfaz y no dependa de implementaciones concretas.

---

## 6. Uso de prompts

Los prompts `.md` se usan como artefactos formales en el proceso de desarrollo. Motivos y prácticas:

- Por qué: permiten definir de forma reproducible las instrucciones que se le dan a la IA (ChatGPT) para diseño, diagnóstico y generación de cambios.
- Qué contienen: descripción de la tarea, contexto de workspace, pasos a ejecutar, restricciones y criterios de aceptación.
- Cómo se integran al flujo: cada task/issue relevante incluye uno o más prompts versionados en la rama de trabajo; los prompts son parte del DoD y se adjuntan al PR como evidencia de decisión y ejecución.

Ejemplos de uso:
- Diagnóstico de wiring VSCT ↔ Package.
- Plan de hardening para `InitializeAsync` del Package.
- Guía de UX para manejo de errores en la ToolWindow.

---

## 7. Estado actual del proyecto

Resumen honesto y verificable al momento de este documento:

Qué funciona
- Compilación: `msbuild` del solution y del proyecto VSIX compila correctamente (target .NET Framework 4.7.2 para el VSIX).
- VSIX: paquete generable e instalable en Experimental Instance.
- Menú y comando: `Tools → Agente IA Local` aparece y el comando está registrado correctamente (corriente de ejecución del comando verificada).
- ToolWindow: abre y muestra controles; UX mejorada para legibilidad y manejo de errores.
- Options Page: configuraciones persistentes disponibles y accesibles desde `Tools → Options`.
- Logging: trazas escritas a archivo en `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log` con entradas de inicialización, registro de comandos y ejecución de acciones.
- Backend en código: proyectos `Core`, `Application` e `Infrastructure` presentes (estructura y mocks para validación de flujo).

Qué está en curso
- Integración final de proveedores LLM (composición real del `AgentService` con LM Studio o JanServer según configuración).
- Endurecimiento adicional sobre threading y patterns VSTHRD para eliminar warnings residuales.
- Tests de integración sobre el flujo VSIX en Experimental Instance (automatización parcial en roadmap).

Qué NO está hecho todavía
- Soporte final y validado para un proveedor LLM en producción (aun hay implementaciones/adapteres pero falta validación end-to-end con proveedor real y seguridad de credenciales).
- Release packaging formal (firma, versionado para marketplace) — pendiente del proceso de Release y QA.

---

## Documentación y trazabilidad

- Cada tarea relevante incluye prompts `.md` y cambios minimos aplicados vía Copilot; los commits referencian la rama y task.
- Este README es el documento base en `src/README.es.md` y debe actualizarse por cada iteración significativa.

---

Si falta detalle operativo (por ejemplo, scripts de build, instrucciones de instalación por versión de Visual Studio o diagramas de arquitectura adicionales), esos artefactos se deben añadir como PRs específicos y quedar referenciados desde `README.architecture` o desde la carpeta `docs/`.

Gracias: este README refleja el estado actual y las decisiones recientes (hardening, wiring y logging) al momento de su última actualización.

---

### Sprint 2 — UI / UX, Observabilidad y Diagnóstico

Alcance del sprint

- Mejora de la ToolWindow (UX y navegación).
- Acceso directo a Options desde la UI (botón y Page existente).
- Mensajería de estado clara para el usuario: estado "configurado", "incompleto" y "backend no disponible".

Observabilidad

- Implementación del Tab "Log" con capacidades mínimas:
  - Visualización completa del log en un TextBox de solo lectura.
  - Botón "Copiar" para copiar todo el contenido al portapapeles.
  - Botón "Borrar" para eliminar el archivo de log del disco y limpiar la vista.
  - Información visible de ruta absoluta y tamaño del archivo (KB/MB).

Estado del backend (diagnóstico)

- Diagnóstico confirmado durante pruebas en la Experimental Instance:
  - La assembly `AgenteIALocal.Infrastructure` no se cargaba en runtime dentro del VSIX.
  - Como resultado, la composición del backend no se completó y `AgentService` quedó en `null`.

Decisiones y alcance pendiente

- El trabajo de backend (composición completa del `AgentService`) y el empaquetado/fine-tuning del VSIX se trasladan a Sprint 3 como deuda técnica.
- El nuevo Tab "Config" queda en estado experimental: permite editar y persistir opciones, pero su integración y pruebas finales quedan en Sprint 3.

Resultado

- Sprint 2 cerrado con entrega de mejoras UI/UX en la ToolWindow y diagnóstico reproducible de observabilidad.
- Las acciones de corrección del backend están planificadas para Sprint 3 con prioridad para estabilizar la extensión en Experimental Instance.

Documentación relacionada

- `src/README.es.md` (este documento)
- `src/README.en.md`
- `src/README.architecture.md`

---

### Sprint 2.5 — UX Foundations (Closed)

Objetivo

- Principal: Consolidar los fundamentos UX orientados a Visual Studio para la ToolWindow y la experiencia del agente, asegurando estados claros y componentes de navegación y observabilidad listos para la siguiente iteración.

Checklist (estado)

- [DONE] Definición de principios UX para VSIX (no bloqueante, integrado al IDE)
- [DONE] Diseño del layout base de la ToolWindow (zonas: input, contexto, acciones, salida)
- [DONE] Estados de experiencia definidos (Idle, Running, Success, Error)
- [DONE] Convenciones visuales de Visual Studio aplicadas (iconografía, espaciado, foco)
- [DONE] Validación de flujos reales (lectura de archivo, ejecución de agente en modo mock)

Notas

- Esta sección se añade como cierre formal de Sprint 2.5 — UX Foundations. No se han eliminado ni reescrito contenidos previos; se documenta el estado cerrado y los entregables mínimos verificados. Las tareas de integración backend y pruebas finales de empaquetado quedan planificadas en Sprint 3 como deuda técnica.

---
