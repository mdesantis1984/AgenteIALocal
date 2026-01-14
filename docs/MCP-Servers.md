# MCP Servers en Visual Studio — criterio rápido para este repositorio

## Qué aporta MCP

MCP permite que el modo Agente use herramientas expuestas por servidores externos (por ejemplo, GitHub, bases de datos, conversión de documentos, etc.). Se configuran por `mcp.json` y se habilitan en el menú de herramientas del chat.

## Recomendación (pragmática)

- **No es necesario** agregar MCP para el flujo normal de refactor/bugfix dentro de la solución, si Copilot ya puede:
  - usar `@workspace`, `#...` y `#output`
  - editar archivos y iterar sobre errores en el IDE
- **Sí conviene** MCP cuando el agente necesita capacidades externas repetibles (por ejemplo):
  - Operar contra GitHub (issues/PRs) mediante servidor MCP de GitHub.
  - Analizar vulnerabilidades de paquetes con servidores MCP específicos.
  - Convertir documentos a Markdown para usarlos como contexto (ej. MarkItDown).

## Dónde se configura

Ubicaciones típicas:
- `<SOLUTIONDIR>\.mcp.json` (versionable en repo)
- `<SOLUTIONDIR>\.vs\mcp.json` (solo para tu usuario/solución)
- `%USERPROFILE%\.mcp.json` (global)
