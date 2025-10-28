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
        public List<int> Route { get; }  // List of machine IDs to visit
        public double ArrivalTime { get; set; }
        public double StartTime { get; set; }
        public double CompletionTime { get; set; }
        public int Priority { get; set; }
        public PartState State { get; set; }

        public Part(string id, List<int> route, double arrivalTime = 0)
        {
            Id = id;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            ArrivalTime = arrivalTime;
            CurrentOperationIndex = 0;
            State = PartState.InStorage;
            Priority = 0;
        }

        public int GetCurrentMachineId()
        {
            if (CurrentOperationIndex >= Route.Count)
                return -1;  // No more operations
            
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

        public override string ToString()
        {
            return $"Part {Id} (Op {CurrentOperationIndex + 1}/{Route.Count})";
        }
    }
}