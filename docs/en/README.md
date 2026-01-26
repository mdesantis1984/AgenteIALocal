# Technical Documentation — Agente IA Local

**Language:** [🇪🇸 Español](../es/README.md) | 🇬🇧 English

---

## 📚 General Index

This is the complete technical documentation for the **Agente IA Local** project - A Visual Studio extension that integrates a local AI agent with Clean Architecture.

### 🏗️ Architecture

| Document | Description |
|----------|-------------|
| **[General Architecture](architecture.md)** | Complete description of Clean Architecture, layers, dependencies and design patterns |

### 🌍 Internationalization System (i18n)

| Document | Description |
|----------|-------------|
| **[i18n System](i18n.md)** | Complete internationalization system: LocalizationService, TranslateExtension, FileSystemWatcher, hot reload |
| **[Language Contribution Guide](../LANGUAGE_CONTRIBUTION_GUIDE.md)** | Guide for external contributors who want to add translations |

### 📝 Logging System

| Document | Description |
|----------|-------------|
| **[Logging System](logging.md)** | Logging pipeline with Serilog, UiLogSink, formatters, sinks and dynamic configuration |

### ⚙️ Configuration

| Document | Description |
|----------|-------------|
| **[Configuration Guide](configuration.md)** | Complete configuration: settings.json, language.json, persistence, defaults |

### 👨‍💻 Development

| Document | Description |
|----------|-------------|
| **[Development Guide](development.md)** | Guide for developers: setup, build, debug, testing, contributing |

### 🔧 Troubleshooting

| Document | Description |
|----------|-------------|
| **[Problem Resolution](troubleshooting.md)** | Common problems and solutions: build errors, runtime, configuration |

---

## 📖 Functional Documentation

In addition to this technical documentation, there is functional and UX documentation:

- **[📘 Functional (EN)](../../src/README.en.md)** — Complete functional system description
- **[🧱 Architecture (EN)](../../src/README.architecture.en.md)** — Technical architecture from functional perspective

---

## 🏛️ System Projects

The system is divided into 6 projects following Clean Architecture:

### Core Layer
- **[AgenteIALocal.Core](../../src/AgenteIALocal.Core/README.md)** — Interfaces, DTOs, domain entities

### Application Layer
- **[AgenteIALocal.Application](../../src/AgenteIALocal.Application/README.md)** — Business logic, services, JSON ↔ DTOs conversion

### Infrastructure Layer
- **[AgenteIALocal.Infrastructure](../../src/AgenteIALocal.Infrastructure/README.md)** — HTTP clients (LmStudioClient, JanClient), file system, persistence

### Cross-Cutting Concerns
- **[AgenteIALocal.Logging](../../src/AgenteIALocal.Logging/README.md)** — Logging pipeline (Serilog + UiLogSink)
- **[AgenteIALocal.Localization](../../src/AgenteIALocal.Localization/README.md)** — i18n system (LocalizationService, TranslateExtension)

### Presentation Layer
- **[AgenteIALocalVSIX](../../src/AgenteIALocalVSIX/README.md)** — Visual Studio Extension (UI/MVVM, ToolWindow)

---

## 📋 Development Plans

Historical documentation of development plans:

| Plan | Description | Status |
|------|-------------|--------|
| **[PLAN_IDIOMA_1.0](../plans/PLAN_IDIOMA_1.0.md)** | Complete i18n system (44 tasks) | ✅ 100% Completed |
| **[PLAN_SERILOG_1.3](../plans/PLAN_SERILOG_1.3.md)** | Logging system with Serilog | ✅ Completed |
| **[PLAN_Log_Configuracion_1.0](../plans/PLAN_Log_Configuracion_1.0.md)** | Logging UI configuration | ✅ Completed |
| **[PLAN_Provider_Configuracion_2.5](../plans/PLAN_Provider_Configuracion_2.5.md)** | LLM providers configuration | ✅ Completed |

---

## 🔗 External Links

- **[GitHub Repository](https://github.com/mdesantis1984/AgenteIALocal)** — Official repository
- **[Issues](https://github.com/mdesantis1984/AgenteIALocal/issues)** — Report problems
- **[Discussions](https://github.com/mdesantis1984/AgenteIALocal/discussions)** — Community discussions

---

## 📄 Other Documents

- **[PRIVACY.md](../../PRIVACY.md)** — Privacy policy and data handling
- **[LICENSE.md](../../LICENSE.md)** — Project license
- **[CHANGELOG.md](../../CHANGELOG.md)** — Change history

---

**🏠 [Back to main README](../../README.md)**
