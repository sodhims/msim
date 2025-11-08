using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Engine;

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

            // Test 4: Simulation Engine
            TestSimulationEngine();

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

            var machine = new Machine(1, "Drill Press", null);
            var part = new Part("P002", new List<int> { 1, 2 }, 0);

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

            System.Console.WriteLine();
        }

        static void TestBufferOperations()
        {
            System.Console.WriteLine("--- Test 3: Buffer Operations ---");

            var buffer = new ManufacturingSimulation.Core.Models.Buffer(capacity: 3, machineId: 2);
            System.Console.WriteLine($"Buffer created for Machine 2: Capacity={buffer.Capacity}");

            var part1 = new Part("P003", new List<int> { 1, 2 }, 0);
            var part2 = new Part("P004", new List<int> { 1, 2 }, 0);
            var part3 = new Part("P005", new List<int> { 1, 2 }, 0);
            var part4 = new Part("P006", new List<int> { 1, 2 }, 0);

            System.Console.WriteLine($"\nAdding P003: {buffer.TryAdd(part1, 0)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Adding P004: {buffer.TryAdd(part2, 0)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Adding P005: {buffer.TryAdd(part3, 0)} - Buffer: {buffer.Count}/{buffer.Capacity}");
            System.Console.WriteLine($"Buffer Full: {buffer.IsFull}");
            System.Console.WriteLine($"Adding P006: {buffer.TryAdd(part4, 0)} - Buffer: {buffer.Count}/{buffer.Capacity} (should fail)");

            System.Console.WriteLine($"\nRemoving parts (FIFO order):");
            var removed1 = buffer.SelectAndRemove("FIFO", 1.0);
            System.Console.WriteLine($"  Removed: {removed1?.Id} - Buffer: {buffer.Count}/{buffer.Capacity}");

            var removed2 = buffer.SelectAndRemove("FIFO", 1.0);
            System.Console.WriteLine($"  Removed: {removed2?.Id} - Buffer: {buffer.Count}/{buffer.Capacity}");

            System.Console.WriteLine();
        }

        static void TestSimulationEngine()
        {
            System.Console.WriteLine("--- Test 4: Simulation Engine ---");
            System.Console.WriteLine("Engine will automatically process events!\n");

            var engine = new SimulationEngine(randomSeed: 42);

            // Add 4 machines with buffers
            engine.AddMachine(new Machine(1, "Drill Press", null), bufferCapacity: 3);
            engine.AddMachine(new Machine(2, "Lathe", null), bufferCapacity: 3);
            engine.AddMachine(new Machine(3, "Mill", null), bufferCapacity: 3);
            engine.AddMachine(new Machine(4, "Grinder", null), bufferCapacity: 3);

            var parts = new List<Part>
            {
                new Part("P201", new List<int> { 1, 2, 3 }, 0.0),
                new Part("P202", new List<int> { 2, 4, 1 }, 2.0),
                new Part("P203", new List<int> { 1, 3, 2, 4 }, 4.0),
                new Part("P204", new List<int> { 4, 3, 1 }, 6.0),
                new Part("P205", new List<int> { 2, 1, 4 }, 8.0)
            };

            System.Console.WriteLine("Parts scheduled:");
            foreach (var part in parts)
            {
                var route = string.Join("→", part.Route.Select(m => $"M{m}"));
                System.Console.WriteLine($"  {part.Id}: {route}");
                engine.SchedulePartArrival(part, part.ArrivalTime);
            }
            System.Console.WriteLine();

            // Subscribe to events
            int eventCount = 0;
            engine.EventProcessed += (sender, evt) =>
            {
                eventCount++;
                if (eventCount <= 20) // Only show first 20 events
                    System.Console.WriteLine($"[{evt.ScheduledTime:F2}] {evt.GetType().Name}");
            };

            System.Console.WriteLine("=== Running Simulation ===\n");
            engine.RunUntil(endTime: 30.0);

            System.Console.WriteLine($"\n=== Simulation Complete (t={engine.CurrentTime:F2}) ===");
            System.Console.WriteLine($"Total events: {eventCount}");

            var stats = engine.GetStatistics();
            System.Console.WriteLine($"Parts Arrived: {stats.TotalPartsArrived}");
            System.Console.WriteLine($"Parts Completed: {stats.TotalPartsCompleted}");
            System.Console.WriteLine($"Throughput: {stats.Throughput:F2} parts/hr");

            System.Console.WriteLine("\nMachine Status:");
            foreach (var machine in engine.Machines)
            {
                var buffer = engine.Buffers[machine.Id];
                var machineStats = stats.MachineStats[machine.Id];
                System.Console.WriteLine($"  {machine.Name}: {machineStats.PartsProcessed} parts, " +
                    $"{machineStats.Utilization:F1}% util, Buffer: {buffer.Count}/{buffer.Capacity}");
            }

            System.Console.WriteLine();
        }
    }
}