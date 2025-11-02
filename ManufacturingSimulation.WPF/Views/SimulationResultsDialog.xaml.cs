using System.Windows;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class SimulationResultsDialog : Window
    {
        public SimulationResultsDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
