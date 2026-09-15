using System.Windows;
using System.Windows.Controls;

using Bloxstrap.UI.ViewModels.Installer;

namespace Bloxstrap.UI.Elements.Installer.Pages
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class InstallPage
    {
        private InstallViewModel _viewModel = null!;

        public InstallPage()
        {
            InitializeComponent();
        }

        private void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow window)
            {
                _viewModel = window.InstallViewModel;
                DataContext = _viewModel;

                _viewModel.SetCanContinueEvent += (_, state) => window.SetButtonEnabled("next", state);

                window.SetNextButtonText(Strings.Common_Navigation_Install);
                window.SetButtonEnabled("next", true);
                window.NextPageCallback += NextPageCallback;
            }
        }

        public bool NextPageCallback() => _viewModel.ValidateInstall();
    }
}
