using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ManufacturingSimulation.WPF.ViewModels;
using ManufacturingSimulation.Bridge;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class SimulationRunnerWindow : Window
    {
        private SimulationRunnerViewModel ViewModel => DataContext as SimulationRunnerViewModel;

        public SimulationRunnerWindow()
        {
            InitializeComponent();
            
            var vm = new SimulationRunnerViewModel();
            
            // Wire up multi-select support
            vm.GetSelectedAvailableOrders = () => 
                AvailableOrdersList.SelectedItems.Cast<ProductionOrder>().ToList();
            
            vm.GetSelectedSimulationOrders = () => 
                SelectedOrdersList.SelectedItems.Cast<ProductionOrder>().ToList();
            
            DataContext = vm;
        }

        private void PastRunsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel != null && PastRunsGrid.SelectedItem is SimulationRunSummary summary)
            {
                ViewModel.ViewResultsCommand.Execute(summary);
            }
        }
    }
}
