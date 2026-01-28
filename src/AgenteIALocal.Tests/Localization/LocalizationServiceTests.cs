// NUEVA CLASE LocalizationServiceTests - ID: 20260127_230700
// Tests unitarios para LocalizationService (sistema JSON-only)
// Valida: carga de JSONs, fallback a keys raw, activación de idiomas, listado dinámico
using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgenteIALocal.Localization;

namespace AgenteIALocal.Tests.Localization
{
    [TestClass]
    public class LocalizationServiceTests
    {
        private string _testLanguagesRoot;
        private string _testSettingsPath;

        [TestInitialize]
        public void Setup()
        {
            // Crear directorio temporal para tests
            _testLanguagesRoot = Path.Combine(Path.GetTempPath(), "AgenteIALocal_Tests", Guid.NewGuid().ToString());
            _testSettingsPath = Path.Combine(_testLanguagesRoot, "language-test.json");

            Directory.CreateDirectory(_testLanguagesRoot);

            // Copiar JSON de prueba válido
            var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "TestData");
            var validJsonSource = Path.Combine(testDataDir, "valid-test.json");

            if (File.Exists(validJsonSource))
            {
                var testLangDir = Path.Combine(_testLanguagesRoot, "test-TE");
                Directory.CreateDirectory(testLangDir);
                File.Copy(validJsonSource, Path.Combine(testLangDir, "strings.json"), true);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Limpiar archivos temporales
            if (Directory.Exists(_testLanguagesRoot))
            {
                try
                {
                    Directory.Delete(_testLanguagesRoot, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void Constructor_WithValidDirectory_InitializesSuccessfully()
        {
            // Arrange & Act
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Assert
            Assert.IsNotNull(service);
            Assert.IsNotNull(service.CurrentLanguageCode);
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void GetString_ExistingKey_ReturnsTranslation()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);
            service.SetLanguage("test-TE");

            // Act
            var result = service.GetString("ui.config.window.title");

            // Assert
            Assert.AreEqual("Test Configuration", result);
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("Fallback")]
        public void GetString_MissingKey_ReturnsKeyRaw()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);
            service.SetLanguage("test-TE");

            // Act
            var result = service.GetString("ui.nonexistent.key.test");

            // Assert
            Assert.AreEqual("ui.nonexistent.key.test", result, "Should return raw key when translation missing");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void ActivateLanguage_ValidLanguage_LoadsSuccessfully()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            service.SetLanguage("test-TE");

            // Assert
            Assert.AreEqual("test-TE", service.CurrentLanguageCode);
            var testString = service.GetString("ui.common.ok");
            Assert.AreEqual("Test OK", testString);
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("Fallback")]
        public void ActivateLanguage_NonExistingLanguage_FallsBackGracefully()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            service.SetLanguage("nonexistent-XX");

            // Assert
            // Should not throw, fallback behavior should handle missing language
            var result = service.GetString("ui.test.key");
            Assert.AreEqual("ui.test.key", result, "Should return raw key when language not found");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void IsLanguageAvailable_ExistingLanguage_ReturnsTrue()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            var result = service.IsLanguageAvailable("test-TE");

            // Assert
            Assert.IsTrue(result, "test-TE language should be available");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void IsLanguageAvailable_NonExistingLanguage_ReturnsFalse()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            var result = service.IsLanguageAvailable("nonexistent-XX");

            // Assert
            Assert.IsFalse(result, "nonexistent-XX language should not be available");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void GetAvailableLanguages_WithValidJson_ReturnsLanguage()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            var languages = service.GetAvailableLanguages().ToList();

            // Assert
            Assert.IsTrue(languages.Count > 0, "Should find at least one language");
            Assert.IsTrue(languages.Any(l => l.Code == "test-TE"), "Should include test-TE language");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        public void GetAvailableLanguages_EmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            var emptyDir = Path.Combine(Path.GetTempPath(), "AgenteIALocal_Empty", Guid.NewGuid().ToString());
            Directory.CreateDirectory(emptyDir);
            var emptySettings = Path.Combine(emptyDir, "settings.json");

            try
            {
                var service = new LocalizationService(emptyDir, emptySettings);

                // Act
                var languages = service.GetAvailableLanguages().ToList();

                // Assert
                Assert.AreEqual(0, languages.Count, "Should return empty list when no languages found");
            }
            finally
            {
                if (Directory.Exists(emptyDir))
                {
                    try { Directory.Delete(emptyDir, true); } catch { }
                }
            }
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("Metadata")]
        public void GetAvailableLanguages_ValidLanguage_ContainsMetadata()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            var languages = service.GetAvailableLanguages().ToList();
            var testLang = languages.FirstOrDefault(l => l.Code == "test-TE");

            // Assert
            Assert.IsNotNull(testLang, "test-TE language should be found");
            Assert.AreEqual("test-TE", testLang.Code);
            Assert.AreEqual("Test Language", testLang.Name);
            Assert.AreEqual("Test Language", testLang.NativeName);
            Assert.IsTrue(testLang.IsAvailable);
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("NestedKeys")]
        public void GetString_NestedKey_ReturnsCorrectValue()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);
            service.SetLanguage("test-TE");

            // Act
            var result = service.GetString("ui.common.cancel");

            // Assert
            Assert.AreEqual("Test Cancel", result);
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("Fallback")]
        public void GetString_NullKey_ReturnsEmptyString()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);
            service.SetLanguage("test-TE");

            // Act
            var result = service.GetString(null);

            // Assert
            // Comportamiento esperado: retornar string vacío o null cuando key es null
            Assert.IsTrue(string.IsNullOrEmpty(result) || result == "null");
        }

        [TestMethod]
        [TestCategory("LocalizationService")]
        [TestCategory("JSON-only")]
        [TestCategory("Settings")]
        public void SetLanguage_PersistsToSettings()
        {
            // Arrange
            var service = new LocalizationService(_testLanguagesRoot, _testSettingsPath);

            // Act
            service.SetLanguage("test-TE");

            // Assert
            Assert.AreEqual("test-TE", service.CurrentLanguageCode);
            
            // Verify settings file was created
            Assert.IsTrue(File.Exists(_testSettingsPath), "Settings file should be created");
        }
    }
}
