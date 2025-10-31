using System;
using System.Collections.Generic;

namespace ManufacturingSimulation.Core.Configuration
{
    /// <summary>
    /// Data model for JSON serialization of simulation configuration.
    /// This is a simple data container (POCO) without UI concerns.
    /// </summary>
    public class ConfigurationData
    {
        /// <summary>
        /// Number of machines in the production line
        /// </summary>
        public int NumberOfMachines { get; set; } = 5;

        /// <summary>
        /// Maximum capacity of each buffer
        /// </summary>
        public int BufferCapacity { get; set; } = 10;

        /// <summary>
        /// Mean time between part arrivals
        /// </summary>
        public double MeanArrivalTime { get; set; } = 5.0;

        /// <summary>
        /// Global mean processing time (can be overridden per machine)
        /// </summary>
        public double MeanProcessingTime { get; set; } = 3.0;

        /// <summary>
        /// Distribution type for part arrivals (Exponential, Normal, etc.)
        /// </summary>
        public string ArrivalDistribution { get; set; } = "Exponential";

        /// <summary>
        /// Dispatching rule (FIFO, LIFO, SPT, Random)
        /// </summary>
        public string DispatchRule { get; set; } = "FIFO";

        /// <summary>
        /// Total simulation time in time units
        /// </summary>
        public int SimulationDuration { get; set; } = 1000;

        /// <summary>
        /// Warm-up period to exclude from statistics
        /// </summary>
        public int WarmupPeriod { get; set; } = 0;

        /// <summary>
        /// Per-machine configuration settings
        /// </summary>
        public List<MachineConfigData> MachineConfigurations { get; set; } = new List<MachineConfigData>();

        /// <summary>
        /// Optional: Configuration name/description
        /// </summary>
        public string ConfigurationName { get; set; } = "Default Configuration";

        /// <summary>
        /// Optional: Notes about this configuration
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Timestamp when configuration was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional: Version for backward compatibility
        /// </summary>
        public string Version { get; set; } = "2.0";

        /// <summary>
        /// Validates the configuration data
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (NumberOfMachines <= 0 || NumberOfMachines > 100)
            {
                errorMessage = "Number of machines must be between 1 and 100";
                return false;
            }

            if (BufferCapacity <= 0)
            {
                errorMessage = "Buffer capacity must be greater than 0";
                return false;
            }

            if (MeanArrivalTime <= 0)
            {
                errorMessage = "Mean arrival time must be greater than 0";
                return false;
            }

            if (MeanProcessingTime <= 0)
            {
                errorMessage = "Mean processing time must be greater than 0";
                return false;
            }

            if (SimulationDuration <= 0)
            {
                errorMessage = "Simulation duration must be greater than 0";
                return false;
            }

            if (WarmupPeriod < 0)
            {
                errorMessage = "Warmup period cannot be negative";
                return false;
            }

            if (WarmupPeriod >= SimulationDuration)
            {
                errorMessage = "Warmup period must be less than simulation duration";
                return false;
            }

            // Validate each machine configuration
            foreach (var machineConfig in MachineConfigurations)
            {
                if (!machineConfig.IsValid(out string machineError))
                {
                    errorMessage = $"Machine {machineConfig.MachineId}: {machineError}";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Creates a deep copy of the configuration
        /// </summary>
        public ConfigurationData Clone()
        {
            var clone = new ConfigurationData
            {
                NumberOfMachines = NumberOfMachines,
                BufferCapacity = BufferCapacity,
                MeanArrivalTime = MeanArrivalTime,
                MeanProcessingTime = MeanProcessingTime,
                ArrivalDistribution = ArrivalDistribution,
                DispatchRule = DispatchRule,
                SimulationDuration = SimulationDuration,
                WarmupPeriod = WarmupPeriod,
                ConfigurationName = ConfigurationName,
                Notes = Notes,
                CreatedDate = CreatedDate,
                Version = Version
            };

            foreach (var machine in MachineConfigurations)
            {
                clone.MachineConfigurations.Add(machine.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// Configuration data for an individual machine
    /// </summary>
    public class MachineConfigData
    {
        /// <summary>
        /// Machine identifier (1-based)
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Distribution type for processing times
        /// </summary>
        public string ProcessingDistribution { get; set; } = "Exponential";

        /// <summary>
        /// Mean processing time for this machine
        /// </summary>
        public double MeanProcessingTime { get; set; } = 3.0;

        /// <summary>
        /// First distribution parameter (e.g., standard deviation for Normal)
        /// </summary>
        public double DistributionParam1 { get; set; } = 0.0;

        /// <summary>
        /// Second distribution parameter (e.g., max for Uniform)
        /// </summary>
        public double DistributionParam2 { get; set; } = 0.0;

        /// <summary>
        /// Optional: Machine name/description
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Machine failure rate (for future use)
        /// </summary>
        public double FailureRate { get; set; } = 0.0;

        /// <summary>
        /// Optional: Mean repair time (for future use)
        /// </summary>
        public double MeanRepairTime { get; set; } = 0.0;

        /// <summary>
        /// Optional: Setup time (for future use)
        /// </summary>
        public double SetupTime { get; set; } = 0.0;

        /// <summary>
        /// Validates the machine configuration
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (MachineId <= 0)
            {
                errorMessage = "Machine ID must be greater than 0";
                return false;
            }

            if (MeanProcessingTime <= 0)
            {
                errorMessage = "Mean processing time must be greater than 0";
                return false;
            }

            // Validate distribution-specific parameters
            switch (ProcessingDistribution)
            {
                case "Normal":
                case "LogNormal":
                    if (DistributionParam1 <= 0)
                    {
                        errorMessage = "Standard deviation must be greater than 0";
                        return false;
                    }
                    break;

                case "Uniform":
                    if (DistributionParam1 >= DistributionParam2)
                    {
                        errorMessage = "Min must be less than Max for Uniform distribution";
                        return false;
                    }
                    if (DistributionParam1 < 0)
                    {
                        errorMessage = "Min must be non-negative for Uniform distribution";
                        return false;
                    }
                    break;

                case "Triangular":
                    if (DistributionParam1 >= MeanProcessingTime || MeanProcessingTime >= DistributionParam2)
                    {
                        errorMessage = "Triangular distribution requires Min < Mode < Max";
                        return false;
                    }
                    if (DistributionParam1 < 0)
                    {
                        errorMessage = "Min must be non-negative for Triangular distribution";
                        return false;
                    }
                    break;

                case "Gamma":
                case "Weibull":
                    if (DistributionParam1 <= 0)
                    {
                        errorMessage = "Shape parameter must be greater than 0";
                        return false;
                    }
                    if (DistributionParam2 <= 0)
                    {
                        errorMessage = "Scale parameter must be greater than 0";
                        return false;
                    }
                    break;

                case "Exponential":
                    // No additional validation needed
                    break;

                default:
                    errorMessage = $"Unknown distribution type: {ProcessingDistribution}";
                    return false;
            }

            if (FailureRate < 0 || FailureRate > 1)
            {
                errorMessage = "Failure rate must be between 0 and 1";
                return false;
            }

            if (MeanRepairTime < 0)
            {
                errorMessage = "Mean repair time cannot be negative";
                return false;
            }

            if (SetupTime < 0)
            {
                errorMessage = "Setup time cannot be negative";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Creates a deep copy of the machine configuration
        /// </summary>
        public MachineConfigData Clone()
        {
            return new MachineConfigData
            {
                MachineId = MachineId,
                ProcessingDistribution = ProcessingDistribution,
                MeanProcessingTime = MeanProcessingTime,
                DistributionParam1 = DistributionParam1,
                DistributionParam2 = DistributionParam2,
                MachineName = MachineName,
                FailureRate = FailureRate,
                MeanRepairTime = MeanRepairTime,
                SetupTime = SetupTime
            };
        }

        /// <summary>
        /// Gets a human-readable description of the distribution
        /// </summary>
        public string GetDistributionDescription()
        {
            return ProcessingDistribution switch
            {
                "Exponential" => $"Exponential(λ={1.0 / MeanProcessingTime:F3})",
                "Normal" => $"Normal(μ={MeanProcessingTime:F2}, σ={DistributionParam1:F2})",
                "Uniform" => $"Uniform(min={DistributionParam1:F2}, max={DistributionParam2:F2})",
                "Triangular" => $"Triangular(min={DistributionParam1:F2}, mode={MeanProcessingTime:F2}, max={DistributionParam2:F2})",
                "LogNormal" => $"LogNormal(μ={MeanProcessingTime:F2}, σ={DistributionParam1:F2})",
                "Gamma" => $"Gamma(k={DistributionParam1:F2}, θ={DistributionParam2:F2})",
                "Weibull" => $"Weibull(k={DistributionParam1:F2}, λ={DistributionParam2:F2})",
                _ => ProcessingDistribution
            };
        }
    }

    /// <summary>
    /// Helper class for configuration file operations
    /// </summary>
    public static class ConfigurationDataHelper
    {
        /// <summary>
        /// Gets default parameter values for a distribution type
        /// </summary>
        public static (double param1, double param2) GetDefaultParameters(string distributionType, double mean)
        {
            return distributionType switch
            {
                "Normal" => (mean * 0.2, 0.0),           // StdDev = 20% of mean
                "LogNormal" => (mean * 0.2, 0.0),        // StdDev = 20% of mean
                "Uniform" => (mean * 0.5, mean * 1.5),   // Min = 50%, Max = 150%
                "Triangular" => (mean * 0.5, mean * 1.5), // Min = 50%, Max = 150%
                "Gamma" => (2.0, mean / 2.0),            // Shape = 2, Scale = mean/2
                "Weibull" => (2.0, mean / 0.886),        // Shape = 2, Scale adjusted
                _ => (0.0, 0.0)                          // Exponential or unknown
            };
        }

        /// <summary>
        /// Creates a default configuration with specified number of machines
        /// </summary>
        public static ConfigurationData CreateDefault(int numMachines = 5)
        {
            var config = new ConfigurationData
            {
                NumberOfMachines = numMachines,
                ConfigurationName = "Default Configuration",
                Notes = "Auto-generated default configuration"
            };

            for (int i = 1; i <= numMachines; i++)
            {
                config.MachineConfigurations.Add(new MachineConfigData
                {
                    MachineId = i,
                    MachineName = $"Machine {i}",
                    ProcessingDistribution = "Exponential",
                    MeanProcessingTime = 3.0
                });
            }

            return config;
        }

        /// <summary>
        /// Creates a balanced line configuration (all machines same speed)
        /// </summary>
        public static ConfigurationData CreateBalancedLine(int numMachines = 5, double processingTime = 3.0)
        {
            var config = CreateDefault(numMachines);
            config.ConfigurationName = "Balanced Line";
            config.Notes = "All machines have identical processing times";
            config.MeanProcessingTime = processingTime;

            foreach (var machine in config.MachineConfigurations)
            {
                machine.MeanProcessingTime = processingTime;
                machine.ProcessingDistribution = "Exponential";
            }

            return config;
        }

        /// <summary>
        /// Creates a bottleneck configuration (one slow machine)
        /// </summary>
        public static ConfigurationData CreateBottleneckLine(int numMachines = 5, int bottleneckPosition = 2)
        {
            var config = CreateDefault(numMachines);
            config.ConfigurationName = "Bottleneck Configuration";
            config.Notes = $"Machine {bottleneckPosition} is slower to create bottleneck";

            for (int i = 0; i < config.MachineConfigurations.Count; i++)
            {
                var machine = config.MachineConfigurations[i];
                if (machine.MachineId == bottleneckPosition)
                {
                    machine.MeanProcessingTime = 6.0; // Twice as slow
                    machine.MachineName = $"Machine {machine.MachineId} (Bottleneck)";
                }
                else
                {
                    machine.MeanProcessingTime = 3.0;
                }
            }

            return config;
        }

        /// <summary>
        /// Creates a configuration with varied distributions
        /// </summary>
        public static ConfigurationData CreateVariedDistributions(int numMachines = 5)
        {
            var config = CreateDefault(numMachines);
            config.ConfigurationName = "Varied Distributions";
            config.Notes = "Each machine uses a different distribution type";

            string[] distributions = { "Exponential", "Normal", "Uniform", "Triangular", "Gamma", "Weibull", "LogNormal" };

            for (int i = 0; i < config.MachineConfigurations.Count; i++)
            {
                var machine = config.MachineConfigurations[i];
                machine.ProcessingDistribution = distributions[i % distributions.Length];
                
                var (param1, param2) = GetDefaultParameters(machine.ProcessingDistribution, 3.0);
                machine.DistributionParam1 = param1;
                machine.DistributionParam2 = param2;
            }

            return config;
        }
    }
}
