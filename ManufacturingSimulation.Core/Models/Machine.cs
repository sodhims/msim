using ManufacturingSimulation.Core.Engine.Rules;

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
        public Part? CurrentPart { get; set; }
        public double ProcessingStartTime { get; set; }
        public double ProcessingEndTime { get; set; }
        public int PartsCompleted { get; set; }
        public IDispatchingRule DispatchingRule { get; set; }

        public Machine(int id, string name, IDispatchingRule? rule = null)
        {
            Id = id;
            Name = name;
            State = MachineState.Idle;
            CurrentPart = null;
            PartsCompleted = 0;
            DispatchingRule = rule ?? new FIFORule();
        }

        public bool IsAvailable() => State == MachineState.Idle;

        public void StartProcessing(Part part, double currentTime, double processingTime)
        {
            if (!IsAvailable())
                throw new InvalidOperationException($"Machine {Name} is not available");
            CurrentPart = part;
            State = MachineState.Busy;
            ProcessingStartTime = currentTime;
            ProcessingEndTime = currentTime + processingTime;
            part.StartTime = currentTime;
        }

        public Part CompleteProcessing(double currentTime)
        {
            if (CurrentPart == null)
                throw new InvalidOperationException($"Machine {Name} has no part to complete");
            var completedPart = CurrentPart;
            completedPart.MoveToNextOperation();
            CurrentPart = null;
            State = MachineState.Idle;
            PartsCompleted++;
            return completedPart;
        }

        public double GetProcessingProgress(double currentTime)
        {
            if (State != MachineState.Busy) return 0;
            double totalTime = ProcessingEndTime - ProcessingStartTime;
            double elapsed = currentTime - ProcessingStartTime;
            return Math.Min(1.0, elapsed / totalTime);
        }
    }
}
