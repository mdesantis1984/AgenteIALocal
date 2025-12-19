Agente IA Local para Visual Studio

---

Descripción general del proyecto

Qué es

Agente IA Local es una extensión clásica de Visual Studio (VSIX, no SDK-style) que aloja capacidades de agente de IA sobre el contexto real del IDE (solución, proyectos y documentos). El objetivo es validar una arquitectura desacoplada entre UI, orquestación y proveedor de IA, con un MVP que demuestre empaquetado VSIX, registro de comandos y una ToolWindow base.

Por qué VSIX clásico

Se eligió VSIX clásico para mantener compatibilidad con APIs históricas del Visual Studio SDK y controlar explícitamente el empaquetado (csproj clásico, VSCT compilado a recurso) y el registro de menús/ToolWindows.

Estado actual del proyecto

Qué funciona hoy

- Build: OK con MSBuild.
- VSIX: generado en `src/AgenteIALocalVSIX/bin/Debug/AgenteIALocalVSIX.vsix` e instalable en la Experimental Instance.
- Menú: el comando aparece en `Tools` cuando la extensión está instalada.

Qué no funciona hoy / incompleto

- ToolWindow: el click en el menú no abre la ToolWindow en el entorno actual (limitación observada). Debe verificarse el registro de ToolWindow en el Package.

Lo que se ve en Visual Studio

- Menú `Tools` con la entrada "Agente IA Local".
- No se visualiza la ToolWindow al ejecutar el comando (estado actual).

Estructura de la solución

- `AgenteIALocal.Core` — Contratos y modelos neutrales (DTOs y interfaces).
- `AgenteIALocal.Application` — Orquestación de pipeline y lógica de alto nivel (mock en MVP).
- `AgenteIALocal.Infrastructure` — Adaptadores concretos para IDE/Filesystem y utilidades.
- `AgenteIALocal.UI` — Controles WPF y helpers compartidos.
- `AgenteIALocal.Tests` — Pruebas unitarias e integración ligera.
- `AgenteIALocalVSIX` — Host VSIX clásico: Package, comandos, ToolWindow y empaquetado.

Dependencias entre capas

- VSIX (host) compone UI e integra adaptadores de `Infrastructure` que consumen contratos de `Core`.
- `Application` coordina flujos sobre `Core`; en MVP puede llamar a mocks.
- `UI` y `VSIX` no deben acoplarse a proveedores de IA concretos.

Arquitectura de la extensión VSIX

Rol del Package

- El `AsyncPackage` registra menús, comandos y ToolWindows, y expone recursos provenientes del VSCT.

Inicialización y autoload

- El package puede autoloadearse en contextos como `SolutionExists` (según atributos). La inicialización llama a `OpenAgenteIALocalCommand.InitializeAsync(this)` para registrar el comando.

Registro de comandos

- El comando se crea con `CommandID(CommandSet, CommandId)` y se registra vía `OleMenuCommandService`.

Relación VSCT / Package / CommandSet

- El VSCT define `GuidSymbol` para el package y el `CommandSet`.
- El GUID del package debe coincidir exactamente con el `[Guid(...)]`/`PackageGuidString` del `AsyncPackage` y con el Id del `.vsixmanifest`.
- El `CommandSet` del VSCT debe coincidir con el GUID usado en el código.

`Menus.ctmenu` no es archivo físico

- Es el nombre de recurso generado al compilar el `.vsct`, y se registra con `[ProvideMenuResource("Menus.ctmenu", 1)]`. No existe en el repositorio como archivo.

Errores VSCT comunes y causa real

- VSCT1102: símbolo GUID referenciado no definido (falta `GuidSymbol` o mismatch nombre/valor).
- VSCT1103: definición inválida o IDs duplicados (IDs no coinciden con el código o están repetidos).

Comandos, menús y UI

Qué comandos existen

- `OpenAgenteIALocalCommand` (id `0x0100`) bajo el `CommandSet` definido en VSCT.

Dónde deberían aparecer

- En el menú `Tools` (placement actual del VSCT para pruebas de visibilidad).

Qué código los registra

- `OpenAgenteIALocalCommand.InitializeAsync(this)` desde `Package.InitializeAsync` agrega el `OleMenuCommand`.

Por qué el menú aparece pero la ToolWindow no

- Falta o error en el registro de la ToolWindow en el Package: sin `[ProvideToolWindow(typeof(AgenteIALocalToolWindow))]`, `ShowToolWindowAsync` puede no crear la ventana y fallar de forma silenciosa.

Qué falta implementar

- Confirmar y aplicar el registro de la ToolWindow y validar su creación al invocar el comando.

Diseño del Agente IA

Qué es el Agente aquí

- Conjunto de contratos y flujos que operan sobre el contexto de la solución, con ejecución mock en el MVP.

Contratos existentes

- `CopilotRequest`, `CopilotResponse` (DTOs) y contratos en `Core`.

Rol de `MockCopilotExecutor`

- Emula respuestas JSON deterministas para validar la UI y el pipeline sin proveedor de IA real.

Qué partes son mock

- Orquestación y proveedor de IA: sin integración real, solo simulación.

Compilación y depuración

Requisitos de entorno

- Visual Studio 2022 + workload de extensiones.
- .NET Framework Developer Pack 4.7.2.
- MSBuild disponible.

Comando MSBuild real

msbuild src/AgenteIALocalVSIX/AgenteIALocalVSIX.csproj /t:Build /p:Configuration=Debug

Instancia experimental

- F5 lanza Visual Studio con `/rootsuffix Exp`.

Diagnóstico de VSIX

- Verificar instalación en `Extensions -> Manage Extensions`.
- Si el menú no aparece o no abre la ToolWindow, revisar `ActivityLog.xml` en:
  `%LOCALAPPDATA%\\Microsoft\\VisualStudio\\<version>_Exp\\ActivityLog.xml` o `%APPDATA%\\Microsoft\\VisualStudio\\<version>_Exp\\ActivityLog.xml`.

Lecciones aprendidas y errores comunes

- Autoload insuficiente del Package: comandos no visibles.
- No inicializar comandos desde el Package: el menú no se registra.
- GUIDs desalineados: menús invisibles sin errores.
- Asumir archivos físicos inexistentes (`Menus.ctmenu`).
- Mezclar patrones SDK-style con clásico sin ajustar el build/registro.

Roadmap técnico

- Habilitar ToolWindow correctamente y validar apertura.
- Resolver warnings VSTHRD (uso consistente de `JoinableTaskFactory`).
- Integrar proveedor LLM real con configuración segura.
- Endurecer threading y ciclo de vida en el VSIX.
- Preparar release (firma, versionado, notas y pruebas multi-VS).
