using ManufacturingSimulation.Core.Logging;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Events;
using MachineBuffer = ManufacturingSimulation.Core.Models.Buffer;

namespace ManufacturingSimulation.Core
{
    public class SimulationEngine
    {
        private readonly EventScheduler _scheduler;
        private readonly Random _random;
        private double _currentTime;
        private int _totalPartsArrived;
        private int _totalPartsCompleted;
        private readonly List<Part> _completedParts;
        private readonly Dictionary<int, double> _machineBusyTime;

        public SimulationLogger Logger { get; }
        public List<Machine> Machines { get; }
        public Dictionary<int, MachineBuffer> Buffers { get; }
        public double CurrentTime => _currentTime;
        public bool IsRunning { get; private set; }
        public event EventHandler<SimulationEvent>? EventProcessed;

        private StreamWriter _debugLog;

        //public SimulationEngine(int randomSeed = 42)
        //{
        //    _scheduler = new EventScheduler();
        //    _random = new Random(randomSeed);
        //    _currentTime = 0;
        //    Machines = new List<Machine>();
        //    Buffers = new Dictionary<int, MachineBuffer>();
        //    _totalPartsArrived = 0;
        //    _totalPartsCompleted = 0;
        //    _completedParts = new List<Part>();
        //    _machineBusyTime = new Dictionary<int, double>();
        //    Logger = new SimulationLogger();
        //}
        public SimulationEngine(int randomSeed = 42)
        {
            _scheduler = new EventScheduler();
            _random = new Random(randomSeed);
            _currentTime = 0;
            Machines = new List<Machine>();
            Buffers = new Dictionary<int, MachineBuffer>();
            _totalPartsArrived = 0;
            _totalPartsCompleted = 0;
            _completedParts = new List<Part>();
            _machineBusyTime = new Dictionary<int, double>();
            Logger = new SimulationLogger();

            // Create debug log file
            string debugPath = @"C:\msim\logs\debug.txt";
            Directory.CreateDirectory(@"C:\msim\logs");
            _debugLog = new StreamWriter(debugPath, false); // false = overwrite
            _debugLog.AutoFlush = true;
            _debugLog.WriteLine($"=== Simulation Started at {DateTime.Now} ===\n");
        }

        private void Debug(string message)
        {
            _debugLog?.WriteLine($"[{_currentTime:F2}] {message}");
        }

        public void AddMachine(Machine machine, int bufferCapacity)
        {
            Machines.Add(machine);
            Buffers[machine.Id] = new MachineBuffer(bufferCapacity, machine.Id);
        }

        public void SchedulePartArrival(Part part, double arrivalTime)
        {
            Logger.LogPartCreated(part);
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

        //public void HandlePartArrival(Part part)
        //{
        //    _totalPartsArrived++;
        //    int firstMachineId = part.GetCurrentMachineId();
        //    var buffer = Buffers[firstMachineId];
        //    Logger.LogPartArrival(_currentTime, part, firstMachineId);
        //    bool added = buffer.TryAdd(part, _currentTime);
        //    if (!added) return;
        //    var machine = Machines.First(m => m.Id == firstMachineId);
        //    TryStartProcessing(machine);
        //}

        //public void HandleProcessingComplete(Machine machine, Part part)
        //{
        //    TrackMachineBusyTime(machine, machine.ProcessingStartTime, _currentTime);
        //    Logger.LogPartProcessingComplete(_currentTime, machine, part);

        //    // Move to next operation
        //    part.MoveToNextOperation();

        //    if (part.HasMoreOperations())
        //    {
        //        int nextMachineId = part.GetCurrentMachineId();
        //        var nextBuffer = Buffers[nextMachineId];
        //        bool added = nextBuffer.TryAdd(part, _currentTime);

        //        if (!added)
        //        {
        //            // BLOCKED - don't clear machine, don't try to start
        //            machine.State = MachineState.Blocked;
        //            machine.CurrentPart = part;
        //            Logger.LogMachineBlocked(_currentTime, machine, part, nextMachineId);
        //            _scheduler.ScheduleEvent(new RetryTransferEvent(_currentTime + 0.5, machine, part, nextMachineId));
        //            return;  // EXIT - don't do anything else!
        //        }

        //        // SUCCESS - transfer complete
        //        Logger.LogPartTransfer(_currentTime, part, machine.Id, nextMachineId);
        //        machine.CurrentPart = null;
        //        machine.State = MachineState.Idle;
        //        machine.PartsCompleted++;

        //        // Try to start next machine
        //        TryStartProcessing(Machines.First(m => m.Id == nextMachineId));

        //        // Try to start THIS machine with next part
        //        TryStartProcessing(machine);  // ? MOVE IT HERE
        //    }
        //    else
        //    {
        //        // Part completely done
        //        machine.CurrentPart = null;
        //        machine.State = MachineState.Idle;
        //        machine.PartsCompleted++;

        //        part.State = PartState.Completed;
        //        part.CompletionTime = _currentTime;
        //        _completedParts.Add(part);
        //        _totalPartsCompleted++;

        //        double flowTime = part.CompletionTime - part.ArrivalTime;
        //        Logger.LogPartCompleted(_currentTime, part, flowTime);

        //        // Try to start THIS machine with next part
        //        TryStartProcessing(machine);  // ? MOVE IT HERE TOO
        //    }

        //    // ? REMOVE the TryStartProcessing call from here!
        //}
        public void HandleProcessingComplete(Machine machine, Part part)
        {
            Debug($"*** {part.Id} COMPLETED on {machine.Name} ***");

            TrackMachineBusyTime(machine, machine.ProcessingStartTime, _currentTime);
            Logger.LogPartProcessingComplete(_currentTime, machine, part);

            part.MoveToNextOperation();
            Debug($"    Moved to operation {part.CurrentOperationIndex}");

            if (part.HasMoreOperations())
            {
                int nextMachineId = part.GetCurrentMachineId();
                var nextBuffer = Buffers[nextMachineId];

                Debug($"    Next: Machine {nextMachineId}, Buffer: {nextBuffer.Count}/{nextBuffer.Capacity}");

                bool added = nextBuffer.TryAdd(part, _currentTime);

                if (!added)
                {
                    Debug($"    *** BLOCKED! {machine.Name} keeping {part.Id} ***");

                    machine.State = MachineState.Blocked;
                    machine.CurrentPart = part;
                    Logger.LogMachineBlocked(_currentTime, machine, part, nextMachineId);
                    _scheduler.ScheduleEvent(new RetryTransferEvent(_currentTime + 0.5, machine, part, nextMachineId));

                    Debug($"    Scheduled retry at {_currentTime + 0.5:F2}");
                    return;
                }

                Debug($"    SUCCESS! Transferred to Machine {nextMachineId}");

                Logger.LogPartTransfer(_currentTime, part, machine.Id, nextMachineId);
                machine.CurrentPart = null;
                machine.State = MachineState.Idle;
                machine.PartsCompleted++;

                TryStartProcessing(Machines.First(m => m.Id == nextMachineId));
                TryStartProcessing(machine);
            }
            else
            {
                Debug($"    ALL OPERATIONS COMPLETE!");

                machine.CurrentPart = null;
                machine.State = MachineState.Idle;
                machine.PartsCompleted++;

                part.State = PartState.Completed;
                part.CompletionTime = _currentTime;
                _completedParts.Add(part);
                _totalPartsCompleted++;

                double flowTime = part.CompletionTime - part.ArrivalTime;
                Logger.LogPartCompleted(_currentTime, part, flowTime);

                // At the very end, after everything:
                Debug($"    About to call TryStartProcessing({machine.Name})");
                TryStartProcessing(machine);
                Debug($"    Finished TryStartProcessing({machine.Name})");
            }

        }

        public void HandlePartArrival(Part part)
        {
            _totalPartsArrived++;
            int firstMachineId = part.GetCurrentMachineId();
            var buffer = Buffers[firstMachineId];

            Debug($"=== {part.Id} ARRIVES at Machine {firstMachineId} buffer ===");

            Logger.LogPartArrival(_currentTime, part, firstMachineId);
            bool added = buffer.TryAdd(part, _currentTime);
            if (!added)
            {
                Debug($"    REJECTED! Buffer full");
                return;
            }

            Debug($"    Buffer now: {buffer.Count}/{buffer.Capacity}");

            var machine = Machines.First(m => m.Id == firstMachineId);
            TryStartProcessing(machine);
        }
        private void TryStartProcessing(Machine machine)
        {
            Debug($"[TryStart] {machine.Name} - State: {machine.State}, Buffer: {Buffers[machine.Id].Count}");

            if (!machine.IsAvailable())
            {
                Debug($"[TryStart] {machine.Name} NOT available (state={machine.State})");
                return;
            }
            var buffer = Buffers[machine.Id];
            var part = buffer.SelectAndRemove(machine.DispatchingRule, _currentTime);
            if (part == null)
            {
                Debug($"[TryStart] {machine.Name} buffer empty, staying Idle");
                return;
            }
            double processingTime = 2.0 + _random.NextDouble() * 4.0;
            part.EstimatedProcessingTime = processingTime;
            machine.StartProcessing(part, _currentTime, processingTime);
            Logger.LogPartProcessingStart(_currentTime, machine, part, processingTime);
            _scheduler.ScheduleEvent(new ProcessingCompleteEvent(_currentTime + processingTime, machine, part));
        }

        //public void Reset()
        //{
        //    _scheduler.Clear();
        //    _currentTime = 0;
        //    IsRunning = false;
        //    _totalPartsArrived = 0;
        //    _totalPartsCompleted = 0;
        //    _completedParts.Clear();
        //    _machineBusyTime.Clear();
        //    Logger.Clear();
        //    foreach (var machine in Machines)
        //    {
        //        machine.State = MachineState.Idle;
        //        machine.CurrentPart = null;
        //    }
        //    foreach (var buffer in Buffers.Values)
        //    {
        //        buffer.Clear();
        //    }
        //}

        public void Reset()
        {
            _debugLog?.WriteLine("\n=== RESET CALLED ===\n");

            _scheduler.Clear();
            _currentTime = 0;
            IsRunning = false;
            _totalPartsArrived = 0;
            _totalPartsCompleted = 0;
            _completedParts.Clear();
            _machineBusyTime.Clear();
            Logger.Clear();

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
        private void TrackMachineBusyTime(Machine machine, double startTime, double endTime)
        {
            if (!_machineBusyTime.ContainsKey(machine.Id))
                _machineBusyTime[machine.Id] = 0;
            _machineBusyTime[machine.Id] += (endTime - startTime);
        }

        public SimulationStatistics GetStatistics()
        {
            var stats = new SimulationStatistics
            {
                CurrentTime = _currentTime,
                TotalPartsArrived = _totalPartsArrived,
                TotalPartsCompleted = _totalPartsCompleted,
                CurrentWIP = _totalPartsArrived - _totalPartsCompleted,
                Throughput = _currentTime > 0 ? _totalPartsCompleted / _currentTime : 0
            };
            if (_completedParts.Count > 0)
            {
                stats.AverageFlowTime = _completedParts.Average(p => p.CompletionTime - p.ArrivalTime);
            }
            foreach (var machine in Machines)
            {
                var machineStats = new MachineStatistics(machine.Id, machine.Name)
                {
                    PartsProcessed = machine.PartsCompleted,
                    CurrentBufferCount = Buffers[machine.Id].Count,
                    TotalBusyTime = _machineBusyTime.ContainsKey(machine.Id) ? _machineBusyTime[machine.Id] : 0
                };
                machineStats.Utilization = _currentTime > 0 ? (machineStats.TotalBusyTime / _currentTime) * 100 : 0;
                stats.MachineStats[machine.Id] = machineStats;
            }
            return stats;
        }

        //public void HandleRetryTransfer(Machine machine, Part part, int targetMachineId)
        //{
        //    if (machine.State != MachineState.Blocked) return;

        //    var targetBuffer = Buffers[targetMachineId];
        //    bool added = targetBuffer.TryAdd(part, _currentTime);

        //    if (added)
        //    {
        //        // SUCCESS - the blocked part finally transferred!
        //        Logger.LogPartTransfer(_currentTime, part, machine.Id, targetMachineId);  // ? ADD THIS!

        //        machine.CurrentPart = null;
        //        machine.State = MachineState.Idle;
        //        machine.PartsCompleted++;  // ? ADD THIS! (part completed processing before blocking)

        //        TryStartProcessing(machine);
        //        TryStartProcessing(Machines.First(m => m.Id == targetMachineId));
        //    }
        //    else
        //    {
        //        _scheduler.ScheduleEvent(new RetryTransferEvent(_currentTime + 0.5, machine, part, targetMachineId));
        //    }
        //}
        public void HandleRetryTransfer(Machine machine, Part part, int targetMachineId)
        {
            Debug($">>> RETRY: {part.Id} from {machine.Name} to Machine {targetMachineId}");
            Debug($"    Machine state: {machine.State}");

            if (machine.State != MachineState.Blocked)
            {
                Debug($"    NOT BLOCKED - Skipping");
                return;
            }

            var targetBuffer = Buffers[targetMachineId];
            Debug($"    Target buffer: {targetBuffer.Count}/{targetBuffer.Capacity}");

            bool added = targetBuffer.TryAdd(part, _currentTime);

            if (added)
            {
                Debug($"    *** UNBLOCKED! Transfer succeeded! ***");

                Logger.LogPartTransfer(_currentTime, part, machine.Id, targetMachineId);

                machine.CurrentPart = null;
                machine.State = MachineState.Idle;
                machine.PartsCompleted++;

                TryStartProcessing(machine);
                TryStartProcessing(Machines.First(m => m.Id == targetMachineId));
            }
            else
            {
                Debug($"    Still full - retry at {_currentTime + 0.5:F2}");
                _scheduler.ScheduleEvent(new RetryTransferEvent(_currentTime + 0.5, machine, part, targetMachineId));
            }
        }

    }
}
