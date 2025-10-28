using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.SimulationEngine.Events;
using MachineBuffer = ManufacturingSimulation.Core.Models.Buffer;

namespace ManufacturingSimulation.Core.SimulationEngine
{
    public class SimulationEngine
    {
        private readonly EventScheduler _scheduler;
        private readonly Random _random;
        private double _currentTime;
        
        public List<Machine> Machines { get; }
        public Dictionary<int, MachineBuffer> Buffers { get; }
        
        public double CurrentTime => _currentTime;
        public bool IsRunning { get; private set; }
        
        public event EventHandler<SimulationEvent>? EventProcessed;

        public SimulationEngine(int randomSeed = 42)
        {
            _scheduler = new EventScheduler();
            _random = new Random(randomSeed);
            _currentTime = 0;
            
            Machines = new List<Machine>();
            Buffers = new Dictionary<int, MachineBuffer>();
        }

        public void AddMachine(Machine machine, int bufferCapacity)
        {
            Machines.Add(machine);
            Buffers[machine.Id] = new MachineBuffer(bufferCapacity, machine.Id);
        }

        public void SchedulePartArrival(Part part, double arrivalTime)
        {
            _scheduler.ScheduleEvent(new PartArrivalEvent(arrivalTime, part));
        }

        public void RunUntil(double endTime)
        {
            IsRunning = true;
            
            while (_scheduler.HasEvents && _currentTime < endTime)
            {
                var nextEvent = _scheduler.GetNextEvent();
                if (nextEvent == null) break;
                
                _currentTime = nextEvent.ScheduledTime;
                
                if (_currentTime > endTime) break;
                
                nextEvent.Execute(this);
                
                EventProcessed?.Invoke(this, nextEvent);
            }
            
            IsRunning = false;
        }

        public void HandlePartArrival(Part part)
        {
            // Part arrives - add to first machine's buffer
            int firstMachineId = part.GetCurrentMachineId();
            var buffer = Buffers[firstMachineId];
            
            bool added = buffer.TryAdd(part);
            
            if (!added)
            {
                Console.WriteLine($"  WARNING: Buffer {firstMachineId} full! Part {part.Id} rejected");
                return;
            }
            
            // Try to start processing if machine is idle
            TryStartProcessing(Machines.First(m => m.Id == firstMachineId));
        }

        public void HandleProcessingComplete(Machine machine, Part part)
        {
            // Complete the operation
            var completedPart = machine.CompleteProcessing(_currentTime);
            
            // Check if part has more operations
            if (completedPart.HasMoreOperations())
            {
                // Move to next machine's buffer
                int nextMachineId = completedPart.GetCurrentMachineId();
                var nextBuffer = Buffers[nextMachineId];
                
                bool added = nextBuffer.TryAdd(completedPart);
                
                if (!added)
                {
                    // Buffer full - machine is blocked!
                    machine.State = MachineState.Blocked;
                    Console.WriteLine($"  Machine {machine.Name} BLOCKED - Buffer {nextMachineId} full");
                    return;
                }
                
                // Try to start processing on next machine
                TryStartProcessing(Machines.First(m => m.Id == nextMachineId));
            }
            else
            {
                // Part is complete!
                completedPart.State = PartState.Completed;
                completedPart.CompletionTime = _currentTime;
            }
            
            // Try to start next part on this machine
            TryStartProcessing(machine);
        }

        private void TryStartProcessing(Machine machine)
        {
            if (!machine.IsAvailable())
                return;
            
            var buffer = Buffers[machine.Id];
            var part = buffer.TryRemove();
            
            if (part == null)
            {
                machine.State = MachineState.Starved;
                return;
            }
            
            // Generate random processing time (between 2 and 6 time units)
            double processingTime = 2.0 + _random.NextDouble() * 4.0;
            
            machine.StartProcessing(part, _currentTime, processingTime);
            
            // Schedule completion event
            _scheduler.ScheduleEvent(
                new ProcessingCompleteEvent(
                    _currentTime + processingTime,
                    machine,
                    part
                )
            );
        }

        public void Reset()
        {
            _scheduler.Clear();
            _currentTime = 0;
            IsRunning = false;
            
            foreach (var machine in Machines)
            {
                machine.State = MachineState.Idle;
                machine.CurrentPart = null;
            }
            
            foreach (var buffer in Buffers.Values)
            {
                buffer.Clear();
            }
        }
    }
}