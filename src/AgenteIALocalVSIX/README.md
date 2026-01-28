# AgenteIALocalVSIX

**Layer:** Presentation
**Target:** .NET Framework 4.7.2
**Dependencies:** All projects + MaterialDesignThemes 5.1.0

## Purpose
VSIX extension, UI/XAML, ToolWindows.

## Key Files
- AgenteIALocalVSIXPackage.cs
- ToolWindows/AgenteIALocalControl.xaml (Main Chat Window)
- ToolWindows/AgenteIALocalConfigWindow.xaml (Configuration)
- ToolWindows/AgenteIALocalAboutWindow.xaml (About/Contact - **NEW**)

## Features

### About Window (NEW - v2.7)
- Product information with author photo
- Multi-language support (es-AR, en-US, fr-FR, extensible)
- Third-party libraries credits
- Repository links (GitHub, Docs, Issues)
- **Contact form** with Telegram Bot integration + mailto: fallback
- **Security**: Config ofuscated with AES-256 + PBKDF2 (embedded resource)

### Configuration Window
- Server management (LM Studio, JAN, Ollama, etc.)
- Language selection with flag icons
- Dark theme with Material Design

### Main Chat Window
- Streaming responses
- Multi-chat sessions
- Code syntax highlighting
- Run mode selector (Q&A vs Agent)

## Security

### Telegram Bot Configuration
Contact form uses encrypted configuration to protect sensitive data:
- **File**: `TelegramConfig.txt` (embedded as EmbeddedResource)
- **Algorithm**: AES-256-CBC + PBKDF2 (100k iterations)
- **Key derivation**: Assembly metadata (GUID + Version)
- **Tool**: `tools/EncryptTelegramConfig` (CLI for local encryption)
- **Git safety**: `TelegramConfig.txt` in `.gitignore` (never versioned)

See [TELEGRAM_CONFIG_SECURITY.md](../../docs/TELEGRAM_CONFIG_SECURITY.md) for details.

## Documentation
See [Architecture](../../docs/es/architecture.md#6-agenteialocalvsix)

