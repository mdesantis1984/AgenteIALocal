# Reglas de trabajo — Agente IA Local (VSIX)

## Principios generales
- Flujo paso a paso con confirmación entre acciones.
- Nunca ejecutar varias tareas en paralelo.
- Al cerrar una fase, crear nueva rama para la siguiente iteración.
- Metodología Scrum con sprints cortos.
- Commits pequeños y atómicos entre tareas.
- Una instrucción a la vez.

## Roles
- Humano: decisiones finales, ejecución y validación.
- Arquitecto (ChatGPT): diseño, planificación y control técnico.
- Copilot: único agente autorizado a modificar código bajo instrucciones exactas.

## Arquitectura
- SOLID obligatorio.
- Clean Architecture / Onion Architecture.
- Microservicios solo cuando aplique.
- HttpClientFactory obligatorio.
- Logging obligatorio con Serilog.
- Sin contenedor DI: composición manual.
- No romper el baseline estable `vsix-stable-baseline`.

## VSIX
- VSIX clásico.
- No modificar archivos críticos:
  - *.vsix
  - *.vsct
  - *.vsixmanifest
  - *.csproj
  - *.sln
- ToolWindow + comandos bajo Tools.
- Tema Dark alineado a Visual Studio / Material Design.
- Foco en UX, packaging y agentes IA locales.

## Git y documentación
- Baseline estable obligatorio.
- Documentar cada iteración:
  - README.md (índice)
  - README.es.md
  - README.en.md
  - README.architecture.md
- Uso de prompts .md como artefactos formales.
- No avanzar sin commit previo.

## Logging y calidad
- Logging obligatorio en cada flujo.
- No silenciar excepciones.
- Código limpio, legible y mantenible.
