using ManufacturingSimulation.Core.Models;

namespace ManufacturingSimulation.Core.Engine.Events
{
    public class RetryTransferEvent : SimulationEvent
    {
        public Machine BlockedMachine { get; }
        public Part Part { get; }
        public int TargetMachineId { get; }

        public RetryTransferEvent(double scheduledTime, Machine blockedMachine, Part part, int targetMachineId) : base(scheduledTime)
        {
            BlockedMachine = blockedMachine;
            Part = part;
            TargetMachineId = targetMachineId;
        }

        public override void Execute(SimulationEngine engine)
        {
            engine.HandleRetryTransfer(BlockedMachine, Part, TargetMachineId);
        }

        public override string ToString()
        {
            return $"[t={ScheduledTime:F2}] RetryTransfer: {Part.Id} from {BlockedMachine.Name} to Machine {TargetMachineId}";
        }
    }
}
