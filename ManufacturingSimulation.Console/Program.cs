using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Engine;  // Add this line
using PartBuffer = ManufacturingSimulation.Core.Models.Buffer;

namespace ManufacturingSimulation.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("=== Manufacturing Simulation Console Test ===\n");

            // Test 1: Basic Part Creation and Routing
            TestPartCreation();
            
            // Test 2: Machine Processing
            TestMachineProcessing();
            
            // Test 3: Buffer Operations
            TestBufferOperations();
            
            // Test 4: Simple Flow Simulation
            TestSimpleFlow();

            System.Console.WriteLine("\n=== All Tests Complete ===");
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }

        static void TestPartCreation()
        {
            System.Console.WriteLine("--- Test 1: Part Creation ---");
            
            var route = new List<int> { 1, 2, 3 };
            var part = new Part("P001", route, arrivalTime: 0);
            
            System.Console.WriteLine($"Created: {part}");
            System.Console.WriteLine($"  Current Machine: {part.GetCurrentMachineId()}");
            System.Console.WriteLine($"  Has More Operations: {part.HasMoreOperations()}");
            System.Console.WriteLine($"  State: {part.State}");
            System.Console.WriteLine();
        }

        static void TestMachineProcessing()
        {
            System.Console.WriteLine("--- Test 2: Machine Processing ---");
            
            var machine = new Machine(1, "Drill Press");
            var part = new Part("P002", new List<int> { 1, 2 });
            
            System.Console.WriteLine($"Machine: {machine.Name} - State: {machine.State}");
            System.Console.WriteLine($"Part: {part.Id} - State: {part.State}");
            
            // Start processing
            double currentTime = 10.0;
            double processingTime = 5.0;
            machine.StartProcessing(part, currentTime, processingTime);
            
            System.Console.WriteLine($"\nAfter StartProcessing:");
            System.Console.WriteLine($"  Machine State: {machine.State}");
            System.Console.WriteLine($"  Part State: {part.State}");
            System.Console.WriteLine($"  Processing will complete at time: {machine.ProcessingEndTime}");
            
            // Simulate progress
            System.Console.WriteLine($"\nProgress at time 12.5: {machine.GetProcessingProgress(12.5):F1}%");
            System.Console.WriteLine($"Progress at time 15.0: {machine.GetProcessingProgress(15.0):F1}%");
            
            // Complete processing
            var completedPart = machine.CompleteProcessing(15.0);
            
            System.Console.WriteLine($"\nAfter CompleteProcessing:");
            System.Console.WriteLine($"  Machine State: {machine.State}");
            System.Console.WriteLine($"  Part Current Operation: {completedPart.CurrentOperationIndex}");
            System.Console.WriteLine($"  Parts Completed by Machine: {machine.PartsCompleted}");
            System.Console.WriteLine();
        }

        static void TestBufferOperations()
        {
            System.Console.WriteLine("--- Test 3: Buffer Operations ---");
            
            var buffer = new PartBuffer(capacity: 3, machineId: 2);
            System.Console.WriteLine($"Buffer created: {buffer}");
            
            // Add parts
            var part1 = new Part("P003", new List<int> { 1, 2 });
            var part2 = new Part("P004", new List<int> { 1, 2 });
            var part3 = new Part("P005", new List<int> { 1, 2 });
            var part4 = new Part("P006", new List<int> { 1, 2 });
            
            System.Console.WriteLine($"\nAdding P003: {buffer.TryAdd(part1)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Adding P004: {buffer.TryAdd(part2)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Adding P005: {buffer.TryAdd(part3)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Buffer Full: {buffer.IsFull}");
            System.Console.WriteLine($"Adding P006: {buffer.TryAdd(part4)} - Buffer: {buffer.Count}/{buffer.Capacity} (should fail)");
            
            // Remove parts (FIFO)
            System.Console.WriteLine($"\nRemoving parts (FIFO order):");
            var removed1 = buffer.TryRemove();
            System.Console.WriteLine($"  Removed: {removed1?.Id} - Buffer: {buffer.Count}/{buffer.Capacity}");
            
            var removed2 = buffer.TryRemove();
            System.Console.WriteLine($"  Removed: {removed2?.Id} - Buffer: {buffer.Count}/{buffer.Capacity}");
            
            System.Console.WriteLine($"\nBuffer Utilization: {buffer.Utilization:P1}");
            System.Console.WriteLine();
        }

        static void TestSimpleFlow()
        {
            System.Console.WriteLine("--- Test 4: Multi-Part Flow Simulation ---");
            System.Console.WriteLine("Simulating: Multiple parts with different routes through 4 machines\n");
            
            // Setup - 4 machines with their input buffers
            var machine1 = new Machine(1, "Drill Press");
            var machine2 = new Machine(2, "Lathe");
            var machine3 = new Machine(3, "Mill");
            var machine4 = new Machine(4, "Grinder");
            
            var buffer1 = new PartBuffer(3, 1);
            var buffer2 = new PartBuffer(3, 2);
            var buffer3 = new PartBuffer(3, 3);
            var buffer4 = new PartBuffer(3, 4);
            
            // Create parts with different routes
            var parts = new List<Part>
            {
                new Part("P101", new List<int> { 1, 2, 3 }, arrivalTime: 0.0),
                new Part("P102", new List<int> { 2, 4, 1 }, arrivalTime: 2.0),
                new Part("P103", new List<int> { 1, 3, 2, 4 }, arrivalTime: 3.0),
                new Part("P104", new List<int> { 4, 3, 1 }, arrivalTime: 5.0),
                new Part("P105", new List<int> { 2, 1, 4, 3 }, arrivalTime: 7.0)
            };
            
            System.Console.WriteLine("=== Parts to Process ===");
            foreach (var part in parts)
            {
                var routeStr = string.Join(" → ", part.Route.Select(m => $"M{m}"));
                System.Console.WriteLine($"{part.Id}: {routeStr} (Arrives at t={part.ArrivalTime})");
            }
            System.Console.WriteLine();
            
            // Helper function to get buffer for a machine
            PartBuffer GetBuffer(int machineId)
            {
                return machineId switch
                {
                    1 => buffer1,
                    2 => buffer2,
                    3 => buffer3,
                    4 => buffer4,
                    _ => throw new ArgumentException($"Invalid machine ID: {machineId}")
                };
            }
            
            // Helper function to get machine
            Machine GetMachine(int machineId)
            {
                return machineId switch
                {
                    1 => machine1,
                    2 => machine2,
                    3 => machine3,
                    4 => machine4,
                    _ => throw new ArgumentException($"Invalid machine ID: {machineId}")
                };
            }
            
            double currentTime = 0.0;
            var completedParts = new List<Part>();
            
            System.Console.WriteLine("=== Simulation Events ===\n");
            
            // Simulate Part P101: [1, 2, 3]
            System.Console.WriteLine($"[t={currentTime:F1}] Part P101 arrives");
            var p101 = parts[0];
            var nextMachine = p101.GetCurrentMachineId();
            GetBuffer(nextMachine).TryAdd(p101);
            System.Console.WriteLine($"  → Added to Buffer {nextMachine} ({GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity})");
            
            // Machine 1 processes P101
            currentTime = 0.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 1 picks up P101");
            var partToProcess = GetBuffer(1).TryRemove();
            GetMachine(1).StartProcessing(partToProcess!, currentTime, 4.0);
            System.Console.WriteLine($"  → Processing on M1 (will finish at t={GetMachine(1).ProcessingEndTime})");
            
            // Part P102 arrives
            currentTime = 2.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Part P102 arrives");
            var p102 = parts[1];
            nextMachine = p102.GetCurrentMachineId();
            GetBuffer(nextMachine).TryAdd(p102);
            System.Console.WriteLine($"  → Added to Buffer {nextMachine} ({GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity})");
            
            // Machine 2 processes P102
            currentTime = 2.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 2 picks up P102");
            partToProcess = GetBuffer(2).TryRemove();
            GetMachine(2).StartProcessing(partToProcess!, currentTime, 3.5);
            System.Console.WriteLine($"  → Processing on M2 (will finish at t={GetMachine(2).ProcessingEndTime})");
            
            // Part P103 arrives
            currentTime = 3.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Part P103 arrives");
            var p103 = parts[2];
            nextMachine = p103.GetCurrentMachineId();
            GetBuffer(nextMachine).TryAdd(p103);
            System.Console.WriteLine($"  → Added to Buffer {nextMachine} ({GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity})");
            System.Console.WriteLine($"  → Buffer 1 now has parts waiting (Machine 1 is busy!)");
            
            // Machine 1 completes P101
            currentTime = 4.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 1 completes P101 (Op 1/{p101.Route.Count})");
            var completed = GetMachine(1).CompleteProcessing(currentTime);
            System.Console.WriteLine($"  → M1 Stats: {GetMachine(1).PartsCompleted} parts completed");
            
            // P101 moves to next machine (M2)
            nextMachine = completed.GetCurrentMachineId();
            System.Console.WriteLine($"  → P101 moves to Buffer {nextMachine}");
            bool added = GetBuffer(nextMachine).TryAdd(completed);
            System.Console.WriteLine($"  → Buffer {nextMachine}: {GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity} (Added: {added})");
            
            // Machine 1 picks up P103 from buffer
            currentTime = 5.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 1 picks up P103 from buffer");
            partToProcess = GetBuffer(1).TryRemove();
            GetMachine(1).StartProcessing(partToProcess!, currentTime, 3.0);
            System.Console.WriteLine($"  → Processing on M1 (will finish at t={GetMachine(1).ProcessingEndTime})");
            
            // Part P104 arrives
            System.Console.WriteLine($"\n[t={currentTime:F1}] Part P104 arrives");
            var p104 = parts[3];
            nextMachine = p104.GetCurrentMachineId();
            GetBuffer(nextMachine).TryAdd(p104);
            System.Console.WriteLine($"  → Added to Buffer {nextMachine} ({GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity})");
            
            // Machine 2 completes P102
            currentTime = 6.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 2 completes P102 (Op 1/{p102.Route.Count})");
            completed = GetMachine(2).CompleteProcessing(currentTime);
            nextMachine = completed.GetCurrentMachineId();
            System.Console.WriteLine($"  → P102 moves to Buffer {nextMachine}");
            GetBuffer(nextMachine).TryAdd(completed);
            
            // Machine 2 picks up P101
            currentTime = 6.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 2 picks up P101 from buffer");
            partToProcess = GetBuffer(2).TryRemove();
            GetMachine(2).StartProcessing(partToProcess!, currentTime, 4.0);
            System.Console.WriteLine($"  → Processing on M2 (will finish at t={GetMachine(2).ProcessingEndTime})");
            
            // Part P105 arrives
            currentTime = 7.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Part P105 arrives");
            var p105 = parts[4];
            nextMachine = p105.GetCurrentMachineId();
            GetBuffer(nextMachine).TryAdd(p105);
            System.Console.WriteLine($"  → Added to Buffer {nextMachine} ({GetBuffer(nextMachine).Count}/{GetBuffer(nextMachine).Capacity})");
            
            // Machine 1 completes P103
            currentTime = 8.0;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 1 completes P103 (Op 1/{p103.Route.Count})");
            completed = GetMachine(1).CompleteProcessing(currentTime);
            nextMachine = completed.GetCurrentMachineId();
            System.Console.WriteLine($"  → P103 moves to Buffer {nextMachine}");
            GetBuffer(nextMachine).TryAdd(completed);
            
            // Machine 4 picks up P102
            currentTime = 8.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 4 picks up P102 from buffer");
            partToProcess = GetBuffer(4).TryRemove();
            GetMachine(4).StartProcessing(partToProcess!, currentTime, 3.0);
            System.Console.WriteLine($"  → Processing on M4 (will finish at t={GetMachine(4).ProcessingEndTime})");
            
            // Machine 2 completes P101
            currentTime = 10.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 2 completes P101 (Op 2/{p101.Route.Count})");
            completed = GetMachine(2).CompleteProcessing(currentTime);
            nextMachine = completed.GetCurrentMachineId();
            System.Console.WriteLine($"  → P101 moves to Buffer {nextMachine}");
            GetBuffer(nextMachine).TryAdd(completed);
            
            // Machine 4 completes P102
            currentTime = 11.5;
            System.Console.WriteLine($"\n[t={currentTime:F1}] Machine 4 completes P102 (Op 2/{p102.Route.Count})");
            completed = GetMachine(4).CompleteProcessing(currentTime);
            nextMachine = completed.GetCurrentMachineId();
            System.Console.WriteLine($"  → P102 moves to Buffer {nextMachine}");
            GetBuffer(nextMachine).TryAdd(completed);
            
            // Summary of system state
            currentTime = 12.0;
            System.Console.WriteLine($"\n=== System State at t={currentTime} ===");
            System.Console.WriteLine($"Machine 1 ({machine1.Name}): {machine1.State} - Parts Completed: {machine1.PartsCompleted}");
            System.Console.WriteLine($"  Buffer 1: {buffer1.Count}/{buffer1.Capacity}");
            
            System.Console.WriteLine($"Machine 2 ({machine2.Name}): {machine2.State} - Parts Completed: {machine2.PartsCompleted}");
            System.Console.WriteLine($"  Buffer 2: {buffer2.Count}/{buffer2.Capacity}");
            
            System.Console.WriteLine($"Machine 3 ({machine3.Name}): {machine3.State} - Parts Completed: {machine3.PartsCompleted}");
            System.Console.WriteLine($"  Buffer 3: {buffer3.Count}/{buffer3.Capacity}");
            
            System.Console.WriteLine($"Machine 4 ({machine4.Name}): {machine4.State} - Parts Completed: {machine4.PartsCompleted}");
            System.Console.WriteLine($"  Buffer 4: {buffer4.Count}/{buffer4.Capacity}");
            
            System.Console.WriteLine("\nParts Status:");
            foreach (var part in parts)
            {
                System.Console.WriteLine($"  {part.Id}: Op {part.CurrentOperationIndex}/{part.Route.Count} - {part.State}");
            }
            
            System.Console.WriteLine();
        }
        static void TestSimulationEngine()
        {
            System.Console.WriteLine("--- Test 5: Automated Simulation Engine ---");
            System.Console.WriteLine("Engine will automatically process events!\n");
            
            // Create engine
            var engine = new SimulationEngine(randomSeed: 42);
            
            // Add 4 machines with buffers
            engine.AddMachine(new Machine(1, "Drill Press"), bufferCapacity: 3);
            engine.AddMachine(new Machine(2, "Lathe"), bufferCapacity: 3);
            engine.AddMachine(new Machine(3, "Mill"), bufferCapacity: 3);
            engine.AddMachine(new Machine(4, "Grinder"), bufferCapacity: 3);
            
            // Create parts with routes
            var parts = new List<Part>
            {
                new Part("P201", new List<int> { 1, 2, 3 }),
                new Part("P202", new List<int> { 2, 4, 1 }),
                new Part("P203", new List<int> { 1, 3, 2, 4 }),
                new Part("P204", new List<int> { 4, 3, 1 }),
                new Part("P205", new List<int> { 2, 1, 4 })
            };
            
            // Schedule arrivals
            engine.SchedulePartArrival(parts[0], arrivalTime: 0.0);
            engine.SchedulePartArrival(parts[1], arrivalTime: 2.0);
            engine.SchedulePartArrival(parts[2], arrivalTime: 4.0);
            engine.SchedulePartArrival(parts[3], arrivalTime: 6.0);
            engine.SchedulePartArrival(parts[4], arrivalTime: 8.0);
            
            System.Console.WriteLine("Parts scheduled:");
            foreach (var part in parts)
            {
                var route = string.Join("→", part.Route.Select(m => $"M{m}"));
                System.Console.WriteLine($"  {part.Id}: {route}");
            }
            System.Console.WriteLine();
            
            // Subscribe to events
            engine.EventProcessed += (sender, evt) =>
            {
                System.Console.WriteLine(evt.ToString());
            };
            
            // Run simulation
            System.Console.WriteLine("=== Running Simulation ===\n");
            engine.RunUntil(endTime: 30.0);
            
            // Results
            System.Console.WriteLine($"\n=== Simulation Complete (t={engine.CurrentTime:F2}) ===");
            foreach (var machine in engine.Machines)
            {
                var buffer = engine.Buffers[machine.Id];
                System.Console.WriteLine($"{machine.Name}: {machine.State} - Completed: {machine.PartsCompleted} - Buffer: {buffer.Count}/{buffer.Capacity}");
            }
            
            System.Console.WriteLine("\nPart Status:");
            foreach (var part in parts)
            {
                var status = part.State == PartState.Completed 
                    ? $"COMPLETE (Flow time: {part.CompletionTime - part.ArrivalTime:F2})" 
                    : $"Op {part.CurrentOperationIndex}/{part.Route.Count}";
                System.Console.WriteLine($"  {part.Id}: {status}");
            }
            System.Console.WriteLine();
        }
    }
}