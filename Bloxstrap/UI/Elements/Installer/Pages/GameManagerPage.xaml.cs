using System.Windows;

using Bloxstrap.UI.ViewModels.Installer;

namespace Bloxstrap.UI.Elements.Installer.Pages
{
    /// <summary>
    /// Interaction logic for GameManagerPage.xaml
    /// </summary>
    public partial class GameManagerPage
    {
        private readonly GameManagerViewModel _viewModel = new();

        public GameManagerPage()
        {
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow window)
                window.SetNextButtonText(Strings.Common_Navigation_Next);
        }
    }
}
