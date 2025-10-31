using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace ManufacturingSimulation
{
    /// <summary>
    /// Enhanced ViewModel for configuration with per-machine distributions and JSON persistence
    /// </summary>
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        private int _numMachines = 5;
        private int _bufferCapacity = 10;
        private double _meanArrivalTime = 5.0;
        private double _meanProcessingTime = 3.0;
        private string _selectedDistribution = "Exponential";
        private string _selectedDispatchRule = "FIFO";
        private int _simulationDuration = 1000;
        private MachineConfigViewModel _selectedMachine;

        public bool? DialogResult { get; set; }
        public ManufacturingSimulation.Core.Configuration.SimulationConfiguration ResultConfiguration { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        // Available distribution types
        public ObservableCollection<string> DistributionTypes { get; }
        public ObservableCollection<string> DispatchRules { get; }
        public ObservableCollection<MachineConfigViewModel> MachineConfigs { get; }

        // Global configuration properties
        public int NumMachines
        {
            get => _numMachines;
            set
            {
                if (_numMachines != value && value > 0 && value <= 20)
                {
                    _numMachines = value;
                    OnPropertyChanged();
                    UpdateMachineConfigs();
                }
            }
        }

        public int BufferCapacity
        {
            get => _bufferCapacity;
            set
            {
                if (_bufferCapacity != value && value > 0)
                {
                    _bufferCapacity = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MeanArrivalTime
        {
            get => _meanArrivalTime;
            set
            {
                if (Math.Abs(_meanArrivalTime - value) > 0.001 && value > 0)
                {
                    _meanArrivalTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MeanProcessingTime
        {
            get => _meanProcessingTime;
            set
            {
                if (Math.Abs(_meanProcessingTime - value) > 0.001 && value > 0)
                {
                    _meanProcessingTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedDistribution
        {
            get => _selectedDistribution;
            set
            {
                if (_selectedDistribution != value)
                {
                    _selectedDistribution = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedDispatchRule
        {
            get => _selectedDispatchRule;
            set
            {
                if (_selectedDispatchRule != value)
                {
                    _selectedDispatchRule = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SimulationDuration
        {
            get => _simulationDuration;
            set
            {
                if (_simulationDuration != value && value > 0)
                {
                    _simulationDuration = value;
                    OnPropertyChanged();
                }
            }
        }

        public MachineConfigViewModel SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                if (_selectedMachine != value)
                {
                    _selectedMachine = value;
                    OnPropertyChanged();
                }
            }
        }

        // Commands
        public ICommand SaveConfigCommand { get; }
        public ICommand LoadConfigCommand { get; }
        public ICommand ApplyToAllMachinesCommand { get; }

        public ConfigurationViewModel()
        {
            DistributionTypes = new ObservableCollection<string>
            {
                "Exponential",
                "Normal",
                "Uniform",
                "Triangular",
                "LogNormal",
                "Gamma",
                "Weibull"
            };

            DispatchRules = new ObservableCollection<string>
            {
                "FIFO",
                "LIFO",
                "SPT",
                "Random"
            };

            MachineConfigs = new ObservableCollection<MachineConfigViewModel>();
            
            SaveConfigCommand = new RelayCommand(SaveConfiguration);
            LoadConfigCommand = new RelayCommand(LoadConfiguration);
            ApplyToAllMachinesCommand = new RelayCommand(ApplyToAllMachines);

            UpdateMachineConfigs();
        }

        private void UpdateMachineConfigs()
        {
            int currentCount = MachineConfigs.Count;
            
            // Add new machine configs if needed
            for (int i = currentCount; i < NumMachines; i++)
            {
                var machineConfig = new MachineConfigViewModel
                {
                    MachineId = i + 1,
                    ProcessingDistribution = SelectedDistribution,
                    MeanProcessingTime = MeanProcessingTime,
                    DistributionParam1 = MeanProcessingTime * 0.2, // Default std dev for Normal
                    DistributionParam2 = MeanProcessingTime * 0.5  // Default for other params
                };
                machineConfig.AvailableDistributions.AddRange(DistributionTypes);
                MachineConfigs.Add(machineConfig);
            }

            // Remove excess machine configs
            while (MachineConfigs.Count > NumMachines)
            {
                MachineConfigs.RemoveAt(MachineConfigs.Count - 1);
            }

            // Select first machine if none selected
            if (SelectedMachine == null && MachineConfigs.Count > 0)
            {
                SelectedMachine = MachineConfigs[0];
            }
        }

        private void ApplyToAllMachines()
        {
            if (SelectedMachine == null) return;

            foreach (var machine in MachineConfigs)
            {
                if (machine != SelectedMachine)
                {
                    machine.ProcessingDistribution = SelectedMachine.ProcessingDistribution;
                    machine.MeanProcessingTime = SelectedMachine.MeanProcessingTime;
                    machine.DistributionParam1 = SelectedMachine.DistributionParam1;
                    machine.DistributionParam2 = SelectedMachine.DistributionParam2;
                }
            }

            MessageBox.Show("Configuration applied to all machines.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveConfiguration()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = "simulation_config.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var config = new SimulationConfig
                    {
                        NumMachines = NumMachines,
                        BufferCapacity = BufferCapacity,
                        MeanArrivalTime = MeanArrivalTime,
                        MeanProcessingTime = MeanProcessingTime,
                        ArrivalDistribution = SelectedDistribution,
                        DispatchRule = SelectedDispatchRule,
                        SimulationDuration = SimulationDuration,
                        MachineConfigs = MachineConfigs.Select(m => new MachineConfig
                        {
                            MachineId = m.MachineId,
                            ProcessingDistribution = m.ProcessingDistribution,
                            MeanProcessingTime = m.MeanProcessingTime,
                            DistributionParam1 = m.DistributionParam1,
                            DistributionParam2 = m.DistributionParam2
                        }).ToList()
                    };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string json = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(dialog.FileName, json);

                    MessageBox.Show($"Configuration saved to {dialog.FileName}", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadConfiguration()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    string json = File.ReadAllText(dialog.FileName);
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var config = JsonSerializer.Deserialize<SimulationConfig>(json, options);

                    if (config != null)
                    {
                        NumMachines = config.NumMachines;
                        BufferCapacity = config.BufferCapacity;
                        MeanArrivalTime = config.MeanArrivalTime;
                        MeanProcessingTime = config.MeanProcessingTime;
                        SelectedDistribution = config.ArrivalDistribution;
                        SelectedDispatchRule = config.DispatchRule;
                        SimulationDuration = config.SimulationDuration;

                        // Load machine-specific configs
                        if (config.MachineConfigs != null)
                        {
                            foreach (var machineConfig in config.MachineConfigs)
                            {
                                var vm = MachineConfigs.FirstOrDefault(m => m.MachineId == machineConfig.MachineId);
                                if (vm != null)
                                {
                                    vm.ProcessingDistribution = machineConfig.ProcessingDistribution;
                                    vm.MeanProcessingTime = machineConfig.MeanProcessingTime;
                                    vm.DistributionParam1 = machineConfig.DistributionParam1;
                                    vm.DistributionParam2 = machineConfig.DistributionParam2;
                                }
                            }
                        }

                        MessageBox.Show($"Configuration loaded from {dialog.FileName}", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for individual machine configuration
    /// </summary>
    public class MachineConfigViewModel : INotifyPropertyChanged
    {
        private int _machineId;
        private string _processingDistribution = "Exponential";
        private double _meanProcessingTime = 3.0;
        private double _distributionParam1 = 0.6; // Std dev for Normal, min for Uniform, etc.
        private double _distributionParam2 = 1.5; // Max for Uniform, mode for Triangular, etc.

        public event PropertyChangedEventHandler PropertyChanged;

        public int MachineId
        {
            get => _machineId;
            set
            {
                if (_machineId != value)
                {
                    _machineId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProcessingDistribution
        {
            get => _processingDistribution;
            set
            {
                if (_processingDistribution != value)
                {
                    _processingDistribution = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Param1Label));
                    OnPropertyChanged(nameof(Param2Label));
                    OnPropertyChanged(nameof(ShowParam1));
                    OnPropertyChanged(nameof(ShowParam2));
                }
            }
        }

        public double MeanProcessingTime
        {
            get => _meanProcessingTime;
            set
            {
                if (Math.Abs(_meanProcessingTime - value) > 0.001 && value > 0)
                {
                    _meanProcessingTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public double DistributionParam1
        {
            get => _distributionParam1;
            set
            {
                if (Math.Abs(_distributionParam1 - value) > 0.001)
                {
                    _distributionParam1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public double DistributionParam2
        {
            get => _distributionParam2;
            set
            {
                if (Math.Abs(_distributionParam2 - value) > 0.001)
                {
                    _distributionParam2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> AvailableDistributions { get; } = new List<string>();

        // Dynamic labels based on distribution type
        public string Param1Label
        {
            get
            {
                return ProcessingDistribution switch
                {
                    "Normal" => "Std Dev:",
                    "Uniform" => "Min:",
                    "Triangular" => "Min:",
                    "LogNormal" => "Std Dev:",
                    "Gamma" => "Shape (k):",
                    "Weibull" => "Shape (k):",
                    _ => "Param 1:"
                };
            }
        }

        public string Param2Label
        {
            get
            {
                return ProcessingDistribution switch
                {
                    "Uniform" => "Max:",
                    "Triangular" => "Mode:",
                    "Gamma" => "Scale (θ):",
                    "Weibull" => "Scale (λ):",
                    _ => "Param 2:"
                };
            }
        }

        public bool ShowParam1 => ProcessingDistribution != "Exponential";
        public bool ShowParam2 => ProcessingDistribution == "Uniform" || 
                                   ProcessingDistribution == "Triangular" ||
                                   ProcessingDistribution == "Gamma" ||
                                   ProcessingDistribution == "Weibull";

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Configuration data model for JSON serialization
    /// </summary>
    public class SimulationConfig
    {
        public int NumMachines { get; set; }
        public int BufferCapacity { get; set; }
        public double MeanArrivalTime { get; set; }
        public double MeanProcessingTime { get; set; }
        public string ArrivalDistribution { get; set; }
        public string DispatchRule { get; set; }
        public int SimulationDuration { get; set; }
        public List<MachineConfig> MachineConfigs { get; set; }
    }

    public class MachineConfig
    {
        public int MachineId { get; set; }
        public string ProcessingDistribution { get; set; }
        public double MeanProcessingTime { get; set; }
        public double DistributionParam1 { get; set; }
        public double DistributionParam2 { get; set; }
    }

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
    }
}
