# Documentación Técnica — Agente IA Local

**Idioma:** 🇪🇸 Español | [🇬🇧 English](../en/README.md)

---

## 📚 Índice General

Esta es la documentación técnica completa del proyecto **Agente IA Local** - Una extensión de Visual Studio que integra un agente de IA local con arquitectura Clean Architecture.

### 🏗️ Arquitectura

| Documento | Descripción |
|-----------|-------------|
| **[Arquitectura General](architecture.md)** | Descripción completa de la arquitectura Clean Architecture, capas, dependencias y patrones de diseño |

### 🌍 Sistema de Internacionalización (i18n)

| Documento | Descripción |
|-----------|-------------|
| **[Sistema i18n](i18n.md)** | Sistema de internacionalización completo: LocalizationService, TranslateExtension, FileSystemWatcher, hot reload |
| **[Guía de Contribución de Idiomas](../../artifacts/LANGUAGE_CONTRIBUTION_GUIDE.md)** | Guía para contributors externos que desean agregar traducciones |

### 📝 Sistema de Logging

| Documento | Descripción |
|-----------|-------------|
| **[Sistema Logging](logging.md)** | Pipeline de logging con Serilog, UiLogSink, formatters, sinks y configuración dinámica |

### ⚙️ Configuración

| Documento | Descripción |
|-----------|-------------|
| **[Guía de Configuración](configuration.md)** | Configuración completa: settings.json, language.json, persistencia, defaults |

### 👨‍💻 Desarrollo

| Documento | Descripción |
|-----------|-------------|
| **[Guía de Desarrollo](development.md)** | Guía para desarrolladores: setup, build, debug, testing, contribuir |
| **[Reglas de Trabajo](../../src/Reglas.md)** | Flujo de trabajo, commits, restricciones de archivos VSIX críticos |

### 🔧 Troubleshooting

| Documento | Descripción |
|-----------|-------------|
| **[Resolución de Problemas](troubleshooting.md)** | Problemas comunes y soluciones: errores de build, runtime, configuración |

---

## 📖 Documentación Funcional

Además de esta documentación técnica, existe documentación funcional y de UX:

- **[📘 Funcional (ES)](../../src/README.es.md)** — Descripción funcional completa del sistema
- **[🎨 UX/UI (ES)](../../src/Readme.UX.md)** — Guía de experiencia de usuario e interfaz
- **[🧱 Arquitectura (ES)](../../src/README.architecture.es.md)** — Arquitectura técnica desde perspectiva funcional

---

## 🏛️ Proyectos del Sistema

El sistema está dividido en 6 proyectos siguiendo Clean Architecture:

### Core Layer
- **[AgenteIALocal.Core](../../src/AgenteIALocal.Core/README.md)** — Interfaces, DTOs, entidades de dominio

### Application Layer
- **[AgenteIALocal.Application](../../src/AgenteIALocal.Application/README.md)** — Lógica de negocio, servicios, conversión JSON ↔ DTOs

### Infrastructure Layer
- **[AgenteIALocal.Infrastructure](../../src/AgenteIALocal.Infrastructure/README.md)** — HTTP clients (LmStudioClient, JanClient), file system, persistencia

### Cross-Cutting Concerns
- **[AgenteIALocal.Logging](../../src/AgenteIALocal.Logging/README.md)** — Pipeline de logging (Serilog + UiLogSink)
- **[AgenteIALocal.Localization](../../src/AgenteIALocal.Localization/README.md)** — Sistema i18n (LocalizationService, TranslateExtension)

### Presentation Layer
- **[AgenteIALocalVSIX](../../src/AgenteIALocalVSIX/README.md)** — Extensión Visual Studio (UI/MVVM, ToolWindow)

---

## 📋 Planes de Desarrollo

Documentación histórica de planes de desarrollo:

| Plan | Descripción | Estado |
|------|-------------|--------|
| **[PLAN_IDIOMA_1.0](../../artifacts/Plan_14-01-2026/PLAN_IDIOMA/PLAN_IDIOMA_1.0.md)** | Sistema i18n completo (44 tareas) | ✅ 100% Completado |
| **[PLAN_SERILOG_1.3](../../artifacts/Plan_14-01-2026/PLAN_SERILOG/PLAN_SERILOG_1.3.md)** | Sistema logging con Serilog | ✅ Completado |
| **[PLAN_LOG_CONFIG_1.0](../../artifacts/Plan_14-01-2026/PLAN_LOG_CONFIG/PLAN_LOG_CONFIG_1.0.md)** | Configuración UI de logging | ✅ Completado |
| **[PLAN_Provider_Configuracion_CONSOLIDATED_2.5](../../artifacts/Plan_14-01-2026/PLAN_Provider_Configuracion/)** | Configuración providers LLM | ✅ Completado |

---

## 🔗 Enlaces Externos

- **[GitHub Repository](https://github.com/mdesantis1984/AgenteIALocal)** — Repositorio oficial
- **[Issues](https://github.com/mdesantis1984/AgenteIALocal/issues)** — Reportar problemas
- **[Discussions](https://github.com/mdesantis1984/AgenteIALocal/discussions)** — Discusiones de la comunidad

---

## 📄 Otros Documentos

- **[PRIVACY.md](../../PRIVACY.md)** — Política de privacidad y manejo de datos
- **[LICENSE.md](../../LICENSE.md)** — Licencia del proyecto
- **[CHANGELOG.md](../../CHANGELOG.md)** — Historial de cambios

---

**🏠 [Volver al README principal](../../README.md)**
