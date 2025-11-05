using ManufacturingSimulation.Database;
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
            var adminWindow = new OrderManagementWindow(new MesDbContext());
            adminWindow.ShowDialog();
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
        private void FactoryLayout_Click(object sender, RoutedEventArgs e)
        {
            var layoutWindow = new Window
            {
                Title = "Factory Layout Editor",
                Content = new FactoryLayoutView(),
                Width = 1200,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            layoutWindow.ShowDialog();
        }
    }
}
