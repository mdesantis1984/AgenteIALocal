// NUEVO - ID: 20260126_190100
// Unit tests para SecureConfigReader (ofuscación AES-256)
using System;
using System.Reflection;
using AgenteIALocal.Infrastructure.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgenteIALocal.Tests.Infrastructure.Security
{
    /// <summary>
    /// Tests para SecureConfigReader - Ofuscación de configuración Telegram
    /// NOTA: Tests usan assembly de Tests (no VSIX) para evitar dependencias VS SDK
    /// </summary>
    [TestClass]
    public class SecureConfigReaderTests
    {
        private Assembly _testAssembly;

        [TestInitialize]
        public void Setup()
        {
            // Usar assembly de Tests (simulando VSIX para unit tests)
            _testAssembly = Assembly.GetExecutingAssembly();
        }

        [TestMethod]
        public void Encrypt_ValidInput_ReturnsBase64String()
        {
            // Arrange
            var plainText = "test_token|test_chatid";

            // Act
            var encrypted = SecureConfigReader.Encrypt(plainText, _testAssembly);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsFalse(string.IsNullOrEmpty(encrypted));
            Assert.IsTrue(IsBase64String(encrypted), "Encrypted text should be valid Base64");
        }

        [TestMethod]
        public void Decrypt_EncryptedText_ReturnsOriginalPlainText()
        {
            // Arrange
            var originalText = "123456789:ABCdefGHI|9876543210";
            var encrypted = SecureConfigReader.Encrypt(originalText, _testAssembly);

            // Act
            var decrypted = SecureConfigReader.Decrypt(encrypted, _testAssembly);

            // Assert
            Assert.AreEqual(originalText, decrypted);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_NullInput_ThrowsArgumentNullException()
        {
            // Act
            SecureConfigReader.Encrypt(null, _testAssembly);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_NullInput_ThrowsArgumentNullException()
        {
            // Act
            SecureConfigReader.Decrypt(null, _testAssembly);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Decrypt_InvalidBase64_ThrowsFormatException()
        {
            // Arrange
            var invalidBase64 = "not-valid-base64!!!";

            // Act
            SecureConfigReader.Decrypt(invalidBase64, _testAssembly);
        }

        [TestMethod]
        public void Encrypt_SameInputTwice_ProducesSameOutput()
        {
            // Arrange
            var plainText = "consistent_token|consistent_chatid";

            // Act
            var encrypted1 = SecureConfigReader.Encrypt(plainText, _testAssembly);
            var encrypted2 = SecureConfigReader.Encrypt(plainText, _testAssembly);

            // Assert
            Assert.AreEqual(encrypted1, encrypted2);
        }

        [TestMethod]
        public void Encrypt_LongInput_HandlesCorrectly()
        {
            // Arrange
            var longToken = new string('A', 500);
            var longChatId = new string('9', 100);
            var plainText = $"{longToken}|{longChatId}";

            // Act
            var encrypted = SecureConfigReader.Encrypt(plainText, _testAssembly);
            var decrypted = SecureConfigReader.Decrypt(encrypted, _testAssembly);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void Encrypt_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var specialChars = "token:!@#$%^&*()_+-=[]{}|;':\"<>?,./|chatid:123";

            // Act
            var encrypted = SecureConfigReader.Encrypt(specialChars, _testAssembly);
            var decrypted = SecureConfigReader.Decrypt(encrypted, _testAssembly);

            // Assert
            Assert.AreEqual(specialChars, decrypted);
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}


