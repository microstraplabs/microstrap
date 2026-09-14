using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace Bloxstrap.GameManager
{
    public partial class App : Application
    {
        public static readonly HttpClient HttpClient = new(
            new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }
        );

        public App()
        {
            HttpClient.Timeout = TimeSpan.FromSeconds(30);
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "MicrostrapGameManager/0.1.0");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show($"An unexpected error occurred:\n\n{args.Exception}", "Game Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
