using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Bloxstrap.UI.Elements.Base;
using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.Elements.Editor;

namespace Bloxstrap.UI.Elements.Dashboard
{
    public partial class DashboardWindow : WpfUiWindow
    {
        private readonly ObservableCollection<string> _themes = new();

        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = _themes;
            ReloadThemes();
        }

        private void ReloadThemes()
        {
            _themes.Clear();
            Directory.CreateDirectory(Paths.CustomThemes);
            foreach (var directory in Directory.GetDirectories(Paths.CustomThemes).OrderBy(x => x))
            {
                if (File.Exists(Path.Combine(directory, "Theme.xml")))
                    _themes.Add(Path.GetFileName(directory));
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool selected = ThemesList.SelectedItem is string;
            EditButton.IsEnabled = selected;
            DeleteButton.IsEnabled = selected;
        }

        private void ThemesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateButtons();

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();
            if (dialog.Created)
            {
                ReloadThemes();
                if (dialog.OpenEditor)
                    new BootstrapperEditorWindow(dialog.ThemeName).ShowDialog();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ThemesList.SelectedItem is string name)
                new BootstrapperEditorWindow(name).ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ThemesList.SelectedItem is not string name)
                return;

            if (Frontend.ShowMessageBox($"Delete the saved creation '{name}'?", MessageBoxImage.Warning, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            Directory.Delete(Path.Combine(Paths.CustomThemes, name), true);
            if (App.Settings.Prop.SelectedCustomTheme == name)
            {
                App.Settings.Prop.SelectedCustomTheme = null;
                App.Settings.Save();
            }
            ReloadThemes();
        }
    }
}
