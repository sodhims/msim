using System.Windows;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    public partial class FlowLogViewer : Window
    {
        public FlowLogViewer(int runId)
        {
            try
            {
                InitializeComponent(); // MUST be first!
                DataContext = new FlowLogViewModel(runId); // Then set DataContext
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Window init error: {ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}