using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using AgenteIALocal.Core.Services;

namespace AgenteIALocalVSIX.ToolWindows
{
    /// <summary>
    /// About window - displays product information, credits, and support
    /// NUEVO - ID: 20260126_122000
    /// MODIFICADO - ID: 20260126_124100 - Inyectar AboutInfoService desde Package
    /// </summary>
    public partial class AgenteIALocalAboutWindow : Window
    {
        // MODIFICADO - ID: 20260126_124101 - Inyectado desde Package en constructor
        private readonly IAboutInfoService _aboutService;

        public AgenteIALocalAboutWindow()
        {
            InitializeComponent();

            // NUEVO - ID: 20260126_124102 - Inyectar service desde Package
            _aboutService = AgenteIALocalVSIXPackage.AboutInfoService;

            // MODIFICADO - ID: 20260126_133000 - Asegurar que LocalizationService esté inicializado
            AgenteIALocalVSIXPackage.InitializeLocalizationServiceOnce();

            DataContext = this;

            // Load first section by default (Product)
            LoadSectionContent("product_name");
        }

        /// <summary>
        /// ToggleButton navigation - mutual exclusion + load content
        /// NUEVO - ID: 20260126_150000
        /// </summary>
        private void NavToggle_Checked(object sender, RoutedEventArgs e)
        {
            var toggle = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (toggle == null || toggle.Tag == null) return;

            // Mutual exclusion (uncheck others)
            if (toggle == NavProductToggle)
            {
                NavCreditsToggle.IsChecked = false;
                NavRepositoryToggle.IsChecked = false;
                NavCompatibilityToggle.IsChecked = false;
                NavSupportToggle.IsChecked = false;
                NavLegalToggle.IsChecked = false;
            }
            else if (toggle == NavCreditsToggle)
            {
                NavProductToggle.IsChecked = false;
                NavRepositoryToggle.IsChecked = false;
                NavCompatibilityToggle.IsChecked = false;
                NavSupportToggle.IsChecked = false;
                NavLegalToggle.IsChecked = false;
            }
            else if (toggle == NavRepositoryToggle)
            {
                NavProductToggle.IsChecked = false;
                NavCreditsToggle.IsChecked = false;
                NavCompatibilityToggle.IsChecked = false;
                NavSupportToggle.IsChecked = false;
                NavLegalToggle.IsChecked = false;
            }
            else if (toggle == NavCompatibilityToggle)
            {
                NavProductToggle.IsChecked = false;
                NavCreditsToggle.IsChecked = false;
                NavRepositoryToggle.IsChecked = false;
                NavSupportToggle.IsChecked = false;
                NavLegalToggle.IsChecked = false;
            }
            else if (toggle == NavSupportToggle)
            {
                NavProductToggle.IsChecked = false;
                NavCreditsToggle.IsChecked = false;
                NavRepositoryToggle.IsChecked = false;
                NavCompatibilityToggle.IsChecked = false;
                NavLegalToggle.IsChecked = false;
            }
            else if (toggle == NavLegalToggle)
            {
                NavProductToggle.IsChecked = false;
                NavCreditsToggle.IsChecked = false;
                NavRepositoryToggle.IsChecked = false;
                NavCompatibilityToggle.IsChecked = false;
                NavSupportToggle.IsChecked = false;
            }

            // Load content
            LoadSectionContent(toggle.Tag.ToString());
        }

        /// <summary>
        /// Load section content dynamically
        /// MODIFICADO - ID: 20260126_151000 - Cada categoría carga TODAS sus sub-secciones
        /// </summary>
        private void LoadSectionContent(string sectionId)
        {
            ContentPanel.Children.Clear();

            if (_aboutService == null)
            {
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = "AboutInfoService not initialized",
                    Style = (Style)FindResource("Text.Body"),
                    Foreground = System.Windows.Media.Brushes.Red
                });
                return;
            }

            try
            {
                switch (sectionId)
                {
                    case "product_name":
                        // MODIFICADO - ID: 20260126_151000 - Producto: Name + Version + Author
                        LoadProductName();
                        LoadProductVersion();
                        LoadProductAuthor();
                        break;

                    case "credits_libraries":
                        // MODIFICADO - ID: 20260126_151000 - Créditos: Libraries + License
                        LoadLibraries();
                        LoadLicense();
                        break;

                    case "repository_source":
                        // MODIFICADO - ID: 20260126_162000 - Repository: TODAS las sub-secciones en Grid 2 cols
                        LoadRepository("repository_all");
                        break;

                    case "compatibility_vs":
                        // MODIFICADO - ID: 20260126_151000 - Compatibilidad: VS + Requirements
                        LoadCompatibility("compatibility_vs");
                        LoadCompatibility("compatibility_requirements");
                        break;

                    case "support_contact":
                        // MODIFICADO - ID: 20260126_151000 - Soporte: Contact Form + Channels
                        LoadSupport("support_contact");
                        LoadSupport("support_channels");
                        break;

                    case "legal_copyright":
                        // MODIFICADO - ID: 20260126_151000 - Legal: Copyright + Terms
                        LoadLegal("legal_copyright");
                        LoadLegal("legal_terms");
                        break;

                    default:
                        ContentPanel.Children.Add(new TextBlock
                        {
                            Text = $"Content for section '{sectionId}' not implemented yet.",
                            Style = (Style)FindResource("Text.Body")
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = $"Error loading content: {ex.Message}",
                    Style = (Style)FindResource("Text.Body"),
                    Foreground = System.Windows.Media.Brushes.Red
                });
            }
        }

        private void LoadProductName()
        {
            var info = _aboutService.GetProductInfo();

            var card = CreateCard();
            var stack = new StackPanel();

            var nameText = new TextBlock
            {
                Text = info.ProductName,
                Style = (Style)FindResource("Text.Headline"),
                FontSize = 28,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(nameText);

            var descText = new TextBlock
            {
                Text = info.Description,
                Style = (Style)FindResource("Text.Body"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            };
            stack.Children.Add(descText);

            card.Child = stack;
            ContentPanel.Children.Add(card);
        }

        private void LoadProductVersion()
        {
            var info = _aboutService.GetProductInfo();

            var card = CreateCard();
            var stack = new StackPanel();

            AddInfoRow(stack, PackIconKind.TagOutline, "Version", info.Version);

            if (info.BuildDate.HasValue)
            {
                AddInfoRow(stack, PackIconKind.CalendarClock, "Build Date", info.BuildDate.Value.ToString("yyyy-MM-dd HH:mm"));
            }

            AddInfoRow(stack, PackIconKind.Identifier, "VSIX ID", info.VsixId);

            card.Child = stack;
            ContentPanel.Children.Add(card);
        }

        private void AddInfoRow(Panel parent, PackIconKind icon, string label, string value)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelStack = new StackPanel { Orientation = Orientation.Horizontal };
            var packIcon = new PackIcon
            {
                Kind = icon,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush")
            };
            labelStack.Children.Add(packIcon);

            var labelText = new TextBlock
            {
                Text = label + ":",
                Style = (Style)FindResource("Text.Body"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            labelStack.Children.Add(labelText);
            Grid.SetColumn(labelStack, 0);
            row.Children.Add(labelStack);

            var valueText = new TextBlock
            {
                Text = value,
                Style = (Style)FindResource("Text.Body"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueText, 1);
            row.Children.Add(valueText);

            parent.Children.Add(row);
        }

        private void LoadProductAuthor()
        {
            var author = _aboutService.GetAuthorInfo();

            // Card container
            var card = CreateCard();

            // Photo + Info layout horizontal
            var horizontalStack = new StackPanel { Orientation = Orientation.Horizontal };

            // Circular photo (if exists)
            try
            {
                var photoUri = new Uri(author.PhotoPath, UriKind.RelativeOrAbsolute);
                var imageBrush = new System.Windows.Media.ImageBrush
                {
                    ImageSource = new System.Windows.Media.Imaging.BitmapImage(photoUri),
                    Stretch = System.Windows.Media.Stretch.UniformToFill
                };

                var photoEllipse = new System.Windows.Shapes.Ellipse
                {
                    Width = 120,
                    Height = 120,
                    Fill = imageBrush,
                    Margin = new Thickness(0, 0, 24, 0)
                };

                horizontalStack.Children.Add(photoEllipse);
            }
            catch
            {
                // Si no hay foto, usar iniciales
                var initialsCircle = new Border
                {
                    Width = 120,
                    Height = 120,
                    CornerRadius = new CornerRadius(60),
                    Background = (System.Windows.Media.Brush)FindResource("AccentBrush"),
                    Margin = new Thickness(0, 0, 24, 0)
                };

                var initialsText = new TextBlock
                {
                    Text = "MD",
                    FontSize = 48,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                initialsCircle.Child = initialsText;
                horizontalStack.Children.Add(initialsCircle);
            }

            // Info column
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var nameText = new TextBlock
            {
                Text = author.Name,
                Style = (Style)FindResource("Text.Headline"),
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 8)
            };
            infoStack.Children.Add(nameText);

            var ageText = new TextBlock
            {
                Text = $"🎂 {author.Age} años • 📅 {author.BirthDate:dd/MM/yyyy}",
                Style = (Style)FindResource("Text.Body"),
                Opacity = 0.8,
                Margin = new Thickness(0, 0, 0, 16)
            };
            infoStack.Children.Add(ageText);

            // Social links con iconos
            var linksWrap = new WrapPanel { Orientation = Orientation.Horizontal };
            AddIconLink(linksWrap, PackIconKind.Linkedin, "LinkedIn", author.LinkedInUrl);
            AddIconLink(linksWrap, PackIconKind.MicrosoftVisualStudio, "Marketplace", author.MarketplaceUrl);
            AddIconLink(linksWrap, PackIconKind.Github, "GitHub", author.GitHubUrl);
            AddIconLink(linksWrap, PackIconKind.Youtube, "YouTube", author.YouTubeUrl);
            AddIconLink(linksWrap, PackIconKind.Web, "Website", author.WebPageUrl);

            infoStack.Children.Add(linksWrap);
            horizontalStack.Children.Add(infoStack);
            card.Child = horizontalStack;

            ContentPanel.Children.Add(card);
        }

        private void LoadLibraries()
        {
            var libs = _aboutService.GetLibraries();
            var loc = AgenteIALocal.Localization.LocalizationProvider.Instance;

            AddHeaderWithIcon(ContentPanel, PackIconKind.PackageVariant, loc["ui.about.content.third_party_libraries"] ?? "Third-Party Libraries");

            // MODIFICADO - ID: 20260126_163000 - Grid 2 cols con CARDS SEPARADOS + i18n
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;
            int col = 0;

            foreach (var lib in libs)
            {
                // CARD SEPARADO para cada librería (patrón banderas)
                var card = new Border
                {
                    Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                    BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, col == 0 ? 8 : 0, 16) // Margen derecha solo en col 0
                };

                var stack = new StackPanel();

                var nameText = new TextBlock
                {
                    Text = lib.Name,
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                stack.Children.Add(nameText);

                var infoText = new TextBlock
                {
                    Text = $"📦 {lib.Version} • ⚖️ {lib.License}",
                    Style = (Style)FindResource("Text.Body"),
                    Opacity = 0.7,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(infoText);

                if (!string.IsNullOrEmpty(lib.Url))
                {
                    var linkPanel = new StackPanel();
                    AddIconLink(linkPanel, PackIconKind.Web, loc["ui.about.content.website"] ?? "Website", lib.Url);
                    stack.Children.Add(linkPanel);
                }

                card.Child = stack;

                // Agregar RowDefinition si no existe
                if (grid.RowDefinitions.Count <= row)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                grid.Children.Add(card);

                // Siguiente columna (2 columnas: 0, 1)
                col++;
                if (col >= 2)
                {
                    col = 0;
                    row++;
                }
            }

            ContentPanel.Children.Add(grid);
        }

        private void LoadLicense()
        {
            var license = _aboutService.GetLicense();

            AddHeadline(license.Type);
            AddBody(license.Text);
            AddHyperlink("View on GitHub", license.Url);
        }

        private void LoadRepository(string sectionId)
        {
            var repo = _aboutService.GetRepository();
            var loc = AgenteIALocal.Localization.LocalizationProvider.Instance;

            // MODIFICADO - ID: 20260126_163000 - Grid 2 cols con 3 CARDS SEPARADOS + i18n
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Card 1: Source Code (fila 0, col 0)
            var sourceCard = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 8, 16)
            };
            var sourceStack = new StackPanel();
            AddHeaderWithIcon(sourceStack, PackIconKind.Github, loc["ui.about.content.source_code"] ?? "Source Code");
            var githubText = new TextBlock
            {
                Text = loc["ui.about.content.repository_source_desc"] ?? "View the complete source code on GitHub",
                Style = (Style)FindResource("Text.Body"),
                Margin = new Thickness(0, 8, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            sourceStack.Children.Add(githubText);
            var sourceLinkPanel = new StackPanel();
            AddIconLink(sourceLinkPanel, PackIconKind.Github, loc["ui.about.content.github_repository"] ?? "GitHub Repository", repo.GitHubUrl);
            sourceStack.Children.Add(sourceLinkPanel);
            sourceCard.Child = sourceStack;
            Grid.SetRow(sourceCard, 0);
            Grid.SetColumn(sourceCard, 0);
            grid.Children.Add(sourceCard);

            // Card 2: Documentation (fila 0, col 1)
            var docsCard = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var docsStack = new StackPanel();
            AddHeaderWithIcon(docsStack, PackIconKind.BookOpenPageVariant, loc["ui.about.content.documentation"] ?? "Documentation");
            var docsText = new TextBlock
            {
                Text = loc["ui.about.content.repository_docs_desc"] ?? "Complete guides, API references, and tutorials",
                Style = (Style)FindResource("Text.Body"),
                Margin = new Thickness(0, 8, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            docsStack.Children.Add(docsText);
            var docsLinkPanel = new StackPanel();
            AddIconLink(docsLinkPanel, PackIconKind.FileDocument, loc["ui.about.content.documentation"] ?? "Documentation", repo.DocsUrl);
            AddIconLink(docsLinkPanel, PackIconKind.FileEdit, loc["ui.about.content.changelog"] ?? "Changelog", repo.ChangelogUrl);
            docsStack.Children.Add(docsLinkPanel);
            docsCard.Child = docsStack;
            Grid.SetRow(docsCard, 0);
            Grid.SetColumn(docsCard, 1);
            grid.Children.Add(docsCard);

            // Card 3: Report Issues (fila 1, col 0 - span 2 para centrar)
            var issuesCard = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 0)
            };
            var issuesStack = new StackPanel();
            AddHeaderWithIcon(issuesStack, PackIconKind.BugOutline, loc["ui.about.content.report_issues"] ?? "Report Issues");
            var issuesText = new TextBlock
            {
                Text = loc["ui.about.content.repository_issues_desc"] ?? "Found a bug or have a feature request? Let us know!",
                Style = (Style)FindResource("Text.Body"),
                Margin = new Thickness(0, 8, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            issuesStack.Children.Add(issuesText);
            var issuesLinkPanel = new StackPanel();
            AddIconLink(issuesLinkPanel, PackIconKind.AlertCircle, loc["ui.about.content.report_issue"] ?? "Report Issue", repo.IssuesUrl);
            issuesStack.Children.Add(issuesLinkPanel);
            issuesCard.Child = issuesStack;
            Grid.SetRow(issuesCard, 1);
            Grid.SetColumn(issuesCard, 0);
            Grid.SetColumnSpan(issuesCard, 2); // Span 2 columnas
            grid.Children.Add(issuesCard);

            ContentPanel.Children.Add(grid);
        }

        // MODIFICADO - ID: 20260126_162001 - Helper que acepta Panel parent
        private void AddHeaderWithIcon(Panel parent, PackIconKind icon, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var packIcon = new PackIcon
            {
                Kind = icon,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush")
            };
            stackPanel.Children.Add(packIcon);

            var textBlock = new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("Text.Headline"),
                VerticalAlignment = VerticalAlignment.Center
            };
            stackPanel.Children.Add(textBlock);

            parent.Children.Add(stackPanel);
        }

        private void LoadCompatibility(string sectionId)
        {
            var compat = _aboutService.GetCompatibility();

            if (sectionId == "compatibility_vs")
            {
                AddHeaderWithIcon(PackIconKind.MicrosoftVisualStudio, "Visual Studio");
                
                var card = CreateCard();
                var stack = new StackPanel();
                
                foreach (var vs in compat.VsVersions)
                {
                    var badge = new Border
                    {
                        Background = (System.Windows.Media.Brush)FindResource("AccentBrush"),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    
                    var badgeText = new TextBlock
                    {
                        Text = $"✓ {vs}",
                        Foreground = System.Windows.Media.Brushes.White,
                        FontWeight = FontWeights.SemiBold
                    };
                    
                    badge.Child = badgeText;
                    stack.Children.Add(badge);
                }
                
                card.Child = stack;
                ContentPanel.Children.Add(card);
            }
            else
            {
                AddHeaderWithIcon(PackIconKind.PackageVariant, "System Requirements");
                
                var card = CreateCard();
                var stack = new StackPanel();
                
                var fwLabel = new TextBlock
                {
                    Text = ".NET Framework:",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(fwLabel);
                
                foreach (var fw in compat.NetFrameworks)
                {
                    var fwText = new TextBlock
                    {
                        Text = $"  ✓ {fw}",
                        Style = (Style)FindResource("Text.Body"),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    stack.Children.Add(fwText);
                }

                var stdLabel = new TextBlock
                {
                    Text = ".NET Standard:",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 16, 0, 8)
                };
                stack.Children.Add(stdLabel);
                
                foreach (var std in compat.NetStandards)
                {
                    var stdText = new TextBlock
                    {
                        Text = $"  ✓ {std}",
                        Style = (Style)FindResource("Text.Body"),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    stack.Children.Add(stdText);
                }
                
                card.Child = stack;
                ContentPanel.Children.Add(card);
            }
        }

        private void LoadSupport(string sectionId)
        {
            var repo = _aboutService.GetRepository();

            if (sectionId == "support_contact")
            {
                AddHeaderWithIcon(PackIconKind.EmailOutline, "Contact Form");
                
                var card = CreateCard();
                var stack = new StackPanel();
                
                var descText = new TextBlock
                {
                    Text = "Send us your questions, suggestions or bug reports:",
                    Style = (Style)FindResource("Text.Body"),
                    Margin = new Thickness(0, 0, 0, 16)
                };
                stack.Children.Add(descText);

                // Email TextBox
                var emailLabel = new TextBlock
                {
                    Text = "Your email:",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                stack.Children.Add(emailLabel);

                var emailBox = new TextBox
                {
                    Name = "ContactEmailBox",
                    Height = 36,
                    Padding = new Thickness(8, 8, 8, 8),
                    Margin = new Thickness(0, 0, 0, 16),
                    Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                    Foreground = (System.Windows.Media.Brush)FindResource("HeaderHighEmphasisBrush"),
                    BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                    BorderThickness = new Thickness(1)
                };
                emailBox.TextChanged += ContactEmail_TextChanged;
                stack.Children.Add(emailBox);

                // Email error message
                var emailError = new TextBlock
                {
                    Name = "ContactEmailError",
                    Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush"),
                    Visibility = Visibility.Collapsed,
                    Margin = new Thickness(0, -12, 0, 12),
                    FontSize = 12
                };
                stack.Children.Add(emailError);

                // Message TextBox
                var messageLabel = new TextBlock
                {
                    Text = "Message:",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                stack.Children.Add(messageLabel);

                var messageBox = new TextBox
                {
                    Name = "ContactMessageBox",
                    Height = 120,
                    Padding = new Thickness(8, 8, 8, 8),
                    Margin = new Thickness(0, 0, 0, 16),
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                    Foreground = (System.Windows.Media.Brush)FindResource("HeaderHighEmphasisBrush"),
                    BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                    BorderThickness = new Thickness(1)
                };
                messageBox.TextChanged += ContactMessage_TextChanged;
                stack.Children.Add(messageBox);

                // Message error
                var messageError = new TextBlock
                {
                    Name = "ContactMessageError",
                    Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush"),
                    Visibility = Visibility.Collapsed,
                    Margin = new Thickness(0, -12, 0, 12),
                    FontSize = 12
                };
                stack.Children.Add(messageError);

                // Send button
                var sendButton = new Button
                {
                    Name = "ContactSendButton",
                    Content = "Send Message",
                    Width = 150,
                    Height = 40,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = (System.Windows.Media.Brush)FindResource("AccentBrush"),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    IsEnabled = false
                };
                sendButton.Click += ContactSend_Click;
                stack.Children.Add(sendButton);

                // Store references for validation
                emailBox.Tag = new { EmailError = emailError, MessageBox = messageBox, MessageError = messageError, SendButton = sendButton };
                messageBox.Tag = new { EmailBox = emailBox, EmailError = emailError, MessageError = messageError, SendButton = sendButton };

                card.Child = stack;
                ContentPanel.Children.Add(card);
            }
            else
            {
                AddHeaderWithIcon(PackIconKind.Lifebuoy, "Support Channels");
                
                // GitHub Issues card
                var issuesCard = CreateCard();
                var issuesStack = new StackPanel();
                var issuesTitle = new TextBlock
                {
                    Text = "GitHub Issues",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                issuesStack.Children.Add(issuesTitle);
                var issuesDesc = new TextBlock
                {
                    Text = "Report bugs or request features",
                    Style = (Style)FindResource("Text.Body"),
                    Opacity = 0.7,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                issuesStack.Children.Add(issuesDesc);
                var issuesLinkPanel = new StackPanel();
                AddIconLink(issuesLinkPanel, PackIconKind.Github, "Issues", repo.IssuesUrl);
                issuesStack.Children.Add(issuesLinkPanel);
                issuesCard.Child = issuesStack;
                ContentPanel.Children.Add(issuesCard);

                // Documentation card
                var docsCard = CreateCard();
                var docsStack = new StackPanel();
                var docsTitle = new TextBlock
                {
                    Text = "Documentation",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                docsStack.Children.Add(docsTitle);
                var docsDesc = new TextBlock
                {
                    Text = "Complete guides and references",
                    Style = (Style)FindResource("Text.Body"),
                    Opacity = 0.7,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                docsStack.Children.Add(docsDesc);
                var docsLinkPanel = new StackPanel();
                AddIconLink(docsLinkPanel, PackIconKind.FileDocument, "Docs", repo.DocsUrl);
                docsStack.Children.Add(docsLinkPanel);
                docsCard.Child = docsStack;
                ContentPanel.Children.Add(docsCard);
            }
        }

        private void LoadLegal(string sectionId)
        {
            if (sectionId == "legal_copyright")
            {
                AddHeaderWithIcon(PackIconKind.Copyright, "Copyright");
                
                var card = CreateCard();
                var stack = new StackPanel();
                
                var info = _aboutService.GetProductInfo();
                var copyrightText = new TextBlock
                {
                    Text = $"© {DateTime.Now.Year} {info.Author}. All rights reserved.",
                    Style = (Style)FindResource("Text.Body"),
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 8)
                };
                stack.Children.Add(copyrightText);
                
                card.Child = stack;
                ContentPanel.Children.Add(card);
            }
            else
            {
                AddHeaderWithIcon(PackIconKind.FileDocumentOutline, "Terms of Use");
                
                var card = CreateCard();
                var stack = new StackPanel();
                
                var termsText = new TextBlock
                {
                    Text = "This software is provided 'as is', without warranties of any kind.",
                    Style = (Style)FindResource("Text.Body"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                stack.Children.Add(termsText);
                
                var license = _aboutService.GetLicense();
                var licenseText = new TextBlock
                {
                    Text = $"License: {license.Type}",
                    Style = (Style)FindResource("Text.Body"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(licenseText);
                
                var linkPanel = new StackPanel();
                AddIconLink(linkPanel, PackIconKind.FileDocument, "Full License", license.Url);
                stack.Children.Add(linkPanel);
                
                card.Child = stack;
                ContentPanel.Children.Add(card);
            }
        }

        // Helper methods to add content
        private void AddHeadline(string text)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("Text.Headline"),
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        private void AddLabel(string text)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("Text.Body"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 4)
            });
        }

        private void AddBody(string text)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("Text.Body"),
                Margin = new Thickness(0, 0, 0, 4)
            });
        }

        private void AddHyperlink(string displayText, string url)
        {
            var hyperlink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(displayText))
            {
                NavigateUri = new Uri(url),
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush")
            };
            hyperlink.RequestNavigate += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                    e.Handled = true;
                }
                catch { }
            };

            var textBlock = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 4)
            };
            textBlock.Inlines.Add(hyperlink);
            ContentPanel.Children.Add(textBlock);
        }

        private void AddSpacer()
        {
            ContentPanel.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Height = 16
            });
        }

        // NUEVO - ID: 20260126_134000 - Helper methods para UI mejorada
        private Border CreateCard()
        {
            return new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("HeaderMediumEmphasisBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private void AddIconLink(Panel parent, PackIconKind icon, string displayText, string url)
        {
            var button = new Button
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6),
                Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("AccentBrush"),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = url,
                OverridesDefaultStyle = true,
                HorizontalAlignment = HorizontalAlignment.Right // NUEVO - ID: 20260126_154000 - Alineado derecha
            };

            // MODIFICADO - ID: 20260126_154000 - Template con hover negro
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.Name = "Bd";
            factory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            factory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var contentPresenter = new FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            contentPresenter.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentPresenter);

            template.VisualTree = factory;
            button.Template = template;

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var packIcon = new PackIcon
            {
                Kind = icon,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush")
            };
            stackPanel.Children.Add(packIcon);

            var textBlock = new TextBlock
            {
                Text = displayText,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("HeaderHighEmphasisBrush")
            };
            stackPanel.Children.Add(textBlock);

            button.Content = stackPanel;
            button.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            };

            // MODIFICADO - ID: 20260126_154000 - Hover NARANJA + BORDE/TEXTO NEGRO
            var orangeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x95, 0x00));
            
            button.MouseEnter += (s, e) =>
            {
                button.Background = orangeBrush;
                button.BorderBrush = System.Windows.Media.Brushes.Black; // Negro
                textBlock.Foreground = System.Windows.Media.Brushes.Black; // Negro
                packIcon.Foreground = System.Windows.Media.Brushes.Black; // Negro
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = (System.Windows.Media.Brush)FindResource("HeaderBackgroundBrush");
                button.BorderBrush = (System.Windows.Media.Brush)FindResource("AccentBrush");
                textBlock.Foreground = (System.Windows.Media.Brush)FindResource("HeaderHighEmphasisBrush");
                packIcon.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            };

            parent.Children.Add(button);
        }

        private void AddHeaderWithIcon(PackIconKind icon, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 16, 0, 12)
            };

            var packIcon = new PackIcon
            {
                Kind = icon,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush")
            };
            stackPanel.Children.Add(packIcon);

            var textBlock = new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("Text.Headline"),
                VerticalAlignment = VerticalAlignment.Center
            };
            stackPanel.Children.Add(textBlock);

            ContentPanel.Children.Add(stackPanel);
        }

        /// <summary>
        /// Close button click - close window
        /// NUEVO - ID: 20260126_122004
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Header drag - enable window dragging
        /// NUEVO - ID: 20260126_145000
        /// </summary>
        private void HeaderDragArea_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // NUEVO - ID: 20260126_140000 - E2+E5: Contact form validation + send
        private void ContactEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            var emailBox = sender as TextBox;
            if (emailBox == null || emailBox.Tag == null) return;

            dynamic refs = emailBox.Tag;
            TextBlock emailError = refs.EmailError;
            TextBox messageBox = refs.MessageBox;
            Button sendButton = refs.SendButton;

            var email = emailBox.Text;
            var isEmailValid = !string.IsNullOrWhiteSpace(email) && System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            if (string.IsNullOrWhiteSpace(email))
            {
                emailError.Text = "Email is required";
                emailError.Visibility = Visibility.Visible;
            }
            else if (!isEmailValid)
            {
                emailError.Text = "Invalid email format";
                emailError.Visibility = Visibility.Visible;
            }
            else
            {
                emailError.Visibility = Visibility.Collapsed;
            }

            // Update send button state
            var isMessageValid = !string.IsNullOrWhiteSpace(messageBox.Text) && messageBox.Text.Length >= 10;
            sendButton.IsEnabled = isEmailValid && isMessageValid;
        }

        private void ContactMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            var messageBox = sender as TextBox;
            if (messageBox == null || messageBox.Tag == null) return;

            dynamic refs = messageBox.Tag;
            TextBox emailBox = refs.EmailBox;
            TextBlock messageError = refs.MessageError;
            Button sendButton = refs.SendButton;

            var message = messageBox.Text;
            var isMessageValid = !string.IsNullOrWhiteSpace(message) && message.Length >= 10;

            if (string.IsNullOrWhiteSpace(message))
            {
                messageError.Text = "Message is required";
                messageError.Visibility = Visibility.Visible;
            }
            else if (message.Length < 10)
            {
                messageError.Text = "Message must be at least 10 characters";
                messageError.Visibility = Visibility.Visible;
            }
            else
            {
                messageError.Visibility = Visibility.Collapsed;
            }

            // Update send button state
            var email = emailBox.Text;
            var isEmailValid = !string.IsNullOrWhiteSpace(email) && System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            sendButton.IsEnabled = isEmailValid && isMessageValid;
        }

        private async void ContactSend_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Find email and message boxes in ContentPanel
            TextBox emailBox = null;
            TextBox messageBox = null;

            foreach (UIElement child in ContentPanel.Children)
            {
                if (child is Border border && border.Child is StackPanel stack)
                {
                    foreach (var item in stack.Children)
                    {
                        if (item is TextBox tb)
                        {
                            if (tb.Name == "ContactEmailBox") emailBox = tb;
                            if (tb.Name == "ContactMessageBox") messageBox = tb;
                        }
                    }
                }
            }

            if (emailBox == null || messageBox == null) return;

            var email = emailBox.Text;
            var message = messageBox.Text;

            // Disable button and show loading
            button.IsEnabled = false;
            button.Content = "Sending...";

            try
            {
                // Open mailto: as fallback (E4 - mailto: fallback implementation)
                var subject = Uri.EscapeDataString("Contact from Agente IA Local");
                var body = Uri.EscapeDataString($"From: {email}\n\nMessage:\n{message}");
                var mailtoUrl = $"mailto:soporte@mdesantis.com.ar?subject={subject}&body={body}";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailtoUrl) { UseShellExecute = true });

                // Clear form
                emailBox.Clear();
                messageBox.Clear();

                // Show success
                MessageBox.Show("Your default email client has been opened with the message. Please send it from there.", "Message Prepared", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "Send Message";
            }
        }
    }
}
