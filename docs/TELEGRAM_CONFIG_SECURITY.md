# Seguridad de Configuración Telegram - AgenteIALocal VSIX

**ID: 20260126_184300**  
**Estrategia:** Ofuscación AES-256 con clave derivada de Assembly

---

## 🔒 Problema de Seguridad

El form de contacto en About Window envía mensajes a un bot de Telegram, lo que requiere:
- **Bot Token** (secret key de Telegram)
- **Chat ID** (destino de los mensajes)

**Desafío:** Estos datos deben estar disponibles en TODAS las instalaciones de la VSIX, pero NO pueden estar en el código fuente público de GitHub.

---

## ✅ Solución Implementada

### Arquitectura

```
TelegramConfig.txt (local)           AboutWindow.xaml.cs
        ↓                                    ↓
  [ofuscado AES-256]            SecureConfigReader.ReadTelegramConfig()
        ↓                                    ↓
  Embedded Resource              Desencripta en runtime
        ↓                                    ↓
    VSIX Package                 POST a Telegram API
```

### Tecnología

- **Algoritmo:** AES-256-CBC
- **Clave:** Derivada de Assembly metadata (GUID + Version + PublicKeyToken)
- **Derivación:** PBKDF2 con 100,000 iteraciones
- **Password base:** Ofuscado con XOR en código fuente
- **Seguridad:** Security through obscurity (adecuado para VSIX distribuido)

---

## 🛠️ Uso

### Paso 1: Configurar datos localmente

**Ejecutar herramienta de encriptación:**

```powershell
cd F:\Proyectos\ThisCloudServices\03-Repo\AgenteIALocalVSIX\AgenteIALocal
dotnet run --project tools\EncryptTelegramConfig\EncryptTelegramConfig.csproj
```

**Ingresar datos:**
```
Bot Token: [TU_BOT_TOKEN_AQUI]
Chat ID: [TU_CHAT_ID_AQUI]
```

**Ejemplo (datos falsos):**
```
Bot Token: 123456789:ABCdefGHIjklMNOpqrsTUVwxyz
Chat ID: 9876543210
```

**Copiar output encriptado**.

---

### Paso 2: Actualizar TelegramConfig.txt

**Archivo:** `src/AgenteIALocalVSIX/TelegramConfig.txt`

**Reemplazar TODO el contenido** con el texto encriptado generado por la herramienta.

**Ejemplo (texto encriptado FALSO - NO USAR):**
```
A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8S9T0U1V2W3X4Y5Z6A7B8C9D0E1F2G3H4I5J6K7L8M9N0O1P2Q3R4S5T6U7V8==
```

**IMPORTANTE:** El ejemplo anterior es FICTICIO. Usa SOLO el output de tu herramienta local.

---

### Paso 3: Build VSIX

```powershell
dotnet build src\AgenteIALocalVSIX\AgenteIALocalVSIX.csproj
```

El archivo `TelegramConfig.txt` se **embebe ofuscado** en la DLL como `EmbeddedResource`.

---

### Paso 4: Verificar ofuscación

**Runtime (AboutWindow):**
```csharp
var assembly = Assembly.GetExecutingAssembly();
var (botToken, chatId) = SecureConfigReader.ReadTelegramConfig(assembly);
// botToken = "[DECRYPTED_TOKEN]" (ejemplo: "123456789:ABCdef...")
// chatId = "[DECRYPTED_CHAT_ID]" (ejemplo: "9876543210")
```

**Decompilador (ILSpy/dnSpy):**
- TelegramConfig.txt visible como recurso → Contenido Base64 ilegible ✅
- Password base ofuscado con XOR → No visible como string literal ✅

**NOTA:** Los ejemplos en comentarios son FICTICIOS para ilustrar el formato de salida.

---

## 🚫 Seguridad en Git

### .gitignore configurado

```gitignore
# CRÍTICO: NUNCA versionar el archivo con contenido real
src/AgenteIALocalVSIX/TelegramConfig.txt
```

### Archivo placeholder en repo

```
PLACEHOLDER_ENCRYPTED_CONFIG
# Instrucciones para desarrolladores...
```

**Workflow:**
1. Repositorio tiene archivo **placeholder dummy**
2. Developer clona → Archivo placeholder (no funcional)
3. Developer ejecuta tool → Genera **config ofuscado real**
4. Developer reemplaza archivo **localmente** (en .gitignore)
5. Build VSIX → Config ofuscado embebido ✅
6. Commit → Solo código (sin config real) ✅

---

## 🔓 Nivel de Seguridad

### ✅ Protege contra:
- Inspección superficial del código fuente ✅
- Búsqueda de strings en GitHub ✅
- Lectura accidental del token en archivos versionados ✅

### ⚠️ NO protege contra:
- Ingeniería reversa dedicada (decompilador + debugger)
- Extracción desde DLL con herramientas especializadas
- Memory dump en runtime

### Veredicto

**Adecuado para VSIX open source** donde:
- Token no es crítico para seguridad del sistema
- Abuse puede mitigarse (rate limiting en Telegram, revocar token)
- Alternativa (backend proxy) sería overkill

**Comparable a:** API keys en apps móviles (React Native, Flutter)

---

## 📝 Archivos del Sistema

| Archivo | Propósito | Versionado |
|---------|-----------|-----------|
| `src/AgenteIALocal.Infrastructure/Security/SecureConfigReader.cs` | Lógica AES-256 | ✅ Sí |
| `src/AgenteIALocalVSIX/TelegramConfig.txt` | Config ofuscado (local) | ❌ No (.gitignore) |
| `tools/EncryptTelegramConfig/Program.cs` | CLI encriptador | ✅ Sí |
| `tools/EncryptTelegramConfig/EncryptTelegramConfig.csproj` | Proyecto tool | ✅ Sí |
| `docs/TELEGRAM_CONFIG_SECURITY.md` | Esta documentación | ✅ Sí |

---

## 🔄 Cambiar Token/ChatId

Si el token se compromete o necesitas cambiar el destinatario:

1. **Regenerar bot token:** @BotFather → `/token` → Revoke
2. **Ejecutar tool:** `dotnet run --project tools\EncryptTelegramConfig\EncryptTelegramConfig.csproj`
3. **Ingresar nuevos datos**
4. **Reemplazar** `TelegramConfig.txt`
5. **Rebuild VSIX**
6. **Redistribuir** (publicar nueva versión en Marketplace)

**NO** necesitas cambiar código fuente.

---

## 🎯 Referencias Técnicas

- **Clase:** `AgenteIALocal.Infrastructure.Security.SecureConfigReader`
- **Método principal:** `ReadTelegramConfig(Assembly)`
- **Uso:** `AgenteIALocalAboutWindow.xaml.cs` línea ~1280
- **Algoritmo:** `System.Security.Cryptography.Aes`
- **Derivación:** `System.Security.Cryptography.Rfc2898DeriveBytes`

---

## ⚡ Alternativas Consideradas

| Solución | Pros | Cons | Seleccionada |
|----------|------|------|--------------|
| **Backend Proxy (Vercel)** | Seguridad 100%, cambio sin rebuild | Requiere internet, setup externo | ❌ No |
| **Variables entorno** | Estándar CI/CD | Incómodo para usuarios finales | ❌ No |
| **Ofuscación AES-256** | Simple, offline, adecuado para VSIX | Reversible con esfuerzo | ✅ **Sí** |
| **Settings.json local** | No embebido | Cada usuario tendría config vacío | ❌ No |

---

**Autor:** Sistema de Seguridad AgenteIALocal  
**Fecha:** 2026-01-26  
**Versión:** 1.0
