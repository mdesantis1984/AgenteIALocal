---
description: "Reglas globales del repo (Agente IA Local): arquitectura, restricciones y formato de respuesta."
applyTo: "**/*"
---

## Reglas globales (aplican a todo)

- Mantener SOLID + Clean Architecture + Onion. Las dependencias apuntan **hacia adentro**.
- No tocar archivos críticos del VSIX: `*.vsix`, `*.vsct`, `*.vsixmanifest`, `*.csproj`, `*.sln`.
- No introducir scripts ni automatizaciones externas.
- Trabajar por pasos y pedir confirmación humana antes del siguiente paso.
- Responder siempre en JSON y listar cambios (creados/modificados/eliminados).
- Usar referencias explícitas en chat (`#...`, `@workspace`, `#output`) cuando el contexto sea relevante.
