using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using ManufacturingSimulation.WPF.ViewModels;
using ManufacturingSimulation.Bridge;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class SimulationRunnerWindow : Window
    {
        public SimulationRunnerWindow()
        {
            InitializeComponent();
            DataContext = new SimulationRunnerViewModel();
        }

        private void PastRunsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as SimulationRunnerViewModel;
            if (vm != null && PastRunsGrid.SelectedItem is SimulationRunSummary summary)
            {
                vm.ViewResultsCommand.Execute(summary);
            }
        }

        private DataGrid PastRunsGrid => (DataGrid)((GroupBox)((Grid)Content).Children[3]).Content;
    }
}
