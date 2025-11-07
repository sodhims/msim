using ManufacturingSimulation.Bridge;
using ManufacturingSimulation.Core.Configuration;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class SimulationRunnerViewModel : INotifyPropertyChanged
    {
        private readonly MesDbContext _db;
        private readonly SimulationService _simService;
        private bool _isRunning;
        private string _statusMessage;
        private string _selectedDispatchRule;

        public ObservableCollection<ProductionOrder> AvailableOrders { get; set; }
        public ObservableCollection<ProductionOrder> SelectedOrders { get; set; }
        public ObservableCollection<SimulationRunSummary> PastRuns { get; set; }
        public ObservableCollection<string> DispatchRules { get; set; }
        public ObservableCollection<WorkCenterConfig> MachineConfigurations { get; set; }

        public double SimulationDuration { get; set; }
        public int RandomSeed { get; set; }

        public string SelectedDispatchRule
        {
            get => _selectedDispatchRule;
            set
            {
                _selectedDispatchRule = value;
                OnPropertyChanged(nameof(SelectedDispatchRule));
            }
        }

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

        public ICommand AddSelectedOrdersCommand { get; }
        public ICommand AddAllOrdersCommand { get; }
        public ICommand RemoveSelectedOrdersCommand { get; }
        public ICommand ClearOrdersCommand { get; }
        public ICommand RunSimulationCommand { get; }
        public ICommand ViewResultsCommand { get; }
        public ICommand SaveMachineConfigCommand { get; }

        public Func<List<ProductionOrder>> GetSelectedAvailableOrders { get; set; }
        public Func<List<ProductionOrder>> GetSelectedSimulationOrders { get; set; }

        public SimulationRunnerViewModel()
        {
            _db = new MesDbContext();
            _simService = new SimulationService(_db);

            AvailableOrders = new ObservableCollection<ProductionOrder>();
            SelectedOrders = new ObservableCollection<ProductionOrder>();
            PastRuns = new ObservableCollection<SimulationRunSummary>();
            MachineConfigurations = new ObservableCollection<WorkCenterConfig>();
            DispatchRules = new ObservableCollection<string>
            {
                "FIFO", "LIFO", "SPT", "LPT", "EDD", "SLACK", "Priority"
            };

            SimulationDuration = 168;
            RandomSeed = 42;
            SelectedDispatchRule = "FIFO";
            StatusMessage = "Ready - Configure machines and select orders";

            AddSelectedOrdersCommand = new RelayCommand(() => AddSelectedOrders());
            AddAllOrdersCommand = new RelayCommand(() => AddAllOrders());
            RemoveSelectedOrdersCommand = new RelayCommand(() => RemoveSelectedOrders());
            ClearOrdersCommand = new RelayCommand(() => ClearOrders());
            RunSimulationCommand = new RelayCommand(() => RunSimulation());
            ViewResultsCommand = new RelayCommand<SimulationRunSummary>(ViewResults);
            SaveMachineConfigCommand = new RelayCommand(() => SaveMachineConfiguration());

            LoadOrders();
            LoadPastRuns();
            LoadMachineConfigurations();
        }

        private void LoadMachineConfigurations()
        {
            MachineConfigurations.Clear();
            var workCenters = _db.WorkCenters
                .Where(w => w.StudentId == 1 && (w.IsActive == null || w.IsActive == true))
                .OrderBy(w => w.WorkCenterId)
                .ToList();

            foreach (var wc in workCenters)
            {
                MachineConfigurations.Add(new WorkCenterConfig
                {
                    WorkCenterId = wc.WorkCenterId,
                    MachineName = wc.WorkCenterName,
                    MachineType = wc.WorkCenterType,
                    Quantity = wc.Quantity ?? 1,
                    BufferCapacity = wc.BufferCapacity ?? 10,
                    OriginalQuantity = wc.Quantity ?? 1,
                    OriginalBuffer = wc.BufferCapacity ?? 10
                });
            }
        }

        private void SaveMachineConfiguration()
        {
            try
            {
                foreach (var config in MachineConfigurations)
                {
                    var wc = _db.WorkCenters.Find(config.WorkCenterId);
                    if (wc != null)
                    {
                        wc.Quantity = config.Quantity;
                        wc.BufferCapacity = config.BufferCapacity;
                        wc.UpdatedDate = DateTime.Now;
                    }
                }

                _db.SaveChanges();
                StatusMessage = "✓ Machine configuration saved";
                MessageBox.Show("Configuration saved successfully!",
                    "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadMachineConfigurations();
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            AvailableOrders.Clear();
            var orders = _db.ProductionOrders
                .Include(o => o.Product)
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

        private void AddSelectedOrders()
        {
            var selected = GetSelectedAvailableOrders?.Invoke();
            if (selected == null || !selected.Any())
            {
                MessageBox.Show("Select orders (Ctrl+Click)",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var order in selected.ToList())
            {
                if (!SelectedOrders.Contains(order))
                {
                    SelectedOrders.Add(order);
                    AvailableOrders.Remove(order);
                }
            }
            OnPropertyChanged(nameof(CanRun));
            StatusMessage = $"Added {selected.Count} orders - Total: {SelectedOrders.Count}";
        }

        private void AddAllOrders()
        {
            foreach (var order in AvailableOrders.ToList())
            {
                if (!SelectedOrders.Contains(order))
                {
                    SelectedOrders.Add(order);
                }
            }
            AvailableOrders.Clear();
        }

        private void RemoveSelectedOrders()
        {
            var selected = GetSelectedSimulationOrders?.Invoke();
            if (selected == null || !selected.Any()) return;

            foreach (var order in selected.ToList())
            {
                SelectedOrders.Remove(order);
                if (!AvailableOrders.Contains(order))
                {
                    AvailableOrders.Add(order);
                }
            }
            OnPropertyChanged(nameof(CanRun));
            StatusMessage = $"Removed {selected.Count} orders";
        }

        private void ClearOrders()
        {
            foreach (var order in SelectedOrders.ToList())
            {
                if (!AvailableOrders.Contains(order))
                {
                    AvailableOrders.Add(order);
                }
            }
            SelectedOrders.Clear();
            OnPropertyChanged(nameof(CanRun));
            StatusMessage = "Orders cleared";
        }

        private async void RunSimulation()
        {
            if (!CanRun) return;

            IsRunning = true;

            var machineConfig = string.Join(", ",
               MachineConfigurations.Select(m => $"{m.MachineName}:{m.Quantity}"));

            StatusMessage = $"Running: {SelectedDispatchRule} | {machineConfig}";

            try
            {
                var orderIds = SelectedOrders.Select(o => o.OrderId).ToList();
                var result = await Task.Run(() =>
                    _simService.QuickRun(
                        studentId: 1,
                        orderIds: orderIds,
                        durationHours: SimulationDuration,
                        randomSeed: RandomSeed,
                        dispatchRule: SelectedDispatchRule,
                        machineConfigs: MachineConfigurations.Select(wc => new MachineConfiguration
                        {
                            Name = wc.MachineName,
                            BufferCapacity = wc.BufferCapacity,
                            DispatchingRule = SelectedDispatchRule,
                            Quantity = wc.Quantity
                        }).ToList()
                    )
                );

                if (result.Success)
                {
                    StatusMessage = $"✓ Completed! Throughput: {result.Statistics?.Throughput:F2} parts/hr";
                    LoadPastRuns();

                    var summary = _simService.GetRunSummary(result.RunId);
                    if (summary != null)
                    {
                        ShowResults(summary);
                    }
                }
                else
                {
                    StatusMessage = $"✗ Error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            var message = $"Simulation Complete!\n\n" +
                          $"Run ID: {summary.RunId}\n" +
                          $"Throughput: {summary.Result?.Throughput:F2} parts/hr\n" +
                          $"Avg Flow Time: {summary.Result?.AvgFlowTimeHours:F2} hrs\n" +
                          $"Avg WIP: {summary.Result?.AvgWip:F1} parts\n" +
                          $"Utilization: {summary.Result?.OverallUtilizationPercent:F1}%\n\n" +
                          $"View Gantt Chart?";

            var result = MessageBox.Show(message, "Simulation Complete",
                MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                var ganttWindow = new GanttChartWindow(summary.RunId);
                ganttWindow.Show();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class WorkCenterConfig : INotifyPropertyChanged
    {
        private int _quantity;
        private int _bufferCapacity;

        public int WorkCenterId { get; set; }
        public string MachineName { get; set; }
        public string MachineType { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(HasChanged));
            }
        }

        public int BufferCapacity
        {
            get => _bufferCapacity;
            set
            {
                _bufferCapacity = value;
                OnPropertyChanged(nameof(BufferCapacity));
                OnPropertyChanged(nameof(HasChanged));
            }
        }

        public int OriginalQuantity { get; set; }
        public int OriginalBuffer { get; set; }

        public bool HasChanged => Quantity != OriginalQuantity || BufferCapacity != OriginalBuffer;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}