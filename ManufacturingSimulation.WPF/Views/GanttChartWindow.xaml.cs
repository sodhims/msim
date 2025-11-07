using ManufacturingSimulation.Database;
using ManufacturingSimulation.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using ManufacturingSimulation.Database;

namespace ManufacturingSimulation.WPF.Views
{
    /// <summary>
    /// Interaction logic for GanttChartWindow.xaml
    /// </summary>
    public partial class GanttChartWindow : Window
    {
        private  GanttChartViewModel _viewModel;

        public GanttChartWindow(int runId)
        {
            InitializeComponent();

            // Initialize ViewModel with run ID
            _viewModel = new GanttChartViewModel(runId, GanttCanvas);
            DataContext = _viewModel;

            LoadRunList(runId);


            // Wire up display elements
            _viewModel.SimulationDateChanged += (date) => txtSimulationDate.Text = date;
            _viewModel.RunInfoChanged += (info) => txtRunInfo.Text = info;              // ADD THIS
            _viewModel.PartsSimulatedChanged += (parts) => txtPartsSimulated.Text = parts;
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

        private void LoadRunList(int currentRunId)
        {
            using (var db = new MesDbContext())
            {
                var runs = db.SimulationRuns
                    .Where(r => r.StudentId == 1)
                    .OrderByDescending(r => r.RunDate)
                    .Take(20)
                    .Select(r => new {
                        RunId = r.RunId,
                        Display = $"Run {r.RunId} - {r.RunDate:MM/dd/yyyy HH:mm}"
                    })
                    .ToList();

                cmbRunSelection.ItemsSource = runs;
                cmbRunSelection.DisplayMemberPath = "Display";
                cmbRunSelection.SelectedValuePath = "RunId";
                cmbRunSelection.SelectedValue = currentRunId;
            }
        }

        private void CmbRunSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRunSelection.SelectedValue is int runId && runId > 0)
            {
                // Create new ViewModel
                _viewModel = new GanttChartViewModel(runId, GanttCanvas);

                // Reconnect all event subscriptions
                _viewModel.RunInfoChanged += (info) => txtRunInfo.Text = info;
                _viewModel.PartsSimulatedChanged += (parts) => txtPartsSimulated.Text = parts;
                _viewModel.SimulationDateChanged += (date) => txtSimulationDate.Text = date;
                _viewModel.MakespanChanged += (makespan) => txtMakespan.Text = makespan;
                _viewModel.TaskCountChanged += (count) => TaskCountText.Text = $"Tasks: {count}";
                _viewModel.MachineCountChanged += (count) => MachineCountText.Text = $"Machines: {count}";
                _viewModel.HoverInfoChanged += (info) => HoverInfoText.Text = info;

                // Load data
                _viewModel.LoadGanttData();
            }
        }
    }
}
