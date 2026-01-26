# Agente IA Local — Visual Studio Extension

**[🇪🇸 Español](#español) | [🇬🇧 English](#english)**

---

## Español

### 📋 Descripción

**Agente IA Local** es una extensión VSIX para Visual Studio que integra un chat con agente de IA local directamente en el IDE. Soporta múltiples proveedores de LLM locales (LM Studio, JAN, llama.cpp, Ollama) y ofrece capacidades de agente autónomo con integración profunda con Visual Studio.

**Estado actual:**
- ✅ VSIX instalable
- ✅ ToolWindow operativa
- ✅ Sistema i18n completo (español, inglés, francés + ∞ extensible)
- ✅ Logging con Serilog + UI integrada
- ✅ Clean Architecture implementada
- 🌿 Rama activa: `docs/project-documentation`

### ✨ Características Principales

- 🤖 **Agente IA Local**: Chat integrado con modelos de lenguaje locales
- 🌍 **Internacionalización (i18n)**: Soporte multi-idioma dinámico sin reiniciar VS
- 🔧 **Múltiples Proveedores LLM**: LM Studio, JAN, llama.cpp, Ollama
- 📝 **Logging Avanzado**: Sistema de logging con Serilog + UI integrada
- ⚙️ **Configuración Flexible**: Settings persistentes + configuración visual
- 🏗️ **Clean Architecture**: Arquitectura en capas (Core, Application, Infrastructure, Logging, Localization, VSIX)
- 🎨 **Material Design**: UI moderna con tema oscuro

### 🚀 Inicio Rápido

```bash
# 1. Clonar el repositorio
git clone https://github.com/mdesantis1984/AgenteIALocal.git

# 2. Abrir en Visual Studio 2022 (versión 17.8 o superior)
# 3. Compilar la solución (F5 o Ctrl+Shift+B)
# 4. Se abrirá una Instancia Experimental de Visual Studio
# 5. Abrir: Tools → Agente IA Local
```

### 📚 Documentación

#### 📖 Documentación Principal

| Documento | Descripción |
|-----------|-------------|
| **[📘 Funcional (ES)](src/README.es.md)** | Descripción funcional completa del sistema |
| **[🎨 UX/UI (ES)](src/Readme.UX.md)** | Guía de experiencia de usuario e interfaz |
| **[🧱 Arquitectura (ES)](src/README.architecture.es.md)** | Arquitectura técnica detallada |

#### 📚 Documentación Técnica Detallada

| Documentación | Descripción |
|---------------|-------------|
| **[🏗️ Arquitectura Clean](docs/es/architecture.md)** | Descripción técnica de capas y dependencias |
| **[🌍 Sistema i18n](docs/es/i18n.md)** | Sistema de internacionalización completo |
| **[📝 Sistema Logging](docs/es/logging.md)** | Pipeline de logging con Serilog + UiLogSink |
| **[⚙️ Configuración](docs/es/configuration.md)** | Guía de settings.json + language.json |
| **[👨‍💻 Guía de Desarrollo](docs/es/development.md)** | Guía para contribuidores |
| **[🔧 Troubleshooting](docs/es/troubleshooting.md)** | Resolución de problemas comunes |
| **[🌐 Contribuir Idiomas](docs/LANGUAGE_CONTRIBUTION_GUIDE.md)** | Guía para agregar traducciones |
| **[📋 Reglas de Trabajo](src/Reglas.md)** | Flujo de trabajo y restricciones |

### 🏛️ Estructura del Proyecto

```
AgenteIALocal/
├── src/
│   ├── AgenteIALocal.Core/              # Interfaces, DTOs, entidades de dominio
│   ├── AgenteIALocal.Application/       # Lógica de negocio, servicios
│   ├── AgenteIALocal.Infrastructure/    # HTTP clients, file system, persistencia
│   ├── AgenteIALocal.Logging/           # Pipeline de logging (Serilog)
│   ├── AgenteIALocal.Localization/      # Sistema i18n
│   └── AgenteIALocalVSIX/               # Extensión Visual Studio (UI/MVVM)
├── docs/
│   ├── es/                               # Documentación técnica en español
│   └── en/                               # Technical documentation in English
├── artifacts/                            # Planes, documentación histórica
└── README.md                             # Este archivo
```

### 🔧 Requisitos del Sistema

- **Visual Studio 2022** (versión 17.8 o superior)
- **.NET Framework 4.7.2** (para proyectos legacy)
- **.NET Standard 2.0** (para proyectos compartidos)
- **LLM Local** (opcional): LM Studio, JAN, llama.cpp u Ollama

### 📦 Dependencias Principales

| Paquete | Versión | Uso |
|---------|---------|-----|
| Newtonsoft.Json | 13.0.3 | Serialización JSON (Application, Infrastructure, Localization) |
| Serilog | 4.2.0 | Pipeline de logging |
| MaterialDesignThemes | 5.1.0 | UI moderna con tema oscuro |
| Community.VisualStudio.Toolkit | 17.0 | Helpers para extensiones VS |

### 🤝 Contribuir

¡Las contribuciones son bienvenidas! Por favor lee:

- **[Guía de Desarrollo](docs/es/development.md#contribuir)** — Proceso general de contribución
- **[Guía de Idiomas](docs/LANGUAGE_CONTRIBUTION_GUIDE.md)** — Cómo agregar un nuevo idioma
- **[Reglas de Trabajo](src/Reglas.md)** — Flujo de commits y restricciones

### 📝 Licencia

Este proyecto está bajo la licencia especificada en [LICENSE.md](LICENSE.md).

### 📧 Soporte

- **Issues**: [GitHub Issues](https://github.com/mdesantis1984/AgenteIALocal/issues)
- **Discussions**: [GitHub Discussions](https://github.com/mdesantis1984/AgenteIALocal/discussions)
- **Email**: Ver [PRIVACY.md](PRIVACY.md) para contacto

---

## English

### 📋 Description

**Agente IA Local** (Local AI Agent) is a VSIX extension for Visual Studio that integrates a local AI agent chat directly into the IDE. It supports multiple local LLM providers (LM Studio, JAN, llama.cpp, Ollama) and offers autonomous agent capabilities with deep Visual Studio integration.

**Current status:**
- ✅ Installable VSIX
- ✅ Operational ToolWindow
- ✅ Complete i18n system (Spanish, English, French + ∞ extensible)
- ✅ Logging with Serilog + integrated UI
- ✅ Clean Architecture implemented
- 🌿 Active branch: `docs/project-documentation`

### ✨ Key Features

- 🤖 **Local AI Agent**: Integrated chat with local language models
- 🌍 **Internationalization (i18n)**: Dynamic multi-language support without restarting VS
- 🔧 **Multiple LLM Providers**: LM Studio, JAN, llama.cpp, Ollama
- 📝 **Advanced Logging**: Logging system with Serilog + integrated UI
- ⚙️ **Flexible Configuration**: Persistent settings + visual configuration
- 🏗️ **Clean Architecture**: Layered architecture (Core, Application, Infrastructure, Logging, Localization, VSIX)
- 🎨 **Material Design**: Modern UI with dark theme

### 🚀 Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/mdesantis1984/AgenteIALocal.git

# 2. Open in Visual Studio 2022 (version 17.8 or higher)
# 3. Build the solution (F5 or Ctrl+Shift+B)
# 4. An Experimental Instance of Visual Studio will open
# 5. Open: Tools → Agente IA Local
```

### 📚 Documentation

#### 📖 Main Documentation

| Document | Description |
|----------|-------------|
| **[📘 Functional (EN)](src/README.en.md)** | Complete functional system description |
| **[🧱 Architecture (EN)](src/README.architecture.en.md)** | Detailed technical architecture |

#### 📚 Detailed Technical Documentation

| Documentation | Description |
|---------------|-------------|
| **[🏗️ Clean Architecture](docs/en/architecture.md)** | Technical description of layers and dependencies |
| **[🌍 i18n System](docs/en/i18n.md)** | Complete internationalization system |
| **[📝 Logging System](docs/en/logging.md)** | Logging pipeline with Serilog + UiLogSink |
| **[⚙️ Configuration](docs/en/configuration.md)** | Guide to settings.json + language.json |
| **[👨‍💻 Development Guide](docs/en/development.md)** | Guide for contributors |
| **[🔧 Troubleshooting](docs/en/troubleshooting.md)** | Common problems resolution |
| **[🌐 Language Contributions](docs/LANGUAGE_CONTRIBUTION_GUIDE.md)** | Guide to add translations |

### 🏛️ Project Structure

```
AgenteIALocal/
├── src/
│   ├── AgenteIALocal.Core/              # Interfaces, DTOs, domain entities
│   ├── AgenteIALocal.Application/       # Business logic, services
│   ├── AgenteIALocal.Infrastructure/    # HTTP clients, file system, persistence
│   ├── AgenteIALocal.Logging/           # Logging pipeline (Serilog)
│   ├── AgenteIALocal.Localization/      # i18n system
│   └── AgenteIALocalVSIX/               # Visual Studio Extension (UI/MVVM)
├── docs/
│   ├── es/                               # Technical documentation in Spanish
│   └── en/                               # Technical documentation in English
├── artifacts/                            # Plans, historical documentation
└── README.md                             # This file
```

### 🔧 System Requirements

- **Visual Studio 2022** (version 17.8 or higher)
- **.NET Framework 4.7.2** (for legacy projects)
- **.NET Standard 2.0** (for shared projects)
- **Local LLM** (optional): LM Studio, JAN, llama.cpp or Ollama

### 📦 Main Dependencies

| Package | Version | Usage |
|---------|---------|-------|
| Newtonsoft.Json | 13.0.3 | JSON serialization (Application, Infrastructure, Localization) |
| Serilog | 4.2.0 | Logging pipeline |
| MaterialDesignThemes | 5.1.0 | Modern UI with dark theme |
| Community.VisualStudio.Toolkit | 17.0 | Helpers for VS extensions |

### 🤝 Contributing

Contributions are welcome! Please read:

- **[Development Guide](docs/en/development.md#contributing)** — General contribution process
- **[Language Guide](docs/LANGUAGE_CONTRIBUTION_GUIDE.md)** — How to add a new language

### 📝 License

This project is licensed under the license specified in [LICENSE.md](LICENSE.md).

### 📧 Support

- **Issues**: [GitHub Issues](https://github.com/mdesantis1984/AgenteIALocal/issues)
- **Discussions**: [GitHub Discussions](https://github.com/mdesantis1984/AgenteIALocal/discussions)
- **Email**: See [PRIVACY.md](PRIVACY.md) for contact

---

**Made with ❤️ by the AgenteIALocal Team**
