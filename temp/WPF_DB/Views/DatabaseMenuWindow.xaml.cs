using System.Windows;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class DatabaseMenuWindow : Window
    {
        public DatabaseMenuWindow()
        {
            InitializeComponent();
        }

        private void ManageOrders_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderManagementWindow();
            window.ShowDialog();
        }

        private void RunSimulation_Click(object sender, RoutedEventArgs e)
        {
            var window = new SimulationRunnerWindow();
            window.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
