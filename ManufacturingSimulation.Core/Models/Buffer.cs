using ManufacturingSimulation.Core.Engine.Rules;

namespace ManufacturingSimulation.Core.Models
{
    public class Buffer
    {
        public int Capacity { get; }
        public Queue<Part> Parts { get; }
        public int MachineId { get; }

        public Buffer(int capacity, int machineId)
        {
            if (capacity <= 0)
                throw new ArgumentException("Buffer capacity must be positive", nameof(capacity));
            Capacity = capacity;
            Parts = new Queue<Part>();
            MachineId = machineId;
        }

        public bool IsFull => Parts.Count >= Capacity;
        public bool IsEmpty => Parts.Count == 0;
        public int Count => Parts.Count;
        public double Utilization => (double)Parts.Count / Capacity;

        // BACKWARD COMPATIBLE: overload without currentTime parameter
        public bool TryAdd(Part part)
        {
            return TryAdd(part, 0.0);
        }

        // New version with currentTime tracking
        public bool TryAdd(Part part, double currentTime)
        {
            if (IsFull) return false;
            part.BufferEntryTime = currentTime;
            Parts.Enqueue(part);
            return true;
        }

        public Part? TryRemove()
        {
            if (IsEmpty) return null;
            return Parts.Dequeue();
        }

        public Part? Peek()
        {
            if (IsEmpty) return null;
            return Parts.Peek();
        }

        public Part? SelectAndRemove(IDispatchingRule rule, double currentTime)
        {
            if (IsEmpty) return null;
            var selectedPart = rule.SelectNextPart(Parts, currentTime);
            if (selectedPart == null) return null;
            
            var tempList = new List<Part>();
            Part? result = null;
            while (Parts.Count > 0)
            {
                var part = Parts.Dequeue();
                if (part == selectedPart && result == null)
                {
                    result = part;
                }
                else
                {
                    tempList.Add(part);
                }
            }
            foreach (var part in tempList)
            {
                Parts.Enqueue(part);
            }
            return result;
        }

        public void Clear()
        {
            Parts.Clear();
        }

        public override string ToString()
        {
            return $"Buffer(Machine={MachineId}, Count={Count}/{Capacity}, Utilization={Utilization:P0})";
        }
    }
}
