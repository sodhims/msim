using ManufacturingSimulation.Core.Engine.Rules;

namespace ManufacturingSimulation.Core.Models
{
    public enum MachineState
    {
        Idle,
        Setup,          // NEW: Machine is performing setup
        Busy,           // Processing (after setup complete)
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

        // NEW: Setup tracking
        public double SetupStartTime { get; set; }
        public double SetupEndTime { get; set; }
        public bool IsInSetup { get; set; }

        public Machine(int id, string name, IDispatchingRule? rule = null)
        {
            Id = id;
            Name = name;
            State = MachineState.Idle;
            CurrentPart = null;
            PartsCompleted = 0;
            DispatchingRule = rule ?? new FIFORule();
            IsInSetup = false;
        }

        public bool IsAvailable() => State == MachineState.Idle;

        /// <summary>
        /// Start setup phase for a part
        /// </summary>
        public void StartSetup(Part part, double currentTime, double setupTime)
        {
            if (!IsAvailable())
                throw new InvalidOperationException($"Machine {Name} is not available");

            CurrentPart = part;
            State = MachineState.Setup;
            IsInSetup = true;
            SetupStartTime = currentTime;
            SetupEndTime = currentTime + setupTime;
            part.State = PartState.InSetup;
            part.SetupStartTime = currentTime;
        }

        /// <summary>
        /// Complete setup and start processing
        /// </summary>
        public void CompleteSetup(double currentTime)
        {
            if (State != MachineState.Setup || CurrentPart == null)
                throw new InvalidOperationException($"Machine {Name} is not in setup");

            IsInSetup = false;
            CurrentPart.SetupEndTime = currentTime;
        }

        /// <summary>
        /// Start processing phase (after setup is complete)
        /// </summary>
        public void StartProcessing(Part part, double currentTime, double processingTime)
        {
            // If we're transitioning from setup, we already have the part
            if (State == MachineState.Setup && CurrentPart == part)
            {
                // Just change state and set processing times
                State = MachineState.Busy;
                ProcessingStartTime = currentTime;
                ProcessingEndTime = currentTime + processingTime;
                part.State = PartState.Processing;
                part.StartTime = currentTime;
            }
            else
            {
                // Direct processing (no setup - for buffer operations)
                if (!IsAvailable())
                    throw new InvalidOperationException($"Machine {Name} is not available");

                CurrentPart = part;
                State = MachineState.Busy;
                ProcessingStartTime = currentTime;
                ProcessingEndTime = currentTime + processingTime;
                part.StartTime = currentTime;
                part.State = PartState.Processing;
            }
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
            IsInSetup = false;

            return completedPart;
        }

        public double GetProcessingProgress(double currentTime)
        {
            if (State == MachineState.Setup)
            {
                double totalTime = SetupEndTime - SetupStartTime;
                double elapsed = currentTime - SetupStartTime;
                return Math.Min(1.0, elapsed / totalTime);
            }
            else if (State == MachineState.Busy)
            {
                double totalTime = ProcessingEndTime - ProcessingStartTime;
                double elapsed = currentTime - ProcessingStartTime;
                return Math.Min(1.0, elapsed / totalTime);
            }

            return 0;
        }

        public string GetCurrentPhase()
        {
            return State switch
            {
                MachineState.Setup => "Setup",
                MachineState.Busy => "Processing",
                MachineState.Idle => "Idle",
                MachineState.Blocked => "Blocked",
                MachineState.Starved => "Starved",
                _ => "Unknown"
            };
        }
    }
}
