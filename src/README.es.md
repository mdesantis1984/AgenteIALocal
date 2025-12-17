Agente IA Local para Visual Studio — Español

🔎 Visión general

Agente IA Local es una solución de extensión para Visual Studio diseñada para reunir componentes que soporten herramientas de ayuda basadas en IA ejecutadas localmente. El repositorio se organiza en múltiples proyectos (librerías núcleo, capa de aplicación, infraestructura, UI y pruebas) y está estructurado para producir un VSIX y librerías de soporte para desarrollo local.

📋 Estado actual

- Línea base de solución y estructura de proyectos consolidada.
- Esqueleto VSIX y proyecto de paquete presentes para desarrollo y pruebas locales.
- Se añadió un comando (sin lógica funcional) y se registró en el package; el comando aparece en el menú View cuando se ejecuta el VSIX en una instancia experimental.
- No se ha implementado ToolWindow ni UI visible más allá del ítem de menú en esta fase.

🏗 Arquitectura de la solución

La solución está organizada en proyectos separados por responsabilidad:
- `AgenteIALocal` — Package e integración para el VSIX
- `AgenteIALocal.Application` — Capa de aplicación
- `AgenteIALocal.Core` — Lógica de dominio y compartida
- `AgenteIALocal.Infrastructure` — Implementaciones específicas de plataforma
- `AgenteIALocal.UI` — Extensión de Visual Studio / proyecto VSIX
- `AgenteIALocal.Tests` — Pruebas unitarias e integración

Los proyectos apuntan a .NET Framework 4.8 y .NET Standard 2.0 cuando corresponde. El artefacto de compilación esperado para la extensión es un paquete VSIX generado desde el proyecto UI.

🛠 Requisitos de build

1. Visual Studio 2019 o 2022 con las cargas de trabajo "Visual Studio extension development" y ".NET desktop development" instaladas.
2. Paquetes de destino para .NET Framework 4.8 y .NET Standard 2.0 disponibles en el sistema.
3. (Opcional) Componentes del SDK de Visual Studio para desarrollo de VSIX.

📦 Compilación y ejecución (pasos manuales)

1. Abra la solución en Visual Studio.
2. Restaure paquetes NuGet si se le solicita.
3. Seleccione la configuración `Debug`.
4. Compile la solución (`Build -> Build Solution` o `Ctrl+Shift+B`).
5. Establezca `AgenteIALocal.UI` como proyecto de inicio y ejecute el VSIX en la Instancia Experimental (`F5`).

🐞 Depuración

- Coloque puntos de interrupción en UI u otros proyectos antes de lanzar la instancia experimental.
- Los registros y salidas de diagnóstico son emitidos por los proyectos; no hay telemetría centralizada configurada.
- Si la extensión no se carga, confirme que el proyecto VSIX se compiló correctamente y que Visual Studio está lanzando la instancia experimental.

🚫 No objetivos

- Publicar una extensión VSIX lista para producción en el Visual Studio Marketplace en esta etapa.
- Proveer inferencia de IA alojada en la nube o servicios de IA externos gestionados.
- Modificar la estructura de proyectos, archivos `.csproj`, archivos de solución o recursos `.vsct` como parte de estas fases iniciales.
