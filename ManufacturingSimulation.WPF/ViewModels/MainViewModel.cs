using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Configuration;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Events;
using ManufacturingSimulation.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ManufacturingSimulation.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;
        private SimulationEngine _engine;
        private SimulationConfiguration _config;
        private readonly StatisticsViewModel _statistics;

        private bool _isRunning;
        private bool _isPaused;
        private double _currentTime;
        private int _simulationSpeed = 50;
        private string _configJson;

        public MainViewModel()
        {
            _statistics = new StatisticsViewModel();

            Machines = new ObservableCollection<MachineViewModel>();
            EventLog = new ObservableCollection<string>();

            // Commands
            StartCommand = new RelayCommand(_ => Start(), _ => !IsRunning);
            PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
            ResetCommand = new RelayCommand(_ => Reset());
            LoadConfigCommand = new RelayCommand(_ => LoadConfiguration());
            SaveConfigCommand = new RelayCommand(_ => SaveConfiguration());
            ClearLogCommand = new RelayCommand(_ => EventLog.Clear());

            // Timer for UI updates
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += Timer_Tick;

            // Load default configuration
            LoadDefaultConfiguration();
        }

        #region Properties

        public ObservableCollection<MachineViewModel> Machines { get; }
        public ObservableCollection<string> EventLog { get; }
        public StatisticsViewModel Statistics => _statistics;

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public double CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string ConfigJson
        {
            get => _configJson;
            set => SetProperty(ref _configJson, value);
        }

        #endregion

        #region Commands

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand LoadConfigCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand ClearLogCommand { get; }

        #endregion

        #region Configuration

        private void LoadDefaultConfiguration()
        {
            try
            {
                // Create basic configuration
                _config = new SimulationConfiguration
                {
                    RunLength = 1000.0,
                    RandomSeed = 42
                };

                // Add 4 machines with default settings
                _config.Machines.Clear();
                _config.Machines.Add(new MachineConfiguration
                {
                    Id = 1,
                    Name = "Drill Press",
                    BufferCapacity = 10,
                    DispatchingRule = "FIFO"
                });
                _config.Machines.Add(new MachineConfiguration
                {
                    Id = 2,
                    Name = "Lathe",
                    BufferCapacity = 10,
                    DispatchingRule = "FIFO"
                });
                _config.Machines.Add(new MachineConfiguration
                {
                    Id = 3,
                    Name = "Mill",
                    BufferCapacity = 10,
                    DispatchingRule = "FIFO"
                });
                _config.Machines.Add(new MachineConfiguration
                {
                    Id = 4,
                    Name = "Grinder",
                    BufferCapacity = 10,
                    DispatchingRule = "FIFO"
                });

                UpdateConfigJson();
                InitializeEngine();
                LogEvent("Default configuration loaded");
            }
            catch (Exception ex)
            {
                LogEvent($"Error loading default config: {ex.Message}");
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Load Simulation Configuration"
                };

                if (dialog.ShowDialog() == true)
                {
                    string json = File.ReadAllText(dialog.FileName);
                    _config = JsonSerializer.Deserialize<SimulationConfiguration>(json);
                    UpdateConfigJson();
                    InitializeEngine();
                    LogEvent($"Configuration loaded from {Path.GetFileName(dialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                // Update config from JSON editor
                _config = JsonSerializer.Deserialize<SimulationConfiguration>(ConfigJson);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Save Simulation Configuration",
                    FileName = "simulation-config.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, ConfigJson);
                    LogEvent($"Configuration saved to {Path.GetFileName(dialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConfigJson()
        {
            ConfigJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        #endregion

        #region Simulation Control

        private void InitializeEngine()
        {
            try
            {
                // Create new engine
                _engine = new SimulationEngine(_config.RandomSeed);

                // Subscribe to events
                _engine.EventProcessed += OnEventProcessed;

                // Add machines from configuration
                Machines.Clear();
                foreach (var machineConfig in _config.Machines)
                {
                    var machine = new Machine(
                        machineConfig.Id,
                        machineConfig.Name
                    );

                    _engine.AddMachine(machine, machineConfig.BufferCapacity);
                    Machines.Add(new MachineViewModel(machine));
                }

                // Schedule part arrivals (simplified - 20 parts with exponential interarrival times)
                var random = new Random(_config.RandomSeed);
                var route = new System.Collections.Generic.List<int> { 1, 2, 3, 4 };
                double currentTime = 0;
                double meanInterarrivalTime = 2.0;

                for (int i = 0; i < 20; i++)
                {
                    // Exponential distribution
                    double interarrivalTime = -meanInterarrivalTime * Math.Log(1 - random.NextDouble());
                    currentTime += interarrivalTime;

                    var part = new Part(
                        $"Part-{i + 1}",
                        new System.Collections.Generic.List<int>(route),
                        currentTime,
                        priority: i + 1,
                        dueDate: currentTime + 50
                    );

                    _engine.SchedulePartArrival(part, currentTime);
                }

                LogEvent("Simulation engine initialized");
            }
            catch (Exception ex)
            {
                LogEvent($"Error initializing engine: {ex.Message}");
            }
        }

        private async void Start()
        {
            try
            {
                if (IsPaused)
                {
                    // Resume from pause
                    IsPaused = false;
                    IsRunning = true;
                    _timer.Start();
                    LogEvent("Simulation resumed");
                    return;
                }

                if (IsRunning) return;

                // Start fresh simulation
                InitializeEngine();
                IsRunning = true;
                IsPaused = false;
                _timer.Start();

                LogEvent("=== Simulation Started ===");

                // Run simulation asynchronously
                await Task.Run(() =>
                {
                    try
                    {
                        _engine.RunUntil(_config.RunLength);
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogEvent($"Simulation error: {ex.Message}");
                        });
                    }
                });

                _timer.Stop();
                IsRunning = false;
                LogEvent("=== Simulation Completed ===");

                MessageBox.Show("Simulation completed!", "Done",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                _timer.Stop();
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Pause()
        {
            if (!IsRunning) return;

            IsPaused = true;
            IsRunning = false;
            _timer.Stop();
            LogEvent("Simulation paused");
        }

        private void Reset()
        {
            _timer.Stop();
            IsRunning = false;
            IsPaused = false;
            CurrentTime = 0;

            InitializeEngine();
            UpdateUI();

            EventLog.Clear();
            LogEvent("Simulation reset");
        }

        #endregion

        #region UI Updates

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_engine == null) return;

            try
            {
                CurrentTime = _engine.CurrentTime;

                var stats = _engine.GetStatistics();
                Statistics.UpdateFromModel(stats);

                for (int i = 0; i < _engine.Machines.Count && i < Machines.Count; i++)
                {
                    var machine = _engine.Machines[i];
                    var buffer = _engine.Buffers[machine.Id];
                    var machineStats = stats.MachineStats.ContainsKey(machine.Id)
                        ? stats.MachineStats[machine.Id]
                        : null;

                    Machines[i].UpdateFromModel(buffer, _engine.CurrentTime, machineStats);
                }
            }
            catch (Exception ex)
            {
                LogEvent($"UI update error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handling

        private void OnEventProcessed(object sender, SimulationEvent evt)
        {
            // Throttle event logging to avoid overwhelming UI
            if (evt is PartArrivalEvent arrival)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    LogEvent($"Part {arrival.Part.Id} arrived at Machine {arrival.Part.GetCurrentMachineId()}");
                });
            }
            else if (evt is ProcessingCompleteEvent complete)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    string nextStep = complete.Part.State == PartState.Completed
                        ? "completed all operations ✓"
                        : $"transferred to Machine {complete.Part.GetCurrentMachineId()}";
                    LogEvent($"Part {complete.Part.Id} {nextStep}");
                });
            }

            // Simulation speed control
            System.Threading.Thread.Sleep(_simulationSpeed);
        }

        private void LogEvent(string message)
        {
            string timestamp = CurrentTime.ToString("F2");
            string logEntry = $"[{timestamp}] {message}";

            EventLog.Add(logEntry);

            // Limit log size
            if (EventLog.Count > 1000)
            {
                EventLog.RemoveAt(0);
            }
        }

        #endregion
    }
}
