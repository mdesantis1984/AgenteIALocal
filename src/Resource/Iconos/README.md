# Exportar iconos desde DLL a Markdown

Este workspace contiene un script para extraer **todos los iconos embebidos** en DLLs y generar una galería en Markdown para visualizarlos.

## Requisitos
- Windows
- PowerShell 5.1 o PowerShell 7+

## Uso

Extraer iconos de todas las DLLs de la carpeta actual:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Export-DllIcons.ps1 -InputPath . -OutputPath .\out
```

Incluir subcarpetas:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Export-DllIcons.ps1 -InputPath . -OutputPath .\out -Recurse
```

Procesar una DLL concreta:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Export-DllIcons.ps1 -InputPath .\tu.dll -OutputPath .\out
```

## Resultado
- `out/icons.md`: galería con vista previa en tamaño medio y enlaces a todos los tamaños extraídos.
- `out/images/...`: PNGs generados.

> Nota: “estilos” aquí se representan como **tamaños** (16/20/24/32/40/48/64/96/128/256). Si una DLL no tiene un tamaño concreto, ese enlace no aparece.
