namespace ManufacturingSimulation.Core.Models
{
    public enum MachineState
    {
        Idle,
        Busy,
        Blocked,
        Starved
    }

    public class Machine
    {
        public int Id { get; }
        public string Name { get; set; }
        public MachineState State { get; set; }
        public Part? CurrentPart { get; set; }  // Made nullable with ?
        public double ProcessingStartTime { get; set; }
        public double ProcessingEndTime { get; set; }
        public int PartsCompleted { get; set; }

        public Machine(int id, string name)
        {
            Id = id;
            Name = name ?? $"Machine {id}";
            State = MachineState.Idle;
            PartsCompleted = 0;
            CurrentPart = null;  // Explicitly set to null
        }

        public bool IsAvailable()
        {
            return State == MachineState.Idle;
        }

        public void StartProcessing(Part part, double currentTime, double processingTime)
        {
            if (!IsAvailable())
                throw new InvalidOperationException($"Machine {Name} is not available");

            CurrentPart = part;
            State = MachineState.Busy;
            ProcessingStartTime = currentTime;
            ProcessingEndTime = currentTime + processingTime;
            part.State = PartState.Processing;
            part.StartTime = currentTime;
        }

        public Part CompleteProcessing(double currentTime)
        {
            if (State != MachineState.Busy || CurrentPart == null)
                throw new InvalidOperationException($"Machine {Name} is not processing");

            var completedPart = CurrentPart;
            completedPart.MoveToNextOperation();
            
            PartsCompleted++;
            CurrentPart = null;
            State = MachineState.Idle;

            return completedPart;
        }

        public double GetProcessingProgress(double currentTime)
        {
            if (State != MachineState.Busy)
                return 0;

            double totalTime = ProcessingEndTime - ProcessingStartTime;
            double elapsed = currentTime - ProcessingStartTime;
            return Math.Min(100, (elapsed / totalTime) * 100);
        }
    }
}