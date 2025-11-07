using System.Windows;
using ManufacturingSimulation.WPF.ViewModels;

namespace ManufacturingSimulation.WPF.Views
{
    /// <summary>
    /// Interaction logic for GanttChartWindow.xaml
    /// </summary>
    public partial class GanttChartWindow : Window
    {
        private readonly GanttChartViewModel _viewModel;

        public GanttChartWindow(int runId)
        {
            InitializeComponent();

            // Initialize ViewModel with run ID
            _viewModel = new GanttChartViewModel(runId, GanttCanvas);
            DataContext = _viewModel;

            // Wire up display elements
            _viewModel.SimulationDateChanged += (date) => txtSimulationDate.Text = date;
            _viewModel.MakespanChanged += (makespan) => txtMakespan.Text = makespan;
            _viewModel.TaskCountChanged += (count) => TaskCountText.Text = $"Tasks: {count}";
            _viewModel.MachineCountChanged += (count) => MachineCountText.Text = $"Machines: {count}";
            _viewModel.HoverInfoChanged += (info) => HoverInfoText.Text = info;

            // Load the Gantt chart data when window loads
            Loaded += GanttChartWindow_Loaded;
        }

        private void GanttChartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadGanttData();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Error loading Gantt chart data:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
