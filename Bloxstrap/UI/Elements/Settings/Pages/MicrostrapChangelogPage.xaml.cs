using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bloxstrap.UI.Elements.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class MicrostrapChangelogPage
    {
        public MicrostrapChangelogPage() => InitializeComponent();

        private async void Page_Loaded(object sender, RoutedEventArgs e) => await RefreshReleasesAsync();

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshReleasesAsync();

        private async Task RefreshReleasesAsync()
        {
            ReleaseStatus.Text = "Loading published releases…";
            ReleaseItems.Children.Clear();
            try
            {
                var releases = await Http.GetJson<List<GithubRelease>>($"https://api.github.com/repos/{App.ProjectRepository}/releases?per_page=20");
                releases ??= new List<GithubRelease>();
                if (releases.Count == 0)
                {
                    ReleaseStatus.Text = "No published releases found.";
                    return;
                }

                ReleaseStatus.Text = $"Latest {releases.Count} published releases";
                foreach (var release in releases)
                {
                    var card = new Border
                    {
                        Padding = new Thickness(16),
                        Margin = new Thickness(0, 0, 0, 10),
                        CornerRadius = new CornerRadius(12),
                        Background = (Brush)FindResource("CardBackgroundFillColorDefaultBrush"),
                        BorderBrush = (Brush)FindResource("ControlElevationBorderBrush"),
                        BorderThickness = new Thickness(1)
                    };
                    var content = new StackPanel();
                    var header = new StackPanel { Orientation = Orientation.Horizontal };
                    header.Children.Add(new Border
                    {
                        Padding = new Thickness(9, 4, 9, 4),
                        CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush(Color.FromRgb(60, 35, 39)),
                        Child = new TextBlock { Text = release.TagName, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(255, 126, 73)) }
                    });
                    header.Children.Add(new TextBlock { Margin = new Thickness(10, 4, 0, 0), Text = release.Name, FontSize = 16, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
                    content.Children.Add(header);
                    content.Children.Add(new TextBlock
                    {
                        Margin = new Thickness(0, 8, 0, 0),
                        Text = DateTime.TryParse(release.CreatedAt, out var date) ? date.ToLocalTime().ToString("MMMM d, yyyy") : "Published release",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("TextFillColorTertiaryBrush")
                    });

                    var notes = new MarkdownTextBlock
                    {
                        Margin = new Thickness(0, 12, 0, 0),
                        MarkdownText = String.IsNullOrWhiteSpace(release.Body) ? "No release notes were provided." : release.Body.Trim(),
                        FontSize = 13,
                        Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                        TextWrapping = TextWrapping.Wrap
                    };
                    content.Children.Add(notes);
                    var url = $"https://github.com/{App.ProjectRepository}/releases/tag/{Uri.EscapeDataString(release.TagName)}";
                    var open = new Button { Content = "View on GitHub", Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
                    open.Click += (_, _) => Utilities.ShellExecute(url);
                    content.Children.Add(open);
                    card.Child = content;
                    ReleaseItems.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapChangelogPage::RefreshReleasesAsync", ex);
                ReleaseStatus.Text = "Couldn't load release notes. Check your connection, or open GitHub Releases.";
                var open = new Button { Content = "Open GitHub Releases", HorizontalAlignment = HorizontalAlignment.Left };
                open.Click += (_, _) => Utilities.ShellExecute($"https://github.com/{App.ProjectRepository}/releases");
                ReleaseItems.Children.Add(open);
            }
        }
    }
}
