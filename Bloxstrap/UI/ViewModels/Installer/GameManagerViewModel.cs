namespace Bloxstrap.UI.ViewModels.Installer
{
    public class GameManagerViewModel : NotifyPropertyChangedViewModel
    {
        private bool _installGameManager = true;

        /// <summary>
        /// Mirrors the shared installer options object.
        /// </summary>
        public bool InstallGameManager
        {
            get => _installGameManager;
            set
            {
                _installGameManager = value;
                App.InstallOptions.InstallGameManager = value;
                OnPropertyChanged(nameof(InstallGameManager));
                OnPropertyChanged(nameof(NoGameManager));
            }
        }

        public bool NoGameManager
        {
            get => !InstallGameManager;
            set => InstallGameManager = !value;
        }
    }
}
