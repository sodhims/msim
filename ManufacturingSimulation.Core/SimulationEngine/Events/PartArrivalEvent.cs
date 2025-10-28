using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.Core.SimulationEngine.Events
{
    public class PartArrivalEvent : SimulationEvent
    {
        public Part Part { get; }

        public PartArrivalEvent(double scheduledTime, Part part) 
            : base(scheduledTime)
        {
            Part = part;
        }

        public override void Execute(SimulationEngine engine)
        {
            engine.HandlePartArrival(Part);
        }

        public override string ToString()
        {
            return $"[t={ScheduledTime:F2}] PartArrival: {Part.Id}";
        }
    }
}