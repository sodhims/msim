using System.Windows;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class OrderManagementWindow : Window
    {
        public OrderManagementWindow()
        {
            InitializeComponent();
            DataContext = new OrderManagementViewModel();
        }
    }
}
