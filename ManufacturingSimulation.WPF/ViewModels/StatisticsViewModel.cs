using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private int _totalPartsArrived;
        private int _totalPartsCompleted;
        private int _currentWIP;
        private double _throughput;
        private double _averageFlowTime;

        public int TotalPartsArrived
        {
            get => _totalPartsArrived;
            set => SetProperty(ref _totalPartsArrived, value);
        }

        public int TotalPartsCompleted
        {
            get => _totalPartsCompleted;
            set => SetProperty(ref _totalPartsCompleted, value);
        }

        public int CurrentWIP
        {
            get => _currentWIP;
            set => SetProperty(ref _currentWIP, value);
        }

        public double Throughput
        {
            get => _throughput;
            set => SetProperty(ref _throughput, value);
        }

        public double AverageFlowTime
        {
            get => _averageFlowTime;
            set => SetProperty(ref _averageFlowTime, value);
        }

        public void UpdateFromModel(SimulationStatistics stats)
        {
            TotalPartsArrived = stats.TotalPartsArrived;
            TotalPartsCompleted = stats.TotalPartsCompleted;
            CurrentWIP = stats.CurrentWIP;
            Throughput = stats.Throughput;
            AverageFlowTime = stats.AverageFlowTime;
        }
    }
}