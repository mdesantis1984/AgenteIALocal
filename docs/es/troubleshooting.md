# Troubleshooting

**[🇪🇸 Español](#) | [🇬🇧 English](../en/troubleshooting.md)**

---

## 🔧 Problemas Comunes

### 1. VSIX no carga

**Síntoma:** Error "El paquete no se cargó correctamente"

**Solución:**
```
1. Verificar: logs en %LOCALAPPDATA%\AgenteIALocal\logs\
2. Reinstalar VSIX
3. Limpiar: C:\Users\{User}\AppData\Local\Microsoft\VisualStudio\17.0_*\ComponentModelCache
```

### 2. Idioma no cambia

**Síntoma:** UI sigue en español después de seleccionar inglés

**Solución:**
```
1. Verificar: %LOCALAPPDATA%\AgenteIALocal\language.json
2. Verificar: languages/en-US/strings.json existe
3. Reiniciar Visual Studio
```

### 3. LLM no responde

**Síntoma:** Request timeout o error de conexión

**Solución:**
```
1. Verificar: LM Studio/JAN está ejecutándose
2. Verificar: BaseUrl correcto (http://127.0.0.1:1234)
3. Verificar: modelo cargado en LM Studio
4. Check logs: %LOCALAPPDATA%\AgenteIALocal\logs\
```

### 4. Settings no persisten

**Síntoma:** Configuración se pierde al cerrar VS

**Solución:**
```
1. Verificar: %LOCALAPPDATA%\AgenteIALocal\settings.json existe
2. Verificar: permisos de escritura en carpeta
3. Click "Guardar" en Config modal
```

---

## 📝 Logs

**Ubicación:** `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`

```bash
# Ver últimas 50 líneas
Get-Content $env:LOCALAPPDATA\AgenteIALocal\logs\AgenteIALocal.log -Tail 50
```

---

## 📚 Referencias

- **[Sistema Logging](logging.md)** — Configuración de logs
- **[Configuración](configuration.md)** — Settings correctos

---

**🏠 [Volver al índice](README.md)**
