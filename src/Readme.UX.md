# UX/UI — Agente IA Local (ToolWindow)

> Documento UX canónico. Describe la experiencia visual y comportamientos observables **verificados en XAML/code-behind**.

## 🎯 Objetivo UX

- Integración nativa Visual Studio (VSIX clásico) con una ToolWindow orientada a flujos de chat/agente.
- Experiencia dark/flat consistente, con feedback inmediato de estado y acciones claras.

## 🧱 Layout (zonas)

### 1) Header

- Ubicación: `Grid.Row=0` (`HeaderContainer`, `Height=64`).
- Contenido (verificable):
  - Contador de solución: icono `md:PackIcon Kind="FolderOutline"` + `TextBlock x:Name="SolutionNameText"`.
  - Contador de proyectos: icono `md:PackIcon Kind="FileTreeOutline"` + `TextBlock x:Name="ProjectCountText"`.
  - Indicador de estado: `md:PackIcon x:Name="StateIcon"` (binding a `StateIconKind` / `StateColor`) + `TextBlock x:Name="StateLabelText"` (binding a `StateLabel` / `StateColor`).
  - Bloque de configuración: icono `md:PackIcon Kind="CogOutline"` + `TextBlock Text={Binding ConfigLabel, FallbackValue=Not Config}`.
  - Acciones a la derecha:
    - `Button x:Name="SettingsButton"` con `Click="SettingsButton_Click"`.
    - Botón de ayuda con icono `HelpCircleOutline` (⚠️ sin `Click` verificable en el XAML actual).
- Tokens visuales:
  - Fondo del header: `HeaderBackgroundBrush` (`#FF1E1E22`).
  - Línea inferior: `BorderThickness="0,0,0,1"` con `HeaderMediumEmphasisBrush` (`#FF424242`).

### 2) Chat toolbar

- Ubicación: `BodyContainer` `Row=0` (`ChatToolbarGrid`, `Height=48`, `Margin="12,0,12,8"`).
- Controles (verificables):
  - Icono izquierdo principal: `md:PackIcon Kind="ChatBubbleOutline"` (Foreground `#D0D0D0`).
  - Selector de chat: `ComboBox x:Name="ChatComboBox"` con estilo `DarkFlatComboBoxStyle` y handler `SelectionChanged="ChatComboBox_SelectionChanged"`.
  - Crear chat: `Button x:Name="NewChatButton"` con icono `ChatAddOutline`, `Foreground="LimeGreen"`, `Click="NewChatButton_Click"`.
  - Borrar chat: `Button x:Name="DeleteChatButton"` con icono `ChatDeleteOutline`, `Foreground="IndianRed"`, `Click="DeleteChatButton_Click"`.
- Comportamiento (verificable en code-behind):
  - Crear y borrar chat muestran confirmación por `MessageBox`.
  - El selector `ChatComboBox` carga el chat activo al cambiar.

### 3) Chat surface (single conversation history)

- Ubicación: `BodyContainer` `Row=1`.
- Contenedor (verificable):
  - `Border` con `Background="{StaticResource Brush.LayoutBg}"` (`#282828`), `CornerRadius=4`, `Padding=12`, `BorderThickness=0`.
- Área de salida (verificable):
  - `TextBox x:Name="ResponseJsonText"`:
    - `IsReadOnly="True"`
    - `TextWrapping="Wrap"`
    - `VerticalScrollBarVisibility="Auto"`
    - `Padding="16,12,16,12"`
    - Fondo `HeaderBackgroundBrush` (`#FF1E1E22`)
    - Sin borde (`BorderThickness=0`)
- Regla visual: el contenedor no dibuja borde exterior; la separación visual se logra por padding y contraste de fondos.

### 4) Changes accordion

- Ubicación: `BodyContainer` `Row=2`.
- Contenedor (verificable):
  - `Expander` con `Background="{StaticResource Brush.ChangesBg}"` (`#2E2E2E`), `BorderThickness=1`, `CornerRadius=4`.
- Header (verificable):
  - Chevron: `md:PackIcon Kind="ChevronDown"` rota 180° al expandir (DataTrigger sobre `IsExpanded`).
  - Título: `TextBlock Text={Binding ModifiedFilesCount, StringFormat=Changes ({0})}`.
  - Zona derecha (acciones) renderizada desde `Expander.Tag` (no forma parte del toggle).
- Acciones (verificables):
  - Apply: `Click="ApplyChanges_Click"` con icono `ContentSave` y color `#FF3FB950`.
  - Revert: `Click="RevertChanges_Click"` con icono `DeleteSweepOutline` y color `#FFD29922`.
  - Clear: `Click="ClearChanges_Click"` con icono `EraserVariant` y color `#FFF85149`.
  - Los iconos usan estilos de zoom (`IconHoverZoomPackIconStrongStyle`).
- Contenido (verificable):
  - Lista: `ItemsControl ItemsSource={Binding ModifiedFiles}` renderiza `TextBlock` por item.
- 🧪 Estado: mock/experimental (verificable en code-behind):
  - `ModifiedFiles` se inicializa con valores de ejemplo.
  - Apply/Revert muestran `MessageBox` y no aplican cambios reales.
  - Clear vacía la lista en memoria.

### 5) Footer (Request) — área de prompt

- Ubicación: `BodyContainer` `Row=3` (`PromptAreaGrid`).
- Contenedor (verificable):
  - `Border` con `Background="{StaticResource Brush.FooterBg}"` (`#383838`), `BorderThickness=1`, `CornerRadius=4`.
- Prompt (verificable):
  - `TextBox x:Name="PromptTextBox"`:
    - `Height=128`, `AcceptsReturn=True`, `TextWrapping=Wrap`
    - `VerticalScrollBarVisibility=Auto`, `HorizontalScrollBarVisibility=Disabled`
    - `KeyDown="PromptTextBox_KeyDown"`
    - Fondo transparente, texto `HeaderHighEmphasisBrush`
- Placeholder (verificable):
  - `TextBlock` overlay con `Opacity="0.55"` y visibilidad controlada por DataTriggers cuando `PromptTextBox.Text` está vacío o null.
  - Texto literal actual: “Escribe tu consulta aquí, puedes presionar # para hacer referencia a un archivo de la solucion / proyecto”.

- Teclado (verificable en code-behind):
  - Enter envía si `RunButtonEnabled == true`.
  - Shift+Enter inserta nueva línea (no dispara envío).

### 6) Footer Row 1 — combos bar with separator + Send

- Contenedor (verificable):
  - `Border` superior como separador (`BorderThickness="0,1,0,0"`) dentro del bloque de footer.
- Combos (verificables):
  - `TypeActivitie`:
    - Items: `Agente`, `Chat Bot`.
    - `SelectedIndex=1` en XAML.
    - Template local basado en `DarkFlatComboBoxStyle` con fondo/borde transparentes y `CornerRadius=6`.
    - ItemTemplate con iconos `md:PackIcon`: por defecto `RobotOutline`, y para `Agente` cambia a `Flash` (color `#FF3FB950`).
  - `ModelOfLLM`:
    - Items: `GPT 5`.
    - ItemTemplate con `md:PackIcon Kind="Brain"` (color base `#FFCC8A00`, cambia a `#FF3FB950` si el item es `GPT 5`).
  - `ServerLLM`:
    - Items: `LM Studio`, `JAN`.
    - ItemTemplate con icono por defecto `Database` (color `#FF58A6FF`) y para `JAN` cambia a `Server` (color `#FF9E7AFF`).
- Comportamiento visual de combos (verificable):
  - Hover sin highlight de fondo en items (triggers con `Background="Transparent"`), y selección con `Background="#FFC27A00"`.
- Botón Send (verificable):
  - `Button` a la derecha con `Click="RunButton_Click"` y `IsEnabled={Binding RunButtonEnabled}`.
  - Icono por defecto: `md:PackIcon Kind="Send"` (LimeGreen).
  - Si `StateLabel == "Running"`: cambia a `md:PackIcon Kind="Stop"` (IndianRed) por DataTrigger.
  - Hover/zoom del botón controlado por storyboard (escala 1 → 1.12).

### 7) Log output (hidden)

- `TextBox x:Name="LogText"` existe en la UI con `Visibility="Collapsed"`.
- Se actualiza por cambios en archivo (verificable en code-behind):
  - Sin polling: usa FileSystemWatcher + debounce (~150ms).
  - Lectura del archivo: `%LOCALAPPDATA%\AgenteIALocal\logs\AgenteIALocal.log`.
- Se usa como buffer de diagnóstico durante ejecución (prepending de mensajes en `Log(...)`).

## 🎛️ Estados visuales

- `ExecutionState` controla icono + color + label (verificable en `UpdateStateProperties`):
  - `Idle` → `PauseCircleOutline` (Gray) / label `Idle`.
  - `Running` → `ProgressClock` (DodgerBlue) / label `Running`.
  - `Completed` → `CheckCircleOutline` (LimeGreen) / label `Completed`.
  - `Error` → `AlertCircleOutline` (IndianRed) / label `Error`.
- Enablement (verificable en `UpdateUiState`):
  - Si no hay LLM configurado (`IsLlmConfigured == false`): `RunButtonEnabled=false` y el prompt pasa a modo solo lectura (`IsPromptReadOnly=true`).
  - Durante `Running`: `RunButtonEnabled=false`.

## 🧩 Reglas de estilo (pixel-perfect)

- Paleta base dark/flat (verificable en recursos XAML):
  - Layout: `Brush.LayoutBg` = `#282828`
  - Footer: `Brush.FooterBg` = `#383838`
  - Changes: `Brush.ChangesBg` = `#2E2E2E`
  - Header: `HeaderBackgroundBrush` = `#FF1E1E22`
- Bordes y separadores:
  - Uso de `HeaderMediumEmphasisBrush` (`#FF424242`) como color de línea de separación (ej. header y separador del footer).
- Iconografía:
  - `md:PackIcon` con colores semánticos (ej. LimeGreen/IndianRed) y estilos de zoom (`IconHoverZoomPackIconStyle` / `IconHoverZoomPackIconStrongStyle`).
- Combos tipo toolbar:
  - Templates locales basados en `DarkFlatComboBoxStyle` para control de fondo transparente, hover y selección.

## ✅ Checklist de validación visual (Experimental Instance)

- Abrir ToolWindow desde `Tools → Agente IA Local` y verificar:
  - El header mantiene altura 64 y muestra contadores + estado.
  - El chat toolbar muestra `ChatComboBox` y los botones New/Delete con colores correctos.
  - El chat surface muestra el área `ResponseJsonText` con scroll vertical y sin borde exterior.
  - El expander de cambios rota el chevron al expandir/colapsar y el contador usa `Changes (N)`.
  - Los botones Apply/Revert/Clear muestran iconos con zoom y colores semánticos.
  - El footer conserva el separador superior antes de la barra de combos.
  - El botón Send cambia a Stop cuando el estado está en `Running`.
  - Enter envía y Shift+Enter inserta nueva línea.

## ⚠️ Elementos no verificables (pendientes de inspección en XAML completo)

- Comportamiento del botón Help (no hay `Click` verificable en el XAML actual).
- Layout exacto de `SettingsPanel` (referenciado en code-behind, no localizado en el XAML actual).
- Botón Clear asociado a `ClearButton_Click` (handler existe; control no localizado en el XAML actual).
