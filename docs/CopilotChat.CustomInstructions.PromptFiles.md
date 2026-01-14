# Copilot Chat — Instrucciones personalizadas y archivos de prompt

## 1) Instrucciones globales del repo

- Archivo recomendado: `.github/copilot-instructions.md`
- Contiene reglas reutilizables que Copilot adjunta como referencia cuando están habilitadas en Visual Studio.

## 2) Instrucciones por tipo de archivo (scoped)

- Carpeta: `.github/instructions/`
- Extensión: `*.instructions.md`
- Formato: frontmatter YAML con:
  - `description`: texto visible al pasar el mouse
  - `applyTo`: glob de archivos/carpetas donde aplica

Ejemplo mínimo:

```yaml
---
description: "Convenciones C#"
applyTo: "**/*.cs"
---
- Regla 1
- Regla 2
```

## 3) Archivos de prompt reutilizables

- Carpeta: `.github/prompts/`
- Extensión: `*.prompt.md`
- Uso: en el chat escribe `#prompt:` y selecciona el archivo (o adjúntalo con ➕).

Recomendación: almacenar prompts operativos en **JSON** (como en este repositorio).
