using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.Bridge;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class SimulationRunnerViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _db;
        private readonly SimulationService _simService;
        private bool _isRunning;
        private string _statusMessage;

        public ObservableCollection<ProductionOrder> AvailableOrders { get; set; }
        public ObservableCollection<ProductionOrder> SelectedOrders { get; set; }
        public ObservableCollection<SimulationRunSummary> PastRuns { get; set; }

        public double SimulationDuration { get; set; }
        public int RandomSeed { get; set; }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(CanRun));
            }
        }

        public bool CanRun => !IsRunning && SelectedOrders.Any();

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ICommand AddOrderCommand { get; }
        public ICommand RemoveOrderCommand { get; }
        public ICommand RunSimulationCommand { get; }
        public ICommand ViewResultsCommand { get; }

        public SimulationRunnerViewModel()
        {
            _db = new MesDbContext();
            _simService = new SimulationService(_db);

            AvailableOrders = new ObservableCollection<ProductionOrder>();
            SelectedOrders = new ObservableCollection<ProductionOrder>();
            PastRuns = new ObservableCollection<SimulationRunSummary>();

            SimulationDuration = 168; // 1 week
            RandomSeed = 42;
            StatusMessage = "Ready";

            AddOrderCommand = new RelayCommand<ProductionOrder>(AddOrder);
            RemoveOrderCommand = new RelayCommand<ProductionOrder>(RemoveOrder);
            RunSimulationCommand = new RelayCommand(RunSimulation, () => CanRun);
            ViewResultsCommand = new RelayCommand<SimulationRunSummary>(ViewResults);

            LoadOrders();
            LoadPastRuns();
        }

        private void LoadOrders()
        {
            AvailableOrders.Clear();
            var orders = _db.ProductionOrders
                .Where(o => o.StudentId == 1 && 
                           (o.Status == "Planned" || o.Status == "Released"))
                .OrderBy(o => o.Priority)
                .ThenBy(o => o.DueDate)
                .ToList();

            foreach (var order in orders)
            {
                AvailableOrders.Add(order);
            }
        }

        private void LoadPastRuns()
        {
            PastRuns.Clear();
            var runs = _db.SimulationRuns
                .Where(r => r.StudentId == 1 && r.Status == "Completed")
                .OrderByDescending(r => r.RunDate)
                .Take(10)
                .ToList();

            foreach (var run in runs)
            {
                var summary = _simService.GetRunSummary(run.RunId);
                if (summary != null)
                {
                    PastRuns.Add(summary);
                }
            }
        }

        private void AddOrder(ProductionOrder order)
        {
            if (order != null && !SelectedOrders.Contains(order))
            {
                SelectedOrders.Add(order);
                OnPropertyChanged(nameof(CanRun));
            }
        }

        private void RemoveOrder(ProductionOrder order)
        {
            if (order != null)
            {
                SelectedOrders.Remove(order);
                OnPropertyChanged(nameof(CanRun));
            }
        }

        private async void RunSimulation()
        {
            IsRunning = true;
            StatusMessage = "Running simulation...";

            try
            {
                var orderIds = SelectedOrders.Select(o => o.OrderId).ToList();

                var result = await System.Threading.Tasks.Task.Run(() =>
                    _simService.QuickRun(
                        studentId: 1,
                        orderIds: orderIds,
                        durationHours: SimulationDuration,
                        randomSeed: RandomSeed
                    )
                );

                if (result.Success)
                {
                    StatusMessage = $"✓ Simulation completed! Throughput: {result.Statistics.Throughput:F2} parts/hour";
                    LoadPastRuns();

                    // Show results
                    var summary = _simService.GetRunSummary(result.RunId);
                    ShowResults(summary);
                }
                else
                {
                    StatusMessage = $"✗ Error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ViewResults(SimulationRunSummary summary)
        {
            if (summary != null)
            {
                ShowResults(summary);
            }
        }

        private void ShowResults(SimulationRunSummary summary)
        {
            var dialog = new SimulationResultsDialog
            {
                DataContext = new SimulationResultsViewModel(summary)
            };
            dialog.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Generic RelayCommand
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute((T)parameter);
        public event EventHandler CanExecuteChanged;
    }
}
