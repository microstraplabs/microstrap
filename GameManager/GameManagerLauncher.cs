using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Bloxstrap.GameManager
{
    /// <summary>
    /// Launches games by asking Microstrap to play a place. Microstrap then
    /// shows the same loading screen it uses everywhere else.
    /// </summary>
    public static class GameManagerLauncher
    {
        private static string MicrostrapLocation
        {
            get
            {
                // GameManager lives in <Microstrap install>\GameManager
                string? installFolder = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));

                if (installFolder is not null)
                {
                    string candidate = Path.Combine(installFolder, "Microstrap.exe");

                    if (File.Exists(candidate))
                        return candidate;
                }

                return "Microstrap";
            }
        }

        public static void Launch(long placeId)
        {
            Launch($"{placeId}");
        }

        public static void Launch(string placeId)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = MicrostrapLocation,
                        Arguments = $"-playplace {placeId}",
                        UseShellExecute = true
                    }
                };

                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start Microstrap:\n\n{ex.Message}", "Game Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
