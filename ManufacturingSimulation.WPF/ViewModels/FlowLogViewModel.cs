using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class FlowLogViewModel : INotifyPropertyChanged
    {
        private readonly int _runId;
        private readonly MesDbContext _db;
        private string _selectedEventType;
        private string _selectedMachine;

        public ObservableCollection<FlowEvent> FlowEvents { get; set; }
        public ObservableCollection<string> EventTypes { get; set; }
        public ObservableCollection<string> Machines { get; set; }

        public string RunInfo { get; }
        public int TotalEvents => FlowEvents.Count;
        public int PartsCompleted => FlowEvents.Count(e => e.EventType == "Completed");
        public int MaxQueueSize => FlowEvents.Any() ? FlowEvents.Max(e => e.QueueSize) : 0;

        public string SelectedEventType
        {
            get => _selectedEventType;
            set
            {
                _selectedEventType = value;
                OnPropertyChanged(nameof(SelectedEventType));
                FilterEvents();
            }
        }

        public string SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                _selectedMachine = value;
                OnPropertyChanged(nameof(SelectedMachine));
                FilterEvents();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }

        private List<FlowEvent> _allEvents;

        public FlowLogViewModel(int runId)
        {
            try
            {
                _runId = runId;
                _db = new MesDbContext();

                FlowEvents = new ObservableCollection<FlowEvent>();
                EventTypes = new ObservableCollection<string> { "All", "Arrival", "Queue Entry", "Start Processing", "End Processing", "Completed" };
                Machines = new ObservableCollection<string> { "All" };
                _allEvents = new List<FlowEvent>();

                var run = _db.SimulationRuns.Find(runId);
                
                if (run == null)
                {
                    RunInfo = $"Run #{runId} | Not Found";
                    return;
                }
                
                RunInfo = $"Run #{runId} | {run.RunDate:yyyy-MM-dd HH:mm} | {run.Scenario?.ScenarioName ?? "Unknown"}";

                SelectedEventType = "All";
                SelectedMachine = "All";

                RefreshCommand = new RelayCommand(() => LoadEvents());
                ExportCommand = new RelayCommand(() => ExportToCsv());

                LoadEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FlowEvents = new ObservableCollection<FlowEvent>();
                _allEvents = new List<FlowEvent>();
                RunInfo = "Error";
            }
        }

        private void LoadEvents()
        {
            _allEvents = new List<FlowEvent>();

            // Load REAL events from database
            var dbEvents = _db.SimulationEvents
                .Where(e => e.RunId == _runId)
                .OrderBy(e => e.EventTime)
                .ToList();

            if (!dbEvents.Any())
            {
                MessageBox.Show($"No events logged for run {_runId}.\n\nThe simulation may not have event logging enabled yet.\nShowing mock data for demonstration.", 
                    "No Real Events", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Fall back to mock data
                LoadMockEvents();
                return;
            }

            // Convert to FlowEvent objects
            foreach (var evt in dbEvents)
            {
                _allEvents.Add(new FlowEvent
                {
                    EventId = evt.EventId,
                    Time = evt.EventTime / 60.0, // Convert minutes to hours
                    EventType = evt.EventType,
                    PartId = evt.PartId ?? "",
                    OrderNumber = evt.OrderNumber ?? "",
                    MachineName = evt.MachineName ?? "System",
                    QueueSize = evt.QueueSize,
                    Details = evt.Details ?? ""
                });

                // Add unique machines
                if (!string.IsNullOrEmpty(evt.MachineName) && !Machines.Contains(evt.MachineName))
                {
                    Machines.Add(evt.MachineName);
                }
            }

            FilterEvents();
        }

        private void LoadMockEvents()
        {
            // Fallback mock data if no real events exist
            var predictions = _db.SimulationOrderPredictions
                .Include(p => p.ProductionOrder)
                .Where(p => p.RunId == _runId)
                .ToList();

            if (!predictions.Any())
            {
                FilterEvents();
                return;
            }

            double currentTime = 0;
            int eventId = 0;

            foreach (var pred in predictions.Take(5))
            {
                var order = pred.ProductionOrder;
                if (order == null) continue;

                var partId = $"{order.OrderNumber}-1";

                _allEvents.Add(new FlowEvent
                {
                    EventId = eventId++,
                    Time = currentTime,
                    EventType = "Arrival",
                    PartId = partId,
                    OrderNumber = order.OrderNumber,
                    MachineName = "System",
                    QueueSize = 0,
                    Details = $"Part arrived for order {order.OrderNumber} (MOCK)"
                });

                currentTime += (pred.PredictedFlowTimeHours ?? 1.0);

                if (pred.Completed)
                {
                    _allEvents.Add(new FlowEvent
                    {
                        EventId = eventId++,
                        Time = currentTime,
                        EventType = "Completed",
                        PartId = partId,
                        OrderNumber = order.OrderNumber,
                        MachineName = "System",
                        QueueSize = 0,
                        Details = $"Part completed (MOCK)"
                    });
                }

                currentTime += 1.0;
            }

            FilterEvents();
        }

        private void FilterEvents()
        {
            FlowEvents.Clear();

            if (_allEvents == null || !_allEvents.Any())
            {
                OnPropertyChanged(nameof(TotalEvents));
                OnPropertyChanged(nameof(PartsCompleted));
                OnPropertyChanged(nameof(MaxQueueSize));
                return;
            }

            var filtered = _allEvents.AsEnumerable();

            if (SelectedEventType != "All")
            {
                filtered = filtered.Where(e => e.EventType == SelectedEventType);
            }

            if (SelectedMachine != "All")
            {
                filtered = filtered.Where(e => e.MachineName != null && e.MachineName == SelectedMachine);
            }

            foreach (var evt in filtered.OrderBy(e => e.Time))
            {
                FlowEvents.Add(evt);
            }

            OnPropertyChanged(nameof(TotalEvents));
            OnPropertyChanged(nameof(PartsCompleted));
            OnPropertyChanged(nameof(MaxQueueSize));
        }

        private void ExportToCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"flow_log_run_{_runId}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Time,Event Type,Part ID,Order Number,Machine,Queue Size,Details");
                        
                        foreach (var evt in FlowEvents)
                        {
                            writer.WriteLine($"{evt.Time:F2},{evt.EventType},{evt.PartId},{evt.OrderNumber},{evt.MachineName},{evt.QueueSize},\"{evt.Details}\"");
                        }
                    }

                    MessageBox.Show($"Exported {FlowEvents.Count} events to {dialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FlowEvent
    {
        public int EventId { get; set; }
        public double Time { get; set; }
        public string EventType { get; set; }
        public string PartId { get; set; }
        public string OrderNumber { get; set; }
        public string MachineName { get; set; }
        public int QueueSize { get; set; }
        public string Details { get; set; }
    }
}
