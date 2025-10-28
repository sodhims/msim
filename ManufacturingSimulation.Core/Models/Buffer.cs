namespace ManufacturingSimulation.Core.Models
{
    public class Buffer
    {
        public int Capacity { get; }
        public Queue<Part> Parts { get; }
        public int MachineId { get; }  // Which machine this buffer feeds

        public Buffer(int capacity, int machineId)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));

            Capacity = capacity;
            MachineId = machineId;
            Parts = new Queue<Part>();
        }

        public bool IsFull => Parts.Count >= Capacity;
        public bool IsEmpty => Parts.Count == 0;
        public int Count => Parts.Count;
        public double Utilization => (double)Parts.Count / Capacity;

        public bool TryAdd(Part part)
        {
            if (IsFull)
                return false;

            Parts.Enqueue(part);
            part.State = PartState.InBuffer;
            return true;
        }

        public Part? TryRemove()
        {
            if (IsEmpty)
                return null;

            return Parts.Dequeue();
        }

        public Part? Peek()
        {
            if (IsEmpty)
                return null;

            return Parts.Peek();
        }

        public void Clear()
        {
            Parts.Clear();
        }

        public override string ToString()
        {
            return $"Buffer (Machine {MachineId}): {Count}/{Capacity}";
        }
    }
}