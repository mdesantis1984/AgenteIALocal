# Paquete de prompts e instrucciones (Copilot)

Incluye:
- `.github/copilot-instructions.md`: reglas globales del repo
- `.github/instructions/*.instructions.md`: reglas por tipo de archivo
- `.github/prompts/*.prompt.md`: prompts reutilizables (JSON)
- `docs/`: guías rápidas sobre contexto, referencias, y MCP

Uso recomendado:
1) Habilitar *Custom instructions* en Visual Studio.
2) En chat, usar `@workspace` + referencias `#...`.
3) Para prompts reutilizables: escribir `#prompt:` y elegir el `.prompt.md`.
