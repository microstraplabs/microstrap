using System.Windows;

namespace Bloxstrap.UI.Elements.Installer.Pages
{
    public partial class InstallProgressPage
    {
        private bool _started;

        public InstallProgressPage()
        {
            InitializeComponent();
        }

        private async void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_started || Window.GetWindow(this) is not MainWindow window)
                return;

            _started = true;
            window.SetNextButtonText("Installing...");
            window.SetButtonEnabled("next", false);
            window.SetButtonEnabled("back", false);

            try
            {
                await Task.Run(() => window.InstallViewModel.DoInstall());
                window.Navigate(typeof(GameManagerPage));
                window.SetNextButtonText(Strings.Common_Navigation_Next);
                window.SetButtonEnabled("next", true);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("InstallProgressPage", ex);
                Frontend.ShowExceptionDialog(ex);
                window.Navigate(typeof(InstallPage));
                window.SetNextButtonText(Strings.Common_Navigation_Install);
                window.SetButtonEnabled("next", true);
            }
        }
    }
}
