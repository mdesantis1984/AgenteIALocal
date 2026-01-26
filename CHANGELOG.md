# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Complete bilingual documentation (ES/EN)
- i18n system (Spanish, English, French + extensible)
- Serilog logging pipeline
- Clean Architecture implementation
- Material Design UI

## [2.6-i18n-ui.6] - 2026-01-26

### Added
- Dynamic language switching (no restart required)
- 67 flag PNG images (h40)
- TranslateExtension for XAML
- Hot reload support (FileSystemWatcher)
- Language contribution guide

### Fixed
- Round-trip preservation in settings.json
- BaseUrl normalization (double schema fix)
- ComboBox Tag invariant pattern

## [2.4-serilog.4] - 2026-01-20

### Added
- Serilog pipeline with File + UI sinks
- Rolling log files (3 MB limit)
- UI log panel (250 lines buffer)
- Dynamic log level configuration

### Removed
- Legacy logging system (AgentLoggerV2, etc.)

## [2.2] - 2026-01-18

### Added
- Provider configuration UI
- RequestDefaults (temperature, maxTokens, etc.)
- Agent behavior settings (ideIntegration, applyChanges, maxSteps)

## [1.0] - 2025

### Added
- Initial VSIX release
- LM Studio + JAN support
- Basic chat UI

---

**Format:** Based on [Keep a Changelog](https://keepachangelog.com/)
