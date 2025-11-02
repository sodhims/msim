using System.Windows;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class SimulationResultsDialog : Window
    {
        private SimulationResultsViewModel ViewModel => DataContext as SimulationResultsViewModel;

        public SimulationResultsDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewFlowLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Button clicked!"); // DEBUG 1

                if (ViewModel?.Summary == null)
                {
                    MessageBox.Show("ViewModel or Summary is null!");
                    return;
                }

                MessageBox.Show($"Creating window for run {ViewModel.Summary.RunId}"); // DEBUG 2

                var flowLogWindow = new FlowLogViewer(ViewModel.Summary.RunId);

                MessageBox.Show("Window created, about to show"); // DEBUG 3

                flowLogWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
