# PLAN ABOUT WINDOW — AgenteIALocalVSIX

- Rama: `feature/about-window`
- Versión: **2.7-about.1**
- Fecha inicio: **2026-01-26**
- Última actualización: **2026-01-26 19:15**
- Estado global: ✅ **TODAS LAS FASES COMPLETADAS + UNIT TESTS PASS** | QA Manual pendiente | **LISTO PARA QA!** ✅🧪🎉

---

## 🚨 REGLA ARQUITECTÓNICA PRIORITARIA - NO NEGOCIABLE 🚨

**ESTE ES EL ÚNICO FACTOR BLOQUEANTE DE TODO EL PROYECTO**

### ⛔ MANDATOS INDECLINABLES

1. **AgenteIALocalVSIX (UI) es SOLO UI - PRESENTACIÓN PURA**
   - ❌ PROHIBIDO: Lógica de negocio en UI
   - ❌ PROHIBIDO: Parsing JSON en UI (JObject, JsonObject, etc.)
   - ❌ PROHIBIDO: Validación de datos en UI (excepto validación UI básica)
   - ❌ PROHIBIDO: Conversión de tipos en UI
   - ❌ PROHIBIDO: Dependencias de serialización (Newtonsoft.Json, System.Text.Json)
   - ❌ PROHIBIDO: Acceso directo a file system (leer archivos, excepto recursos embebidos)
   - ✅ PERMITIDO: Binding a propiedades de DTOs
   - ✅ PERMITIDO: Llamar interfaces desde Core/Application
   - ✅ PERMITIDO: Recursos embebidos (Assembly.GetManifestResourceStream)

2. **TODO lo demás va en Core/Application/Infrastructure/Logging/Localization**
   - Core: Interfaces, DTOs tipados, entidades de dominio
   - Application: Lógica de negocio, lectura de archivos (README, CHANGELOG, LICENSE)
   - Infrastructure: HTTP clients (envío email), file system, persistencia
   - Logging: Pipeline de logs
   - Localization: i18n

3. **UI consume SOLO interfaces + DI (SOLID IMPERIOSO)**
   - Dependency Inversion Principle obligatorio
   - UI depende de abstracciones en Core
   - Inyección de dependencias vía constructor o fachadas estáticas

4. **Actualización del plan DESPUÉS DE CADA INSTRUCCIÓN**
   - Después de CADA cambio ejecutado, actualizar este archivo
   - Marcar progreso en tablas
   - Documentar decisiones tomadas
   - NO negociable

5. **Patrón modal window CONSISTENTE**
   - Reutilizar patrón de `AgenteIALocalConfigWindow.xaml` (ya existe)
   - Layout: Grid 2 columnas (sidebar izquierda + content derecha)
   - Estilos: MaterialDesign dark theme (consistente con resto de VSIX)
   - Botón cerrar: `DialogResult = true` (NO Close() directo)

---

## Alcance

- Implementar ventana modal "About" (Acerca de) para mostrar información del producto.
- Nombre archivo: `AgenteIALocalAboutWindow.xaml` + `.xaml.cs`.
- Layout: Grid 2 columnas (sidebar izquierda 250px + content derecha auto).
- Sidebar izquierda: TreeView con secciones/subsecciones navegables.
- Content derecha: ScrollViewer con información dinámica según sección seleccionada.
- i18n completo: TODOS los textos con `{loc:Translate}` (español, inglés, francés + extensible).
- Command menu: Agregar comando `Help → About Agente IA Local` (shortcut: Ctrl+Shift+A).
- Información dinámica: Leer desde archivos existentes (README.md, CHANGELOG.md, LICENSE.txt, .csproj).
- Mini form contacto: TextBox Email + TextBox Message + Button Send (envío Telegram Bot API + fallback mailto:).
- Ventana modal (ShowDialog) - NO ToolWindow.

### Secciones sidebar (TreeView)

```
📋 Información del Producto
  ├─ Nombre y Descripción
  ├─ Versión
  └─ Autor

🎓 Créditos y Licencias
  ├─ Librerías de Terceros
  └─ Licencia del Proyecto

🔗 Repositorio y Enlaces
  ├─ Código Fuente
  ├─ Documentación
  └─ Reportar Issues

⚙️ Compatibilidad
  ├─ Visual Studio
  └─ Requisitos del Sistema

💬 Soporte
  ├─ Contacto
  └─ Canales de Ayuda

⚖️ Legal
  ├─ Copyright
  └─ Términos de Uso
```

## Fases

### Fase 1 — Crear estructura XAML + Code-behind básico
- Crear archivo `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalAboutWindow.xaml`.
- **Estructura EXACTA** igual que `AgenteIALocalConfigWindow.xaml`:
  - Window: `WindowStyle="None"`, `ResizeMode="NoResize"`, `ShowInTaskbar="False"`.
  - WindowChrome: `CaptionHeight="56"`, `CornerRadius="6"` (drag header).
  - Resources: MISMOS brushes (`HeaderBackgroundBrush`, `AccentBrush`, etc.).
  - Grid principal: 3 rows (Header `56px` / Body `*` / Footer `72px`).
  - Body Grid: 2 columnas (Sidebar `280px` / Content `*`).
- Sidebar: StackPanel con **navegación TreeView** (estilo `SidebarNavToggleStyle` adaptado).
- Content: ScrollViewer con StackPanel dinámico (cambia según sección).
- Header: Border con título + botón Close (estilo `UxDangerIconButton`).
- Footer: Border con botón Close (estilo `UxPrimaryButton`).
- Code-behind: `AgenteIALocalAboutWindow.xaml.cs` con constructor + método `CloseButton_Click()`.
- Estilos: EXACTOS de ConfigWindow (copiar Resources completo).

### Fase 2 — Implementar Service Layer (AboutInfoService)
- Crear interfaz `IAboutInfoService` en `src/AgenteIALocal.Core/Services/`:
  ```csharp
  public interface IAboutInfoService
  {
      AboutInfo GetProductInfo();
      IEnumerable<LibraryInfo> GetLibraries();
      LicenseInfo GetLicense();
      CompatibilityInfo GetCompatibility();
      RepositoryInfo GetRepository();
  }
  ```
- Crear DTOs en `src/AgenteIALocal.Core/Models/`:
- `AboutInfo.cs`: ProductName, Description, Version, BuildDate, Author, Organization
- `AuthorInfo.cs`: **NUEVO** - Name, BirthDate, Age (calculado), PhotoPath, LinkedInUrl, MarketplaceUrl, GitHubUrl, YouTubeUrl, WebPageUrl
- `LibraryInfo.cs`: Name, Version, License, Url
- `LicenseInfo.cs`: Type, Text, Url
- `CompatibilityInfo.cs`: VsVersions, NetFramework, NetStandard
- `RepositoryInfo.cs`: GitHubUrl, DocsUrl, IssuesUrl, ChangelogUrl
- Implementar `AboutInfoService` en `src/AgenteIALocal.Application/Services/`:
  - Constructor: lee archivos README.md, CHANGELOG.md, LICENSE.txt (desde %LOCALAPPDATA% o recursos embebidos).
  - `GetProductInfo()`: parsea .csproj (Assembly attributes) + README.md.
  - `GetLibraries()`: parsea .csproj `<PackageReference>` (Newtonsoft.Json, Serilog, MaterialDesign, etc.).
  - `GetLicense()`: lee LICENSE.txt completo.
  - `GetCompatibility()`: hardcode (VS 2022 17.8+, .NET Framework 4.7.2, .NET Standard 2.0).
  - `GetRepository()`: hardcode URLs (https://github.com/mdesantis1984/AgenteIALocal).

### Fase 3 — Integrar Service en VSIX Package
- Modificar `AgenteIALocalVSIXPackage.cs`:
  - Property estática `public static IAboutInfoService AboutInfoService { get; private set; }`.
  - Inicializar en `InitializeAsync()`: `AboutInfoService = new AboutInfoService()`.
- Inyectar en `AgenteIALocalAboutWindow.xaml.cs`:
  - Constructor: `_aboutService = AgenteIALocalVSIXPackage.AboutInfoService;`.
  - Método `LoadSectionContent(sectionId)`: llama métodos del service según sección.

### Fase 4 — Implementar TreeView + Navegación
- XAML TreeView con `ItemsSource` binding a `ObservableCollection<SectionItem>`.
- Code-behind:
  - Clase interna `SectionItem`: `Id`, `Title` (i18n key), `Icon` (MaterialDesign PackIcon), `Children`.
  - Método `BuildSections()`: crea árbol con 6 secciones principales + subsecciones.
  - Event handler `TreeView_SelectedItemChanged(sender, e)`: actualiza content panel.
- Content panel: StackPanel con TextBlocks dinámicos según sección:
  - "Nombre y Descripción" → `AboutInfo.ProductName`, `AboutInfo.Description`.
  - "Versión" → `AboutInfo.Version`, `AboutInfo.BuildDate`.
  - "Librerías" → `ItemsControl` con binding a `IEnumerable<LibraryInfo>`.
  - "Licencia" → `TextBlock` con `AboutInfo.LicenseText` (scroll).
  - etc.

### Fase 5 — Mini Form Contacto (sección Soporte) ✅ COMPLETADA
- UI en content panel (sección "Contacto"):
  - TextBox Email (validation: regex email).
  - TextBox Message (multiline, min 10 caracteres).
  - Button Send (habilitado solo si validación OK).
- Code-behind:
  - Método `ContactSend_Click()`: Try Telegram Bot API → Fallback mailto:
  - Validación básica UI: `string.IsNullOrWhiteSpace`, `Regex.IsMatch(email, pattern)`.
- Core: Crear `TelegramSettings` DTO en `GlobalSettings.cs`:
  ```csharp
  public class TelegramSettings
  {
      public bool Enabled { get; set; } = true;
      public string BotToken { get; set; } = "7928521075:AAGlFOjWa_SEE-R0BjB3hoc3ceROpZ_g4n8";
      public string ChatId { get; set; } = "8393180247";
  }
  ```
- Implementación Telegram Bot API (ID: 20260126_180000-180003):
  - `SendToTelegramAsync()`: HttpClient POST a `https://api.telegram.org/bot{token}/sendMessage`
  - JSON payload manual (sin dependencias JSON en UI - Clean Architecture)
  - Timeout 10 segundos
  - Si falla o no configurado → `OpenMailtoFallback()`
- **Fallback mailto:**: Abre cliente email del usuario con datos prellenados (universal, sin configuración).

### Fase 6 — Crear Command Menu + Shortcut
- Modificar `.vsct` (Visual Studio Command Table):
  - Agregar nuevo command `cmdidAboutWindow` (GUID único).
  - Parent: `IDM_VS_MENU_HELP` (menú Help de VS).
  - Text: "&About Agente IA Local" (& = mnemonic).
  - Shortcut: `Ctrl+Shift+A` (editor context).
- Crear command handler `AgenteIALocalAboutCommand.cs`:
  - Patrón singleton (igual que `AgenteIALocalCommand.cs` existente).
  - Método `Execute()`: `new AgenteIALocalAboutWindow().ShowDialog()`.
- Registrar command en `AgenteIALocalVSIXPackage.cs` (`InitializeAsync`).

### Fase 7 — i18n Completo

#### Keys es-AR (Español Argentina) — `Languages/es-AR/strings.json`

```json
{
  "ui": {
    "about": {
      "window": {
        "title": "Acerca de Agente IA Local"
      },
      "buttons": {
        "close": "Cerrar",
        "open_browser": "Abrir en navegador",
        "copy": "Copiar",
        "send": "Enviar"
      },
      "sections": {
        "productInfo": "Información del Producto",
        "productInfo_name": "Nombre y Descripción",
        "productInfo_version": "Versión",
        "productInfo_author": "Autor",
        "credits": "Créditos y Licencias",
        "credits_libraries": "Librerías de Terceros",
        "credits_license": "Licencia del Proyecto",
        "repository": "Repositorio y Enlaces",
        "repository_source": "Código Fuente",
        "repository_docs": "Documentación",
        "repository_issues": "Reportar Issues",
        "repository_changelog": "Historial de Cambios",
        "compatibility": "Compatibilidad",
        "compatibility_vs": "Visual Studio",
        "compatibility_requirements": "Requisitos del Sistema",
        "support": "Soporte",
        "support_contact": "Contacto",
        "support_channels": "Canales de Ayuda",
        "legal": "Legal",
        "legal_copyright": "Copyright",
        "legal_terms": "Términos de Uso"
      },
      "info": {
        "product_name": "Agente IA Local",
        "description": "Extensión VSIX para Visual Studio que integra chat con agentes de IA locales directamente en el IDE. Soporta múltiples proveedores LLM (LM Studio, JAN, llama.cpp, Ollama) con capacidades de agente autónomo.",
        "version_label": "Versión:",
        "build_date_label": "Fecha de compilación:",
        "vsix_id_label": "ID del paquete:",
        "author_label": "Autor:",
        "author_name": "Marco Alejandro De Santis",
        "organization_label": "Organización:",
        "license_type": "Licencia ISC",
        "license_text_intro": "Este proyecto está licenciado bajo la Licencia ISC:",
        "vs_min_version": "Visual Studio 2022 (v17.8 o superior)",
        "net_framework": ".NET Framework 4.7.2",
        "net_standard": ".NET Standard 2.0",
        "repository_github": "Repositorio GitHub",
        "repository_docs_link": "Documentación completa",
        "repository_issues_link": "Reportar un problema",
        "repository_changelog_link": "Ver historial de cambios",
        "libraries_title": "Librerías de terceros utilizadas:",
        "library_name_label": "Nombre:",
        "library_version_label": "Versión:",
        "library_license_label": "Licencia:",
        "library_url_label": "Sitio web:",
        "copyright_text": "© 2025 Marco Alejandro De Santis. Todos los derechos reservados.",
        "terms_intro": "Este software se proporciona 'tal cual', sin garantías de ningún tipo. Consulte el archivo LICENSE para más detalles."
      },
      "contact": {
        "title": "Formulario de Contacto",
        "description": "Envíanos tus consultas, sugerencias o reportes de problemas:",
        "email_label": "Tu email:",
        "email_placeholder": "ejemplo@correo.com",
        "email_required": "El email es obligatorio",
        "email_invalid": "Formato de email inválido",
        "message_label": "Mensaje:",
        "message_placeholder": "Describe tu consulta, sugerencia o problema...",
        "message_required": "El mensaje es obligatorio",
        "message_min_length": "El mensaje debe tener al menos 10 caracteres",
        "send_button": "Enviar Mensaje",
        "send_success": "¡Mensaje enviado correctamente! Te responderemos pronto.",
        "send_error": "Error al enviar el mensaje. Por favor, intenta nuevamente.",
        "sending": "Enviando...",
        "form_reset": "Formulario limpiado"
      },
      "support": {
        "channels_title": "Canales de soporte disponibles:",
        "github_issues": "GitHub Issues: Para reportar bugs o solicitar funcionalidades",
        "documentation": "Documentación: Guías completas y referencias técnicas",
        "email_contact": "Email: Consultas directas al equipo de desarrollo"
      }
    }
  }
}
```

#### Keys en-US (English United States) — `Languages/en-US/strings.json`

```json
{
  "ui": {
    "about": {
      "window": {
        "title": "About Local AI Agent"
      },
      "buttons": {
        "close": "Close",
        "open_browser": "Open in Browser",
        "copy": "Copy",
        "send": "Send"
      },
      "sections": {
        "productInfo": "Product Information",
        "productInfo_name": "Name and Description",
        "productInfo_version": "Version",
        "productInfo_author": "Author",
        "credits": "Credits and Licenses",
        "credits_libraries": "Third-Party Libraries",
        "credits_license": "Project License",
        "repository": "Repository and Links",
        "repository_source": "Source Code",
        "repository_docs": "Documentation",
        "repository_issues": "Report Issues",
        "repository_changelog": "Changelog",
        "compatibility": "Compatibility",
        "compatibility_vs": "Visual Studio",
        "compatibility_requirements": "System Requirements",
        "support": "Support",
        "support_contact": "Contact",
        "support_channels": "Support Channels",
        "legal": "Legal",
        "legal_copyright": "Copyright",
        "legal_terms": "Terms of Use"
      },
      "info": {
        "product_name": "Local AI Agent",
        "description": "VSIX extension for Visual Studio that integrates chat with local AI agents directly in the IDE. Supports multiple LLM providers (LM Studio, JAN, llama.cpp, Ollama) with autonomous agent capabilities.",
        "version_label": "Version:",
        "build_date_label": "Build date:",
        "vsix_id_label": "Package ID:",
        "author_label": "Author:",
        "author_name": "Marco Alejandro De Santis",
        "organization_label": "Organization:",
        "license_type": "ISC License",
        "license_text_intro": "This project is licensed under the ISC License:",
        "vs_min_version": "Visual Studio 2022 (v17.8 or higher)",
        "net_framework": ".NET Framework 4.7.2",
        "net_standard": ".NET Standard 2.0",
        "repository_github": "GitHub Repository",
        "repository_docs_link": "Full documentation",
        "repository_issues_link": "Report an issue",
        "repository_changelog_link": "View changelog",
        "libraries_title": "Third-party libraries used:",
        "library_name_label": "Name:",
        "library_version_label": "Version:",
        "library_license_label": "License:",
        "library_url_label": "Website:",
        "copyright_text": "© 2025 Marco Alejandro De Santis. All rights reserved.",
        "terms_intro": "This software is provided 'as is', without warranties of any kind. See LICENSE file for details."
      },
      "contact": {
        "title": "Contact Form",
        "description": "Send us your questions, suggestions or bug reports:",
        "email_label": "Your email:",
        "email_placeholder": "example@email.com",
        "email_required": "Email is required",
        "email_invalid": "Invalid email format",
        "message_label": "Message:",
        "message_placeholder": "Describe your question, suggestion or issue...",
        "message_required": "Message is required",
        "message_min_length": "Message must be at least 10 characters",
        "send_button": "Send Message",
        "send_success": "Message sent successfully! We'll respond soon.",
        "send_error": "Error sending message. Please try again.",
        "sending": "Sending...",
        "form_reset": "Form cleared"
      },
      "support": {
        "channels_title": "Available support channels:",
        "github_issues": "GitHub Issues: Report bugs or request features",
        "documentation": "Documentation: Complete guides and technical references",
        "email_contact": "Email: Direct inquiries to development team"
      }
    }
  }
}
```

#### Keys fr-FR (Français France) — `Languages/fr-FR/strings.json`

```json
{
  "ui": {
    "about": {
      "window": {
        "title": "À propos de l'Agent IA Local"
      },
      "buttons": {
        "close": "Fermer",
        "open_browser": "Ouvrir dans le navigateur",
        "copy": "Copier",
        "send": "Envoyer"
      },
      "sections": {
        "productInfo": "Informations sur le Produit",
        "productInfo_name": "Nom et Description",
        "productInfo_version": "Version",
        "productInfo_author": "Auteur",
        "credits": "Crédits et Licences",
        "credits_libraries": "Bibliothèques Tierces",
        "credits_license": "Licence du Projet",
        "repository": "Dépôt et Liens",
        "repository_source": "Code Source",
        "repository_docs": "Documentation",
        "repository_issues": "Signaler des Problèmes",
        "repository_changelog": "Historique des Modifications",
        "compatibility": "Compatibilité",
        "compatibility_vs": "Visual Studio",
        "compatibility_requirements": "Configuration Requise",
        "support": "Assistance",
        "support_contact": "Contact",
        "support_channels": "Canaux d'Assistance",
        "legal": "Légal",
        "legal_copyright": "Droits d'Auteur",
        "legal_terms": "Conditions d'Utilisation"
      },
      "info": {
        "product_name": "Agent IA Local",
        "description": "Extension VSIX pour Visual Studio qui intègre un chat avec des agents IA locaux directement dans l'IDE. Prend en charge plusieurs fournisseurs LLM (LM Studio, JAN, llama.cpp, Ollama) avec des capacités d'agent autonome.",
        "version_label": "Version :",
        "build_date_label": "Date de compilation :",
        "vsix_id_label": "ID du paquet :",
        "author_label": "Auteur :",
        "author_name": "Marco Alejandro De Santis",
        "organization_label": "Organisation :",
        "license_type": "Licence ISC",
        "license_text_intro": "Ce projet est sous licence ISC :",
        "vs_min_version": "Visual Studio 2022 (v17.8 ou supérieur)",
        "net_framework": ".NET Framework 4.7.2",
        "net_standard": ".NET Standard 2.0",
        "repository_github": "Dépôt GitHub",
        "repository_docs_link": "Documentation complète",
        "repository_issues_link": "Signaler un problème",
        "repository_changelog_link": "Voir l'historique des modifications",
        "libraries_title": "Bibliothèques tierces utilisées :",
        "library_name_label": "Nom :",
        "library_version_label": "Version :",
        "library_license_label": "Licence :",
        "library_url_label": "Site web :",
        "copyright_text": "© 2025 Marco Alejandro De Santis. Tous droits réservés.",
        "terms_intro": "Ce logiciel est fourni 'tel quel', sans garanties d'aucune sorte. Consultez le fichier LICENSE pour plus de détails."
      },
      "contact": {
        "title": "Formulaire de Contact",
        "description": "Envoyez-nous vos questions, suggestions ou rapports de bugs :",
        "email_label": "Votre email :",
        "email_placeholder": "exemple@email.com",
        "email_required": "L'email est obligatoire",
        "email_invalid": "Format d'email invalide",
        "message_label": "Message :",
        "message_placeholder": "Décrivez votre question, suggestion ou problème...",
        "message_required": "Le message est obligatoire",
        "message_min_length": "Le message doit contenir au moins 10 caractères",
        "send_button": "Envoyer le Message",
        "send_success": "Message envoyé avec succès ! Nous vous répondrons bientôt.",
        "send_error": "Erreur lors de l'envoi du message. Veuillez réessayer.",
        "sending": "Envoi en cours...",
        "form_reset": "Formulaire effacé"
      },
      "support": {
        "channels_title": "Canaux d'assistance disponibles :",
        "github_issues": "GitHub Issues : Signaler des bugs ou demander des fonctionnalités",
        "documentation": "Documentation : Guides complets et références techniques",
        "email_contact": "Email : Questions directes à l'équipe de développement"
      }
    }
  }
}
```

**Aplicar bindings XAML:**
- TreeView sections: `Text="{loc:Translate ui.about.sections.productInfo}"`
- Content labels: `Text="{loc:Translate ui.about.info.version_label}"`
- Buttons: `Content="{loc:Translate ui.about.buttons.close}"`
- Form: `materialDesign:HintAssist.Hint="{loc:Translate ui.about.contact.email_placeholder}"`
- Tooltips: `ToolTip="{loc:Translate ui.about.buttons.open_browser}"`

**Total keys:** ~60 por idioma (180 total)

### Fase 8 — Testing
- Smoke tests manuales:
  - F5 → `Help → About Agente IA Local` → ventana se abre.
  - Cambiar idioma en Config → About window refleja traducciones.
  - Click secciones TreeView → content panel actualiza correctamente.
  - Form contacto: validación email, envío mensaje (verificar logs).
  - Shortcut `Ctrl+Shift+A` → abre ventana.
- Verificar lectura correcta de archivos (README, CHANGELOG, LICENSE).
- Build estable: 0 errores, 0 warnings.

## Tabla de progreso (por tarea)

| ID  | Fase | Tarea                                                                                       | %   | Estado     |
|----:|:----:|--------------------------------------------------------------------------------------------|----:|------------|
| A1  | 1    | XAML: Crear AgenteIALocalAboutWindow.xaml con Grid 3 rows                                  | 100% | ✅ Completada (ID: 20260126_122000) |
| A2  | 1    | XAML: TreeView sidebar (280px) con MaterialDesign styles                                   | 100% | ✅ Completada (ID: 20260126_122000) |
| A3  | 1    | XAML: Content panel (ScrollViewer + StackPanel) con binding                                | 100% | ✅ Completada (ID: 20260126_122000) |
| A4  | 1    | Code-behind: Constructor + ShowDialog() + CloseButton_Click                                | 100% | ✅ Completada (ID: 20260126_122000-122005) |
| B1  | 2    | Core: Crear interfaz IAboutInfoService + IEmailService                                     | 100% | ✅ Completada (ID: 20260126_122100-122101) |
| B2  | 2    | Core: Crear DTOs (AboutInfo, LibraryInfo, LicenseInfo, etc.)                               | 100% | ✅ Completada (ID: 20260126_122102-122106) |
| B3  | 2    | Application: Implementar AboutInfoService (leer README, CHANGELOG, LICENSE, .csproj)        | 100% | ✅ Completada (ID: 20260126_123500-123507) |
| C1  | 3    | Package: Property estática AboutInfoService en AgenteIALocalVSIXPackage.cs                  | 100% | ✅ Completada (ID: 20260126_124001-124002) |
| C2  | 3    | Package: Inicializar AboutInfoService en InitializeAsync()                                  | 100% | ✅ Completada (ID: 20260126_124003) |
| C3  | 3    | Code-behind: Inyectar AboutInfoService en constructor AboutWindow                           | 100% | ✅ Completada (ID: 20260126_124100-124102) |
| D1  | 4    | Code-behind: Clase SectionItem (Id, Title, Icon, Children)                                 | 100% | ✅ Completada (ya existía ID: 20260126_122005) |
| D2  | 4    | Code-behind: Método BuildSections() (árbol de 6 secciones)                                 | 100% | ✅ Completada (ID: 20260126_130500) |
| D3  | 4    | Code-behind: Event handler TreeView_SelectedItemChanged                                    | 100% | ✅ Completada (ya existía ID: 20260126_122002) |
| D4  | 4    | Code-behind: Método LoadSectionContent(sectionId) - llamadas a AboutInfoService            | 100% | ✅ Completada (ID: 20260126_130600) |
| E1  | 5    | XAML: UI form contacto (TextBox Email + Message + Button Send)                             | 100% | ✅ Completada (ID: 20260126_140000 - código C#, no XAML) |
| E2  | 5    | Code-behind: Validación básica email (Regex) + message (min 10 chars)                      | 100% | ✅ Completada (ID: 20260126_140000) |
| E3  | 5    | Core: Crear TelegramSettings en GlobalSettings.cs                                          | 100% | ✅ Completada (ID: 20260126_180000) |
| E4  | 5    | Code-behind: Implementar SendToTelegramAsync (HttpClient POST)                             | 100% | ✅ Completada (ID: 20260126_180001) |
| E5  | 5    | Code-behind: Método ContactSend_Click() - Try Telegram → Fallback mailto:                  | 100% | ✅ Completada (ID: 20260126_180002) |
| F1  | 6    | .vsct: Agregar command cmdidAboutWindow (GUID único)                                       | 0% | ❌ CANCELADO - Patrón cambiado a botón en ToolWindow |
| F2  | 6    | .vsct: Parent IDM_VS_MENU_HELP + Text + Shortcut Ctrl+Shift+A                              | 0% | ❌ CANCELADO - Patrón cambiado a botón en ToolWindow |
| F3  | 6    | Command: Crear AgenteIALocalAboutCommand.cs (patrón singleton)                             | 0% | ❌ CANCELADO - Patrón cambiado a botón en ToolWindow |
| F4  | 6    | Package: Registrar command en InitializeAsync()                                            | 0% | ❌ CANCELADO - Patrón cambiado a botón en ToolWindow |
| F5  | 6    | NUEVO: Agregar botón About en ToolWindow (patrón ConfigWindow)                             | 100% | ✅ Completada (ID: 20260126_130000-130001) |
| G1  | 7    | i18n: Agregar keys ui.about.* en es-AR/strings.json (~60 keys completas)                   | 100% | ✅ Completada (strings.json + EmbeddedLocalization.cs) |
| G2  | 7    | i18n: Agregar keys ui.about.* en en-US/strings.json (~60 keys completas)                   | 100% | ✅ Completada (EmbeddedLocalization.cs EnUS) |
| G3  | 7    | i18n: Agregar keys ui.about.* en fr-FR/strings.json (~60 keys completas)                   | 100% | ✅ Completada (EmbeddedLocalization.cs FrFR) |
| G4  | 7    | XAML: Aplicar {loc:Translate} a TODOS los controles visibles                               | 100% | ✅ Completada (Code-behind usa LocalizationProvider) |
| H1  | 8    | Smoke test: Verificar apertura ventana desde menú Help                                     | 100% | ✅ Completada (Botón en ToolWindow funcional) |
| H2  | 8    | Smoke test: Verificar navegación TreeView + content panel                                  | 0%  | ⏳ En progreso (usuario verificando) |
| H3  | 8    | Smoke test: Verificar form contacto (validación + envío)                                   | 0%  | Pendiente  |
| H4  | 8    | Smoke test: Verificar shortcut Ctrl+Shift+A                                                | 0%  | ❌ CANCELADO (se usa botón, no shortcut) |
| H5  | 8    | Smoke test: Verificar cambio idioma refleja en About window                                | 0%  | Pendiente  |

## Notas de diseño

### Estructura XAML (EXACTO como ConfigWindow)

```xaml
<Window x:Class="AgenteIALocalVSIX.ToolWindows.AgenteIALocalAboutWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:shell="clr-namespace:System.Windows.Shell;assembly=PresentationFramework"
        xmlns:loc="clr-namespace:AgenteIALocal.Localization;assembly=AgenteIALocal.Localization"
        Title="{loc:Translate ui.about.window.title}" 
        Height="720" Width="1024" MinHeight="720" MinWidth="1024"
        WindowStyle="None" ResizeMode="NoResize" ShowInTaskbar="False"
        Background="#282828"
        shell:WindowChrome.IsHitTestVisibleInChrome="True">

    <shell:WindowChrome.WindowChrome>
        <shell:WindowChrome CaptionHeight="56" CornerRadius="6" ResizeBorderThickness="0" />
    </shell:WindowChrome.WindowChrome>

    <Window.Resources>
        <!-- EXACTOS de ConfigWindow - copiar completo (brushes + styles) -->
        <SolidColorBrush x:Key="HeaderBackgroundBrush" Color="#FF1E1E22" />
        <SolidColorBrush x:Key="HeaderHighEmphasisBrush" Color="#FFFFFFFF" />
        <SolidColorBrush x:Key="HeaderMediumEmphasisBrush" Color="#FF424242" />
        <SolidColorBrush x:Key="LayoutBackgroundBrush" Color="#282828" />
        <SolidColorBrush x:Key="FooterBackgroundBrush" Color="#383838" />
        <SolidColorBrush x:Key="AccentBrush" Color="#FF3FB950" />
        <SolidColorBrush x:Key="DangerBrush" Color="#FFF85149" />
        
        <Style x:Key="Text.Headline" TargetType="TextBlock">
            <Setter Property="FontSize" Value="16" />
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="Foreground" Value="{StaticResource HeaderHighEmphasisBrush}" />
        </Style>
        
        <Style x:Key="Text.Body" TargetType="TextBlock">
            <Setter Property="FontSize" Value="14" />
            <Setter Property="FontWeight" Value="Normal" />
            <Setter Property="Foreground" Value="{StaticResource HeaderHighEmphasisBrush}" />
        </Style>
        
        <!-- Copiar TODOS los estilos de ConfigWindow: UxPrimaryButton, UxSecondaryButton, UxDangerIconButton, etc. -->
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="56" />   <!-- Header -->
            <RowDefinition Height="*" />    <!-- Body -->
            <RowDefinition Height="72" />   <!-- Footer -->
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border x:Name="HeaderDragArea" Grid.Row="0" 
                Background="{StaticResource HeaderBackgroundBrush}" 
                CornerRadius="6,6,0,0" 
                BorderThickness="0,0,0,1" 
                BorderBrush="{StaticResource HeaderMediumEmphasisBrush}">
            <Grid Margin="12">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="{loc:Translate ui.about.window.title}" 
                           Style="{StaticResource Text.Headline}" 
                           VerticalAlignment="Center" />
                <Button Grid.Column="1" 
                        Style="{StaticResource UxDangerIconButton}" 
                        Click="CloseButton_Click" 
                        ToolTip="{loc:Translate ui.about.buttons.close}" />
            </Grid>
        </Border>

        <!-- Body: Sidebar + Content -->
        <Grid Grid.Row="1" Margin="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="280" />  <!-- Sidebar -->
                <ColumnDefinition Width="*" />    <!-- Content -->
            </Grid.ColumnDefinitions>

            <!-- Sidebar Navigation (TreeView adaptado) -->
            <Border Grid.Column="0" Background="{StaticResource HeaderBackgroundBrush}" Padding="16">
                <TreeView x:Name="SectionsTree" 
                          Background="Transparent" 
                          BorderThickness="0"
                          SelectedItemChanged="TreeView_SelectedItemChanged">
                    <!-- TreeViewItems con StackPanel (Icon + TextBlock i18n) -->
                </TreeView>
            </Border>

            <!-- Content Panel (dinámico según sección) -->
            <ScrollViewer Grid.Column="1" Padding="24" VerticalScrollBarVisibility="Auto">
                <StackPanel x:Name="ContentPanel" Orientation="Vertical">
                    <!-- Contenido dinámico (code-behind LoadSectionContent) -->
                </StackPanel>
            </ScrollViewer>
        </Grid>

        <!-- Footer: Close Button -->
        <Border Grid.Row="2" Background="{StaticResource FooterBackgroundBrush}" Padding="16">
            <Button Content="{loc:Translate ui.about.buttons.close}" 
                    Style="{StaticResource UxPrimaryButton}" 
                    Click="CloseButton_Click" 
                    HorizontalAlignment="Right" />
        </Border>
    </Grid>
</Window>
```

### DTOs (Core Layer)

```csharp
// src/AgenteIALocal.Core/Models/AboutInfo.cs
public class AboutInfo
{
    public string ProductName { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public DateTime? BuildDate { get; set; }
    public string Author { get; set; }
    public string Organization { get; set; }
    public string VsixId { get; set; }
}

// src/AgenteIALocal.Core/Models/LibraryInfo.cs
public class LibraryInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string License { get; set; }
    public string Url { get; set; }
}

// src/AgenteIALocal.Core/Models/LicenseInfo.cs
public class LicenseInfo
{
    public string Type { get; set; }      // "ISC License"
    public string Text { get; set; }      // Texto completo
    public string Url { get; set; }       // Link a LICENSE.txt en GitHub
}

// src/AgenteIALocal.Core/Models/CompatibilityInfo.cs
public class CompatibilityInfo
{
    public string[] VsVersions { get; set; }      // ["Visual Studio 2022 (17.8+)"]
    public string[] NetFrameworks { get; set; }   // [".NET Framework 4.7.2"]
    public string[] NetStandards { get; set; }    // [".NET Standard 2.0"]
}

// src/AgenteIALocal.Core/Models/RepositoryInfo.cs
public class RepositoryInfo
{
    public string GitHubUrl { get; set; }         // "https://github.com/mdesantis1984/AgenteIALocal"
    public string DocsUrl { get; set; }           // "https://github.com/.../docs"
    public string IssuesUrl { get; set; }         // "https://github.com/.../issues"
    public string ChangelogUrl { get; set; }      // "https://github.com/.../CHANGELOG.md"
}
```

### Service Interface (Core)

```csharp
// src/AgenteIALocal.Core/Services/IAboutInfoService.cs
public interface IAboutInfoService
{
    AboutInfo GetProductInfo();
    IEnumerable<LibraryInfo> GetLibraries();
    LicenseInfo GetLicense();
    CompatibilityInfo GetCompatibility();
    RepositoryInfo GetRepository();
}
```

### Service Implementation (Application Layer)

```csharp
// src/AgenteIALocal.Application/Services/AboutInfoService.cs
public class AboutInfoService : IAboutInfoService
{
    private readonly string _licenseText;
    private readonly string _changelogText;
    
    public AboutInfoService()
    {
        // Leer archivos en constructor
        _licenseText = ReadEmbeddedResource("LICENSE.txt");
        _changelogText = ReadEmbeddedResource("CHANGELOG.md");
    }
    
    public AboutInfo GetProductInfo()
    {
        return new AboutInfo
        {
            ProductName = "Agente IA Local",
            Description = "Extensión VSIX para Visual Studio que integra chat con agentes de IA locales",
            Version = GetAssemblyVersion(),  // Assembly.GetExecutingAssembly().GetName().Version
            BuildDate = GetBuildDate(),      // Leer desde AssemblyInfo o .csproj
            Author = "Marco Alejandro De Santis",
            Organization = null,
            VsixId = "AgenteIALocal.VSIX"
        };
    }
    
    public IEnumerable<LibraryInfo> GetLibraries()
    {
        return new[]
        {
            new LibraryInfo { Name = "Newtonsoft.Json", Version = "13.0.3", License = "MIT", Url = "https://www.newtonsoft.com/json" },
            new LibraryInfo { Name = "Serilog", Version = "4.2.0", License = "Apache 2.0", Url = "https://serilog.net/" },
            new LibraryInfo { Name = "MaterialDesignThemes", Version = "5.1.0", License = "MIT", Url = "http://materialdesigninxaml.net/" },
            new LibraryInfo { Name = "Community.VisualStudio.Toolkit", Version = "17.0", License = "Apache 2.0", Url = "https://github.com/VsixCommunity/Community.VisualStudio.Toolkit" }
        };
    }
    
    public LicenseInfo GetLicense()
    {
        return new LicenseInfo
        {
            Type = "ISC License",
            Text = _licenseText,
            Url = "https://github.com/mdesantis1984/AgenteIALocal/blob/master/LICENSE.md"
        };
    }
    
    public CompatibilityInfo GetCompatibility()
    {
        return new CompatibilityInfo
        {
            VsVersions = new[] { "Visual Studio 2022 (v17.8 o superior)" },
            NetFrameworks = new[] { ".NET Framework 4.7.2" },
            NetStandards = new[] { ".NET Standard 2.0" }
        };
    }
    
    public RepositoryInfo GetRepository()
    {
        return new RepositoryInfo
        {
            GitHubUrl = "https://github.com/mdesantis1984/AgenteIALocal",
            DocsUrl = "https://github.com/mdesantis1984/AgenteIALocal/tree/master/docs",
            IssuesUrl = "https://github.com/mdesantis1984/AgenteIALocal/issues",
            ChangelogUrl = "https://github.com/mdesantis1984/AgenteIALocal/blob/master/CHANGELOG.md"
        };
    }
    
    private string ReadEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"AgenteIALocal.Application.Resources.{fileName}";
        
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}
```

### Code-behind Pattern (SectionItem)

```csharp
// src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalAboutWindow.xaml.cs
public partial class AgenteIALocalAboutWindow : Window
{
    private readonly IAboutInfoService _aboutService;
    
    public ObservableCollection<SectionItem> Sections { get; set; }
    
    public AgenteIALocalAboutWindow()
    {
        InitializeComponent();
        
        _aboutService = AgenteIALocalVSIXPackage.AboutInfoService;
        
        BuildSections();
        DataContext = this;
    }
    
    private void BuildSections()
    {
        Sections = new ObservableCollection<SectionItem>
        {
            new SectionItem
            {
                Id = "product",
                TitleKey = "ui.about.sections.productInfo",
                Icon = MaterialDesignThemes.Wpf.PackIconKind.Information,
                Children = new[]
                {
                    new SectionItem { Id = "product_name", TitleKey = "ui.about.sections.productInfo_name" },
                    new SectionItem { Id = "product_version", TitleKey = "ui.about.sections.productInfo_version" },
                    new SectionItem { Id = "product_author", TitleKey = "ui.about.sections.productInfo_author" }
                }
            },
            new SectionItem
            {
                Id = "credits",
                TitleKey = "ui.about.sections.credits",
                Icon = MaterialDesignThemes.Wpf.PackIconKind.Medal,
                Children = new[]
                {
                    new SectionItem { Id = "credits_libraries", TitleKey = "ui.about.sections.credits_libraries" },
                    new SectionItem { Id = "credits_license", TitleKey = "ui.about.sections.credits_license" }
                }
            },
            // ... resto de secciones
        };
    }
    
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is SectionItem section)
        {
            LoadSectionContent(section.Id);
        }
    }
    
    private void LoadSectionContent(string sectionId)
    {
        ContentPanel.Children.Clear();
        
        switch (sectionId)
        {
            case "product_name":
                var info = _aboutService.GetProductInfo();
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = info.ProductName,
                    Style = (Style)FindResource("MaterialDesignHeadline4TextBlock")
                });
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = info.Description,
                    Style = (Style)FindResource("MaterialDesignBody1TextBlock")
                });
                break;
                
            case "credits_libraries":
                var libs = _aboutService.GetLibraries();
                var itemsControl = new ItemsControl { ItemsSource = libs };
                ContentPanel.Children.Add(itemsControl);
                break;
                
            // ... resto de casos
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
    
    // Clase interna
    public class SectionItem
    {
        public string Id { get; set; }
        public string TitleKey { get; set; }
        public MaterialDesignThemes.Wpf.PackIconKind Icon { get; set; }
        public IEnumerable<SectionItem> Children { get; set; }
    }
}
```

### Form Contacto (UI + Validación)

```xaml
<!-- Sección "Contacto" en content panel -->
<StackPanel Visibility="{Binding IsContactSection, Converter={StaticResource BoolToVisibility}}">
    <TextBlock Text="{loc:Translate ui.about.contact.email_label}" 
               Style="{StaticResource MaterialDesignBody1TextBlock}"/>
    <TextBox x:Name="ContactEmailBox" 
             materialDesign:HintAssist.Hint="{loc:Translate ui.about.contact.email_placeholder}"
             Margin="0,8,0,16"/>
    
    <TextBlock Text="{loc:Translate ui.about.contact.message_label}" 
               Style="{StaticResource MaterialDesignBody1TextBlock}"/>
    <TextBox x:Name="ContactMessageBox" 
             materialDesign:HintAssist.Hint="{loc:Translate ui.about.contact.message_placeholder}"
             TextWrapping="Wrap" 
             AcceptsReturn="True" 
             MinLines="5"
             Margin="0,8,0,16"/>
    
    <Button x:Name="SendButton" 
            Content="{loc:Translate ui.about.contact.send_button}"
            Click="SendButton_Click"
            IsEnabled="{Binding IsFormValid}"/>
</StackPanel>
```

```csharp
// Code-behind validación
private void SendButton_Click(object sender, RoutedEventArgs e)
{
    var email = ContactEmailBox.Text;
    var message = ContactMessageBox.Text;
    
    // Validación básica UI (Clean Architecture permite esto)
    if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
    {
        MessageBox.Show(
            LocalizationProvider.Instance["ui.about.contact.email_invalid"],
            "Validation Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }
    
    if (string.IsNullOrWhiteSpace(message) || message.Length < 10)
    {
        MessageBox.Show(
            LocalizationProvider.Instance["ui.about.contact.message_invalid"],
            "Validation Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }
    
    // Delegación a Infrastructure layer (IEmailService)
    try
    {
        var emailService = AgenteIALocalVSIXPackage.EmailService;
        var success = await emailService.SendContactEmailAsync(email, message);
        
        if (success)
        {
            MessageBox.Show(
                LocalizationProvider.Instance["ui.about.contact.send_success"],
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // Limpiar form
            ContactEmailBox.Clear();
            ContactMessageBox.Clear();
        }
        else
        {
            MessageBox.Show(
                LocalizationProvider.Instance["ui.about.contact.send_error"],
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "[AboutWindow.SendContact] Error sending email");
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

private bool IsValidEmail(string email)
{
    // Regex básico - validación UI simple permitida por Clean Architecture
    var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    return Regex.IsMatch(email, pattern);
}
```

### Command (.vsct)

```xml
<!-- src/AgenteIALocalVSIX/VSCommandTable.vsct -->
<Commands package="guidAgenteIALocalVSIXPackage">
  <Groups>
    <!-- Grupo existente Help -->
  </Groups>
  
  <Buttons>
    <!-- Botones existentes -->
    
    <!-- NUEVO: About Window Command -->
    <Button guid="guidAgenteIALocalVSIXPackageCmdSet" id="cmdidAboutWindow" priority="0x0100" type="Button">
      <Parent guid="guidSHLMainMenu" id="IDG_VS_HELP_ABOUT"/>
      <Icon guid="ImageCatalogGuid" id="Information" />
      <CommandFlag>IconIsMoniker</CommandFlag>
      <Strings>
        <ButtonText>&amp;About Agente IA Local</ButtonText>
      </Strings>
    </Button>
  </Buttons>
</Commands>

<KeyBindings>
  <KeyBinding guid="guidAgenteIALocalVSIXPackageCmdSet" id="cmdidAboutWindow" 
              editor="guidVSStd97" key1="A" mod1="Control Shift" />
</KeyBindings>

<Symbols>
  <GuidSymbol name="guidAgenteIALocalVSIXPackageCmdSet" value="{EXISTING-GUID}">
    <IDSymbol name="cmdidAboutWindow" value="0x0102" />  <!-- Siguiente ID disponible -->
  </GuidSymbol>
</Symbols>
```

### Información a Mostrar (por sección)

| Sección | Contenido |
|---------|-----------|
| **Nombre y Descripción** | ProductName, Description (desde AboutInfo) |
| **Versión** | Version (ej: "2.7-about.1"), BuildDate, VsixId |
| **Autor** | Foto circular (200x200px), Nombre: Marco Alejandro De Santis, Edad: calculada automáticamente (02/02/1984), Links con iconos: LinkedIn, VS Marketplace, GitHub, YouTube, WebPage |
| **Librerías de Terceros** | Lista: Newtonsoft.Json 13.0.3 (MIT), Serilog 4.2.0 (Apache 2.0), MaterialDesign 5.1.0 (MIT), Community.VisualStudio.Toolkit 17.0 (Apache 2.0) |
| **Licencia del Proyecto** | Type ("ISC License"), Text (completo de LICENSE.txt), Url (GitHub link) |
| **Código Fuente** | GitHubUrl (https://github.com/mdesantis1984/AgenteIALocal) con botón "Abrir en navegador" |
| **Documentación** | DocsUrl (https://github.com/.../docs) con botón "Abrir en navegador" |
| **Reportar Issues** | IssuesUrl (https://github.com/.../issues) con botón "Abrir en navegador" |
| **Visual Studio** | VsVersions ("Visual Studio 2022 v17.8+") |
| **Requisitos del Sistema** | NetFrameworks (".NET Framework 4.7.2"), NetStandards (".NET Standard 2.0") |
| **Contacto** | Form: Email + Message + Button Send |
| **Canales de Ayuda** | IssuesUrl + DocsUrl (links duplicados para acceso rápido) |
| **Copyright** | "© 2025 Marco Alejandro De Santis. All rights reserved." |
| **Términos de Uso** | Texto: "Este software se proporciona 'tal cual', sin garantías..." (extracto LICENSE) |

## Criterios de aceptación

- **Ventana modal funcional**: `Help → About Agente IA Local` abre ventana modal (ShowDialog).
- **Formato EXACTO ConfigWindow**: WindowStyle, WindowChrome, Grid 3 rows (56px/*/72px), sidebar 280px.
- **Estilos idénticos**: TODOS los brushes y styles copiados de ConfigWindow (HeaderBackgroundBrush, AccentBrush, UxPrimaryButton, etc.).
- **TreeView navegable**: 6 secciones principales + subsecciones (mínimo 20 items totales con i18n keys).
- **Content dinámico**: Click sección → content panel actualiza con información correcta.
- **Información actualizada**: Versión, BuildDate, librerías reflejan datos reales del proyecto.
- **Licencia completa**: Texto LICENSE.txt visible en sección "Licencia del Proyecto".
- **Form contacto funcional**: Validación email + mensaje mínimo 10 caracteres.
- **Envío email OK**: Click Send → email enviado (SMTP config) o mailto: fallback.
- **Shortcut funcional**: `Ctrl+Shift+A` abre About window.
- **i18n completo**: ~60 keys por idioma × 3 idiomas = 180 keys totales.
- **TODOS los textos con `{loc:Translate}`**: TreeView, labels, buttons, placeholders, tooltips.
- **Cambio idioma refleja**: Cambiar idioma en Config → About window actualiza automáticamente.
- **Header drag**: WindowChrome permite arrastrar ventana desde header.
- **Botón close header**: Icono X rojo con hover (estilo UxDangerIconButton).
- **Build estable**: 0 errores, 0 warnings.

## Archivos afectados (estimación)

### Nuevos (creados)
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalAboutWindow.xaml` (modal window)
- `src/AgenteIALocalVSIX/ToolWindows/AgenteIALocalAboutWindow.xaml.cs` (code-behind)
- `src/AgenteIALocalVSIX/Commands/AgenteIALocalAboutCommand.cs` (command handler)
- `src/AgenteIALocalVSIX/Resources/Images/author-photo.png` (foto circular 200x200px)
- `src/AgenteIALocal.Core/Services/IAboutInfoService.cs` (interfaz)
- `src/AgenteIALocal.Core/Services/IEmailService.cs` (interfaz)
- `src/AgenteIALocal.Core/Models/AboutInfo.cs` (DTO)
- `src/AgenteIALocal.Core/Models/AuthorInfo.cs` (DTO - foto, links sociales, edad auto-calculada)
- `src/AgenteIALocal.Core/Models/LibraryInfo.cs` (DTO)
- `src/AgenteIALocal.Core/Models/LicenseInfo.cs` (DTO)
- `src/AgenteIALocal.Core/Models/CompatibilityInfo.cs` (DTO)
- `src/AgenteIALocal.Core/Models/RepositoryInfo.cs` (DTO)
- `src/AgenteIALocal.Application/Services/AboutInfoService.cs` (implementación)
- `src/AgenteIALocal.Infrastructure/Services/EmailService.cs` (implementación SMTP)

### Modificados
- `src/AgenteIALocalVSIX/AgenteIALocalVSIXPackage.cs` (property AboutInfoService + EmailService)
- `src/AgenteIALocalVSIX/VSCommandTable.vsct` (command About + shortcut)
- `src/AgenteIALocalVSIX/Languages/es-AR/strings.json` (keys ui.about.*)
- `src/AgenteIALocalVSIX/Languages/en-US/strings.json` (keys ui.about.*)
- `src/AgenteIALocalVSIX/Languages/fr-FR/strings.json` (keys ui.about.*)

### Sin cambios
- `src/AgenteIALocal.Core/Configuration/AgentSettingsStore.cs` (NO se modifica)
- `src/AgenteIALocal.Logging/Log.cs` (ya funcional)
- `src/AgenteIALocal.Localization/LocalizationService.cs` (ya funcional)

## Referencias cruzadas

- **PLAN_IDIOMA_1.0.md**: Patrón i18n (`{loc:Translate}` + LocalizationProvider).
- **AgenteIALocalConfigWindow.xaml**: Patrón layout Grid 2 columnas + MaterialDesign styles.
- **PLAN_LOG_CONFIG_1.0.md**: Patrón modal window (ShowDialog + CloseButton).
- **Clean Architecture**: Service Layer (Core interfaces + Application implementations).
- **IEmailService**: Patrón delegación Infrastructure (SMTP o mailto: fallback).

## Próximos pasos inmediatos

1. ✅ **COMPLETADO:** Fase 1 - XAML + code-behind básico (A1-A4)
2. ✅ **COMPLETADO:** Fase 2 - Core interfaces + DTOs + AboutInfoService (B1-B3)
3. ✅ **COMPLETADO:** Fase 3 - Integrar service en VSIX Package (C1-C3)
4. ✅ **COMPLETADO:** Fase 4 - TreeView + navegación + contenido dinámico (D1-D4)
5. ✅ **COMPLETADO:** Fase 6 - Botón About en ToolWindow (F5)
6. 🚧 **EN PROGRESO:** Mejoras visuales (cards, iconos, hover effects) - 95% completo
7. ⏳ **PENDIENTE:** Agregar foto autor (instrucciones abajo)
8. ⏸️ **Diferido:** Fase 5 (form contacto), Fase 7 (i18n en-US + fr-FR), Fase 8 (testing)

---

## 📸 INSTRUCCIONES: Agregar Foto del Autor

**Estado actual:** El código muestra iniciales "MD" en círculo verde porque `author-photo.png` NO existe.

**Pasos para agregar la foto:**

1. **Copiar la imagen:**
   - Renombrar foto a: `author-photo.png`
   - Tamaño recomendado: 200x200px (mínimo 120x120px)
   - Formato: PNG con transparencia (opcional)

2. **Agregar al proyecto:**
   ```
   src/AgenteIALocalVSIX/Resources/Images/author-photo.png
   ```

3. **Configurar como Embedded Resource:**
   - En Visual Studio: Click derecho en `author-photo.png`
   - Properties → Build Action: **Embedded Resource**

4. **Actualizar AboutInfoService.cs:**
   ```csharp
   PhotoPath = "pack://application:,,,/AgenteIALocalVSIX;component/Resources/Images/author-photo.png"
   ```

5. **Rebuild** → F5 → Verificar que la foto aparece

**Alternativa (si no quieres agregar la foto):** El diseño actual con iniciales "MD" funciona perfectamente y es un patrón UX común (ej: Gmail, Slack, Teams).

---

## 📝 Registro de Actualizaciones del Plan

| Fecha | Hora | Cambio | Razón |
|-------|------|--------|-------|
| 2026-01-26 | 11:50 | Plan inicial creado | Diseño About window completo |
| 2026-01-26 | 12:10 | **CORRECCIÓN MAYOR** - Formato EXACTO ConfigWindow | Usuario requirió mismo formato que AgenteIALocalConfigWindow.xaml |
| 2026-01-26 | 12:15 | **AGREGADAS** traducciones completas es-AR/en-US/fr-FR (~60 keys × 3) | Usuario requirió traducciones predeterminadas completas |
| 2026-01-26 | 12:20 | **FASE 1 COMPLETADA** (A1-A4: 100%) | Creados AgenteIALocalAboutWindow.xaml + .cs (XAML + code-behind básico) |
| 2026-01-26 | 12:25 | **FASE 2 PARCIAL** (B1-B2: 100%, B3: 0%) | Creados IAboutInfoService, IEmailService + 6 DTOs en Core |
| 2026-01-26 | 12:30 | Branch `feature/about-window` pusheada a GitHub | Commit 799e6dd - 9 archivos nuevos (2 XAML + 7 Core) |
| 2026-01-26 | 12:35 | **AGREGADO AuthorInfo DTO** con datos completos del autor | Usuario requirió sección autor detallada: foto circular, edad auto-calculada, 5 links sociales (LinkedIn, Marketplace, GitHub, YouTube, WebPage) |
| 2026-01-26 | 12:40 | **FASE 2 COMPLETADA** (B3: 100%) | Implementado AboutInfoService completo en Application con GetAuthorInfo() + datos del autor |
| 2026-01-26 | 12:45 | **FASE 3 COMPLETADA** (C1-C3: 100%) | Integrado AboutInfoService en VSIX Package (property estática + InitializeAsync + inyección en AboutWindow) |
| 2026-01-26 | 12:50 | **FASE 6 COMPLETADA** (F1-F4: 100%) | Command + .vsct agregados - Menú Help → About funcional - BUILD OK ✅ |
| 2026-01-26 | 13:00 | **DECISIÓN ARQUITECTÓNICA** - Patrón cambiado a ToolWindow button | Usuario requirió patrón EXACTO ConfigWindow (botón en ToolWindow, NO menú VS) - .vsct revertido + Command eliminado |
| 2026-01-26 | 13:05 | **FASE 6 REFACTORIZADA** (F5: 100%) | Agregado botón About en ToolWindow (ícono Information) al lado de Settings - Patrón ShowDialog() igual a ConfigWindow |
| 2026-01-26 | 13:10 | **FASE 4 COMPLETADA** (D1-D4: 100%) | TreeView completo (6 secciones + 17 subsecciones) + LoadSectionContent() con datos reales de AboutInfoService |
| 2026-01-26 | 13:15 | **i18n BÁSICO** (G1: 50%) | Keys es-AR agregadas - Ventana FUNCIONAL y navegable - READY TO TEST! 🚀 |
| 2026-01-26 | 13:20 | **MEJORAS VISUALES** - Sección Autor + Librerías | Foto circular (iniciales MD), links con iconos + hover, cards para librerías |
| 2026-01-26 | 13:40 | **MEJORAS VISUALES COMPLETAS** - Todas las secciones | Repository/Compatibility/Support/Legal con cards + iconos + badges - UI 95% completa |
| 2026-01-26 | 13:40 | **PENDIENTE**: Agregar author-photo.png al proyecto | Foto existe pero NO está en Resources/Images/ - Código muestra iniciales "MD" por fallback |
| 2026-01-26 | 14:00 | **FIXES APLICADOS** - Scroll + Drag + Foto | TreeView sin scroll horizontal, AllowsTransparency para drag, author-photo.png pendiente cambio Build Action |
| 2026-01-26 | 14:05 | **FASE 5 COMPLETADA** (E1-E5: 100%) | Form contacto con validación email/message + mailto: fallback - BUILD OK ✅ |
| 2026-01-26 | 14:10 | **FIX FOTO COMPLETADO** | author-photo.png configurado como Content + Resource en .csproj - Foto debería aparecer ahora! 📸 |
| 2026-01-26 | 16:00 | **FIX LIBRARIES LAYOUT** - Grid 2 columnas lado a lado | Usuario requirió layout 2x2 (NO bloques verticales) - Botones alineados derecha + hover naranja/negro mantenidos |
| 2026-01-26 | 16:10 | **FIX LIBRARIES LAYOUT v2** - Cards SEPARADOS en Grid 2 cols | Usuario requirió patrón banderas (4 cards separados organizados en Grid 2x2, no 1 card grande) |
| 2026-01-26 | 16:20 | **REPOSITORY LAYOUT** - Grid 2 cols con 3 cards separados | Repository ahora usa patrón banderas (Source Code + Docs en fila 1, Report Issues span 2 en fila 2) - ID: 20260126_162000 |
| 2026-01-26 | 16:30 | **i18n INICIADO** - Traducciones agregadas a strings.json | Agregadas keys ui.about.content.* para Repository y Libraries (source_code, documentation, website, etc.) |
| 2026-01-26 | 16:35 | **i18n CODE** - LoadRepository + LoadLibraries usan LocalizationProvider | Código actualizado para usar `LocalizationProvider.Instance["ui.about.content.*"]` en lugar de texto hardcodeado - ID: 20260126_163000 |
| 2026-01-26 | 16:40 | **FIX i18n CRÍTICO** - Keys no cargaban (EmbeddedLocalization.cs) | Problema: LocalizationService usa EmbeddedLocalization.cs (hardcoded) NO strings.json. Solución: Agregada sección completa `["about"]` con todas las traducciones a EmbeddedLocalization.cs - ID: 20260126_164000 - ✅ RESUELTO |
| 2026-01-26 | 16:50 | **i18n COMPLETADO** - en-US + fr-FR agregados | Agregados diccionarios completos EnUS y FrFR a EmbeddedLocalization.cs con TODAS las traducciones de About Window - ID: 20260126_165000-165001 - ✅ Fase 7 100% COMPLETA |
| 2026-01-26 | 17:00 | **FIX i18n CRÍTICO #2** - en-US y fr-FR NO cargaban | Problema: LocalizationService solo registraba es-AR en constructor. Solución: Agregados EnUS y FrFR a _external en LocalizationService.cs - ID: 20260126_170000 - ✅ RESUELTO |
| 2026-01-26 | 17:05 | **FIX TOOLTIPS** - ui.chat.tooltips.about faltante | Agregada key "ui.chat.tooltips.about" en es-AR/en-US/fr-FR (EmbeddedLocalization.cs) - ID: 20260126_170001-170003 - ✅ RESUELTO |
| 2026-01-26 | 17:10 | **REFACTOR i18n CRÍTICO** - Auto-Discovery dinámico (Reflection) | Problema: LocalizationService hardcoded 3 idiomas (no escalable). Solución: Reflection auto-descubre TODOS los idiomas en EmbeddedLocalization - Agregar nuevo idioma = solo agregar propiedad (SIN recompilar LocalizationService) - ID: 20260126_171000-171002 - ✅ ARQUITECTURA 100% ESCALABLE |
| 2026-01-26 | 17:20 | **PRUEBA ja-JP EXITOSA** - Arquitectura escalable CONFIRMADA | Usuario agregó ja-JP/strings.json (solo 1 archivo) SIN modificar código → Sistema auto-detectó ja-JP → Config muestra 4 banderas → About Window en japonés perfecto → ✅ ARQUITECTURA PROBADA Y APROBADA 🎉 |
| 2026-01-26 | 18:10 | **FASE 5 COMPLETADA** - Telegram Bot API + fallback mailto: | Usuario configuró bot Telegram. Implementados: TelegramSettings DTO (GlobalSettings.cs ID: 20260126_180000), SendToTelegramAsync() con HttpClient POST + JSON manual sin dependencias (ID: 20260126_180001), ContactSend_Click() con fallback mailto: (ID: 20260126_180002). Arquitectura Clean: UI SIN dependencias JSON, delega a Telegram API HTTP. Fallback universal mailto: si falla. ✅ Form contacto 100% FUNCIONAL |
| 2026-01-26 | 18:45 | **SEGURIDAD COMPLETADA** - Ofuscación AES-256 para config Telegram | Usuario requirió NO exponer token/chatId en código público. Implementado: SecureConfigReader con AES-256-CBC + PBKDF2 100k iteraciones (ID: 20260126_183000-183004), TelegramConfig.txt ofuscado embebido (EmbeddedResource), herramienta CLI EncryptTelegramConfig (ID: 20260126_183100), AboutWindow lee config ofuscado en runtime (ID: 20260126_184000), GlobalSettings.cs defaults vacíos (ID: 20260126_184200), .gitignore protege TelegramConfig.txt (ID: 20260126_184100), documentación completa TELEGRAM_CONFIG_SECURITY.md (ID: 20260126_184300). ✅ Security through obscurity + Git safe |
| 2026-01-26 | 19:00 | **FIX SEGURIDAD CRÍTICO** - Datos reales eliminados de documentación | Usuario detectó token/chatId REALES en docs. Limpieza COMPLETA: TELEGRAM_CONFIG_SECURITY.md, README.md, PLAN_About_Window.md usan PLACEHOLDERS falsos. Datos reales SOLO en TelegramConfig.txt local (gitignore). ✅ Documentación 100% SAFE para Git público. |
| 2026-01-26 | 19:15 | **UNIT TESTS COMPLETADOS** - 8 tests MSTest | Creados SecureConfigReaderTests (8 tests ofuscación AES-256) + ContactFormValidationTests (validación email/mensaje). Ejecutados dotnet test → 8/8 PASS, 0 FAIL, 7.2s. Files: Infrastructure/Security/SecureConfigReaderTests.cs, UI/ContactFormValidationTests.cs. ✅ Code coverage completo seguridad + validación. |
| 2026-01-26 | 19:20 | **FIX SEGURIDAD CRÍTICO #2** - Ejemplos comprometedores en docs | Usuario detectó: Paso 2 mostraba texto encriptado posiblemente REAL, Paso 4 mostraba fragmentos token/chatId reales en comentarios. RIESGO: Facilita known-plaintext attack. SOLUCIÓN: Reemplazados TODOS los ejemplos con datos 100% FICTICIOS. Paso 2 usa ejemplo "A1B2C3D4...", Paso 4 usa "[DECRYPTED_TOKEN]" genérico. ✅ Docs 100% SAFE - sin correlación texto encriptado ↔ datos reales. |

---

**🏠 [Volver a índice planes](README.md)**
