# QA Manual Checklist - About Window + Contact Form

**Versión:** 2.7-about.1  
**Fecha:** 2026-01-26  
**Responsable:** Marco De Santis

---

## 📋 **Checklist QA Manual**

### **1. About Window - Funcionalidad Básica**

- [ ] **Abrir About Window** desde botón en ToolWindow principal
- [ ] Ventana se abre como **modal** (bloquea ToolWindow padre)
- [ ] **Tamaño**: 1024x720px, centrada en pantalla
- [ ] **Header drag**: Arrastrar ventana desde header funciona
- [ ] **Botón Close (X)**: Cierra ventana correctamente

### **2. Navegación Sidebar**

- [ ] **TreeView sidebar** visible con 6 secciones principales
- [ ] Click en **"Información del Producto"** carga contenido
- [ ] Click en **"Créditos y Licencias"** carga contenido
- [ ] Click en **"Repositorio y Enlaces"** carga contenido
- [ ] Click en **"Compatibilidad"** carga contenido
- [ ] Click en **"Soporte"** carga contenido
- [ ] Click en **"Legal"** carga contenido
- [ ] **Mutual exclusion**: Solo 1 sección activa a la vez

### **3. Content Panel - Sección Producto**

- [ ] **Nombre producto**: "Agente IA Local" visible
- [ ] **Descripción**: Texto descriptivo completo
- [ ] **Versión**: Muestra número versión
- [ ] **Fecha Build**: Formato YYYY-MM-DD HH:mm
- [ ] **VSIX ID**: GUID correcto
- [ ] **Foto autor**: Circular, 120x120px (o iniciales "MD")
- [ ] **Datos autor**: Nombre, edad, fecha nacimiento
- [ ] **Links sociales**: 5 botones (LinkedIn, Marketplace, GitHub, YouTube, Website)
- [ ] **Hover botones**: Naranja + texto/borde negro

### **4. Content Panel - Créditos**

- [ ] **Third-Party Libraries**: Grid 2 columnas
- [ ] Cada librería en **card separado**
- [ ] Muestra: Nombre, Versión, Licencia, Link Website
- [ ] **Licencia proyecto**: Tipo + texto + link GitHub

### **5. Content Panel - Repositorio**

- [ ] **Source Code card**: GitHub link funcional
- [ ] **Documentation card**: 2 links (Docs + Changelog)
- [ ] **Report Issues card**: Link Issues GitHub
- [ ] **Cards span**: Issues card ocupa 2 columnas
- [ ] **Click links**: Abren navegador correctamente

### **6. Content Panel - Compatibilidad**

- [ ] **Visual Studio**: Lista versiones compatibles
- [ ] **Requisitos**: .NET Framework + .NET Standard
- [ ] **Badges**: Fondo accent, texto blanco, checkmark visible

### **7. Content Panel - Soporte → Contact Form** ⭐

- [ ] **Email TextBox**: Visible, placeholder correcto
- [ ] **Message TextBox**: Multi-línea, 120px altura
- [ ] **Send Button**: Inicialmente **deshabilitado**

#### **Validación Email**

- [ ] Email vacío → **Error**: "Email is required"
- [ ] Email inválido (`test`) → **Error**: "Invalid email format"
- [ ] Email válido (`test@example.com`) → **Sin error**

#### **Validación Mensaje**

- [ ] Mensaje vacío → **Error**: "Message is required"
- [ ] Mensaje < 10 chars → **Error**: "Message must be at least 10 characters"
- [ ] Mensaje ≥ 10 chars → **Sin error**

#### **Botón Send Habilitado**

- [ ] Email válido + Mensaje válido → **Send button ENABLED**
- [ ] Email inválido O Mensaje inválido → **Send button DISABLED**

#### **Envío Telegram** ⭐⭐⭐

- [ ] **Preparación**: Abrir Telegram en móvil/desktop
- [ ] Llenar form: Email válido + Mensaje "Prueba QA About Window"
- [ ] Click **Send**
- [ ] **Loading state**: Botón muestra "Sending..."
- [ ] **Verificar Telegram**: Mensaje llega a chat **@mdesantis_Telegram_bot** 📱
- [ ] **Formato mensaje**:
  ```
  📧 Contact Form - Agente IA Local
  
  From: [email ingresado]
  
  Message:
  [mensaje ingresado]
  
  Sent: [timestamp]
  ```
- [ ] **Success**: MessageBox "Message sent successfully! We'll respond soon."
- [ ] **Form limpio**: Email y Message vacíos después de envío

#### **Fallback mailto: (si Telegram falla)**

- [ ] Deshabilitar internet → Llenar form → Click Send
- [ ] **Abre cliente email** (Outlook/Gmail) con:
  - **To**: soporte@mdesantis.com.ar
  - **Subject**: Contact from Agente IA Local
  - **Body**: From: [email] + Message: [mensaje]
- [ ] **MessageBox**: "Your default email client has been opened..."

### **8. Content Panel - Legal**

- [ ] **Copyright**: Año actual + nombre autor
- [ ] **Terms of Use**: Texto disclaimer + link licencia

### **9. Internacionalización (i18n)**

- [ ] **Cambiar idioma en Config** a English
- [ ] **Reabrir About Window** → Textos en inglés ✅
- [ ] **Cambiar idioma** a Français
- [ ] **Reabrir About Window** → Textos en francés ✅
- [ ] **Volver a español** → Textos en español ✅

### **10. Dark Theme**

- [ ] **Background**: Color oscuro (#282828)
- [ ] **Cards**: Fondo contraste, bordes visibles
- [ ] **Texto**: Legible (high emphasis)
- [ ] **Accent**: Color naranja consistente
- [ ] **Sin artefactos**: Render limpio sin glitches

### **11. Performance**

- [ ] **Apertura ventana**: < 2 segundos
- [ ] **Navegación secciones**: < 500ms cambio content
- [ ] **Envío Telegram**: < 5 segundos (con internet)
- [ ] **Sin memory leaks**: Cerrar/reabrir 5 veces sin degradación

### **12. Seguridad (Post-QA Verificación)**

- [ ] **git status**: `TelegramConfig.txt` **NO aparece** (gitignore funciona)
- [ ] **Documentación**: Solo placeholders falsos (NO datos reales)
- [ ] **VSIX build**: TelegramConfig.txt embebido como `EmbeddedResource`
- [ ] **Decompilador** (ILSpy): TelegramConfig.txt visible pero **ilegible** (Base64 AES-256)

---

## 🎯 **Criterios de Aprobación**

- ✅ **Todos** los checkboxes marcados
- ✅ **0 crashes** o exceptions
- ✅ **Envío Telegram** funcional al 100%
- ✅ **Fallback mailto:** funciona si Telegram falla
- ✅ **i18n** sin strings hardcodeados
- ✅ **Seguridad Git** verificada

---

## 📝 **Notas de Testing**

**Fecha:** _____________  
**Testeado por:** Marco De Santis  

**Issues encontrados:**
1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

**Aprobado para merge:** ☐ SÍ  ☐ NO

**Firma:** _______________
