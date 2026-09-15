using System.Diagnostics;
using System.Windows;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class MicrostrapHomePage
    {
        public MicrostrapHomePage() => InitializeComponent();

        // TEMPORARY: adds Microstrap's install locations to Microsoft Defender exclusions.
        // This exists to work around antivirus false positives (e.g. Trojan:Win32/Cloxer)
        // and should be removed once the executable is code-signed and has built up reputation.
        private void AddToExclusions_Click(object sender, RoutedEventArgs e)
        {
            const string logId = "MicrostrapHomePage::AddToExclusions_Click";

            // the install folder covers everything underneath it (Versions, Downloads,
            // GameManager, ...), and the executables are added in case they sit elsewhere
            var targets = new List<string>();

            void Add(string? path)
            {
                if (!String.IsNullOrEmpty(path) && !targets.Contains(path, StringComparer.OrdinalIgnoreCase))
                    targets.Add(path!);
            }

            Add(Paths.Base);
            Add(Paths.Application);
            Add(Paths.Process);

            if (targets.All(x => !File.Exists(x) && !Directory.Exists(x)))
            {
                Frontend.ShowMessageBox("Could not find the Microstrap install location to exclude.", MessageBoxImage.Error);
                return;
            }

            // double any single quotes so the paths survive PowerShell's quoting
            string arguments = String.Join(" ", targets.Select(x => $"-ExclusionPath '{x.Replace("'", "''")}'"));
            string powerShellArgs = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Add-MpPreference {arguments}\"";

            App.Logger.WriteLine(logId, $"Adding Microsoft Defender exclusions: {String.Join(", ", targets)}");

            // try without elevation first, then escalate through UAC if Defender refuses
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = powerShellArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            string errors = "";

            try
            {
                using var process = Process.Start(startInfo)!;

                process.StandardOutput.ReadToEnd();
                errors = process.StandardError.ReadToEnd().TrimEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && String.IsNullOrEmpty(errors))
                {
                    App.Logger.WriteLine(logId, "Exclusions added successfully.");

                    Frontend.ShowMessageBox(
                        $"Microsoft Defender exclusions were added successfully for:\n\n{String.Join("\n", targets)}\n\n" +
                        "If Microstrap was flagged earlier, you may also need to restore it from quarantine:\n" +
                        "Windows Security > Virus & threat protection > Protection history.",
                        MessageBoxImage.Information
                    );

                    return;
                }

                App.Logger.WriteLine(logId, $"Add-MpPreference failed (exit code {process.ExitCode}): {errors}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logId, $"Could not run PowerShell: {ex.Message}");
            }

            if (errors.Contains("is not recognized", StringComparison.OrdinalIgnoreCase))
            {
                Frontend.ShowMessageBox(
                    "Microsoft Defender doesn't seem to be available on this system (its PowerShell module was not found).\n\n" +
                    "If you use another antivirus, add these exclusions through its own settings:\n" +
                    String.Join("\n", targets),
                    MessageBoxImage.Warning
                );

                return;
            }

            // Defender requires admin rights to change exclusions, so ask through UAC
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            startInfo.CreateNoWindow = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                Process.Start(startInfo);

                App.Logger.WriteLine(logId, "Launched elevated PowerShell to add exclusions.");

                Frontend.ShowMessageBox(
                    "Adding exclusions requires administrator permission, so a User Account Control prompt has been shown.\n\n" +
                    "Accept it and the following will be excluded from Microsoft Defender scans:\n\n" + String.Join("\n", targets),
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logId, $"Elevated attempt failed: {ex.Message}");

                Frontend.ShowMessageBox(
                    "Could not add the Microsoft Defender exclusions.\n\n" +
                    "You can add them manually under Windows Security > Virus & threat protection > Manage settings > Exclusions:\n" +
                    String.Join("\n", targets),
                    MessageBoxImage.Error
                );
            }
        }
    }
}
