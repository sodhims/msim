using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class OrderManagementWindow : Window
    {
        public OrderManagementWindow(MesDbContext context)
        {
            InitializeComponent();

            var viewModel = new OrderManagementViewModel(context);
            DataContext = viewModel;

            // Wire up selection change event
            ordersDataGrid.SelectionChanged += (s, e) =>
            {
                var selectedOrders = ordersDataGrid.SelectedItems.Cast<ProductionOrder>().ToList();
                viewModel.SetSelectedOrders(selectedOrders);

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Selected {selectedOrders.Count} orders");
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}