# 🔧 DIAGNÓSTICO RUNMODE SYNC - Guía de Verificación

**Fecha:** 2026-01-28 00:15  
**Fix ID:** 20260128_000100, 20260128_000300, 20260128_000400  
**Tests:** 33/33 PASARON ✅

---

## ✅ CAMBIOS APLICADOS

### 1. Corrección Mapping Índices

**Problema original:**
```csharp
// ❌ INCORRECTO (invertido)
int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
```

**Fix aplicado:**
```csharp
// ✅ CORRECTO (match orden XAML)
int targetIndex = runMode.Equals("agente", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
```

**Orden XAML (AgenteIALocalControl.xaml líneas 1051-1052):**
```xaml
<ComboBoxItem Tag="agente" Content="{loc:Translate ui.config.llm.runmode.agente}" />     <!-- Index 0 -->
<ComboBoxItem Tag="preguntar" Content="{loc:Translate ui.config.llm.runmode.preguntar}" /> <!-- Index 1 -->
```

### 2. Logging Diagnóstico Agregado

**Ubicaciones:**
- `OnSettingsSaved` (Config → Toolbox): Línea ~327-347
- `TypeActivitie_SelectionChanged` (Toolbox → Config): Línea ~166-210

**Logs a buscar en Output Window:**

```
[Control.TypeActivitie] USER changed RunMode in toolbox: Tag=agente, Normalized=agente, SelectedIndex=0
[Control.TypeActivitie] PERSISTING RunMode change: 'preguntar' → 'agente'
[Control.TypeActivitie] ✓ RunMode saved to settings: agente
[Control.OnSettingsSaved.RunModeSync] DIAGNOSTICO: runMode=agente, targetIndex=0, TypeActivitie.SelectedIndex=1, IsLoaded=True
[Control.OnSettingsSaved.RunModeSync] SINCRONIZANDO: 1 → 0 (runMode=agente)
[Control.OnSettingsSaved.RunModeSync] ✓ RunMode sincronizado exitosamente: agente → index 0
```

---

## 🧪 PASOS PARA VERIFICAR SINCRONIZACIÓN

### Test 1: Toolbox → Config (ya funcionaba)

1. **F5 Debug** (Experimental Instance)
2. Abrir **Local AI Agent Chat** toolbox
3. Cambiar combo inferior de **"Preguntar"** a **"Agente"**
4. **Logs esperados:**
   ```
   [Control.TypeActivitie] USER changed RunMode in toolbox: Tag=agente, Normalized=agente, SelectedIndex=0
   [Control.TypeActivitie] PERSISTING RunMode change: 'preguntar' → 'agente'
   [Control.TypeActivitie] ✓ RunMode saved to settings: agente
   ```
5. Abrir **Config Window** (botón engranaje arriba derecha)
6. **Verificar:** Combo "Modo de Ejecución" muestra **"Agente"** ✅

### Test 2: Config → Toolbox (FIX APLICADO)

1. **F5 Debug** (Experimental Instance)
2. Abrir **Config Window** (botón engranaje)
3. Ir a sección **"LLMs locales"**
4. Cambiar **"Modo de Ejecución"** de **"Agente"** a **"Preguntar"**
5. Click **"Guardar"** (botón verde abajo)
6. **Logs esperados:**
   ```
   [Control.OnSettingsSaved.RunModeSync] DIAGNOSTICO: runMode=preguntar, targetIndex=1, TypeActivitie.SelectedIndex=0, IsLoaded=True
   [Control.OnSettingsSaved.RunModeSync] SINCRONIZANDO: 0 → 1 (runMode=preguntar)
   [Control.OnSettingsSaved.RunModeSync] ✓ RunMode sincronizado exitosamente: preguntar → index 1
   ```
7. **Verificar:** Combo en toolbox (abajo) cambia automáticamente a **"Preguntar"** ✅

### Test 3: Bidireccional (múltiples cambios)

1. Cambiar en toolbox: **Preguntar → Agente**
2. Abrir Config → verificar muestra "Agente"
3. Cambiar en Config: **Agente → Preguntar** → Guardar
4. Verificar toolbox muestra "Preguntar"
5. Repetir ciclo 2-3 veces

**Esperado:** Cada cambio se sincroniza inmediatamente en ambas direcciones.

---

## 🐛 TROUBLESHOOTING

### Problema: Combo toolbox NO cambia cuando guardo Config

**Posibles causas:**

1. **TypeActivitie es NULL**
   - Buscar en logs: `TypeActivitie combo es NULL - no se puede sincronizar`
   - Solución: Verificar que toolbox esté visible y cargado

2. **_isRefreshingFromSettings está TRUE**
   - Buscar en logs: `SKIP - Refreshing from settings (avoid loop)`
   - Solución: Esto es normal durante el primer refresh, pero no debería persistir

3. **SettingsSaved event no se dispara**
   - Buscar en logs después de hacer click "Guardar" en Config
   - Debe aparecer: `[Control.OnSettingsSaved.RunModeSync] DIAGNOSTICO: ...`
   - Si NO aparece: Event no se suscribió o no se disparó

4. **Mapping aún incorrecto**
   - Buscar en logs: `DIAGNOSTICO: runMode=agente, targetIndex=...`
   - Si agente → targetIndex=1: **mapping sigue MAL** (debería ser 0)
   - Si preguntar → targetIndex=0: **mapping sigue MAL** (debería ser 1)

### Problema: Logs no aparecen

**Solución:**
1. Abrir **View → Output**
2. Dropdown "Mostrar resultados desde:" seleccionar **"Debug"**
3. Filtrar por texto: `RunModeSync` o `TypeActivitie`

### Problema: Combo muestra texto diferente (traducido)

**Comportamiento correcto:**
- El CONTENIDO del combo cambia según idioma (español: "Agente"/"Preguntar", inglés: "Agent"/"Ask")
- El TAG es invariante: "agente"/"preguntar" (siempre minúsculas)
- La sincronización funciona por ÍNDICE (0/1), NO por texto

---

## 📊 TESTS UNITARIOS

**Total:** 33 tests  
**Resultado:** ✅ 33/33 PASARON

### Breakdown por Categoría

**RunModeSyncTests (12 tests):**
- ✅ Mapping forward (runMode → index): 3 tests
- ✅ Mapping reverse (index → runMode): 2 tests
- ✅ Round-trip (ida y vuelta): 2 tests
- ✅ Fallback (null, empty, invalid): 3 tests
- ✅ Settings property: 2 tests

**LocalizationServiceTests (13 tests):**
- ✅ JSON-only loading: 3 tests
- ✅ Fallback key raw: 3 tests
- ✅ Metadata extraction: 2 tests
- ✅ Dynamic listing: 2 tests
- ✅ Nested keys: 1 test
- ✅ Settings persistence: 2 tests

**Tests preexistentes:** 8 tests

### Ejecutar solo tests RunMode

```bash
dotnet test --filter "TestCategory=RunMode"
```

### Ejecutar solo tests Localization

```bash
dotnet test --filter "TestCategory=LocalizationService"
```

### Ejecutar todos los tests

```bash
dotnet test
```

---

## 📁 ARCHIVOS MODIFICADOS

| Archivo | Cambio | Líneas |
|---------|--------|--------|
| `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs` | Fix mapping + logging OnSettingsSaved | ~325-347 |
| `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalControl.xaml.cs` | Logging TypeActivitie_SelectionChanged | ~166-210 |
| `src/AgenteIALocal.Tests/Core/RunModeSyncTests.cs` | 12 tests unitarios RunMode | NUEVO |

---

## ✅ CHECKLIST VALIDACIÓN

- [x] Build: 0 errores, 0 warnings
- [x] Tests unitarios: 33/33 pasan
- [x] Mapping correcto: agente=0, preguntar=1
- [x] Logging diagnóstico agregado
- [ ] F5 debug manual: Test 1 (Toolbox → Config)
- [ ] F5 debug manual: Test 2 (Config → Toolbox)
- [ ] F5 debug manual: Test 3 (Bidireccional múltiple)

**Próximo paso:** Ejecutar Tests manuales (Fase 6) con F5 debug.
