# Copilot Chat en Visual Studio — Cómo usa contexto (resumen operativo)

## Contexto implícito (siempre presente)

- Texto seleccionado en el editor activo.
- Archivo activo en el editor.
- Copilot puede leer contenido del archivo activo según el prompt; para incluir otro archivo, adjuntarlo con ➕ o referenciarlo explícitamente.

## Contexto de solución

Copilot construye contexto a partir de la solución abierta (proyectos, archivos y configuración). En repos alojados en GitHub/Azure DevOps puede usar indexación remota; en otros casos, indexación local.

## Recomendación para este repositorio

En modo Agente, para tareas multiarchivo:
1) empezar con `@workspace`  
2) agregar referencias explícitas con `#Archivo.cs` / `#Clase` / `#Metodo`  
3) si hay errores de build/runtime, adjuntar `#output` (logs de salida).
