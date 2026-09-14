using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for GamesPage.xaml
    /// </summary>
    public partial class GamesPage
    {
        private readonly GamesViewModel _viewModel;

        public GamesPage()
        {
            DataContext = _viewModel = new GamesViewModel();

            InitializeComponent();

            Loaded += (_, _) =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(this);

                if (scrollViewer is not null)
                {
                    scrollViewer.ScrollChanged += (_, e) =>
                    {
                        // infinite scroll - grab the next page of games from
                        // Roblox as the user approaches the end of the page
                        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 8)
                            _viewModel.LoadMoreGamesCommand.Execute(null);
                    };
                }
            };
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var nested = FindVisualChild<T>(child);

                if (nested is not null)
                    return nested;
            }

            return null;
        }
    }
}
