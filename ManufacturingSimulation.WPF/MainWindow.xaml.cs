using ManufacturingSimulation.Bridge;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Configuration;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Events;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.WPF.Services;
using ManufacturingSimulation.WPF.ViewModels;
using ManufacturingSimulation.WPF.ViewModels.Admin;
using ManufacturingSimulation.WPF.Views;
using ManufacturingSimulation.WPF.Views;
using ManufacturingSimulation.WPF.Views.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;  // ← ADDED
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;  // ← ADDED
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CoreMachine = ManufacturingSimulation.Core.Models.Machine;
using CoreSimEvent = ManufacturingSimulation.Core.Engine.SimulationEvent;  // ← Correct
using DbMachine = ManufacturingSimulation.Database.Models.Machine;
using DbSimEvent = ManufacturingSimulation.Database.Models.SimulationEvent;  // ← Correct
using ManufacturingSimulation.WPF.Views;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Bridge;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

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
        private List<ProductionOrder> _selectedOrders;

        private MesDbContext _context;
        private Student _currentStudent;
        private int _currentRunId = 0; 
        public MainWindow()
        {
            InitializeComponent();
            _context = new MesDbContext();
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

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var simWindow = new SimulationRunnerWindow();
            simWindow.Show();
        }
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

                ApplySimulationParameters();

                // CREATE RUN RECORD
                using (var db = new MesDbContext())
                {
                    var run = new SimulationRun
                    {
                        ScenarioId = 1,
                        StudentId = 1,
                        RunDate = DateTime.Now,
                        Status = "Running",
                        ConfigJson = $"Duration: {_config.RunLength}h, Seed: {_config.RandomSeed}"
                    };
                    db.SimulationRuns.Add(run);
                    db.SaveChanges();
                    _currentRunId = run.RunId;
                }

                // Initialize engine
                _engine = new SimulationEngine(_config.RandomSeed);
                _engine.EventProcessed += OnDbSimEventProcessed;

                var logger = new Bridge.SimulationEventLogger(_currentRunId, new MesDbContext());
                _engine.SetEventLogger(logger);

                // Load machines from database
                int machineCount = 0;
                using (var db = new MesDbContext())
                {
                    var workCenters = db.WorkCenters.Where(wc => wc.IsActive == true).OrderBy(wc => wc.WorkCenterId).ToList();
                    foreach (var wc in workCenters)
                    {
                        var machine = new CoreMachine(wc.WorkCenterId, wc.WorkCenterName, null);
                        _engine.AddMachine(machine, 10);
                        machineCount++;
                    }
                }

                // Generate parts from orders
                if (_selectedOrders == null || !_selectedOrders.Any())
                {
                    LogEvent("ERROR: No orders selected");
                    MessageBox.Show("Select production orders first", "No Orders", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int totalParts = 0;
                double currentArrivalTime = 0;
                var mapper = new MesToSimulationMapper(_config.RandomSeed);

                using (var db = new MesDbContext())
                {
                    foreach (var order in _selectedOrders)
                    {
                        var fullOrder = db.ProductionOrders
                            .Include(o => o.Product)
                                .ThenInclude(p => p.Routings)
                                    .ThenInclude(r => r.WorkCenter)
                            .FirstOrDefault(o => o.OrderId == order.OrderId);

                        if (fullOrder?.Product?.Routings == null || !fullOrder.Product.Routings.Any())
                        {
                            LogEvent($"WARNING: No routing for order {order.OrderNumber}");
                            continue;
                        }

                        var parts = mapper.MapToParts(fullOrder, currentArrivalTime);
                        foreach (var part in parts)
                            _engine.SchedulePartArrival(part, part.ArrivalTime);

                        totalParts += parts.Count;
                        currentArrivalTime += 0.5;
                    }
                }

                LogEvent($"Run {_currentRunId}: {_selectedOrders.Count} orders, {totalParts} parts, {machineCount} machines, {_config.RunLength}h");

                _isRunning = true;
                _uiUpdateTimer.Start();
                UpdateControlStates(true);
                UpdateStatus("Running");

                Task.Run(() =>
                {
                    try
                    {
                        _engine.RunUntil(_config.RunLength);
                        Dispatcher.Invoke(() => OnSimulationCompleted(_currentRunId));
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
                // Read duration from UI
                if (double.TryParse(txtDuration.Text, out double duration) && duration > 0)
                {
                    _config.RunLength = duration;
                    LogEvent($"Duration set to {duration} hours");
                }

                // Read number of jobs from UI
                if (int.TryParse(txtNumberOfJobs.Text, out int numJobs) && numJobs > 0)
                {
                    _config.NumberOfParts = numJobs;
                    LogEvent($"Number of jobs set to {numJobs}");
                }

                LogEvent($"Parameters applied: Duration={_config.RunLength}h, Jobs={_config.NumberOfParts}");
            }
            catch (Exception ex)
            {
                ShowError($"Error applying parameters: {ex.Message}");
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
                txtOnTimeDelivery.Text = "N/A";

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

        private void MenuNewSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning && MessageBox.Show("Stop current simulation?", "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            ResetSimulation();
            LoadDefaultConfiguration();
        }

        private void MenuConfigureSystem_Click(object sender, RoutedEventArgs e) { }
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
        private void RoutingManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new MesDbContext();

                // HARDCODED for development - replace with actual student selection later
                int hardcodedStudentId = 1;  // ← Change this to match a student in your database

                var student = db.Students.FirstOrDefault(s => s.StudentId == hardcodedStudentId);

                if (student == null)
                {
                    MessageBox.Show($"Student with ID {hardcodedStudentId} not found in database.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var routingWindow = new RoutingManagementWindow(new MesDbContext(), student);
                routingWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Routing Management: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void OnDbSimEventProcessed(object sender, CoreSimEvent e)
        {
            // Already using Dispatcher.BeginInvoke - make sure ALL LogEvent calls use it
            if (e is PartArrivalEvent arrival)
            {
                Dispatcher.BeginInvoke(() =>
                    LogEvent($"Part {arrival.Part.Id} arrived at Machine {arrival.Part.GetCurrentMachineId()}"));
            }
            else if (e is ProcessingCompleteEvent complete)
            {
                Dispatcher.BeginInvoke(() =>
                    LogEvent($"{complete.Part.Id} completed on {complete.Machine.Name}"));
            }
        }

        private void LogEvent(string message)
        {
            // Always use Dispatcher to modify ObservableCollection
            Dispatcher.Invoke(() =>
            {
                _eventLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

                if (chkAutoScroll?.IsChecked == true && lstEventLog?.Items.Count > 0)
                    lstEventLog.ScrollIntoView(lstEventLog.Items[lstEventLog.Items.Count - 1]);

                if (_eventLog.Count > 10000)
                    _eventLog.RemoveAt(0);
            });
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

        private void DatabaseAdmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = new MesDbContext();


                var adminService = new AdminService(context);
                var viewModel = new DatabaseAdminViewModel(context, adminService, null);
                var adminWindow = new DatabaseAdminWindow(viewModel);
                adminWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Database Admin Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SeedTestData(MesDbContext context)
        {
            var student = new Student
            {
                Username = "testuser",
                PasswordHash = "hash123",
                FullName = "Test User",
                Email = "test@example.com",
                IsActive = true
            };
            context.Students.Add(student);
            context.SaveChanges();

            var product = new Product
            {
                StudentId = student.StudentId,
                ProductNumber = "P001",
                ProductName = "Test Product",
                IsActive = true
            };
            context.Products.Add(product);

            var workCenter = new WorkCenter
            {
                StudentId = student.StudentId,
                WorkCenterCode = "WC001",
                WorkCenterName = "Mill",
                BufferCapacity = 10,
                IsActive = true
            };
            context.WorkCenters.Add(workCenter);

            context.SaveChanges();
        }

        private void ManageOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = new MesDbContext();
                var orderWindow = new Views.OrderManagementWindow(context);

                // Subscribe to simulation request event
                var viewModel = orderWindow.DataContext as OrderManagementViewModel;
                if (viewModel != null)
                {
                    viewModel.SimulationRequested += OnSimulationRequested;
                }

                orderWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening order management: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSimulationRequested(object sender, List<ProductionOrder> selectedOrders)
        {
            // Store selected orders for simulation
            _selectedOrders = selectedOrders;

            // Update UI with order info
            LogEvent($"=== SIMULATION WITH {selectedOrders.Count} ORDERS ===");
            foreach (var order in selectedOrders)
            {
                LogEvent($"Order {order.OrderNumber}: {order.Product.ProductName} x{order.Quantity}");
            }

            // Auto-start simulation with these orders
            MessageBox.Show($"Ready to simulate {selectedOrders.Count} orders!",
                "Orders Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private int? _lastRunId; // Add this field

        private void TestGantt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int runId = _currentRunId > 0 ? _currentRunId : (_lastRunId ?? 54);

                LogEvent($"Opening Gantt Chart for run ID: {runId}");

                var ganttWindow = new GanttChartWindow(runId);
                ganttWindow.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Gantt Chart:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // After your simulation completes, call this:
        private void OnSimulationCompleted(int runId)
        {
            MessageBox.Show($"OnSimulationCompleted called with runId: {runId}");
            _currentRunId = runId;
            _lastRunId = runId;

            if (_engine?.EventLogger != null)
            {
                _engine.EventLogger.Flush();
                LogEvent($"Events flushed for run {runId}");
            }

            // Calculate and display KPIs
            var stats = _engine.GetStatistics();

            txtThroughput.Text = $"{stats.Throughput:F2} parts/hr";
            txtAvgLeadTime.Text = $"{(stats.AverageFlowTime / 60):F2} hrs"; // Convert min to hrs
            txtWIP.Text = $"{stats.CurrentWIP} parts";

            // Calculate efficiency (avg machine utilization)
            double avgUtilization = stats.MachineStats.Values.Average(m => m.Utilization);
            txtOverallEfficiency.Text = $"{avgUtilization:F1}%";

            // Find bottleneck (highest utilization)
            var bottleneck = stats.MachineStats.Values.OrderByDescending(m => m.Utilization).FirstOrDefault();
            txtBottleneck.Text = bottleneck?.MachineName ?? "N/A";

            // On-time delivery needs due dates - set to N/A for now
            txtOnTimeDelivery.Text = "N/A";

            StopSimulation();
            progressBar.Value = 100;
            UpdateStatus("Completed");
            LogEvent($"=== Completed - Run {runId} ===");

            btnViewGanttChart.IsEnabled = true;

            MessageBox.Show($"Simulation Complete!\nRun ID: {runId}\nThroughput: {stats.Throughput:F2} parts/hr",
                "Done", MessageBoxButton.OK);
        }

        private void btnViewGanttChart_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Gantt Chart feature coming soon!\n\nWe need to add the GanttChartWindow files first.",
                "Feature Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            /* UNCOMMENT AFTER ADDING GANTT FILES:
            try
            {
                if (_lastRunId.HasValue)
                {
                    var ganttWindow = new GanttChartWindow(_lastRunId.Value);
                    ganttWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            */
        }
    }

    // ← SUPPORTING CLASS OUTSIDE THE MAIN CLASS
    public class ResourceUtilizationInfo
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Utilization { get; set; }
    }
}