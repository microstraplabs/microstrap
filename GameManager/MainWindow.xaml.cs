using System.Windows;

namespace Bloxstrap.GameManager
{
    public partial class MainWindow : Wpf.Ui.Controls.UiWindow
    {
        private readonly MainViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;

            SearchBox.TextChanged += (_, _) => _viewModel.SearchTerm = SearchBox.Text;

            GamesScrollViewer.ScrollChanged += (_, e) =>
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 24)
                    _viewModel.LoadMoreGamesCommand.Execute(null);
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
