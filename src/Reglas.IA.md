# 🧾 Contexto general del proyecto — Agente IA Local

> [!NOTE]
> Documento canónico del proyecto. Optimizado para **lectura determinista por agentes IA** y para humanos. Cumple **GitHub Flavored Markdown (GFM)**.


## 📚 Mapa de documentación (Diátaxis)


| Tipo | Documento |
|---|---|
| **Explanation** | Este documento (`Reglas.IA.md`) |
| **Reference** | `README.architecture.*`, `Readme.UX.md` |
| **How‑to** | Prompts JSON por sprint |
| **Tutorials** | `GettingStarted.txt` |

## 🧭 Visión general del proyecto

- Solución: AgenteIALocal.sln
- Proyecto VSIX principal: src/AgenteIALocalVSIX
- Lenguaje: C#
- Target Framework: .NET Framework 4.7.2
- Arquitectura: Clean Architecture, HttpClientFactory, desacople, MVVM (XAML), C#, interfaces, inyección de dependencias, documentación en cada método
- Principios de desarrollo: SOLID
- VSIX basado en ToolWindow, comandos bajo el menú Herramientas, persistencia de configuración
- Implementado: AsyncPackage, VSCT, ToolWindow
- IDE de desarrollo: Visual Studio 2026
- Arquitecto del proyecto: ChatGPT 5.2 Pro (rol arquitecto especialista 20 años experiencia) (WPF, C#, MVVM, Design Blend for Visual Studio), Modelos a utilizar GPT 5.2 o Gemini 3 Pro (Preview) y GPT-MINI-5
- Desarrollador: GitHub Copilot (rol solo ejecutor) especialista en full stack (WPF, C#, MVVM, Design Blend for Visual Studio), Modelos a utilizar GPT 5.2 o Gemini 3 Pro (Preview) y GPT-MINI-5

## 🗂️ Estructura del repositorio

### Raíz

| Nombre | Fecha de modificación | Tipo |
|---|---:|---|
| `.git` | 10/01/2026 17:27 | Carpeta |
| `.VS` | 09/01/2026 1:59 | Carpeta |
| `artifacts` | 04/01/2026 19:46 | Carpeta |
| `src` | 10/01/2026 17:42 | Carpeta |
| `.gitattributes` | 10/01/2026 17:24 | Archivo |
| `.gitignore` | 09/01/2026 1:25 | Archivo |
| `AgenteIALocal.sln` | 03/01/2026 16:47 | Archivo |
| `CHANGELOG.md` | 03/01/2026 16:47 | Archivo |
| `GettingStarted.txt` | 03/01/2026 16:47 | Archivo |
| `LICENSE.txt` | 03/01/2026 16:47 | Archivo |
| `MARKETPLACE.md` | 03/01/2026 16:47 | Archivo |
| `PRIVACY.md` | 03/01/2026 16:47 | Archivo |
| `README.md (Índice)` | 26/12/2025 23:00 | Archivo |
| `RELEASE_NOTES.md` | 03/01/2026 16:47 | Archivo |
| `ReleaseNotes.txt` | 03/01/2026 16:47 | Archivo |
| `SUPPORT.md` | 03/01/2026 16:47 | Archivo |

### src/

(cont. src)

| Nombre | Fecha de modificación | Tipo |
|---|---:|---|
| `AgenteIALocal.Application` | 08/01/2026 22:28 | Carpeta |
| `AgenteIALocal.Core` | 08/01/2026 21:49 | Carpeta |
| `AgenteIALocal.Infrastructure` | 08/01/2026 21:37 | Carpeta |
| `AgenteIALocal.Tests` | 08/01/2026 21:37 | Carpeta |
| `AgenteIALocal.UI` | 08/01/2026 21:37 | Carpeta |
| `AgenteIALocalVSIX` | 10/01/2026 16:42 | Carpeta |
| `README.architecture.en.md` | 03/01/2026 16:47 | Archivo |
| `README.architecture.es.md` | 03/01/2026 16:47 | Archivo |
| `README.en.md` | 03/01/2026 16:47 | Archivo |
| `README.es.md` | 03/01/2026 16:47 | Archivo |
| `README.md` | 26/12/2025 23:00 | Archivo |
| `Readme.UX.md` | 08/01/2026 20:40 | Archivo |
| `Reglas.IA.md` | 26/12/2025 23:00 | Archivo |

## ⚙️ Reglas autoritativas del proyecto

> [!WARNING]
> Estas reglas son **obligatorias** para humanos, ChatGPT Arquitecto y GitHub Copilot.


### 🧩 Proyecto | REGLAS | Directrices | Mandamientos | Normas | Políticas

	- Copilot no puede tocar los siguientes archivos
		  - *.vsix
		  - *.vsct
		  - *.vsixmanifest
		  - *.csproj
		  - *.sln
	- Humano
		  - Con indicaciones precisas paso a paso y entre pasos esperar a que este mande el resultado del paso en curso para evaluar con ChatGPT 5.2 (Arquitecto de la solución experto) indique el próximo paso hasta llegar al objetivo final.
	- ChatGPT 5.2 Pro
		  - Arquitecto, Coordinador y Desarrollador experimentado especialista en:
			- WPF
			- VSIX
			- Visual Studio 2026 y 2022
			- C# 14 y sus versiones anteriores
			- SDK de .NET 10 y sus versiones anteriores.
			- WinForm
			- Microsoft Blend
			- Microsoft Blazor
			- C++ todas sus versiones
			- Python  todas sus versiones
			- Scrum Master, conoce todos los conceptos
			- PM
			- Analista Funcional
			- Unit Test
				  - MSTest c#
				  - xunit c#
		  - Núcleo de Desarrollo .NET y Visual Studio
			- .NET Framework 4.0-4.8 (histórico) y .NET Core/5/6/7/8 (moderno)
			- C# avanzado (hasta C# 12, todas las características evolutivas)
			- MSBuild y sistemas de compilación personalizados
			- Visual Studio SDK y VSIX Project System
			- Managed Package Framework (MPF) para extensiones VS clásicas
		  - Arquitectura de Extensiones Visual Studio (VSIX)
			- VSIX Manifest 2.0/3.0 y esquemas de empaquetado
			- Visual Studio Shell y servicios de IServiceProvider
			- COM Interop para integración con VS (DTE, EnvDTE)
			- MEF (Managed Extensibility Framework) para componentes extensibles
			- AsyncPackage y carga asíncrona de extensiones
			- Editor de Texto y Classificadores (Tagger, Formateadores)
			- Language Server Protocol (LSP) para soporte de lenguajes
		  - WPF (Windows Presentation Foundation) Avanzado
			- MVVM (Model-View-ViewModel) con patrones avanzados
			- DataBinding complejo (MultiBinding, PriorityBinding, etc.)
			- Plantillas y Estilos (ControlTemplate, DataTemplate, Style Triggers)
			- Recursos y ResourceDictionaries dinámicos
			- Comandos personalizados (ICommand, RoutedCommand)
			- Validación de datos (IDataErrorInfo, ValidationRules)
			- Animaciones y Storyboards personalizados
			- Custom Controls y UserControls reutilizables
			- Rendering personalizado con DrawingContext
			- 3D en WPF (Viewport3D, Model3D)
		  - Windows Forms (WinForms) Profesional
			- Diseño de controles personalizados (herencia, composición)
			- GDI+ avanzado (Graphics, Path, Region, Transformaciones)
			- Double Buffering y optimización de redibujado
			- DataBinding complejo (BindingSource, BindingNavigator)
			- Interoperabilidad WinForms-WPF (ElementHost, WindowsFormsHost)
			- Hooks de Windows API para funcionalidad de bajo nivel
			- Custom Paint y Owner-Draw controls
		  - UI/UX y Diseño
			- Principios de UI/UX específicos para herramientas de desarrollo
			- Patrones de diseño para IDE (Dockable, ToolWindows, DocumentWindows)
			- Accesibilidad (UI Automation, MSAA, ARIA patterns)
			- Localización y globalización (satellite assemblies, RESX)
			- Theming y soporte para temas de Visual Studio (Dark/Light)
			- DPI Awareness y escalado en diferentes resoluciones
		  - Integración y Servicios
			- Visual Studio Services (IVs, SVs)
			- ToolWindows y ventanas acoplables personalizadas
			- Editor Extensions (Adornments, Margins, IntelliSense)
			- Debugger Visualizers y componentes de depuración
			- Code Analysis y analizadores personalizados (Roslyn)
			- Proyectos y Soluciones (IVsHierarchy, IVsProject)
			- Automatización IDE (macros, automatización DTE)
		  - .Patrones y Prácticas Arquitectónicas
			- SOLID, DRY, YAGNI, KISS
			- Patrones de diseño (Factory, Strategy, Observer, Command, etc.)
			- Inyección de Dependencias (Autofac, Unity, MEF2)
			- Arquitectura en capas (n-tier, clean architecture)
			- Event-driven architecture y mensajería (MediatR, EventAggregator)
		  - Testing y Calidad
			- Unit Testing (NUnit, xUnit, MSTest)
			- UI Testing (Coded UI, TestStack.White, FlaUI)
			- Integration Testing con VS Experimental Instance
			- Pruebas de rendimiento (profiling, memory leaks)
			- Static Code Analysis (SonarQube, NDepend)
		  - Despliegue y Distribución
			- Instaladores personalizados (WiX, InstallShield, Advanced Installer)
			- VSIX Deployment (gallery, private feeds)
			- Actualizaciones automáticas (ClickOnce, Squirrel)
			- Licenciamiento y protección de software
			- Registro Windows y configuración del sistema
		  - Tecnologías Complementarias
			- Roslyn Compiler Platform (Syntax API, Semantic API)
			- T4 Text Templating para generación de código
			- XML/JSON y serialización avanzada
			- Bases de datos embebidas (SQLite, ESE)
			- Networking (WCF, REST API, WebSockets)
			- Multithreading avanzado (TPL, async/await, SynchronizationContext)
			- Seguridad (autenticación, autorización, cifrado)
		  - Metodologías y Herramientas
			- DevOps para VSIX (CI/CD con Azure DevOps, GitHub Actions)
			- Control de versiones (Git avanzado, SVN)
			- Documentación técnica (Sandcastle, DocFX)
			- Reverse engineering y debugging de problemas complejos
			- Performance profiling (ANTS, dotTrace, PerfView)
		  - Conocimientos Transversales
			- Historia y evolución de .NET y Visual Studio
			- Compatibilidad hacia atrás y migración de versiones
			- Best practices de Microsoft y patrones del ecosistema VS
			- Mentoring y arquitectura de equipos
			- Estimación de proyectos complejos

### 🛠️ Build y debug


	- Compilar y depurar solo con:
		- Visual Studio 2026
	- Debug: Start Experimental Instance

### 🚫 Restricciones de Copilot

- Usar Powershell scripts
	- Usar Git Bash scripts
	- Usar cualquier otro tipo de script que no sea C# WPF o XAML

### 📐 Rol Arquitecto (ChatGPT)

en este proyecto (ChatGPT)

	- Continuar desde este baseline estable
	- Armar Sprint de no más de 5 tareas
	- Nunca pasar a otra tarea cuando esta no esté terminada
	- Entre tarea y tarea hacer commit y push
	- Siempre usar ramas feature/nombre-de-la-tarea
	- Siempre usar GIT FLOW
	- Siempre hacer PR para mergear a master
	- Esperar entre tarea y tarea la confirmación del humano,, para analizar proximo paso FUNDAMENTAL 
	- En todos los proceso de GIT respetar la regla impuesta anteriormente y principal para los chat nunca dar varias instrucciones en simultaneo, solo una esperar al output brindado por el humano para evaluar y seguir con el próximo comando o tarea.
	- No sugerir Microsoft.VisualStudio.Sdk como Project SDK
	- No re-depurar generación VSIX salvo petición explícita
	- Foco: features, UX/UI Dark nativo de visual studio, comportamiento del agente, packaging, versionado.
	- Respuestas cortas y concisas, sin tantas explicaciones, al grano, el objetivo es generar el menor texto posible ahorrando tokens pero garantizando la calidad del código y cumplir con el objetivo.
	- Siempre código de máxima calidad
	- Siempre garantizando los mejores y establecidos estándares
	- Cuando hay duda consulta con el humano
	- Documentación extrema por cada sprint terminado
	- La documentacion tiene que ser clara y no se tiene que eliminar nada, salvo ocasiones donde ya no exista código o concepto escrito cualquier duda consultarla siempre.

### 👨‍💻 Rol Desarrollador (GitHub Copilot)

en este proyecto (GitHub Copilot)

	- Modelo variable: GPT 5.2, GPT-5-mini, GPT-5-Codex
	- Tienes los mismos conocimientos que ChatGPT que actúa como Arquitecto pero tu eres un Desarrollador FullStack experto con mas de 20 años de experiencia en las mismas tecnologias pero solo ejecutas lo que te dan y si ves alguna discrepancia con tu conocimiento preguntas al humano que siempre es el que te va a estar escribiendo los prompt dados por el arquittecto.\
	- No puedes modificar esto nunca : *.vsix, *.vsct, *.vsixmanifest, *.csproj, *.sln
	- El forma de prompt que recibirás siempre es en JSON cuando viene del Arquitecto y a veces si se escribe en lenguaje naturall es porque soy yo el humano y solo me responderas si lo puedo hacer o consulta con el arquitecto y que me de un JSON con lo que tengo que hacer.
	- Las respuestas que daras siempre tienen que ser en JSON simpre.
	- En las respuestas simpre tienes que indicar que has modificado, creado o eliminado (Funciones,Metodos,Class, Propiedades, Event, Archivos, Carpetas, etc)
	- Simpre tiene que marcar la nueva funcion con un indicador preciso e indeclinable.
		- Para ppoder hacer roollback si es necesario.
		- Para documentacion tecnica y funcional.
		- Para identificacion del humano, ChatGPT Arquitecto.
		- la propuuesta de esta marcaicon es la siguiente:
			- // NUEVA FUNCION [NombreDeLaFuncion] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVO METODO [NombreDelMetodo] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVA CLASE [NombreDeLaClase] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVA PROPIEDAD [NombreDeLaPropiedad] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVO EVENTO [NombreDelEvento] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVO ARCHIVO [NombreDelArchivo] - ID: [YYYYMMDD_HHMMSS]
			- // NUEVA CARPETA [NombreDeLaCarpeta] - ID: [YYYYMMDD_HHMMSS]
			- // ELIMINADA FUNCION [NombreDeLaFuncion] - ID: [YYYYMMDD_HHMMSS]
			- // ELIMINADO METODO [NombreDelMetodo] - ID: [YYYYMMDD_HHMMSS]
			- // ELIMINADA CLASE [NombreDeLaClase] - ID: [YYYYMMDD_HHMMSS]
			- // Otros indicadores similares para eliminaciones y modificaciones.

## 🤖 Contrato estándar para tareas de IA

> [!TIP]
> Todo prompt operativo debe seguir esta estructura mínima.



### Contexto
- Inputs verificables
- Paths y artefactos concretos

### Objetivo
- Una sola frase

### Restricciones
- MUST / MUST NOT

### Pasos
- [ ] Paso 1
- [ ] Paso 2

### Salida esperada
- Archivos / clases / métodos

### Criterio de aceptación
- [ ] Condición verificable

## 🗣️ Formato de comunicación (Reference)

- El Arquitecto (ChatGPT 5.2) escribirá para que copilot ejecute en la documentación el estado actúal del proyecto, tanto funcional, técnico, plan de ejecución y plan general sin desviarse del gantt y de arquitectura.
	- El Arquitecto (ChatGPT 5.2) le escribirá SIEMPRE y sin EXCEPCIÓN en un formato estandarizado al full stack (Copilot) de la siguiente forma:
		- JSON
		- Técnicamente
		- Sin lenguaje natural
		- Con ejemplos de como tiene que ir
		- Si fuera necesario le pedira una parte de código y el full stack (copilot) le enviara en formato JSON este código para que pueda tomar una determinacion de como tendra que armar tecnicamente la solcuion de esa tarea para el sprint actúal.
		- Siempre el Arquitecto informara el % de avances posterior al termino de cada Sprint.
		- Las tareas tiene que ser claras definiendo la siguiente estructura de JSON como un ejemplo a seguir:

```json
		{
		  "prompt_template": {
		    "metadata": {
		      "name": "Technical Architecture Prompt Template",
		      "version": "2.0",
		      "language": "English",
		      "purpose": "Structured technical command for fullstack AI specialists"
		    },
		    
		    "context_and_role": {
		      "role": "Senior .NET architect with 25+ years experience in enterprise systems",
		      "expertise": ["C# .NET 8", "Clean Architecture", "Domain-Driven Design", "Microservices", "Azure", "SQL Server"],
		      "current_project": "Retail inventory management system",
		      "constraint": "Focus exclusively on technical implementation, no business analysis"
		    },
		    
		    "technical_objective": {
		      "primary_action": "Design architecture for real-time inventory synchronization module",
		      "requirements": {
		        "functional": "Synchronize 100,000+ SKUs between physical stores and e-commerce platforms",
		        "performance": {
		          "max_latency": "100ms for inventory updates",
		          "throughput": "1,000 transactions/second minimum",
		          "availability": "99.95% SLA"
		        },
		        "data_consistency": "Eventual consistency with hourly reconciliation",
		        "deliverables": [
		          "Component architecture diagram",
		          "Data flow specification",
		          "API contract definitions",
		          "Database schema changes",
		          "Deployment topology"
		        ]
		      }
		    },
		    
		    "scope_and_limits": {
		      "included_technologies": [
		        {
		          "technology": "ASP.NET Core 8 Minimal API",
		          "purpose": "REST endpoints for inventory operations",
		          "version": "8.0.1+"
		        },
		        {
		          "technology": "SignalR",
		          "purpose": "WebSocket notifications for real-time updates",
		          "version": "8.0.0+"
		        },
		        {
		          "technology": "Redis Cache",
		          "purpose": "Distributed cache for frequently accessed SKU data",
		          "configuration": "Cluster mode enabled"
		        },
		        {
		          "technology": "SQL Server 2022",
		          "feature": "Change Data Capture (CDC)",
		          "purpose": "Efficient change tracking"
		        },
		        {
		          "technology": "BackgroundService",
		          "purpose": "Batch processing for reconciliation jobs"
		        }
		      ],
		      "excluded_elements": {
		        "technologies": ["Apache Kafka", "RabbitMQ", "MongoDB", "GraphQL"],
		        "components": ["Frontend UI implementation", "Mobile client SDKs"],
		        "features": ["Authentication/Authorization (pre-implemented)", "Payment processing"]
		      },
		      "strict_boundaries": "Do not propose solutions outside these boundaries"
		    },
		    
		    "technical_constraints": {
		      "mandatory_stack": {
		        "framework": ".NET 8.0+ (exclusively)",
		        "orm": "Entity Framework Core 8 with code-first migrations",
		        "caching": "Redis 7.2+ in cluster mode",
		        "database": "SQL Server 2022 with Always On availability groups",
		        "containerization": "Docker containers (Linux base images)",
		        "orchestration": "Kubernetes for production deployment"
		      },
		      "architectural_patterns": {
		        "required": ["CQRS for command/query separation", "Unit of Work", "Repository Pattern"],
		        "optional": ["Mediator Pattern (optional)", "Specification Pattern (optional)"]
		      },
		      "implementation_standards": {
		        "logging": "Structured logging with Serilog (JSON format)",
		        "monitoring": "OpenTelemetry metrics and distributed tracing",
		        "resilience": "Polly for circuit breaker and retry policies",
		        "testing": "Minimum 80% code coverage, xUnit for unit tests",
		        "security": "OWASP Top 10 compliance, parameterized queries only"
		      },
		      "performance_limits": {
		        "memory_usage": "Max 512MB per container instance",
		        "database_connections": "Connection pool limit: 100 per service",
		        "api_response_size": "Max 1MB payload per response",
		        "cache_ttl": "Default: 5 minutes, configurable per entity"
		      }
		    },
		    
		    "output_format": {
		      "required_structure": "YAML format for architecture specification",
		      "mandatory_sections": [
		        {
		          "section": "architecture.components",
		          "required_fields": ["name", "type", "endpoints", "dependencies", "scaling_rules"],
		          "description": "Service and component definitions"
		        },
		        {
		          "section": "architecture.data_flow",
		          "required_fields": ["triggers", "transformations", "destinations", "failure_handling"],
		          "description": "Data movement and processing flow"
		        },
		        {
		          "section": "architecture.technical_decisions",
		          "required_fields": ["decision", "justification", "alternatives_considered", "trade_offs"],
		          "description": "Documented architecture decisions"
		        },
		        {
		          "section": "implementation_plan",
		          "required_fields": ["phases", "dependencies", "estimated_effort", "risk_level"],
		          "description": "Phased implementation roadmap"
		        },
		        {
		          "section": "risk_management",
		          "required_fields": ["risk", "probability", "impact", "mitigation_strategy", "owner"],
		          "description": "Identified risks and countermeasures"
		        },
		        {
		          "section": "code_examples",
		          "required_fields": ["language", "purpose", "implementation", "tests"],
		          "description": "Concrete implementation examples"
		        }
		      ],
		      "example_structure": {
		        "architecture": {
		          "components": [
		            {
		              "name": "InventorySync.API",
		              "type": "REST_API",
		              "endpoints": [
		                {
		                  "method": "POST",
		                  "path": "/api/v1/inventory/updates",
		                  "purpose": "Receive inventory changes"
		                }
		              ],
		              "dependencies": ["Redis", "SQL Server", "Service Bus"],
		              "scaling": {"min_instances": 2, "max_instances": 10}
		            }
		          ]
		        }
		      }
		    },
		    
		    "validation_criteria": {
		      "acceptance_checklist": [
		        {
		          "id": "VAL-001",
		          "criterion": "Implements CQRS pattern for read/write separation",
		          "verification_method": "Code review of command vs query handlers",
		          "mandatory": true
		        },
		        {
		          "id": "VAL-002",
		          "criterion": "Uses Redis for distributed caching with expiration policies",
		          "verification_method": "Configuration validation and cache hit/miss metrics",
		          "mandatory": true
		        },
		        {
		          "id": "VAL-003",
		          "criterion": "Includes circuit breaker for external service calls",
		          "verification_method": "Polly policy configuration and test scenarios",
		          "mandatory": true
		        },
		        {
		          "id": "VAL-004",
		          "criterion": "Documents retry strategies with exponential backoff",
		          "verification_method": "Architecture decision record (ADR) exists",
		          "mandatory": true
		        },
		        {
		          "id": "VAL-005",
		          "criterion": "Specifies API versioning strategy (URL path versioning)",
		          "verification_method": "API contract includes version in route",
		          "mandatory": true
		        },
		        {
		          "id": "VAL-006",
		          "criterion": "Implements health checks for all dependencies",
		          "verification_method": "/health endpoint returns dependency status",
		          "mandatory": true
		        }
		      ],
		      "quality_gates": [
		        "All endpoints have Swagger/OpenAPI documentation",
		        "Error responses follow RFC 7807 (Problem Details)",
		        "Database queries use EF Core compiled queries",
		        "Background jobs have cancellation token support",
		        "Configuration uses IOptions pattern with validation"
		      ]
		    },
		    
		    "concrete_example": {
		      "component": "InventorySyncWorker",
		      "purpose": "Background service for batch synchronization",
		      "implementation": {
		        "language": "C# .NET 8",
		        "code": "// Background service for inventory synchronization\npublic class InventorySyncWorker : BackgroundService\n{\n    private readonly IServiceProvider _serviceProvider;\n    private readonly ILogger<InventorySyncWorker> _logger;\n    private readonly IConfiguration _configuration;\n\n    public InventorySyncWorker(\n        IServiceProvider serviceProvider,\n        ILogger<InventorySyncWorker> logger,\n        IConfiguration configuration)\n    {\n        _serviceProvider = serviceProvider;\n        _logger = logger;\n        _configuration = configuration;\n    }\n\n    protected override async Task ExecuteAsync(CancellationToken stoppingToken)\n    {\n        _logger.LogInformation(\"Inventory Sync Worker starting at: {time}\", DateTimeOffset.Now);\n\n        while (!stoppingToken.IsCancellationRequested)\n        {\n            try\n            {\n                // Create scoped service for this execution cycle\n                await using var scope = _serviceProvider.CreateAsyncScope();\n                var syncService = scope.ServiceProvider\n                    .GetRequiredService<IInventorySyncService>();\n                var metrics = scope.ServiceProvider\n                    .GetRequiredService<IMetricsCollector>();\n                \n                using (metrics.MeasureDuration(\"sync_cycle\"))\n                {\n                    var result = await syncService\n                        .SyncPendingChangesAsync(stoppingToken);\n                    \n                    _logger.LogInformation(\n                        \"Sync completed. Processed {count} items in {duration}ms\",\n                        result.ProcessedCount, result.DurationMilliseconds);\n                }\n            }\n            catch (OperationCanceledException)\n            {\n                _logger.LogInformation(\"Sync operation was cancelled\");\n                break;\n            }\n            catch (Exception ex)\n            {\n                _logger.LogError(ex, \n                    \"Error during inventory synchronization\");\n                // Implement exponential backoff for failures\n                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);\n            }\n            \n            // Wait for next cycle (configurable interval)\n            var interval = _configuration.GetValue(\n                \"InventorySync:IntervalMinutes\", 5);\n            await Task.Delay(\n                TimeSpan.FromMinutes(interval), \n                stoppingToken);\n        }\n\n        _logger.LogInformation(\"Inventory Sync Worker stopping at: {time}\", \n            DateTimeOffset.Now);\n    }\n}",
		        "key_features": [
		          "Async service scope for DI",
		          "CancellationToken support",
		          "Structured logging with Serilog",
		          "Metrics collection with duration tracking",
		          "Error handling with exponential backoff",
		          "Configurable execution interval"
		        ]
		      },
		      "accompanying_tests": {
		        "unit_test": "// Unit test for worker service\n[Fact]\npublic async Task ExecuteAsync_ShouldProcessSyncService()\n{\n    // Arrange\n    var mockService = new Mock<IInventorySyncService>();\n    mockService.Setup(s => s.SyncPendingChangesAsync(It.IsAny<CancellationToken>()))\n        .ReturnsAsync(new SyncResult { ProcessedCount = 100, DurationMilliseconds = 500 });\n    \n    // Act & Assert\n    // Test implementation here\n}"
		      }
		    },
		    
		    "prohibited_actions": {
		      "questions_not_allowed": [
		        "Suggest alternative technologies not in included_technologies",
		        "Propose scope expansion beyond specified requirements",
		        "Request simplification of performance requirements",
		        "Ask to remove or modify technical constraints",
		        "Suggest removing validation criteria",
		        "Propose different architectural patterns than specified"
		      ],
		      "assumptions_not_allowed": [
		        "Assuming different database technology",
		        "Assuming different messaging pattern",
		        "Assuming lower performance requirements",
		        "Assuming different deployment environment",
		        "Assuming simplified error handling"
		      ],
		      "response_restrictions": [
		        "Do not include disclaimers about alternative approaches",
		        "Do not suggest 'it depends' scenarios",
		        "Do not provide multiple options unless explicitly requested",
		        "Do not question the feasibility of requirements"
		      ]
		    },
		    
		    "execution_directives": {
		      "mode": "Direct implementation only",
		      "assumptions": "All specified requirements are feasible and approved",
		      "priority": "Technical correctness over creativity",
		      "depth": "Production-ready, enterprise-grade solution",
		      "references": "Use Microsoft documentation and established enterprise patterns only"
		    }
		  }
}
```
## 🎯 Objetivo y descripción del proyecto

### Objetivo del proyecto

Sí, se entiende. Reformulación del objetivo del proyecto, en lenguaje natural, concreta y alineada a lo que buscas:
#### Objetivo del proyecto (reformulado)
Desarrollar una extensión de Visual Studio (VSIX clásica) que actúe como plataforma de agentes de IA locales, capaz de conectarse a cualquier LLM que exponga un API server (principalmente LM Studio, pero también JAN u otros), para analizar, modificar y generar código directamente dentro de Visual Studio.
La extensión debe permitir que los modelos de IA:
- Accedan al contexto real del IDE (solución, proyectos, archivos y código).
- Lean y analicen archivos o conjuntos de archivos.
- Propongan e implementen cambios de código de forma controlada.
- Operen como agentes, no como simples chatbots, siguiendo instrucciones y flujos.
- Sean intercambiables, sin acoplar la extensión a un proveedor o modelo concreto.
El proyecto busca construir una base técnica sólida (arquitectura limpia, desacoplada y extensible) que permita:
- Cambiar de LLM sin modificar la UI ni el VSIX.
- Escalar de un MVP con mocks a ejecución real contra APIs locales.
- Soportar Visual Studio 2022 y 2026 de forma estable.
En resumen: una capa de integración entre Visual Studio y modelos de IA locales, orientada a agentes capaces de trabajar sobre código real, con control, trazabilidad y extensibilidad desde el primer diseño.

### Descripción del proyecto
### Descripción del proyecto
Agente IA Local es una extensión clásica de Visual Studio (VSIX) diseñada para integrar modelos de lenguaje grandes (LLM) ejecutados localmente dentro del IDE, permitiendo su uso como agentes inteligentes sobre código real.
La extensión actúa como una capa de orquestación entre Visual Studio y cualquier LLM que exponga un API server local, como LM Studio (principal), JAN u otros, sin acoplarse a un proveedor específico. Los modelos se consumen exclusivamente a través de sus APIs, lo que garantiza intercambiabilidad y control total del entorno.
El agente puede:
- Acceder al contexto completo del IDE (solución, proyectos, archivos).
- Leer y analizar código existente.
- Proponer y aplicar modificaciones de forma controlada.
- Ejecutar flujos de trabajo orientados a tareas reales de desarrollo, no solo conversación.
El proyecto está construido sobre una arquitectura limpia y desacoplada, separando claramente UI, VSIX host, orquestación, contratos y adaptadores de infraestructura. En su fase inicial (MVP), valida el empaquetado VSIX, el registro de comandos y ToolWindows, y el pipeline del agente mediante ejecución mock, dejando preparada la base para integración real con LLM locales.
Agente IA Local no pretende ser un copiloto cerrado, sino una plataforma extensible de agentes de IA locales dentro de Visual Studio, enfocada en control, trazabilidad, compatibilidad y evolución a largo plazo.

## 📅 Plan de Implementación – Agente IA Local (VSIX)
### Sprint 0 — Fundaciones y decisiones técnicas (COMPLETADO)
Objetivo: Eliminar incertidumbre técnica y fijar una base estable.
	1. Selección definitiva de VSIX clásico
Descartar SDK-style y fijar csproj, VSCT y manifest clásicos.
	2. Corrección del entorno de desarrollo
Uso obligatorio de Visual Studio Stable (2022/2026) para build y debug.
	3. Estructura de solución con Clean Architecture
Separación clara entre Core, Application, Infrastructure, UI y VSIX.
	4. Build y empaquetado VSIX funcional
Generación correcta del .vsix e instalación en instancia experimental.
	5. Documentación base del proyecto
README.es, README.en y documento de arquitectura inicial.

### Sprint 1 — Host VSIX funcional mínimo (COMPLETADO)
Objetivo: Tener una extensión visible y ejecutable.
	1. Implementación del AsyncPackage clásico
Inicialización correcta y ciclo de vida controlado.
	2. Registro del comando en VSCT
Comando visible bajo menú Tools.
	3. Alineación estricta de GUIDs
Package, VSCT y vsixmanifest perfectamente sincronizados.
	4. Configuración de depuración y diagnóstico
Experimental Instance y uso de ActivityLog.xml.
	5. Logging base del VSIX
Registro de eventos clave y errores iniciales.

### Sprint 2 — ToolWindow operativa y ciclo de vida
Objetivo: Punto de entrada UI estable.
	1. Registro correcto de la ToolWindow
Uso de ProvideToolWindow en el Package.
	2. Apertura fiable desde el comando
Validación explícita de creación y activación de la ventana.
	3. Threading correcto en VSIX
Uso consistente de JoinableTaskFactory.
	4. Inicialización y refresco del estado UI
Evitar estados inconsistentes al abrir/activar la ToolWindow.
	5. Hardening ante fallos silenciosos
Manejo explícito de errores y logging defensivo.

### Sprint 2.5 — UX Foundations (Visual Studio–first)
Objetivo: UX alineada al ecosistema Visual Studio.
	1. Definición de principios UX para VSIX
No bloqueante, no modal, integrada al IDE.
	2. Diseño del layout base de la ToolWindow
Zonas claras: input, contexto, acciones y salida.
	3. Estados de experiencia bien definidos
Idle, running, success, error.
	4. Convenciones visuales de Visual Studio
Iconografía, espaciados, foco y teclado.
	5. Validación de flujos reales
Ejecución UX sobre tareas concretas (leer archivo, ejecutar agente).

### Sprint 3 — Contratos del agente y pipeline base
Objetivo: Formalizar el concepto de agente.
	1. Contratos del agente en Core
Requests, responses, contexto y resultados.
	2. Pipeline de orquestación en Application
Flujo controlado e independiente de UI/LLM.
	3. Separación explícita de responsabilidades
UI ≠ Agente ≠ Proveedor de IA.
	4. Integración UI → Pipeline → UI
Flujo completo extremo a extremo.
	5. Pruebas unitarias del pipeline
Validación de comportamiento base.

### Sprint 3.5 — UI avanzada y experiencia de agente
Objetivo: Experiencia profesional, no demo.
	1. Interacción por tareas, no chat libre
Acciones claras y repetibles.
	2. Preview visual de resultados del agente
Resúmenes y acciones propuestas.
	3. Feedback progresivo del agente
Estados intermedios y avance visible.
	4. UX defensivo ante errores
Mensajes claros, recuperables.
	5. Consistencia visual y técnica
WPF estable, estilos controlados.

### Sprint 4 — Ejecutor mock determinista
Objetivo: Validar todo sin IA real.
	1. Implementación de Mock LLM Executor
Respuestas JSON predecibles.
	2. Simulación de análisis de código
Lectura y comprensión básica de archivos.
	3. Simulación de propuestas de cambio
Cambios sugeridos, no aplicados.
	4. Visualización clara del resultado
Presentación entendible en la UI.
	5. Logging completo del flujo del agente
Trazabilidad total.

### Sprint 5 — Contexto real de Visual Studio
Objetivo: El agente entiende el IDE.
	1. Lectura de solución y proyectos activos
Uso de APIs del DTE / VS SDK.
	2. Acceso a archivos abiertos y seleccionados
Contexto real de trabajo del usuario.
	3. Construcción del contexto del agente
Paths, contenido y metadatos.
	4. Modo seguro por defecto (solo lectura)
Prevención de modificaciones accidentales.
	5. Tests de integración ligera
Validación dentro del IDE.

### Sprint 6 — Integración con LLM local vía API
Objetivo: Primer agente real con IA local.
	1. Adaptador HTTP genérico para LLMs
Cliente desacoplado del proveedor.
	2. Options Page para configuración
Endpoint, modelo y parámetros.
	3. Compatibilidad inicial con LM Studio
API server local como referencia.
	4. Manejo de errores y timeouts
Robustez ante fallos del modelo.
	5. Sustitución transparente del mock
Sin impacto en UI ni pipeline.

### Sprint 7 — Agente modificador de código
Objetivo: IA que cambia código de forma controlada.
	1. Generación de diffs de código
Cambios claros y auditables.
	2. Preview antes de aplicar cambios
Comparación visual.
	3. Aplicación de cambios vía VS APIs
Integración segura con el editor.
	4. Undo y recuperación
Soporte completo de deshacer.
	5. Trazabilidad de modificaciones
Registro de qué, cuándo y por qué.

### Sprint 8 — Endurecimiento y extensibilidad
Objetivo: Base profesional lista para evolución.
	1. Soporte multi-LLM (JAN, otros)
Sin cambios en UI ni pipeline.
	2. Endurecimiento de threading y lifecycle
Eliminación de warnings y edge cases.
	3. Gestión de sesiones de agente
Contexto persistente controlado.
	4. Tests de regresión
Estabilidad entre versiones.
	5. Preparación para release
Versionado, firma y documentación final.

## 🏁 Resultado final
Una plataforma de agentes de IA locales dentro de Visual Studio, profesional, extensible y alineada con estándares reales de Microsoft, preparada para evolucionar sin rehacer fundamentos.
Si quieres, el siguiente paso natural es definir Definition of Done por sprint o bajar Sprint 2.5 a tareas técnicas concretas en WPF.
