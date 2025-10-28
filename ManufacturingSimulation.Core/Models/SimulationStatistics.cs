namespace ManufacturingSimulation.Core.Models
{
    public class SimulationStatistics
    {
        public double CurrentTime { get; set; }
        public int TotalPartsArrived { get; set; }
        public int TotalPartsCompleted { get; set; }
        public int CurrentWIP { get; set; }  // Work in Progress
        public double AverageFlowTime { get; set; }
        public double Throughput { get; set; }  // Parts per time unit
        
        public Dictionary<int, MachineStatistics> MachineStats { get; set; }

        public SimulationStatistics()
        {
            MachineStats = new Dictionary<int, MachineStatistics>();
        }
    }

    public class MachineStatistics
    {
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public int PartsProcessed { get; set; }
        public double TotalBusyTime { get; set; }
        public double Utilization { get; set; }  // Percentage
        public int CurrentBufferCount { get; set; }
        public double AverageBufferCount { get; set; }

        public MachineStatistics(int machineId, string machineName)
        {
            MachineId = machineId;
            MachineName = machineName;
        }
    }
}