namespace ManufacturingSimulation.Core.Models
{
    public enum PartState
    {
        InStorage,
        InBuffer,
        InSetup,        // NEW: Part is being set up on machine
        Processing,
        Completed
    }

    public class Part
    {
        public string Id { get; }
        public int CurrentOperationIndex { get; set; }
        public List<int> Route { get; }
        public double ArrivalTime { get; set; }
        public double StartTime { get; set; }
        public double CompletionTime { get; set; }
        public int Priority { get; set; }
        public PartState State { get; set; }

        // Dispatching properties
        public double DueDate { get; set; }
        public double EstimatedProcessingTime { get; set; }
        public double TimeInSystem => CurrentTime - ArrivalTime;
        public double BufferEntryTime { get; set; }

        // NEW: Routing-specific timing information
        // Store setup and cycle times for each operation in the route
        public List<OperationTiming> OperationTimings { get; set; }

        // NEW: Track current operation timing
        public double CurrentSetupTime { get; set; }
        public double CurrentCycleTime { get; set; }
        public double SetupStartTime { get; set; }
        public double SetupEndTime { get; set; }
        public int CurrentBatchSize { get; set; }
        public bool CurrentOperationIsBufferOnly { get; set; }

        private static double CurrentTime { get; set; }

        public Part(string id, List<int> route, double arrivalTime = 0, int priority = 0, double dueDate = double.MaxValue)
        {
            Id = id;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            ArrivalTime = arrivalTime;
            CurrentOperationIndex = 0;
            State = PartState.InStorage;
            Priority = priority;
            DueDate = dueDate;
            EstimatedProcessingTime = 4.0;
            BufferEntryTime = arrivalTime;
            OperationTimings = new List<OperationTiming>();
            CurrentBatchSize = 1;
            CurrentOperationIsBufferOnly = false;
        }

        public int GetCurrentMachineId()
        {
            if (CurrentOperationIndex >= Route.Count)
                return -1;

            return Route[CurrentOperationIndex];
        }

        public bool HasMoreOperations()
        {
            return CurrentOperationIndex < Route.Count;
        }

        public void MoveToNextOperation()
        {
            CurrentOperationIndex++;

            // Update current operation timing if available
            if (CurrentOperationIndex < OperationTimings.Count)
            {
                var timing = OperationTimings[CurrentOperationIndex];
                CurrentSetupTime = timing.SetupTime;
                CurrentCycleTime = timing.CycleTime;
                CurrentBatchSize = timing.BatchSize;
                CurrentOperationIsBufferOnly = timing.IsBufferOnly;
            }
        }

        public void SetCurrentOperationTiming()
        {
            if (CurrentOperationIndex < OperationTimings.Count)
            {
                var timing = OperationTimings[CurrentOperationIndex];
                CurrentSetupTime = timing.SetupTime;
                CurrentCycleTime = timing.CycleTime;
                CurrentBatchSize = timing.BatchSize;
                CurrentOperationIsBufferOnly = timing.IsBufferOnly;
            }
        }

        public double GetSlack(double currentTime)
        {
            double remainingOps = Route.Count - CurrentOperationIndex;
            double estimatedRemainingTime = remainingOps * EstimatedProcessingTime;
            return DueDate - currentTime - estimatedRemainingTime;
        }

        public double GetCriticalRatio(double currentTime)
        {
            double remainingOps = Route.Count - CurrentOperationIndex;
            double estimatedRemainingTime = remainingOps * EstimatedProcessingTime;

            if (estimatedRemainingTime == 0) return double.MaxValue;

            return (DueDate - currentTime) / estimatedRemainingTime;
        }

        public override string ToString()
        {
            return $"Part {Id} (Op {CurrentOperationIndex + 1}/{Route.Count})";
        }
    }

    /// <summary>
    /// Stores timing information for a specific operation in a part's route
    /// </summary>
    public class OperationTiming
    {
        public int MachineId { get; set; }
        public double SetupTime { get; set; }
        public double CycleTime { get; set; }
        public int BatchSize { get; set; }
        public bool IsBufferOnly { get; set; }
        public string DistributionType { get; set; }

        public OperationTiming(int machineId, double setupTime, double cycleTime,
            int batchSize = 1, bool isBufferOnly = false, string distributionType = "Constant")
        {
            MachineId = machineId;
            SetupTime = setupTime;
            CycleTime = cycleTime;
            BatchSize = batchSize;
            IsBufferOnly = isBufferOnly;
            DistributionType = distributionType;
        }

        public double GetTotalTime()
        {
            // For buffer-only operations, return 0
            if (IsBufferOnly) return 0;

            // Total time = setup + (cycle time * batch size)
            return SetupTime + (CycleTime * BatchSize);
        }
    }
}
