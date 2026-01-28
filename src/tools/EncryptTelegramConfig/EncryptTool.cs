// MODIFICADO - ID: 20260126_193000
// EncryptTool - Genera config ofuscado usando Assembly VSIX REAL
// FIX: Usa el mismo algoritmo de derivación que SecureConfigReader
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AgenteIALocal.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Telegram Config Encryptor ===");
            Console.WriteLine();

            Console.Write("Bot Token: ");
            var botToken = Console.ReadLine();

            Console.Write("Chat ID: ");
            var chatId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                Console.WriteLine("ERROR: Token y ChatId son obligatorios");
                return;
            }

            var plainText = $"{botToken}|{chatId}";
            
            // MODIFICADO - ID: 20260126_193002 - Path desde raíz del repo
            var vsixDllPath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "src", "AgenteIALocalVSIX", "bin", "Debug", "AgenteIALocalVSIX.dll"
            );
            
            if (!File.Exists(vsixDllPath))
            {
                Console.WriteLine($"ERROR: AgenteIALocalVSIX.dll no encontrado");
                Console.WriteLine($"Path buscado: {vsixDllPath}");
                Console.WriteLine($"Current dir: {Directory.GetCurrentDirectory()}");
                Console.WriteLine("SOLUCIÓN: Compilar VSIX primero (F5 o dotnet build)");
                return;
            }

            Console.WriteLine($"Cargando assembly: {vsixDllPath}");
            var vsixAssembly = Assembly.LoadFrom(vsixDllPath);
            Console.WriteLine($"Assembly cargado: {vsixAssembly.FullName}");

            var encrypted = Encrypt(plainText, vsixAssembly);

            Console.WriteLine();
            Console.WriteLine("=== RESULTADO ENCRIPTADO ===");
            Console.WriteLine(encrypted);
            Console.WriteLine();
            
            // Actualizar TelegramConfig.txt
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "AgenteIALocalVSIX", "TelegramConfig.txt");
            
            if (File.Exists(configPath))
            {
                File.WriteAllText(configPath, encrypted);
                Console.WriteLine($"✅ Archivo actualizado: {configPath}");
            }
            else
            {
                Console.WriteLine($"⚠️ TelegramConfig.txt no encontrado en: {configPath}");
            }
        }

        private static string Encrypt(string plainText, Assembly assembly)
        {
            var (key, iv) = DeriveKey(assembly);
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
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static (byte[] Key, byte[] IV) DeriveKey(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0.0";
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var publicKeyHex = publicKeyToken != null && publicKeyToken.Length > 0
                ? BitConverter.ToString(publicKeyToken).Replace("-", "")
                : "00000000";

            var guidAttr = assembly.GetCustomAttribute<System.Runtime.InteropServices.GuidAttribute>();
            var guid = guidAttr?.Value ?? "00000000-0000-0000-0000-000000000000";

            var saltString = $"{guid}|{version}|{publicKeyHex}";
            var saltBytes = Encoding.UTF8.GetBytes(saltString);
            var passwordBase = XorString("AgenteIALocal.Telegram.SecureConfig.2025", 0x42);

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBase, saltBytes, 100000))
            {
                return (deriveBytes.GetBytes(32), deriveBytes.GetBytes(16));
            }
        }

        private static string XorString(string input, byte xorKey)
        {
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)(chars[i] ^ xorKey);
            return new string(chars);
        }
    }
}
