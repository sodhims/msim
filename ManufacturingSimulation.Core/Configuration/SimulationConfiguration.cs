using ManufacturingSimulation.Core.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManufacturingSimulation.Core.Configuration
{
    /// <summary>
    /// Configuration for a single machine in the simulation
    /// </summary>
    public class MachineConfiguration
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BufferCapacity { get; set; }
        public string DispatchingRule { get; set; }
        public int Quantity { get; set; }  // For grouping similar machines

        public MachineConfiguration()
        {
            Id = 1;
            Name = "Machine";
            BufferCapacity = 10;
            DispatchingRule = "FIFO";
            Quantity = 1;
        }

        public MachineConfiguration(int id, string name, int bufferCapacity = 10, string rule = "FIFO", int quantity = 1)
        {
            Id = id;
            Name = name;
            BufferCapacity = bufferCapacity;
            DispatchingRule = rule;
            Quantity = quantity;
        }

        public MachineConfiguration Clone()
        {
            return new MachineConfiguration
            {
                Id = this.Id,
                Name = this.Name,
                BufferCapacity = this.BufferCapacity,
                DispatchingRule = this.DispatchingRule,
                Quantity = this.Quantity
            };
        }
    }

    /// <summary>
    /// Complete simulation configuration
    /// </summary>
    public class SimulationConfiguration
    {
        public List<MachineConfiguration> Machines { get; set; }
        public double RunLength { get; set; }
        public int RandomSeed { get; set; }
        public int NumberOfParts { get; set; }
        
        // Arrival distribution
        public string ArrivalDistributionType { get; set; }
        public List<double> ArrivalDistributionParams { get; set; }
        
        // Processing time distribution
        public string ProcessingDistributionType { get; set; }
        public List<double> ProcessingDistributionParams { get; set; }
        
        // Part routing configuration
        public bool UseStandardRoute { get; set; }
        public List<int> StandardRoute { get; set; }

        public SimulationConfiguration()
        {
            Machines = new List<MachineConfiguration>();
            RunLength = 1000.0;
            RandomSeed = 42;
            NumberOfParts = 20;
            
            ArrivalDistributionType = "Exponential";
            ArrivalDistributionParams = new List<double> { 0.5 };  // rate = 0.5, mean = 2.0
            
            ProcessingDistributionType = "Uniform";
            ProcessingDistributionParams = new List<double> { 2.0, 6.0 };  // min, max
            
            UseStandardRoute = true;
            StandardRoute = new List<int> { 1, 2, 3, 4 };
            
            // Default machines
            Machines.Add(new MachineConfiguration(1, "Drill Press", 10, "FIFO", 1));
            Machines.Add(new MachineConfiguration(2, "Lathe", 10, "FIFO", 1));
            Machines.Add(new MachineConfiguration(3, "Mill", 10, "FIFO", 1));
            Machines.Add(new MachineConfiguration(4, "Grinder", 10, "FIFO", 1));
        }

        public IDistribution GetArrivalDistribution()
        {
            return DistributionManager.CreateDistribution(
                ArrivalDistributionType, 
                ArrivalDistributionParams.ToArray());
        }

        public IDistribution GetProcessingDistribution()
        {
            return DistributionManager.CreateDistribution(
                ProcessingDistributionType, 
                ProcessingDistributionParams.ToArray());
        }

        /// <summary>
        /// Expand machines based on quantity (e.g., "2 Drills" becomes two separate drill machines)
        /// </summary>
        public List<MachineConfiguration> GetExpandedMachines()
        {
            var expanded = new List<MachineConfiguration>();
            int currentId = 1;

            foreach (var machineConfig in Machines)
            {
                for (int i = 0; i < machineConfig.Quantity; i++)
                {
                    var clone = machineConfig.Clone();
                    clone.Id = currentId++;
                    
                    // If multiple instances, append number to name
                    if (machineConfig.Quantity > 1)
                    {
                        clone.Name = $"{machineConfig.Name} {i + 1}";
                    }
                    
                    expanded.Add(clone);
                }
            }

            return expanded;
        }

        public SimulationConfiguration Clone()
        {
            return new SimulationConfiguration
            {
                Machines = Machines.Select(m => m.Clone()).ToList(),
                RunLength = RunLength,
                RandomSeed = RandomSeed,
                NumberOfParts = NumberOfParts,
                ArrivalDistributionType = ArrivalDistributionType,
                ArrivalDistributionParams = new List<double>(ArrivalDistributionParams),
                ProcessingDistributionType = ProcessingDistributionType,
                ProcessingDistributionParams = new List<double>(ProcessingDistributionParams),
                UseStandardRoute = UseStandardRoute,
                StandardRoute = new List<int>(StandardRoute)
            };
        }
    }
}
