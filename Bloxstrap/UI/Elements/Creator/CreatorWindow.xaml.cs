using System.Windows;
using Bloxstrap.UI.Elements.Base;
using Bloxstrap.UI.Elements.Dashboard;
using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.Elements.Editor;

namespace Bloxstrap.UI.Elements.Creator
{
    public partial class CreatorWindow : WpfUiWindow
    {
        public CreatorWindow()
        {
            InitializeComponent();
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCustomThemeDialog();
            dialog.ShowDialog();
            if (dialog.Created && dialog.OpenEditor)
                new BootstrapperEditorWindow(dialog.ThemeName).ShowDialog();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            new DashboardWindow().ShowDialog();
        }
    }
}
