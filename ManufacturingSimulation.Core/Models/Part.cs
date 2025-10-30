namespace ManufacturingSimulation.Core.Models
{
    public enum PartState
    {
        InStorage,
        InBuffer,
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
        public int Priority { get; set; }  // Already exists
        public PartState State { get; set; }

        // NEW PROPERTIES FOR DISPATCHING RULES:
        public double DueDate { get; set; }
        public double EstimatedProcessingTime { get; set; }
        public double TimeInSystem => CurrentTime - ArrivalTime;  // We'll pass current time

        // Track when part entered current buffer
        public double BufferEntryTime { get; set; }

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
            BufferEntryTime = arrivalTime;  // Initialize this too
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
        }

        public double GetSlack(double currentTime)
        {
            // Slack = Due Date - Current Time - Remaining Processing Time
            double remainingOps = Route.Count - CurrentOperationIndex;
            double estimatedRemainingTime = remainingOps * EstimatedProcessingTime;
            return DueDate - currentTime - estimatedRemainingTime;
        }

        public double GetCriticalRatio(double currentTime)
        {
            // CR = (Due Date - Current Time) / Remaining Processing Time
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
}