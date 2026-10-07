using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class MicrostrapNewsPage
    {
        private IReadOnlyList<RobloxNewsTopic> _topics = Array.Empty<RobloxNewsTopic>();

        public MicrostrapNewsPage() => InitializeComponent();

        private async void Page_Loaded(object sender, RoutedEventArgs e) => await RefreshFeedAsync();

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshFeedAsync();

        private void Status_Click(object sender, RoutedEventArgs e) => Utilities.ShellExecute("https://status.roblox.com");

        private async Task RefreshFeedAsync()
        {
            FeedStatus.Text = "Loading official Roblox updates…";
            try
            {
                _topics = await RobloxNewsFeed.GetAnnouncementsAsync();
                if (_topics.Count == 0)
                {
                    FeedStatus.Text = "No Roblox updates are available right now.";
                    NewsItems.Children.Clear();
                    return;
                }

                FeedStatus.Text = $"{_topics.Count} official announcements · Roblox Developer Forum";
                RenderTopics();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapNewsPage::RefreshFeedAsync", ex);
                FeedStatus.Text = "Couldn't load live news. Check your connection, or visit the DevForum directly.";
                NewsItems.Children.Clear();
                var open = new Button { Content = "Open Roblox announcements", HorizontalAlignment = HorizontalAlignment.Left };
                open.Click += (_, _) => Utilities.ShellExecute("https://devforum.roblox.com/c/updates/announcements/36");
                NewsItems.Children.Add(open);
            }
        }

        private void RenderTopics()
        {
            NewsItems.Children.Clear();
            foreach (var topic in _topics.Take(20))
            {
                var card = new Border
                {
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 9),
                    CornerRadius = new CornerRadius(12),
                    Background = (Brush)FindResource("CardBackgroundFillColorDefaultBrush"),
                    BorderBrush = (Brush)FindResource("ControlElevationBorderBrush"),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Child = CreateTopicContent(topic)
                };

                card.MouseLeftButtonUp += (_, _) => Utilities.ShellExecute(topic.Url);
                NewsItems.Children.Add(card);
            }
        }

        private static UIElement CreateTopicContent(RobloxNewsTopic topic)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = topic.DisplayTitle,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(new TextBlock
            {
                Text = $"{topic.Category}  ·  {topic.CreatedAt.ToLocalTime():MMM d, yyyy}",
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 11,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush")
            });

            string summary = topic.Summary;
            if (!String.IsNullOrWhiteSpace(summary))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = summary,
                    Margin = new Thickness(0, 8, 0, 0),
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 58
                });
            }
            return stack;
        }
    }
}
