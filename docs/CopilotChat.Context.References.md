# Copilot Chat — Referencias de contexto (Visual Studio)

## Referenciar archivos y símbolos

- Archivo: `#NombreArchivo.cs` (elige desde el autocompletado tras escribir `#`).
- Método / clase / función: `#NombreMetodo` / `#NombreClase`.

## Referenciar solución (workspace)

- Solución actual abierta: `@workspace`  
  Úsalo para preguntas sobre "dónde está X", arquitectura, dependencias, o cambios coordinados en varios archivos.

## Referenciar logs de salida

- Ventana de salida como contexto: `#output`  
  Útil para errores de build, debug, tests, control de código fuente, etc.

## Referenciar URLs e imágenes

- Pega una URL pública en el prompt para que Copilot use el HTML estático como contexto.
- Adjunta imágenes (VS 17.14+) cuando el objetivo sea UI, diagramas, o errores visuales.

## Buenas prácticas (rápidas)

- No dependas de contexto implícito; agrega referencias explícitas.
- Usa hilos (threads) diferentes por tarea para no contaminar contexto.
