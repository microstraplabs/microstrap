using System.Windows.Controls;

using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for IntegrationsPage.xaml
    /// </summary>
    public partial class IntegrationsPage
    {
        public IntegrationsPage()
        {
            InitializeComponent();

            var viewModel = new IntegrationsViewModel();
            if (viewModel.CustomIntegrations.Count == 0)
            {
                viewModel.CustomIntegrations.Add(new CustomIntegration
                {
                    Name = "Microstrap Overlay",
                    Location = OverlayPaths.Launcher,
                    Enabled = false,
                    AutoClose = true
                });
                viewModel.SelectedCustomIntegration = viewModel.CustomIntegrations[0];
                viewModel.SelectedCustomIntegrationIndex = 0;
            }

            DataContext = viewModel;
            CustomIntegrationsListBox.ItemsSource = viewModel.CustomIntegrations;
            CustomIntegrationsListBox.SelectedIndex = viewModel.SelectedCustomIntegrationIndex;
        }

        public void CustomIntegrationSelection(object sender, SelectionChangedEventArgs e)
        {
            IntegrationsViewModel viewModel = (IntegrationsViewModel)DataContext;
            viewModel.SelectedCustomIntegration = (CustomIntegration)((ListBox)sender).SelectedItem;
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomIntegration));
        }
    }
}
