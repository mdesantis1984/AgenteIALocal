Agente IA Local for Visual Studio / Agente IA Local para Visual Studio

---

Project Title / Título del proyecto

- English: Agente IA Local for Visual Studio
- Español: Agente IA Local para Visual Studio

Project Description / Descripción del proyecto

- English
  - What the project is: A local Visual Studio extension solution that hosts components intended to provide AI-assisted developer tooling running locally. The repository contains multiple projects (core libraries, infrastructure, application logic, UI and tests) organized to produce a VSIX extension and supporting libraries.
  - What the project is NOT: This is not a cloud-hosted AI service, not a published marketplace extension, and not a feature-complete product. It does not describe deployed services or guarantees about external integrations.

- Español
  - Qué es el proyecto: Una solución para una extensión de Visual Studio que agrupa componentes para soportar funcionalidades de asistencia mediante IA ejecutadas localmente. El repositorio contiene proyectos (núcleo, infraestructura, aplicación, UI y pruebas) organizados para generar un VSIX y librerías de soporte.
  - Qué NO es el proyecto: No es un servicio de IA en la nube, no es una extensión publicada en ningún marketplace, ni un producto con todas las funcionalidades finales. No incluye despliegues ni garantías sobre integraciones externas.

Current Status / Estado actual

- English
  - Baseline solution and project structure are consolidated. The VSIX scaffolding is present for local development. No production release or external deployment configuration is included.

- Español
  - Línea base de solución y estructura de proyectos consolidada. El esqueleto de VSIX está presente para desarrollo local. No hay lanzamiento a producción ni configuraciones de despliegue externas incluidas.

Solution Architecture / Arquitectura de alto nivel

- English
  - The solution is organized into separate projects by responsibility: core libraries, application layer, infrastructure, UI (Visual Studio extension), and tests. Projects target .NET Framework 4.8 and .NET Standard 2.0 where appropriate. The intended build artifact for the extension is a VSIX package produced from the UI project.

- Español
  - La solución está organizada en proyectos separados por responsabilidad: librerías núcleo, capa de aplicación, infraestructura, UI (extensión de Visual Studio) y pruebas. Los proyectos apuntan a .NET Framework 4.8 y .NET Standard 2.0 cuando corresponde. El artefacto de compilación previsto para la extensión es un paquete VSIX generado desde el proyecto de UI.

Projects Overview / Resumen de proyectos

- English
  - `AgenteIALocal` (root or launcher projects)
  - `AgenteIALocal.Application` (application layer)
  - `AgenteIALocal.Core` (core domain and shared logic)
  - `AgenteIALocal.Infrastructure` (implementations and platform-specific code)
  - `AgenteIALocal.UI` (Visual Studio extension / VSIX project)
  - `AgenteIALocal.Tests` (unit and integration tests)

- Español
  - `AgenteIALocal` (proyectos raíz o de lanzamiento)
  - `AgenteIALocal.Application` (capa de aplicación)
  - `AgenteIALocal.Core` (dominio y lógica compartida)
  - `AgenteIALocal.Infrastructure` (implementaciones y código específico de plataforma)
  - `AgenteIALocal.UI` (extensión de Visual Studio / proyecto VSIX)
  - `AgenteIALocal.Tests` (pruebas unitarias e integración)

Non-Goals (Explicit) / No objetivos (Explícito)

- English
  - Shipping a production-ready, published VSIX to the Visual Studio Marketplace.
  - Providing cloud-hosted AI inference or managed external AI services.
  - Changing project structure, .csproj files, solution files, or VSCT resources in this repository at this stage.

- Español
  - Publicar una extensión VSIX lista para producción en el Visual Studio Marketplace.
  - Proveer inferencia de IA alojada en la nube o servicios de IA externos gestionados.
  - Modificar la estructura de proyectos, archivos .csproj, archivos de solución (.sln) o recursos .vsct en este repositorio en esta fase.

Build and Debug / Build y Depuración

- English
  - Build Requirements:
    1. Visual Studio 2019 or 2022 with the "Visual Studio extension development" and ".NET desktop development" workloads installed. The solution targets .NET Framework 4.8 and .NET Standard 2.0; ensure the corresponding targeting packs are available on the system.
    2. (Optional) Visual Studio SDK components for VSIX development when prompted by the UI project.

  - How to Build the Solution (manual steps):
    1. Open `AgenteIALocal.sln` (or the solution file at the repository root) in Visual Studio.
    2. Restore NuGet packages if Visual Studio prompts (Tools -> NuGet Package Manager -> Restore or right-click solution -> Restore NuGet Packages).
    3. Set the solution configuration to `Debug` (or `Release` if desired).
    4. Build the solution from the Build menu: `Build -> Build Solution` (or press `Ctrl+Shift+B`).
    5. Confirm the `AgenteIALocal.UI` (VSIX) project builds successfully and produces a VSIX output under its `bin` directory.

  - Running the VSIX (Experimental Instance):
    1. In Visual Studio, set the startup project to `AgenteIALocal.UI`.
    2. Start debugging the VSIX project using `Debug -> Start Debugging` (F5) or `Start Without Debugging` (Ctrl+F5) to launch the Visual Studio Experimental Instance. This launches a separate Visual Studio process (the experimental instance) with the extension installed for testing.
    3. Use the experimental instance to validate extension menus, tool windows or commands relevant to this repository. The experimental instance does not reflect any published extension or external services.

  - Debugging Notes:
    - Place breakpoints in the UI or other projects as needed before starting the experimental instance.
    - Logs and diagnostic output are limited to what the projects emit; there is no centralized telemetry.
    - If the experimental instance does not load the extension, ensure the VSIX project built successfully and that the correct experimental instance is being launched by Visual Studio.

- Español
  - Requisitos de Build:
    1. Visual Studio 2019 o 2022 con las cargas de trabajo "Visual Studio extension development" y ".NET desktop development" instaladas. La solución apunta a .NET Framework 4.8 y .NET Standard 2.0; confirme que los paquetes de destino (targeting packs) correspondientes estén disponibles.
    2. (Opcional) Componentes del SDK de Visual Studio para desarrollo de VSIX si el proyecto de UI los solicita.

  - Cómo compilar la solución (pasos manuales):
    1. Abra `AgenteIALocal.sln` (o el archivo de solución en la raíz del repositorio) en Visual Studio.
    2. Restaure los paquetes NuGet si Visual Studio lo solicita (Tools -> NuGet Package Manager -> Restore o clic derecho en la solución -> Restore NuGet Packages).
    3. Seleccione la configuración de solución `Debug` (o `Release` si se prefiere).
    4. Compile la solución desde el menú Build: `Build -> Build Solution` (o presione `Ctrl+Shift+B`).
    5. Confirme que el proyecto `AgenteIALocal.UI` (VSIX) se compiló correctamente y generó un VSIX en su carpeta `bin`.

  - Ejecución del VSIX (Instancia experimental):
    1. En Visual Studio, establezca el proyecto de inicio en `AgenteIALocal.UI`.
    2. Inicie la depuración del proyecto VSIX con `Debug -> Start Debugging` (F5) o `Start Without Debugging` (Ctrl+F5) para lanzar la Instancia Experimental de Visual Studio. Esto inicia un proceso de Visual Studio separado con la extensión instalada para pruebas.
    3. Use la instancia experimental para validar menús, ventanas de herramientas o comandos de la extensión relevantes para este repositorio. La instancia experimental no corresponde a una extensión publicada.

  - Notas de depuración:
    - Coloque puntos de interrupción en la UI u otros proyectos según sea necesario antes de iniciar la instancia experimental.
    - Los registros y diagnósticos están limitados a lo que emiten los proyectos; no existe telemetría centralizada.
    - Si la instancia experimental no carga la extensión, verifique que el proyecto VSIX se compilará correctamente y que Visual Studio esté lanzando la instancia experimental adecuada.

Notes / Notas (append)

- The instructions above reflect the current local development workflow and do not assume any CI/CD or external services.
- Las instrucciones anteriores reflejan el flujo de desarrollo local actual y no asumen existencias de CI/CD ni servicios externos.
