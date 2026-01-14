---
description: "Convenciones WPF/XAML para ToolWindow (UX Visual Studio-first, dark)."
applyTo: "**/*.xaml"
---

## WPF/XAML (ToolWindow)

- Respetar el documento UX canónico del repo (`Readme.UX.md`) cuando modifiques layout/estilos.
- Preferir recursos/estilos existentes; evitar colores hardcodeados salvo que el diseño ya los use.
- Bindings:
  - Usar `INotifyPropertyChanged` correctamente (propiedades de estado deben notificar).
  - Evitar converters innecesarios si se puede resolver con triggers/bindings directos.
- UX:
  - Feedback inmediato de estado (Idle/Running/Success/Error).
  - No usar UI bloqueante (evitar operaciones pesadas en UI thread).
- Accesibilidad básica: foco/teclado en botones y entradas principales.
