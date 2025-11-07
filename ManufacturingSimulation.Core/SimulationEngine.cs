using ManufacturingSimulation.Core;  // For IDbSimEventLogger interface
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.Engine;
using ManufacturingSimulation.Core.Engine.Events;
using ManufacturingSimulation.Core.Distributions;
using MachineBuffer = ManufacturingSimulation.Core.Models.Buffer;


namespace ManufacturingSimulation.Core
{
    /// <summary>public ISimulationEventLogger
    /// ENHANCED Simulation Engine with setup/operation phase tracking
    /// Now uses routing-based timing with distribution support
    /// </summary>
    public class SimulationEngine : IDisposable
    {
        private readonly EventScheduler _scheduler;
        private readonly Random _random;
        private double _currentTime;
        private int _totalPartsArrived;
        private int _totalPartsCompleted;
        private readonly List<Part> _completedParts;
        private readonly Dictionary<int, double> _machineBusyTime;
        private readonly Dictionary<int, double> _machineSetupTime;  // NEW: Track setup time separately
        private StreamWriter? _debugLog;
        private bool _disposed = false;

        private IDistribution? _processingTimeDistribution;
        private ISimulationEventLogger? _eventLogger;
        public ISimulationEventLogger? EventLogger => _eventLogger;

   
        public List<Machine> Machines { get; }
        public Dictionary<int, MachineBuffer> Buffers { get; }
        public double CurrentTime => _currentTime;
        public bool IsRunning { get; private set; }
        public event EventHandler<SimulationEvent>? EventProcessed;

        public SimulationEngine(int randomSeed = 42, IDistribution? processingDistribution = null)
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
            _machineSetupTime = new Dictionary<int, double>();

            _processingTimeDistribution = processingDistribution ?? new UniformDistribution(2.0, 6.0);

            try
            {
                string debugPath = @"C:\msim\logs\debug.txt";
                Directory.CreateDirectory(@"C:\msim\logs");
                _debugLog = new StreamWriter(debugPath, false);
                _debugLog.AutoFlush = true;
                _debugLog.WriteLine($"=== Simulation Started at {DateTime.Now} ===\n");
            }
            catch
            {
                _debugLog = null;
            }
        }

        public void SetEventLogger(ISimulationEventLogger logger)
        {
            _eventLogger = logger;
        }

        public void SetProcessingDistribution(IDistribution distribution)
        {
            _processingTimeDistribution = distribution;
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
            _eventLogger?.LogPartArrival(arrivalTime, part.Id, part.Id);
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

            _eventLogger?.Flush();
        }

        public void HandlePartArrival(Part part)
        {
            _totalPartsArrived++;
            int firstMachineId = part.GetCurrentMachineId();
            var buffer = Buffers[firstMachineId];

            Debug($"=== {part.Id} ARRIVES at Machine {firstMachineId} buffer ===");

            bool added = buffer.TryAdd(part, _currentTime);
            if (!added)
            {
                Debug($"    REJECTED! Buffer full");
                return;
            }

            Debug($"    Buffer now: {buffer.Count}/{buffer.Capacity}");

            var machine = Machines.First(m => m.Id == firstMachineId);
            _eventLogger?.LogQueueEntry(_currentTime, part.Id, part.Id, machine.Name, buffer.Count);
            TryStartProcessing(machine);
        }

        /// <summary>
        /// NEW: Handle setup completion - transition to processing phase
        /// </summary>
        public void HandleSetupComplete(Machine machine, Part part)
        {
            Debug($"*** {part.Id} SETUP COMPLETE on {machine.Name} ***");

            // Complete setup phase
            machine.CompleteSetup(_currentTime);

            double setupDuration = _currentTime - machine.SetupStartTime;
            TrackMachineSetupTime(machine, machine.SetupStartTime, _currentTime);

            _eventLogger?.LogSetupComplete(_currentTime, part.Id, part.Id, machine.Name, setupDuration);

            // Check if this is a buffer-only operation (no processing needed)
            if (part.CurrentOperationIsBufferOnly)
            {
                Debug($"    Buffer-only operation - skipping processing");
                _eventLogger?.LogBufferPass(_currentTime, part.Id, part.Id, machine.Name);

                // Immediately complete (move to next operation)
                machine.State = MachineState.Idle;
                HandleOperationComplete(machine, part);
                return;
            }

            // Start processing phase
            double cycleTime = part.CurrentCycleTime;
            int batchSize = part.CurrentBatchSize;
            double totalProcessingTime = cycleTime * batchSize;

            Debug($"    Starting processing: {cycleTime:F2} min/part × {batchSize} parts = {totalProcessingTime:F2} min");

            if (batchSize > 1)
            {
                _eventLogger?.LogBatchProcessing(_currentTime, part.Id, part.Id,
                    machine.Name, batchSize, totalProcessingTime);
            }

            machine.StartProcessing(part, _currentTime, totalProcessingTime);
            _eventLogger?.LogProcessingStart(_currentTime, part.Id, part.Id, machine.Name);

            _scheduler.ScheduleEvent(new ProcessingCompleteEvent(
                _currentTime + totalProcessingTime, machine, part));
        }

        public void HandleProcessingComplete(Machine machine, Part part)
        {
            Debug($"*** {part.Id} PROCESSING COMPLETE on {machine.Name} ***");

            double processingTime = _currentTime - machine.ProcessingStartTime;
            TrackMachineBusyTime(machine, machine.ProcessingStartTime, _currentTime);

            _eventLogger?.LogProcessingEnd(_currentTime, part.Id, part.Id, machine.Name, processingTime);

            HandleOperationComplete(machine, part);
        }

        /// <summary>
        /// Handle completion of an operation (setup + processing)
        /// </summary>
        private void HandleOperationComplete(Machine machine, Part part)
        {
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
                    _scheduler.ScheduleEvent(new RetryTransferEvent(_currentTime + 0.5, machine, part, nextMachineId));

                    Debug($"    Scheduled retry at {_currentTime + 0.5:F2}");
                    return;
                }

                Debug($"    SUCCESS! Transferred to Machine {nextMachineId}");

                machine.CurrentPart = null;
                machine.State = MachineState.Idle;
                machine.PartsCompleted++;

                var nextMachine = Machines.First(m => m.Id == nextMachineId);
                _eventLogger?.LogQueueEntry(_currentTime, part.Id, part.Id, nextMachine.Name, nextBuffer.Count);

                TryStartProcessing(nextMachine);
                TryStartProcessing(machine);
            }
            else
            {
                machine.CurrentPart = null;
                machine.State = MachineState.Idle;
                machine.PartsCompleted++;

                part.State = PartState.Completed;
                part.CompletionTime = _currentTime;
                _completedParts.Add(part);
                _totalPartsCompleted++;

                double flowTime = part.CompletionTime - part.ArrivalTime;
                _eventLogger?.LogPartCompletion(_currentTime, part.Id, part.Id, flowTime);

                Debug($"    About to call TryStartProcessing({machine.Name})");
                TryStartProcessing(machine);
                Debug($"    Finished TryStartProcessing({machine.Name})");
            }
        }

        /// <summary>
        /// ENHANCED: Try to start processing - now handles setup phase
        /// </summary>
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

            // Update part's current operation timing
            part.SetCurrentOperationTiming();

            double setupTime = part.CurrentSetupTime;
            double cycleTime = part.CurrentCycleTime;
            bool isBufferOnly = part.CurrentOperationIsBufferOnly;

            Debug($"[TryStart] {part.Id} - Setup: {setupTime:F2} min, Cycle: {cycleTime:F2} min, BufferOnly: {isBufferOnly}");

            // If no setup time or buffer-only, skip setup phase
            if (setupTime <= 0.01 || isBufferOnly)
            {
                Debug($"[TryStart] No setup needed - going directly to processing");

                if (isBufferOnly)
                {
                    // Buffer operation - instant pass through
                    _eventLogger?.LogBufferPass(_currentTime, part.Id, part.Id, machine.Name);
                    HandleOperationComplete(machine, part);
                }
                else
                {
                    // Start processing directly
                    int batchSize = part.CurrentBatchSize;
                    double totalProcessingTime = cycleTime * batchSize;

                    machine.StartProcessing(part, _currentTime, totalProcessingTime);
                    _eventLogger?.LogProcessingStart(_currentTime, part.Id, part.Id, machine.Name);

                    _scheduler.ScheduleEvent(new ProcessingCompleteEvent(
                        _currentTime + totalProcessingTime, machine, part));
                }
            }
            else
            {
                // Start setup phase
                Debug($"[TryStart] Starting setup phase");
                machine.StartSetup(part, _currentTime, setupTime);
                _eventLogger?.LogSetupStart(_currentTime, part.Id, part.Id, machine.Name, setupTime);

                _scheduler.ScheduleEvent(new SetupCompleteEvent(
                    _currentTime + setupTime, machine, part));
            }
        }


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
            _machineSetupTime.Clear();

            foreach (var machine in Machines)
            {
                machine.State = MachineState.Idle;
                machine.CurrentPart = null;
                machine.PartsCompleted = 0;
                machine.IsInSetup = false;
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

        private void TrackMachineSetupTime(Machine machine, double startTime, double endTime)
        {
            if (!_machineSetupTime.ContainsKey(machine.Id))
                _machineSetupTime[machine.Id] = 0;
            _machineSetupTime[machine.Id] += (endTime - startTime);
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

                // Calculate utilization including setup time
                double totalActiveTime = machineStats.TotalBusyTime;
                if (_machineSetupTime.ContainsKey(machine.Id))
                {
                    totalActiveTime += _machineSetupTime[machine.Id];
                }

                machineStats.Utilization = _currentTime > 0 ? (totalActiveTime / _currentTime) * 100 : 0;
                stats.MachineStats[machine.Id] = machineStats;
            }

            return stats;
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_debugLog != null)
                    {
                        _debugLog.Flush();
                        _debugLog.Close();
                        _debugLog.Dispose();
                        _debugLog = null;
                    }
                }
                _disposed = true;
            }
        }

        ~SimulationEngine()
        {
            Dispose(false);
        }
    }
}
