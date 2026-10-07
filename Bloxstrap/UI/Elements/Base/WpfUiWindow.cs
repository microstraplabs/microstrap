using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();
        private const int SeasonalThemeIndex = 4;
        private static readonly DateTimeOffset HalloweenThemeEndsAt = new(2026, 11, 1, 14, 0, 0, TimeSpan.FromHours(2));
        private static readonly DateTimeOffset WinterUpdateStartsAt = new(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(2));
        private static readonly Uri HalloweenThemeUri = new("pack://application:,,,/UI/Style/Halloween.xaml");

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            bool halloweenActive = DateTimeOffset.UtcNow < HalloweenThemeEndsAt;
            var theme = App.Settings.Prop.Theme.GetFinal();

            _themeService.SetTheme(theme == Enums.Theme.Dark ? ThemeType.Dark : ThemeType.Light);
            // Index 2 follows the existing dark/light theme. Keep the seasonal dictionary last so it
            // takes precedence over both the system theme and the app's regular custom resources.
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(theme)}.xaml") };
            mergedDictionaries[customThemeIndex] = dict;

            var seasonalDictionary = mergedDictionaries[SeasonalThemeIndex];
            if (halloweenActive && seasonalDictionary.Source != HalloweenThemeUri)
            {
                seasonalDictionary.Clear();
                seasonalDictionary.Source = HalloweenThemeUri;
            }
            else if (!halloweenActive && seasonalDictionary.Source != null)
            {
                seasonalDictionary.Clear();
                seasonalDictionary.Source = null;
            }

            if (halloweenActive)
            {
                // Override the system accent after loading the season palette so all WPF controls stay orange.
                Wpf.Ui.Appearance.Accent.Apply(Color.FromRgb(255, 112, 46), Color.FromRgb(255, 138, 61), Color.FromRgb(255, 170, 85), Color.FromRgb(255, 192, 120));
            }
            else
            {
                _themeService.SetSystemAccent();
            }

            bool winterPromoActive = DateTimeOffset.UtcNow < WinterUpdateStartsAt;
            Application.Current.Resources["MicrostrapHalloweenActive"] = halloweenActive;
            Application.Current.Resources["MicrostrapHalloweenVisibility"] = halloweenActive ? Visibility.Visible : Visibility.Collapsed;
            Application.Current.Resources["MicrostrapWinterVisibility"] = winterPromoActive ? Visibility.Visible : Visibility.Collapsed;
            var iconUri = new Uri(halloweenActive
                ? "pack://application:,,,/Resources/Seasonal/MicrostrapHalloween.png"
                : "pack://application:,,,/Microstrap.ico");
            Application.Current.Resources["MicrostrapSeasonalIcon"] = new BitmapImage(iconUri);

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
            this.BorderThickness = new Thickness(4);
#endif
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            base.OnSourceInitialized(e);
        }
    }
}
