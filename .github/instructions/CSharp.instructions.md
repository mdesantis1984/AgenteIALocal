---
description: "Convenciones C# para Agente IA Local (VSIX/WPF/MVVM)."
applyTo: "**/*.cs"
---

## C# (VSIX / WPF / MVVM)

- Mantener separación: UI (WPF) ≠ Orquestación (Application) ≠ Contratos (Core) ≠ Adaptadores (Infrastructure).
- Evitar lógica de negocio en code-behind. Preferir ViewModel + comandos + binding.
- Async:
  - No bloquear el UI thread.
  - Usar `CancellationToken` cuando aplique.
  - En capas no-UI, preferir `ConfigureAwait(false)` si el contexto no es requerido.
- VSIX threading: usar `JoinableTaskFactory`/`ThreadHelper` según VS SDK.
- Logging: no usar `Console.*`; usar el logger del proyecto (Serilog/abstracción existente).
- Cada nueva clase/método/propiedad importante debe incluir marcador `// NUEVO ... - ID: ...`.
