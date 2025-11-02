using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Configuration;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Events;
using ManufacturingSimulation.Core.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ManufacturingSimulation.WPF
{
    public partial class MainWindow : Window
    {
        private SimulationEngine _engine;
        private SimulationConfiguration _config;
        private DispatcherTimer _uiUpdateTimer;
        private bool _isRunning = false;
        private bool _isPaused = false;

        private ObservableCollection<ResourceUtilizationInfo> _resourceUtilization;
        private ObservableCollection<string> _eventLog;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCollections();
            InitializeTimer();
            LoadDefaultConfiguration();
        }

        private void InitializeCollections()
        {
            _resourceUtilization = new ObservableCollection<ResourceUtilizationInfo>();
            _eventLog = new ObservableCollection<string>();
            lstResourceUtilization.ItemsSource = _resourceUtilization;
            lstEventLog.ItemsSource = _eventLog;
        }

        private void InitializeTimer()
        {
            _uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        private void LoadDefaultConfiguration()
        {
            try
            {
                _config = new SimulationConfiguration();
                txtConfigEditor.Text = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                UpdateStatus("Configuration initialized");
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Ready");
            LogEvent("Application started");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e) => StartSimulation();
        private void BtnPause_Click(object sender, RoutedEventArgs e) => PauseSimulation();
        private void BtnStop_Click(object sender, RoutedEventArgs e) => StopSimulation();
        private void BtnReset_Click(object sender, RoutedEventArgs e) => ResetSimulation();

        private void StartSimulation()
        {
            try
            {
                if (_isPaused)
                {
                    _isPaused = false;
                    _uiUpdateTimer.Start();
                    UpdateControlStates(true);
                    UpdateStatus("Resumed");
                    LogEvent("Simulation resumed");
                    return;
                }
                if (_isRunning) return;

                // Initialize engine with random seed from config
                _engine = new SimulationEngine(_config.RandomSeed);

                // Subscribe to simulation events for logging
                _engine.EventProcessed += OnSimulationEventProcessed;

                // Configure machines from config
                foreach (var machineConfig in _config.Machines)
                {
                    // Machine constructor expects IDispatchingRule, not string
                    // Pass null to use default FIFO rule
                    var machine = new Machine(
                        machineConfig.Id,
                        machineConfig.Name,
                        null); // Will use default dispatching rule
                    _engine.AddMachine(machine, machineConfig.BufferCapacity);
                    LogEvent($"Added machine: {machine.Name} (Buffer: {machineConfig.BufferCapacity}, Rule: {machineConfig.DispatchingRule})");
                }

                // Generate and schedule part arrivals
                var arrivalDistribution = _config.GetArrivalDistribution();
                var random = new Random(_config.RandomSeed);
                double currentArrivalTime = 0;

                for (int i = 0; i < _config.NumberOfParts; i++)
                {
                    // Generate inter-arrival time
                    double interArrivalTime = arrivalDistribution.Sample(random);
                    currentArrivalTime += interArrivalTime;

                    // Create part with standard route (id must be string)
                    var part = new Part(
                        $"Part-{i + 1}",                          // id (string)
                        new List<int>(_config.StandardRoute),    // route
                        currentArrivalTime);                      // arrivalTime

                    _engine.SchedulePartArrival(part, currentArrivalTime);
                }

                LogEvent($"Scheduled {_config.NumberOfParts} parts with arrival distribution: {_config.ArrivalDistributionType}");

                _isRunning = true;
                _uiUpdateTimer.Start();
                UpdateControlStates(true);
                UpdateStatus("Started");
                LogEvent("=== Simulation Started ===");

                // Start simulation in background thread
                double duration = _config.RunLength; // Use config default

                // Allow UI override if specified
                if (double.TryParse(txtDuration.Text, out double uiDuration) && uiDuration > 0)
                {
                    duration = uiDuration;
                }

                LogEvent($"Starting simulation: Duration={duration}, Parts={_config.NumberOfParts}, Seed={_config.RandomSeed}");

                Task.Run(() =>
                {
                    try
                    {
                        _engine.RunUntil(duration);
                        Dispatcher.Invoke(() => OnSimulationCompleted());
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => ShowError($"Simulation error: {ex.Message}"));
                    }
                });
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
                StopSimulation();
            }
        }

        private void PauseSimulation()
        {
            if (!_isRunning || _isPaused) return;
            _isPaused = true;
            _uiUpdateTimer.Stop();
            UpdateControlStates(false);
            UpdateStatus("Paused");
            LogEvent("Paused");
        }

        private void StopSimulation()
        {
            if (!_isRunning) return;
            _uiUpdateTimer.Stop();
            _isRunning = false;
            _isPaused = false;
            UpdateControlStates(false);
            UpdateStatus("Stopped");
            LogEvent("=== Stopped ===");
        }

        private void ResetSimulation()
        {
            StopSimulation();
            _eventLog.Clear();
            _resourceUtilization.Clear();
            txtState.Text = "Not Started";
            txtSimTime.Text = "00:00:00";
            txtJobsCompleted.Text = "0 / 0";
            txtActiveTasks.Text = "0";
            txtEfficiency.Text = "0.0%";
            ResetAnalytics();
            progressBar.Value = 0;
            UpdateStatus("Reset");
            LogEvent("Reset");
        }

        private void ApplySimulationParameters()
        {
            try
            {
                double.TryParse(txtDuration.Text, out double duration);
                int.TryParse(txtNumberOfJobs.Text, out int numJobs);
                LogEvent($"Parameters: Duration={duration}h, Jobs={numJobs}");
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_engine == null || !_isRunning || _isPaused) return;
            try
            {
                UpdateSimulationStatus();
                UpdateResourceUtilization();
                UpdateAnalytics();
                UpdateProgressBar();
            }
            catch { }
        }

        private void UpdateSimulationStatus()
        {
            if (_engine == null) return;

            try
            {
                var stats = _engine.GetStatistics();

                txtState.Text = _isPaused ? "Paused" : "Running";
                txtSimTime.Text = FormatTime(stats.CurrentTime);
                txtJobsCompleted.Text = $"{stats.TotalPartsCompleted} / {stats.TotalPartsArrived}";
                txtActiveTasks.Text = stats.CurrentWIP.ToString();

                // Calculate overall efficiency
                double totalUtilization = 0;
                int machineCount = 0;
                foreach (var machineStat in stats.MachineStats.Values)
                {
                    totalUtilization += machineStat.Utilization;
                    machineCount++;
                }
                double avgUtilization = machineCount > 0 ? totalUtilization / machineCount : 0;
                txtEfficiency.Text = $"{avgUtilization:F1}%";
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating status: {ex.Message}");
            }
        }

        private string FormatTime(double time)
        {
            int hours = (int)(time / 60);
            int minutes = (int)(time % 60);
            int seconds = (int)((time % 1) * 60);
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        private void UpdateResourceUtilization()
        {
            if (_engine == null) return;

            try
            {
                var stats = _engine.GetStatistics();
                _resourceUtilization.Clear();

                foreach (var machine in _engine.Machines)
                {
                    var machineStat = stats.MachineStats[machine.Id];

                    _resourceUtilization.Add(new ResourceUtilizationInfo
                    {
                        Name = machine.Name,
                        Status = machine.State.ToString(),
                        Utilization = $"{machineStat.Utilization:F1}%"
                    });
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating resource utilization: {ex.Message}");
            }
        }

        private void UpdateAnalytics()
        {
            if (_engine == null) return;

            try
            {
                var stats = _engine.GetStatistics();

                txtThroughput.Text = $"{stats.Throughput:F2} jobs/hr";
                txtAvgLeadTime.Text = stats.AverageFlowTime > 0 ? $"{stats.AverageFlowTime:F2} hrs" : "N/A";

                // Overall efficiency from machine utilizations
                double totalUtilization = 0;
                int machineCount = 0;
                foreach (var machineStat in stats.MachineStats.Values)
                {
                    totalUtilization += machineStat.Utilization;
                    machineCount++;
                }
                double avgUtilization = machineCount > 0 ? totalUtilization / machineCount : 0;
                txtOverallEfficiency.Text = $"{avgUtilization:F2}%";

                txtWIP.Text = stats.CurrentWIP.ToString();

                // On-time delivery (placeholder - you may need to add this to your stats)
                txtOnTimeDelivery.Text = "N/A";

                // Find bottleneck (machine with highest utilization)
                string bottleneck = "N/A";
                double maxUtil = 0;
                foreach (var kvp in stats.MachineStats)
                {
                    if (kvp.Value.Utilization > maxUtil)
                    {
                        maxUtil = kvp.Value.Utilization;
                        bottleneck = kvp.Value.MachineName;
                    }
                }
                txtBottleneck.Text = bottleneck;
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating analytics: {ex.Message}");
            }
        }

        private void UpdateProgressBar()
        {
            if (_engine == null || _config == null) return;

            try
            {
                double progress = (_engine.CurrentTime / _config.RunLength) * 100.0;
                progress = Math.Min(100, Math.Max(0, progress));

                progressBar.Value = progress;
                txtProgressStatus.Text = $"Progress: {progress:F1}%";
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating progress: {ex.Message}");
            }
        }

        private void ResetAnalytics()
        {
            txtThroughput.Text = "0.00 jobs/hr";
            txtAvgLeadTime.Text = "0.00 hrs";
            txtOverallEfficiency.Text = "0.00%";
            txtWIP.Text = "0";
            txtOnTimeDelivery.Text = "0.00%";
            txtBottleneck.Text = "N/A";
            if (dgMetrics != null) dgMetrics.ItemsSource = null;
        }

        private void UpdateControlStates(bool isRunning)
        {
            btnStart.IsEnabled = !isRunning;
            btnPause.IsEnabled = isRunning && !_isPaused;
            btnStop.IsEnabled = isRunning;
            btnReset.IsEnabled = !isRunning;
            txtDuration.IsEnabled = !isRunning;
            txtNumberOfJobs.IsEnabled = !isRunning;
            cmbSchedulingMode.IsEnabled = !isRunning;
        }

        // Menu Handlers
        private void MenuNewSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning && MessageBox.Show("Stop current simulation?", "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            ResetSimulation();
            LoadDefaultConfiguration();
        }

        private void MenuConfigureSystem_Click(object sender, RoutedEventArgs e)
        {/* commented out for now 
            var configWindow = new Views.ConfigurationEditorWindow();

            if (configWindow.ShowDialog() == true && configWindow.WasApplied)
            {
                _config = configWindow.ResultConfiguration;

                // Update UI to show new config
                txtConfigEditor.Text = JsonSerializer.Serialize(_config,
                    new JsonSerializerOptions { WriteIndented = true });

                txtDuration.Text = _config.RunLength.ToString();
                txtNumberOfJobs.Text = _config.NumberOfParts.ToString();

                UpdateStatus("Configuration updated from editor");
                LogEvent($"Loaded config: {_config.Machines.Count} machines, {_config.NumberOfParts} parts");

                MessageBox.Show("Configuration applied successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            */
        }

        private void MenuLoadConfig_Click(object sender, RoutedEventArgs e) => LoadConfigurationFromFile();
        private void MenuSaveConfig_Click(object sender, RoutedEventArgs e) => SaveConfigurationToFile();
        private void MenuExportResults_Click(object sender, RoutedEventArgs e) => ExportResults();
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
        private void MenuStartSimulation_Click(object sender, RoutedEventArgs e) => StartSimulation();
        private void MenuPauseSimulation_Click(object sender, RoutedEventArgs e) => PauseSimulation();
        private void MenuStopSimulation_Click(object sender, RoutedEventArgs e) => StopSimulation();
        private void MenuResetSimulation_Click(object sender, RoutedEventArgs e) => ResetSimulation();
        private void MenuSpeedUp_Click(object sender, RoutedEventArgs e) => LogEvent("Speed up");
        private void MenuSlowDown_Click(object sender, RoutedEventArgs e) => LogEvent("Slow down");
        private void MenuShowGantt_Click(object sender, RoutedEventArgs e) { }
        private void MenuShowAnalytics_Click(object sender, RoutedEventArgs e) { }
        private void MenuShowLog_Click(object sender, RoutedEventArgs e) { }
        private void MenuRefreshAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateSimulationStatus();
            UpdateResourceUtilization();
            UpdateAnalytics();
            LogEvent("Refreshed");
        }
        private void MenuDocumentation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Manufacturing Simulation System", "Documentation", MessageBoxButton.OK);
        }
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Manufacturing Simulation System v2.0", "About", MessageBoxButton.OK);
        }

        // Configuration
        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e) => LoadConfigurationFromFile();
        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e) => SaveConfigurationToFile();
        private void BtnLoadFromDb_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Database integration not implemented.", "Info", MessageBoxButton.OK);
        }
        private void BtnSaveToDb_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Database integration not implemented.", "Info", MessageBoxButton.OK);
        }
        private void BtnValidateConfig_Click(object sender, RoutedEventArgs e) => ValidateConfiguration();
        private void BtnApplyParameters_Click(object sender, RoutedEventArgs e)
        {
            ApplySimulationParameters();
            MessageBox.Show("Parameters applied.", "Success", MessageBoxButton.OK);
        }

        private void LoadConfigurationFromFile()
        {
            var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    _config = JsonSerializer.Deserialize<SimulationConfiguration>(json);
                    txtConfigEditor.Text = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                    UpdateStatus($"Loaded: {Path.GetFileName(dialog.FileName)}");
                    LogEvent($"Config loaded: {dialog.FileName}");
                    MessageBox.Show("Configuration loaded.", "Success", MessageBoxButton.OK);
                }
                catch (Exception ex) { ShowError($"Error: {ex.Message}"); }
            }
        }

        private void SaveConfigurationToFile()
        {
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "config.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _config = JsonSerializer.Deserialize<SimulationConfiguration>(txtConfigEditor.Text);
                    string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json);
                    UpdateStatus($"Saved: {Path.GetFileName(dialog.FileName)}");
                    LogEvent($"Config saved: {dialog.FileName}");
                    MessageBox.Show("Configuration saved.", "Success", MessageBoxButton.OK);
                }
                catch (Exception ex) { ShowError($"Error: {ex.Message}"); }
            }
        }

        private void ValidateConfiguration()
        {
            try
            {
                JsonSerializer.Deserialize<SimulationConfiguration>(txtConfigEditor.Text);
                MessageBox.Show("Configuration is valid!", "Success", MessageBoxButton.OK);
                LogEvent("Config validated");
            }
            catch (Exception ex) { ShowError($"Validation failed: {ex.Message}"); }
        }

        private void CmbSpeed_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            _eventLog.Clear();
            LogEvent("Log cleared");
        }

        private void BtnExportLog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllLines(dialog.FileName, _eventLog);
                    MessageBox.Show("Log exported.", "Success", MessageBoxButton.OK);
                }
                catch (Exception ex) { ShowError($"Error: {ex.Message}"); }
            }
        }

        private void ExportResults()
        {
            if (_engine == null)
            {
                MessageBox.Show("No simulation results.", "Info", MessageBoxButton.OK);
                return;
            }
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"results_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, "{}");
                    UpdateStatus($"Exported: {Path.GetFileName(dialog.FileName)}");
                    MessageBox.Show("Results exported.", "Success", MessageBoxButton.OK);
                }
                catch (Exception ex) { ShowError($"Error: {ex.Message}"); }
            }
        }

        private void OnSimulationCompleted()
        {
            StopSimulation();
            progressBar.Value = 100;
            UpdateStatus("Completed");
            LogEvent("=== Completed ===");
            MessageBox.Show("Simulation Complete!", "Done", MessageBoxButton.OK);
        }

        private void OnSimulationEventProcessed(object sender, SimulationEvent e)
        {
            // Log key events to UI (throttled to avoid overwhelming)
            if (e is PartArrivalEvent arrival)
            {
                Dispatcher.BeginInvoke(() => LogEvent($"Part {arrival.Part.Id} arrived at Machine {arrival.Part.GetCurrentMachineId()}"));
            }
            else if (e is ProcessingCompleteEvent complete)
            {
                Dispatcher.BeginInvoke(() => LogEvent($"{complete.Part.Id} completed on {complete.Machine.Name}"));
            }
        }

        private void LogEvent(string message)
        {
            _eventLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            if (chkAutoScroll?.IsChecked == true && lstEventLog?.Items.Count > 0)
                lstEventLog.ScrollIntoView(lstEventLog.Items[lstEventLog.Items.Count - 1]);
            if (_eventLog.Count > 10000) _eventLog.RemoveAt(0);
        }

        private void UpdateStatus(string message) => txtStatusBar.Text = message;

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogEvent($"ERROR: {message}");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isRunning && MessageBox.Show("Exit while running?", "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            StopSimulation();
            _uiUpdateTimer?.Stop();
        }
    }

    public class ResourceUtilizationInfo
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Utilization { get; set; }
    }
}
