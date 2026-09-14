using System.ComponentModel;

using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Progress dialog shown while the Game Manager is being installed or uninstalled.
    /// </summary>
    public partial class GameManagerProgressDialog : WpfUiWindow
    {
        private readonly GameManagerProgressViewModel _viewModel = new();

        public GameManagerProgressDialog()
        {
            DataContext = _viewModel;
            InitializeComponent();
        }

        public GameManagerProgressViewModel ViewModel => _viewModel;

        /// <summary>
        /// Runs the given action on a background thread while showing progress,
        /// and closes the dialog once it's done.
        /// </summary>
        public void Run(string title, Action<GameManagerProgressViewModel> action)
        {
            _viewModel.Title = title;
            _viewModel.Message = "";
            _viewModel.ProgressValue = 0;
            _viewModel.ProgressText = "0%";

            var thread = new Thread(() =>
            {
                try
                {
                    action(_viewModel);
                }
                finally
                {
                    Dispatcher.BeginInvoke(Close);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            ShowDialog();
        }
    }

    public class GameManagerProgressViewModel : INotifyPropertyChanged
    {
        private string _title = "";
        private string _message = "";
        private int _progressValue;
        private string _progressText = "0%";

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public void SetProgress(int percent, string message)
        {
            // clamp - the copy loop can overshoot slightly between updates
            ProgressValue = Math.Clamp(percent, 0, 100);
            ProgressText = $"{Math.Clamp(percent, 0, 100)}%";
            Message = message;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
