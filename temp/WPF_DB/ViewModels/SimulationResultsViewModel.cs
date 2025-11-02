using System.Collections.ObjectModel;
using System.ComponentModel;
using ManufacturingSimulation.Bridge;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class SimulationResultsViewModel : INotifyPropertyChanged
    {
        public SimulationRunSummary Summary { get; }

        public ObservableCollection<SimulationWorkCenterResult> WorkCenterResults { get; set; }
        public ObservableCollection<SimulationOrderPrediction> OrderPredictions { get; set; }

        public string ScenarioName => Summary.ScenarioName;
        public string RunDate => Summary.RunDate.ToString("yyyy-MM-dd HH:mm");
        public string Throughput => $"{Summary.Result?.Throughput:F2} parts/hour";
        public string AvgFlowTime => $"{Summary.Result?.AvgFlowTimeHours:F1} hours";
        public string TotalCompleted => $"{Summary.Result?.TotalPartsCompleted} parts";
        public string Bottleneck => Summary.BottleneckWorkCenter?.WorkCenterName ?? "N/A";

        public SimulationResultsViewModel(SimulationRunSummary summary)
        {
            Summary = summary;
            WorkCenterResults = new ObservableCollection<SimulationWorkCenterResult>(summary.WorkCenterResults);
            OrderPredictions = new ObservableCollection<SimulationOrderPrediction>(summary.OrderPredictions);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
