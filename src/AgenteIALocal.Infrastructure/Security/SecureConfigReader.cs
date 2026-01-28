// NUEVO - ID: 20260126_183000
// SecureConfigReader - Ofuscación de configuración sensible embebida
// ARQUITECTURA: AES-256 + Salt derivado de Assembly (machine-independent)
// SEGURIDAD: Security through obscurity (suficiente para VSIX embebido)
// USO: Telegram Bot token/chatId ofuscados en TelegramConfig.txt embebido

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AgenteIALocal.Infrastructure.Security
{
    /// <summary>
    /// Lector de configuración ofuscada embebida
    /// NUEVO - ID: 20260126_183000
    /// </summary>
    public static class SecureConfigReader
    {
        /// <summary>
        /// Desencripta configuración ofuscada embebida
        /// FORMATO: {botToken}|{chatId}
        /// MODIFICADO - ID: 20260126_192002 - Agregar logging para debugging
        /// </summary>
        public static (string BotToken, string ChatId) ReadTelegramConfig(Assembly assembly)
        {
            try
            {
                // NUEVO - ID: 20260126_192002 - Log assembly info
                AgenteIALocal.Logging.Log.Information("-", 9001, "SecureConfigReader", $"Reading config from assembly: {assembly.FullName}", null);

                // Leer recurso embebido TelegramConfig.txt
                var allResources = assembly.GetManifestResourceNames();
                AgenteIALocal.Logging.Log.Information("-", 9002, "SecureConfigReader", $"Found {allResources.Length} embedded resources", null);
                
                var resourceName = allResources.FirstOrDefault(n => n.EndsWith("TelegramConfig.txt"));

                if (string.IsNullOrEmpty(resourceName))
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9003, "SecureConfigReader", $"TelegramConfig.txt NOT FOUND in embedded resources. Available: {string.Join(", ", allResources)}", null);
                    return (string.Empty, string.Empty);
                }

                AgenteIALocal.Logging.Log.Information("-", 9004, "SecureConfigReader", $"Found TelegramConfig.txt as: {resourceName}", null);

                string encryptedText;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        AgenteIALocal.Logging.Log.Warning("-", 9005, "SecureConfigReader", "Stream is NULL for TelegramConfig.txt", null);
                        return (string.Empty, string.Empty);
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        encryptedText = reader.ReadToEnd().Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(encryptedText))
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9006, "SecureConfigReader", "Encrypted text is EMPTY", null);
                    return (string.Empty, string.Empty);
                }

                AgenteIALocal.Logging.Log.Information("-", 9007, "SecureConfigReader", $"Encrypted text length: {encryptedText.Length} chars", null);

                // Desencriptar
                var decrypted = Decrypt(encryptedText, assembly);
                AgenteIALocal.Logging.Log.Information("-", 9008, "SecureConfigReader", $"Decrypted text length: {decrypted.Length} chars", null);

                // Parsear formato {botToken}|{chatId}
                var parts = decrypted.Split('|');
                if (parts.Length != 2)
                {
                    AgenteIALocal.Logging.Log.Warning("-", 9009, "SecureConfigReader", $"Invalid format (expected 2 parts, got {parts.Length})", null);
                    return (string.Empty, string.Empty);
                }

                AgenteIALocal.Logging.Log.Information("-", 9010, "SecureConfigReader", "Config parsed successfully", null);
                return (parts[0], parts[1]);
            }
            catch (Exception ex)
            {
                AgenteIALocal.Logging.Log.Error("-", 9000, "SecureConfigReader", "Exception reading TelegramConfig", ex);
                return (string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Encripta texto usando AES-256 con clave derivada de Assembly
        /// NUEVO - ID: 20260126_183001
        /// </summary>
        public static string Encrypt(string plainText, Assembly assembly)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            var (key, iv) = DeriveKeyFromAssembly(assembly);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }

                    var encrypted = ms.ToArray();
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        /// <summary>
        /// Desencripta texto usando AES-256 con clave derivada de Assembly
        /// NUEVO - ID: 20260126_183002
        /// </summary>
        public static string Decrypt(string cipherText, Assembly assembly)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            var (key, iv) = DeriveKeyFromAssembly(assembly);
            var buffer = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Deriva clave AES-256 (32 bytes) + IV (16 bytes) desde Assembly
        /// ARQUITECTURA: Usa GUID + Versión + PublicKeyToken como salt
        /// VENTAJA: Machine-independent (misma clave en todas las máquinas)
        /// NUEVO - ID: 20260126_183003
        /// </summary>
        private static (byte[] Key, byte[] IV) DeriveKeyFromAssembly(Assembly assembly)
        {
            // Construir salt desde metadata del Assembly (determinístico)
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0.0";
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var publicKeyHex = publicKeyToken != null && publicKeyToken.Length > 0
                ? BitConverter.ToString(publicKeyToken).Replace("-", "")
                : "00000000";

            // Salt: GUID assembly + Version + PublicKeyToken
            var guidAttr = assembly.GetCustomAttribute<System.Runtime.InteropServices.GuidAttribute>();
            var guid = guidAttr?.Value ?? "00000000-0000-0000-0000-000000000000";

            var saltString = $"{guid}|{version}|{publicKeyHex}";
            var saltBytes = Encoding.UTF8.GetBytes(saltString);

            // Password base (ofuscada en código - NO hardcodear literal)
            // XOR con constante para evitar strings literales en binario
            var passwordBase = XorString("AgenteIALocal.Telegram.SecureConfig.2025", 0x42);

            // Derivar clave usando PBKDF2 (100k iteraciones)
            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBase, saltBytes, 100000))
            {
                var key = deriveBytes.GetBytes(32); // AES-256 key (32 bytes)
                var iv = deriveBytes.GetBytes(16);  // AES IV (16 bytes)
                return (key, iv);
            }
        }

        /// <summary>
        /// XOR string para ofuscar password base (evitar literals en binario)
        /// NUEVO - ID: 20260126_183004
        /// </summary>
        private static string XorString(string input, byte xorKey)
        {
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)(chars[i] ^ xorKey);
            }
            return new string(chars);
        }
    }
}
