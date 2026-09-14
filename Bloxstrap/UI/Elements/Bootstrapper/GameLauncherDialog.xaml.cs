using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shell;

using Bloxstrap.UI.ViewModels.Bootstrapper;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    public partial class GameLauncherDialog : IBootstrapperDialog
    {
        private readonly GameLauncherViewModel _viewModel;
        private bool _isClosing;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        public string Message
        {
            get => _viewModel.Message;
            set => _viewModel.Message = value;
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set
            {
                _viewModel.ProgressIndeterminate = value == ProgressBarStyle.Marquee;
                _viewModel.OnPropertyChanged(nameof(GameLauncherViewModel.ProgressIndeterminate));
            }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set => _viewModel.ProgressValue = value;
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set => _viewModel.ProgressMaximum = value;
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set => _viewModel.TaskbarProgressState = value;
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set => _viewModel.TaskbarProgressValue = value;
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set => _viewModel.CancelEnabled = value;
        }

        public GameLauncherDialog(RecommendedGame game)
        {
            _viewModel = new GameLauncherViewModel(game);
            DataContext = _viewModel;

            InitializeComponent();

            SearchBox.TextChanged += (sender, e) => _viewModel.SearchTerm = SearchBox.Text;

            GamesList.SelectionChanged += (sender, e) =>
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is RecommendedGame selectedGame)
                {
                    _viewModel.SelectGame(selectedGame);

                    // retarget the pending launch to the newly selected experience
                    Bootstrapper?.SetLaunchCommandLine($"roblox://experiences/start?placeId={selectedGame.PlaceId}");
                }
            };

            // infinite scroll - load the next page of games from Roblox as the
            // user scrolls towards the end of the list, until the cap is hit
            GamesList.Loaded += (sender, e) =>
            {
                var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(GamesList);

                if (scrollViewer is not null)
                {
                    scrollViewer.ScrollChanged += (sender, e) =>
                    {
                        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 4)
                            _ = _viewModel.LoadMoreGamesAsync();
                    };
                }
            };

            _ = _viewModel.InitializeAsync();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        public void ShowBootstrapper() => ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(Close);
        }

        public void ShowSuccess(string message, Action? callback = null)
        {
            Message = message;
            callback?.Invoke();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

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
