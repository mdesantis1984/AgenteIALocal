# EncryptTelegramConfig Tool

**Herramienta CLI para ofuscar configuración de Telegram Bot**

## Uso

```powershell
cd F:\Proyectos\ThisCloudServices\03-Repo\AgenteIALocalVSIX\AgenteIALocal
dotnet run --project tools\EncryptTelegramConfig\EncryptTelegramConfig.csproj
```

## Input

```
Bot Token: [Ingresá tu bot token de @BotFather]
Chat ID: [Ingresá tu chat ID]
```

**Ejemplo (datos falsos):**
```
Bot Token: 123456789:ABCdefGHIjklMNOpqrsTUVwxyz
Chat ID: 9876543210
```

## Output

```
=== RESULTADO ENCRIPTADO ===
[TEXTO_BASE64_ENCRIPTADO]

INSTRUCCIONES:
1. Copiar el texto encriptado
2. REEMPLAZAR todo el contenido de src/AgenteIALocalVSIX/TelegramConfig.txt
3. Build VSIX
4. El archivo encriptado se embebe en la DLL
```

## Algoritmo

- **AES-256-CBC**
- **PBKDF2** con 100,000 iteraciones
- Clave derivada de Assembly metadata (GUID + Version + PublicKeyToken)
- Password base ofuscado con XOR

## Seguridad

Ver [TELEGRAM_CONFIG_SECURITY.md](../../docs/TELEGRAM_CONFIG_SECURITY.md)
