using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.Core.SimulationEngine.Events
{
    public class ProcessingCompleteEvent : SimulationEvent
    {
        public Machine Machine { get; }
        public Part Part { get; }

        public ProcessingCompleteEvent(double scheduledTime, Machine machine, Part part) 
            : base(scheduledTime)
        {
            Machine = machine;
            Part = part;
        }

        public override void Execute(SimulationEngine engine)
        {
            engine.HandleProcessingComplete(Machine, Part);
        }

        public override string ToString()
        {
            return $"[t={ScheduledTime:F2}] ProcessingComplete: {Part.Id} on {Machine.Name}";
        }
    }
}