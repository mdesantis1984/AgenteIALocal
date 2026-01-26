# Guía de Desarrollo

**[🇪🇸 Español](#) | [🇬🇧 English](../en/development.md)**

---

## 🚀 Setup del Entorno

### Requisitos

- **Visual Studio 2022** (versión 17.8+)
- **.NET Framework 4.7.2** SDK
- **.NET Standard 2.0** SDK
- **Git** (para clonar el repositorio)

### Instalación

```bash
# 1. Clonar repositorio
git clone https://github.com/mdesantis1984/AgenteIALocal.git
cd AgenteIALocal

# 2. Abrir solución
start AgenteIALocal.sln

# 3. Restaurar paquetes NuGet (automático en VS)

# 4. Build (F5 o Ctrl+Shift+B)
```

---

## 🔨 Build y Debug

### Build Release

```bash
# CLI
msbuild AgenteIALocal.sln /p:Configuration=Release

# Visual Studio
Build → Build Solution (Ctrl+Shift+B)
```

### Debug (F5)

1. Set `AgenteIALocalVSIX` como StartUp Project
2. F5 → Abre **Instancia Experimental** de VS
3. Debugging con breakpoints habilitados

---

## 🧪 Testing

### Manual Testing

```
Tools → Agente IA Local
```

Verificar:
- ✅ UI carga correctamente
- ✅ Cambio de idioma funciona
- ✅ Config persiste correctamente
- ✅ Chat con LLM responde

### Smoke Tests

Ver: `docs/plans/PLAN_IDIOMA_1.0.md` (sección G1-G5)

---

## 🤝 Contribuir

### Proceso

1. **Fork** del repositorio
2. **Branch** nueva: `feature/nombre-descriptivo`
3. **Commits** con mensajes descriptivos
4. **Push** a tu fork
5. **Pull Request** a `main`

### Reglas de Commits

```bash
# Formato
<type>(<scope>): <subject>

# Ejemplos
feat(i18n): Add Portuguese translation
fix(logging): Resolve file lock issue
docs: Update README with new features
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

---

## 📚 Referencias

- **[Arquitectura](architecture.md)** — Clean Architecture
- **[Reglas de Trabajo](../../src/Reglas.md)** — Flujo de trabajo

---

**🏠 [Volver al índice](README.md)**
