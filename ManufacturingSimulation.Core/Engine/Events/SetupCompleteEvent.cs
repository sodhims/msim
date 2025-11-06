using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.Core.Engine.Events
{
    /// <summary>
    /// Event fired when setup phase completes and processing can begin
    /// </summary>
    public class SetupCompleteEvent : SimulationEvent
    {
        public Machine Machine { get; }
        public Part Part { get; }

        public SetupCompleteEvent(double scheduledTime, Machine machine, Part part)
            : base(scheduledTime)
        {
            Machine = machine ?? throw new ArgumentNullException(nameof(machine));
            Part = part ?? throw new ArgumentNullException(nameof(part));
        }

        public override void Execute(SimulationEngine engine)
        {
            engine.HandleSetupComplete(Machine, Part);
        }

        public override string ToString()
        {
            return $"SetupComplete({Part.Id} on {Machine.Name} at {ScheduledTime:F2})";
        }
    }
}
