using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using Bloxstrap.UI.Elements.About;

namespace Bloxstrap.UI.ViewModels.Installer
{
    public class LaunchMenuViewModel
    {
        public string Version => string.Format(Strings.Menu_About_Version, App.Version);

        public string PlayerStatus => App.IsPlayerInstalled ? "Ready to play" : "Installs on first launch";

        public string StudioStatus => App.IsStudioInstalled ? "Ready to create" : "Optional";

        public string SetupStatus => App.IsPlayerInstalled ? "Microstrap is ready for Roblox" : "Roblox will be set up automatically";

        public ICommand LaunchSettingsCommand => new RelayCommand(LaunchSettings);

        public ICommand LaunchRobloxCommand => new RelayCommand(LaunchRoblox);

        public ICommand LaunchRobloxStudioCommand => new RelayCommand(LaunchRobloxStudio);

        public ICommand LaunchAboutCommand => new RelayCommand(LaunchAbout);

        public ICommand LaunchGameManagerCommand => new RelayCommand(LaunchGameManager);

        public event EventHandler<NextAction>? CloseWindowRequest;

        private void LaunchSettings() => CloseWindowRequest?.Invoke(this, NextAction.LaunchSettings);

        private void LaunchRoblox() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRoblox);

        private void LaunchRobloxStudio() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxStudio);

        private void LaunchAbout() => new MainWindow().ShowDialog();

        private void LaunchGameManager() => CloseWindowRequest?.Invoke(this, NextAction.LaunchGameManager);
    }
}
