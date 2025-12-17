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

---

## Fase 1 – VSIX Shell (Notas técnicas)

Alcance de la Fase 1

- Establecer un esqueleto mínimo de VSIX para desarrollo y pruebas locales.
- Proveer un punto de entrada (comando de menú) y una ToolWindow acoplable con UI mínima para ampliar en fases posteriores.

Qué está implementado

- Una clase de comando (`OpenAgenteIALocalCommand`) registrada en el package y expuesta en el menú `View` mediante el archivo VSCT existente.
- Una implementación mínima de ToolWindow (`AgenteIALocalToolWindow`) y su control de usuario asociado (`AgenteIALocalToolWindowControl`).
- Un ViewModel ligero (`AgenteIALocalToolWindowViewModel`) que implementa `INotifyPropertyChanged` con la propiedad `StatusText`.
- Binding desde el control al ViewModel (actualmente realizado desde el code-behind para mantener la simplicidad inicial).
- Eliminación de `App.xaml` del proyecto VSIX para mantener el proyecto como librería de clases (resuelve errores de construcción relacionados con ApplicationDefinition).

Qué NO está implementado intencionalmente

- No hay lógica de negocio, servicios ni inyección de dependencias.
- No hay análisis del workspace, modelo de documentos ni integración con servicios de IA externos.
- No hay persistencia, telemetría ni configuración de CI/CD.
- El VSIX no está publicado ni listo para producción; está orientado únicamente a desarrollo y pruebas locales.

Decisiones técnicas clave

- El proyecto VSIX se implementa como una librería de clases (sin `ApplicationDefinition` / `App.xaml`). Esto evita la semántica de aplicación WPF en el paquete.
- La superficie principal de UI es una ToolWindow; esto mantiene la extensión mínima y compatible con patrones de UI de Visual Studio.
- La activación se realiza mediante comandos definidos en el VSCT; la clase de comando registra el comando de menú y abre la ToolWindow cuando se invoca.
- Se establece un patrón MVVM simple sin frameworks externos: un ViewModel con `INotifyPropertyChanged` y un objetivo de binding. El binding inicial se realiza desde el code-behind del control para simplicidad y evitar un cableado XAML prematuro.
- No se introducen servicios ni frameworks de DI en esta fase para reducir la complejidad y mantener el esqueleto transferible.

Estado funcional actual

- La solución se compila con éxito apuntando a .NET Framework 4.8 y .NET Standard 2.0 donde corresponda.
- El ítem de menú `View -> Abrir Agente IA Local` está registrado y es visible al ejecutar el VSIX en la Instancia Experimental de Visual Studio.
- Invocar el comando abre la ToolWindow acoplable con contenido placeholder enlazado a `StatusText`.

Cómo esta fase prepara la Fase 2

- Provee un shell de UI estable y visible (comando + ToolWindow) donde se podrán añadir capacidades de awareness del workspace.
- Establece un contrato ViewModel sencillo (`INotifyPropertyChanged`) que se extenderá con servicios y proveedores de datos en la Fase 2.
- Mantiene la base de código limpia de decisiones de infraestructura (sin DI ni servicios) para permitir evaluar enfoques en la siguiente fase sin refactorings costosos.

No objetivos explícitos (repetido)

- No asumir capacidades en la nube ni de IA externa.
- No publicar ni liberar este VSIX como artefacto de producción en este estado.

Notas

- Todos los cambios durante la Fase 1 se limitaron a agregar comando/ToolWindow y un ViewModel mínimo; no se modificaron archivos de proyecto, solución ni recursos VSCT más allá de utilizar identificadores de comando existentes en `AgenteIALocal.vsct`.
- El proyecto evita intencionalmente adicionar paquetes o frameworks en esta fase.
