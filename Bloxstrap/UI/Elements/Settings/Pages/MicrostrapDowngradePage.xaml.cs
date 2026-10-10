using System.Security.Cryptography;
using System.Windows;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class MicrostrapDowngradePage
    {
        private bool _isDowngrading;

        public MicrostrapDowngradePage()
        {
            InitializeComponent();
            CurrentVersionText.Text = $"Installed version: {App.Version}";
        }

        private async void Downgrade_Click(object sender, RoutedEventArgs e)
        {
            var answer = Frontend.ShowMessageBox(
                "This feature is in beta. It may auto update old Microstrap releases or break your Microstrap build. Proceed?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (answer != MessageBoxResult.Yes || _isDowngrading)
                return;

            _isDowngrading = true;
            DowngradeButton.IsEnabled = false;
            DowngradeStatus.Text = "Finding an earlier official release…";

            try
            {
                var releases = await Http.GetJson<List<GithubRelease>>($"https://api.github.com/repos/{App.ProjectRepository}/releases?per_page=100");
                Version currentVersion = Version.Parse(App.Version);
                var release = releases?
                    .Where(item => item.Assets is { Count: > 0 })
                    .Select(item => new { Release = item, Parsed = Version.TryParse(item.TagName.TrimStart('v'), out var version) ? version : null })
                    .Where(item => item.Parsed is not null && item.Parsed < currentVersion)
                    .OrderByDescending(item => item.Parsed)
                    .Select(item => item.Release)
                    .FirstOrDefault();

                var asset = release?.Assets?.FirstOrDefault(item => item.Name.Equals("Microstrap.exe", StringComparison.OrdinalIgnoreCase));
                if (asset is null)
                    throw new FileNotFoundException("No earlier official release with a Microstrap.exe installer was found.");
                if (!Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var assetUri)
                    || assetUri.Scheme != Uri.UriSchemeHttps
                    || !assetUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("GitHub returned an unexpected download URL.");

                DowngradeStatus.Text = $"Downloading official {release!.TagName}…";
                byte[] bytes = await App.HttpClient.GetByteArrayAsync(asset.BrowserDownloadUrl);

                if (!String.IsNullOrWhiteSpace(asset.Digest) && asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    string digest = Convert.ToHexString(SHA256.HashData(bytes));
                    if (!digest.Equals(asset.Digest[7..], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The downloaded release failed GitHub's SHA-256 integrity check.");
                }

                Directory.CreateDirectory(Paths.Temp);
                string executable = Path.Combine(Paths.Temp, $"Microstrap-{release.TagName}.exe");
                await File.WriteAllBytesAsync(executable, bytes);
                try
                {
                    string? fileVersion = FileVersionInfo.GetVersionInfo(executable).FileVersion;
                    if (!Version.TryParse(fileVersion, out var downloadedVersion)
                        || downloadedVersion != Version.Parse(release.TagName.TrimStart('v')))
                        throw new InvalidDataException("The downloaded executable version does not match the selected GitHub release.");
                }
                catch
                {
                    File.Delete(executable);
                    throw;
                }
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = true
                };
                startInfo.ArgumentList.Add("-upgrade");
                System.Diagnostics.Process.Start(startInfo);

                DowngradeStatus.Text = $"The official {release.TagName} executable has been downloaded and verified. Microstrap will close and install that version.";
                await Task.Delay(900);
                App.Terminate();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("MicrostrapDowngradePage::Downgrade_Click", ex);
                DowngradeStatus.Text = $"Couldn't downgrade Microstrap: {ex.Message}";
                _isDowngrading = false;
                DowngradeButton.IsEnabled = true;
            }
        }
    }
}
