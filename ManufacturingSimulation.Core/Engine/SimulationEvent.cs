namespace ManufacturingSimulation.Core.Engine
{
    public abstract class SimulationEvent : IComparable<SimulationEvent>
    {
        public double ScheduledTime { get; set; }
        public string EventType => GetType().Name;

        protected SimulationEvent(double scheduledTime)
        {
            ScheduledTime = scheduledTime;
        }

        public abstract void Execute(SimulationEngine engine);

        public int CompareTo(SimulationEvent? other)
        {
            if (other == null) return 1;
            return ScheduledTime.CompareTo(other.ScheduledTime);
        }

        public override string ToString()
        {
            return $"[t={ScheduledTime:F2}] {EventType}";
        }
    }
}
