Agente IA Local para Visual Studio

---

Estado del proyecto

MVP: Extensión clásica de Visual Studio (VSIX) targeting .NET Framework 4.7.2 (proyecto no SDK). Se compila con MSBuild y genera un paquete VSIX. El paquete presenta una ToolWindow de ejemplo que muestra un pipeline simulado basado en JSON (petición / respuesta) para prototipar la experiencia de integración de un LLM local.

Requisitos

- Visual Studio 2022 (instalar la workload "Desarrollo de extensiones de Visual Studio").
- .NET Framework Developer Pack 4.7.2.
- Git (para operaciones de repositorio).

Cómo compilar

Usar MSBuild desde la raíz del repositorio:

msbuild src/AgenteIALocalVSIX/AgenteIALocalVSIX.csproj /t:Build /p:Configuration=Debug

Cómo ejecutar / debug

1. Abrir la solución en Visual Studio.
2. Establecer el proyecto `AgenteIALocalVSIX` como proyecto de inicio.
3. Presionar F5 para iniciar la instancia experimental de Visual Studio (la ejecución de VSIX abre una instancia con `/rootsuffix Exp`).
4. En la instancia experimental, abrir la ToolWindow desde el menú correspondiente.

Comportamiento esperado en el MVP:

- Interfaz simple con botones `Run` y `Clear`.
- Área de texto que muestra la petición JSON enviada por la UI y el JSON de respuesta simulado.
- El flujo es mock: el pipeline de ejecución es de ejemplo para validar la experiencia de UI y la serialización/deserialización de JSON.

Estructura de la solución

- `AgenteIALocal.Core` — Modelos y utilidades compartidas (tipos de datos, contratos).
- `AgenteIALocal.Application` — Lógica de aplicación y orquestación del pipeline (mock en el MVP).
- `AgenteIALocal.Infrastructure` — Implementaciones de infraestructura (accesos, adaptadores, serialización).
- `AgenteIALocal.UI` — Componentes y controles compartidos para la UI (WPF, helpers de binding).
- `AgenteIALocal.Tests` — Pruebas unitarias y de integración ligeras.
- `AgenteIALocalVSIX` — Proyecto VSIX clásico que empaqueta la extensión y define la ToolWindow y comandos.

Notas importantes (VSIX clásico)

- El proyecto VSIX es clásico (no SDK-style). Los archivos deben estar incluidos explícitamente en el `csproj` para aparecer en el Solution Explorer.
- Algunas APIs históricas (p. ej. servicios de comandos) requieren referencias a assemblies del framework como `System.Design` en proyectos no SDK.
- Para la serialización/deserialización en este target se usa `Newtonsoft.Json` (compatible con net472).

Próximos pasos

1. Hardenizar el manejo de concurrencia y advertencias de VSTHRD usando `JoinableTaskFactory`/`JoinableTaskContext` donde sea necesario.
2. Añadir configuración y seguridad para endpoints y modelos (p. ej. LLM Studio) — mover a settings seguros y variables de entorno.
3. Implementar el pipeline de ejecución real con control de errores seguro y limitación de tiempo/recursos.
4. Checklist de empaquetado y release: firmar, versionado, notas de release, pruebas en distintas versiones de VS.

Reglas de trabajo del proyecto

- Operación: una acción/command a la vez — ejecutar un único comando y esperar el resultado antes de la siguiente acción.
- Siempre mostrar la salida del comando ejecutado (logs/resultados) y decidir el siguiente paso basándose en ese resultado.
- Copilot (o scripts automatizados) no debe modificar archivos `*.csproj`, `*.vsixmanifest` ni archivos de código fuente a menos que se indique explícitamente y con autorización.

No-go / Alcance

El repositorio actual contiene solo un MVP de UX/integación. No se han integrado modelos reales ni pipelines de producción.
