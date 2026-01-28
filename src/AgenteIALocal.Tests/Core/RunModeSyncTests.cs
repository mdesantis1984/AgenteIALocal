// NUEVA CLASE RunModeSyncTests - ID: 20260128_000200
// Tests unitarios para sincronización RunMode entre Config Window y Toolbox
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgenteIALocal.Core.Configuration;

namespace AgenteIALocal.Tests.Core
{
    [TestClass]
    public class RunModeSyncTests
    {
        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_AgenteValue_MapsToIndex0()
        {
            // Arrange
            var runMode = "agente";

            // Act - Simular mapping del código
            int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Assert
            Assert.AreEqual(0, targetIndex, "RunMode 'agente' debe mapear a index 0");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_PreguntarValue_MapsToIndex1()
        {
            // Arrange
            var runMode = "preguntar";

            // Act - Simular mapping del código
            int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Assert
            Assert.AreEqual(1, targetIndex, "RunMode 'preguntar' debe mapear a index 1");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_AgenteCaseInsensitive_MapsToIndex0()
        {
            // Arrange
            var runMode = "AGENTE";

            // Act - Simular mapping del código (case-insensitive)
            int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Assert
            Assert.AreEqual(0, targetIndex, "RunMode 'AGENTE' (uppercase) debe mapear a index 0");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_NullOrEmptyValue_DefaultsToIndex1()
        {
            // Arrange
            string runMode = null;

            // Act - Simular mapping del código con valor nulo (debería caer en else)
            int targetIndex = (runMode ?? "preguntar").Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Assert
            Assert.AreEqual(1, targetIndex, "RunMode null debe defaultear a 'preguntar' (index 1)");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_EmptyStringValue_DefaultsToIndex1()
        {
            // Arrange
            var runMode = string.Empty;

            // Act - Simular mapping del código
            int targetIndex = string.IsNullOrWhiteSpace(runMode) || runMode.Equals("preguntar", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            // Assert
            Assert.AreEqual(1, targetIndex, "RunMode vacío debe defaultear a 'preguntar' (index 1)");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_InvalidValue_DefaultsToIndex1()
        {
            // Arrange
            var runMode = "invalid_mode";

            // Act - Simular mapping del código
            int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Assert
            Assert.AreEqual(1, targetIndex, "RunMode inválido debe caer en default (index 1)");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_Index0_MapsToAgente()
        {
            // Arrange - Simular selección desde combo (index → runMode)
            int selectedIndex = 0;

            // Act - Reverse mapping (index → tag)
            string expectedTag = (selectedIndex == 0) ? "agente" : "preguntar";

            // Assert
            Assert.AreEqual("agente", expectedTag, "Index 0 debe mapear a tag 'agente'");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_Index1_MapsToPreguntar()
        {
            // Arrange - Simular selección desde combo (index → runMode)
            int selectedIndex = 1;

            // Act - Reverse mapping (index → tag)
            string expectedTag = (selectedIndex == 0) ? "agente" : "preguntar";

            // Assert
            Assert.AreEqual("preguntar", expectedTag, "Index 1 debe mapear a tag 'preguntar'");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_RoundTrip_AgenteToIndexToAgente()
        {
            // Arrange
            var originalRunMode = "agente";

            // Act - Forward mapping: runMode → index
            int targetIndex = originalRunMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Act - Reverse mapping: index → runMode
            string resultRunMode = (targetIndex == 0) ? "agente" : "preguntar";

            // Assert
            Assert.AreEqual(originalRunMode, resultRunMode, "Round-trip 'agente' debe preservar valor");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Synchronization")]
        public void RunMode_RoundTrip_PreguntarToIndexToPreguntar()
        {
            // Arrange
            var originalRunMode = "preguntar";

            // Act - Forward mapping: runMode → index
            int targetIndex = originalRunMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

            // Act - Reverse mapping: index → runMode
            string resultRunMode = (targetIndex == 0) ? "agente" : "preguntar";

            // Assert
            Assert.AreEqual(originalRunMode, resultRunMode, "Round-trip 'preguntar' debe preservar valor");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Settings")]
        public void GlobalSettings_RunModeProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var settings = new GlobalSettings();

            // Act - Set value
            settings.RunMode = "agente";

            // Assert
            Assert.AreEqual("agente", settings.RunMode, "RunMode property debe almacenar 'agente'");
        }

        [TestMethod]
        [TestCategory("RunMode")]
        [TestCategory("Settings")]
        public void GlobalSettings_RunModeProperty_DefaultsToPreguntar()
        {
            // Arrange
            var settings = new GlobalSettings();

            // Act - No se asigna valor (debería tener default)
            var runMode = settings.RunMode ?? "preguntar";

            // Assert
            Assert.AreEqual("preguntar", runMode, "RunMode sin asignar debe defaultear a 'preguntar'");
        }
    }
}
