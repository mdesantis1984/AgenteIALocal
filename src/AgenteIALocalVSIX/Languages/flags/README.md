# Languages Flags - PNG Icons (32×24px)

## ✅ Ubicación en Proyecto

```
src/AgenteIALocalVSIX/Languages/flags/img/
├── es-AR.png  (32×24px - Bandera Argentina)
└── en-US.png  (32×24px - Bandera Estados Unidos)
```

---

## 🎨 Obtener Banderas

### **Opción A: Flaticon (Recomendado - Gratis con atribución)**

1. Ir a: https://www.flaticon.com/packs/international-flags
2. Descargar banderas PNG 32×24px:
   - Argentina: `ar.png` → renombrar a `es-AR.png`
   - Estados Unidos: `us.png` → renombrar a `en-US.png`
3. Copiar a `src/AgenteIALocalVSIX/Languages/flags/img/`

**Licencia:** Atribución requerida (agregar crédito en LANGUAGE_CONTRIBUTION_GUIDE.md)

---

### **Opción B: Emojipedia (Emoji → PNG)**

1. Ir a: https://emojipedia.org/
2. Buscar: "Flag: Argentina" / "Flag: United States"
3. Descargar como PNG
4. Redimensionar a 32×24px (usar https://www.iloveimg.com/resize-image)
5. Copiar a `src/AgenteIALocalVSIX/Languages/flags/img/`

**Licencia:** Dominio público (emoji Unicode)

---

### **Opción C: Country Flags API (Automático - PNG directo)**

```bash
# Bandera Argentina (32×24px)
curl -o src/AgenteIALocalVSIX/Languages/flags/img/es-AR.png "https://flagcdn.com/32x24/ar.png"

# Bandera Estados Unidos (32×24px)
curl -o src/AgenteIALocalVSIX/Languages/flags/img/en-US.png "https://flagcdn.com/32x24/us.png"
```

**Licencia:** Dominio público (https://flagcdn.com/)

---

### **Opción D: Generar PNG Placeholder Temporal (Testing)**

Si solo quieres probar la funcionalidad SIN banderas reales:

```powershell
# Crear PNG placeholder vacío 32×24px (magenta para debug)
# Ejecutar en PowerShell desde raíz del proyecto:

Add-Type -AssemblyName System.Drawing

# Bandera Argentina (placeholder)
$bmp = New-Object System.Drawing.Bitmap(32, 24)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.Clear([System.Drawing.Color]::Magenta)
$bmp.Save("src/AgenteIALocalVSIX/Languages/flags/img/es-AR.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bmp.Dispose()

# Bandera Estados Unidos (placeholder)
$bmp2 = New-Object System.Drawing.Bitmap(32, 24)
$graphics2 = [System.Drawing.Graphics]::FromImage($bmp2)
$graphics2.Clear([System.Drawing.Color]::Cyan)
$bmp2.Save("src/AgenteIALocalVSIX/Languages/flags/img/en-US.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics2.Dispose()
$bmp2.Dispose()

Write-Host "Placeholders creados: es-AR.png (magenta), en-US.png (cyan)"
```

---

## 📦 Después de Obtener Banderas

1. Verificar tamaño correcto: `32×24px` (estándar flags en UI)
2. Formato: PNG con transparencia (opcional)
3. Naming: `{language-code}.png` (ej: `es-AR.png`, `en-US.png`)
4. Copiar a: `src/AgenteIALocalVSIX/Languages/flags/img/`

---

## ✅ Verificación

```powershell
# Listar archivos creados
Get-ChildItem -Path "src\AgenteIALocalVSIX\Languages\flags\img" -Recurse
```

**Output esperado:**
```
es-AR.png  (32×24px, ~1-3KB)
en-US.png  (32×24px, ~1-3KB)
```

---

## 🔄 Estado Actual

| Archivo | Estado | Tamaño | Método |
|---------|--------|--------|--------|
| `es-AR.png` | ⏳ Pendiente | 32×24px | (elegir Opción A/B/C/D) |
| `en-US.png` | ⏳ Pendiente | 32×24px | (elegir Opción A/B/C/D) |

**Nota:** Para testing inmediato, usa **Opción D** (placeholders). Para producción, usa **Opción C** (flagcdn.com - más rápido).
