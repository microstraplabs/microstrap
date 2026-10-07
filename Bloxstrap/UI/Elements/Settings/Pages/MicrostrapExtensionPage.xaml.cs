using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class MicrostrapExtensionPage
    {
        public MicrostrapExtensionPage() => InitializeComponent();

        private async void InstallLocalExtension_Click(object sender, RoutedEventArgs e)
        {
            InstallLocalExtensionButton.IsEnabled = false;
            try
            {
                InstallStatus.Text = "Downloading the official Microstrap browser extension…";
                string directory = await DownloadAndExtractExtensionAsync();
                InstallStatus.Text = $"Downloaded extension to:\n{directory}\n\nIn your browser's extensions page, enable Developer mode and choose Load unpacked. The folder is open for you to select.";
                OpenPath(directory);
                OpenBrowserExtensionsPage();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapExtensionPage::InstallLocalExtension_Click", ex);
                InstallStatus.Text = $"Couldn't install the extension locally: {ex.Message}\nDownload the Chromium ZIP from GitHub Releases and load its extracted folder manually.";
                Utilities.ShellExecute($"https://github.com/{App.ProjectRepository}/releases/latest");
            }
            finally
            {
                InstallLocalExtensionButton.IsEnabled = true;
            }
        }

        private void OpenInstructions_Click(object sender, RoutedEventArgs e)
        {
            string directory = FindExtensionDirectory();
            InstallStatus.Text = String.IsNullOrEmpty(directory)
                ? "Click Install local extension to download and prepare its folder."
                : $"Load this local folder as an unpacked extension: {directory}";
            if (!String.IsNullOrEmpty(directory))
                OpenPath(directory);
            OpenBrowserExtensionsPage();
        }

        private async Task<string> DownloadAndExtractExtensionAsync()
        {
            var release = await Http.GetJson<GithubRelease>($"https://api.github.com/repos/{App.ProjectRepository}/releases/latest");
            var asset = release.Assets?.FirstOrDefault(item => item.Name.StartsWith("MicrostrapExtension-", StringComparison.OrdinalIgnoreCase)
                && item.Name.EndsWith("-chromium.zip", StringComparison.OrdinalIgnoreCase));
            if (asset is null)
                throw new FileNotFoundException("The latest public release does not contain the Chromium extension ZIP.");

            string destinationRoot = Path.Combine(Paths.Base, "Extensions", "MicrostrapExtension");
            string destinationPrefix = Path.GetFullPath(destinationRoot) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(destinationRoot);
            Directory.CreateDirectory(Paths.Temp);

            byte[] bytes = await App.HttpClient.GetByteArrayAsync(asset.BrowserDownloadUrl);
            if (!String.IsNullOrWhiteSpace(asset.Digest) && asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            {
                using var sha = SHA256.Create();
                string actual = Convert.ToHexString(sha.ComputeHash(bytes));
                if (!actual.Equals(asset.Digest[7..], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded extension failed GitHub's SHA-256 integrity check.");
            }

            string zipPath = Path.Combine(Paths.Temp, asset.Name);
            await File.WriteAllBytesAsync(zipPath, bytes);
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                string path = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
                if (!path.StartsWith(destinationPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The extension archive contains an unsafe path.");
                if (String.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(path);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                entry.ExtractToFile(path, true);
            }

            if (!File.Exists(Path.Combine(destinationRoot, "manifest.json")))
                throw new InvalidDataException("The extension archive is missing manifest.json.");
            return destinationRoot;
        }

        private static string FindExtensionDirectory()
        {
            string exeDirectory = Path.GetDirectoryName(Paths.Process) ?? "";
            string[] candidates =
            {
                Path.Combine(exeDirectory, "MicrostrapExtension"),
                Path.Combine(exeDirectory, "Extensions", "MicrostrapExtension"),
                Path.Combine(Paths.Base, "Extensions", "MicrostrapExtension"),
                Path.Combine(AppContext.BaseDirectory, "MicrostrapExtension")
            };
            return candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "manifest.json"))) ?? "";
        }

        private static void OpenBrowserExtensionsPage()
        {
            var browser = FindBrowser("Google\\Chrome\\Application\\chrome.exe")
                ?? FindBrowser("Microsoft\\Edge\\Application\\msedge.exe");
            if (browser is null)
            {
                Frontend.ShowMessageBox("Open Chrome or Edge and navigate to chrome://extensions or edge://extensions. Enable Developer mode, choose Load unpacked, then select the MicrostrapExtension folder.", MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = browser.Value.Executable,
                    Arguments = $"--new-tab {browser.Value.ExtensionsPage}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapExtensionPage::OpenBrowserExtensionsPage", ex);
                Frontend.ShowMessageBox("Open your browser extensions page, enable Developer mode, and select Load unpacked.", MessageBoxImage.Information);
            }
        }

        private static (string Executable, string ExtensionsPage)? FindBrowser(string relativePath)
        {
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            string? executable = roots.Select(root => Path.Combine(root, relativePath)).FirstOrDefault(File.Exists);
            if (executable is null)
                return null;
            string page = relativePath.StartsWith("Google\\", StringComparison.OrdinalIgnoreCase)
                ? "chrome://extensions"
                : "edge://extensions";
            return (executable, page);
        }

        private static void OpenPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapExtensionPage::OpenPath", ex);
                Frontend.ShowMessageBox($"Open this folder in Explorer and select it with Load unpacked:\n\n{path}", MessageBoxImage.Information);
            }
        }
    }
}
