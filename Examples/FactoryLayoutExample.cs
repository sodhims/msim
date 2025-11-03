using System;
using System.Collections.Generic;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.Utilities;

namespace ManufacturingSimulation.Examples
{
    /// <summary>
    /// Example code demonstrating Factory Layout and Distance Calculator usage
    /// Use this as a reference for students
    /// </summary>
    public class FactoryLayoutExample
    {
        public static void RunExamples()
        {
            Console.WriteLine("=== Factory Layout Distance Calculator Examples ===\n");

            // Example 1: Calculate distance between two machines
            Example1_BasicDistance();

            // Example 2: Build distance matrix
            Example2_DistanceMatrix();

            // Example 3: Calculate routing distance
            Example3_RoutingDistance();

            // Example 4: Evaluate layout efficiency
            Example4_LayoutEfficiency();

            // Example 5: Layout statistics
            Example5_LayoutStatistics();

            // Example 6: Travel time with acceleration
            Example6_TravelTime();
        }

        /// <summary>
        /// Example 1: Calculate distance between two machines
        /// </summary>
        private static void Example1_BasicDistance()
        {
            Console.WriteLine("Example 1: Basic Distance Calculation");
            Console.WriteLine("=====================================");

            // Machine positions (in pixels on canvas)
            double x1 = 100, y1 = 100;  // Machine 1
            double x2 = 400, y2 = 300;  // Machine 2

            // Calculate Euclidean distance
            double euclidean = DistanceCalculator.CalculateEuclideanDistance(x1, y1, x2, y2);
            Console.WriteLine($"Euclidean Distance: {euclidean:F2} meters");

            // Calculate Manhattan distance
            double manhattan = DistanceCalculator.CalculateManhattanDistance(x1, y1, x2, y2);
            Console.WriteLine($"Manhattan Distance: {manhattan:F2} meters");

            // Calculate travel time
            double travelTime = DistanceCalculator.CalculateTravelTime(euclidean, 1.0);
            Console.WriteLine($"Travel Time: {travelTime:F2} seconds");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 2: Build complete distance matrix
        /// </summary>
        private static void Example2_DistanceMatrix()
        {
            Console.WriteLine("Example 2: Distance Matrix Generation");
            Console.WriteLine("=====================================");

            // Create sample machine locations
            var machines = new List<MachineLocation>
            {
                new MachineLocation { MachineId = 1, XCoordinate = 100, YCoordinate = 100 },
                new MachineLocation { MachineId = 2, XCoordinate = 300, YCoordinate = 100 },
                new MachineLocation { MachineId = 3, XCoordinate = 500, YCoordinate = 100 },
                new MachineLocation { MachineId = 4, XCoordinate = 100, YCoordinate = 300 },
            };

            // Build distance matrix
            var distances = DistanceCalculator.BuildDistanceMatrix(
                machines, 
                useManhattan: false, 
                includeReverse: true
            );

            Console.WriteLine($"Generated {distances.Count} distance pairs");
            Console.WriteLine("\nDistance Matrix:");
            Console.WriteLine("From → To | Distance (m) | Time (s)");
            Console.WriteLine("----------------------------------------");

            foreach (var d in distances.Take(8))  // Show first 8 for brevity
            {
                Console.WriteLine($"  {d.FromMachineId} → {d.ToMachineId}   |    {d.DistanceMeters,6:F2}    |  {d.TravelTimeSeconds,5:F2}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example 3: Calculate total distance for a routing
        /// </summary>
        private static void Example3_RoutingDistance()
        {
            Console.WriteLine("Example 3: Product Routing Distance");
            Console.WriteLine("===================================");

            // Sample distance matrix
            var distances = new List<MachineDistance>
            {
                new MachineDistance { FromMachineId = 1, ToMachineId = 2, DistanceMeters = 20.0 },
                new MachineDistance { FromMachineId = 2, ToMachineId = 3, DistanceMeters = 25.0 },
                new MachineDistance { FromMachineId = 3, ToMachineId = 4, DistanceMeters = 30.0 },
            };

            // Product routing: Machine 1 → 2 → 3 → 4
            var routing = new List<int> { 1, 2, 3, 4 };

            double totalDistance = DistanceCalculator.CalculateRoutingDistance(distances, routing);
            
            Console.WriteLine($"Routing: {string.Join(" → ", routing)}");
            Console.WriteLine($"Total Distance: {totalDistance:F2} meters");
            Console.WriteLine($"Total Time: {totalDistance / 1.0:F2} seconds (@ 1.0 m/s)");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 4: Evaluate layout efficiency for multiple products
        /// </summary>
        private static void Example4_LayoutEfficiency()
        {
            Console.WriteLine("Example 4: Layout Efficiency Evaluation");
            Console.WriteLine("=======================================");

            // Sample distance matrix (simplified)
            var distances = new List<MachineDistance>
            {
                new MachineDistance { FromMachineId = 1, ToMachineId = 2, DistanceMeters = 20.0 },
                new MachineDistance { FromMachineId = 2, ToMachineId = 3, DistanceMeters = 25.0 },
                new MachineDistance { FromMachineId = 3, ToMachineId = 4, DistanceMeters = 30.0 },
                new MachineDistance { FromMachineId = 1, ToMachineId = 3, DistanceMeters = 35.0 },
                new MachineDistance { FromMachineId = 2, ToMachineId = 4, DistanceMeters = 40.0 },
            };

            // Product routings
            var routings = new List<List<int>>
            {
                new List<int> { 1, 2, 3, 4 },    // Product A: Full sequence
                new List<int> { 1, 3, 4 },       // Product B: Skip machine 2
                new List<int> { 2, 3, 4 },       // Product C: Start at machine 2
            };

            double efficiency = DistanceCalculator.EvaluateLayoutEfficiency(distances, routings);

            Console.WriteLine("Product Routings:");
            Console.WriteLine("  Product A: 1 → 2 → 3 → 4");
            Console.WriteLine("  Product B: 1 → 3 → 4");
            Console.WriteLine("  Product C: 2 → 3 → 4");
            Console.WriteLine($"\nTotal Layout Efficiency Score: {efficiency:F2} meters");
            Console.WriteLine("(Lower is better)");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 5: Calculate layout statistics
        /// </summary>
        private static void Example5_LayoutStatistics()
        {
            Console.WriteLine("Example 5: Layout Statistics");
            Console.WriteLine("============================");

            // Create sample machines in a square pattern
            var machines = new List<MachineLocation>
            {
                new MachineLocation { MachineId = 1, XCoordinate = 100, YCoordinate = 100 },
                new MachineLocation { MachineId = 2, XCoordinate = 400, YCoordinate = 100 },
                new MachineLocation { MachineId = 3, XCoordinate = 400, YCoordinate = 400 },
                new MachineLocation { MachineId = 4, XCoordinate = 100, YCoordinate = 400 },
            };

            // Build distance matrix
            var distances = DistanceCalculator.BuildDistanceMatrix(machines);

            // Calculate statistics
            var stats = DistanceCalculator.CalculateStatistics(distances);

            // Display results
            Console.WriteLine(stats.ToString());

            // Interpretation
            Console.WriteLine("\nInterpretation:");
            if (stats.CompactnessScore > 70)
                Console.WriteLine("✅ Layout is COMPACT - machines are close together");
            else if (stats.CompactnessScore > 40)
                Console.WriteLine("⚠️  Layout is MODERATE - some optimization possible");
            else
                Console.WriteLine("❌ Layout is SPREAD OUT - consider bringing machines closer");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 6: Travel time with acceleration model
        /// </summary>
        private static void Example6_TravelTime()
        {
            Console.WriteLine("Example 6: Realistic Travel Time (with acceleration)");
            Console.WriteLine("===================================================");

            double[] distances = { 5.0, 10.0, 25.0, 50.0 };

            Console.WriteLine("Distance (m) | Simple Model (s) | Acceleration Model (s)");
            Console.WriteLine("--------------------------------------------------------");

            foreach (var distance in distances)
            {
                double simpleTime = DistanceCalculator.CalculateTravelTime(distance, 1.0);
                double realisticTime = DistanceCalculator.CalculateTravelTimeWithAcceleration(
                    distance, 
                    maxSpeedMps: 1.5, 
                    accelerationMps2: 0.5
                );

                Console.WriteLine($"   {distance,5:F1}     |      {simpleTime,5:F2}       |        {realisticTime,5:F2}");
            }

            Console.WriteLine("\nNote: Acceleration model accounts for speed-up and slow-down,");
            Console.WriteLine("      making it more realistic for AGVs and material handling equipment.");

            Console.WriteLine();
        }

        /// <summary>
        /// Example 7: Find bottleneck paths
        /// </summary>
        public static void Example7_BottleneckAnalysis()
        {
            Console.WriteLine("Example 7: Bottleneck Path Analysis");
            Console.WriteLine("===================================");

            // Product routings (what paths do products take?)
            var routings = new List<List<int>>
            {
                new List<int> { 1, 2, 3 },       // Product A
                new List<int> { 1, 2, 4 },       // Product B
                new List<int> { 1, 2, 3 },       // Product C (same as A)
                new List<int> { 2, 3, 4 },       // Product D
                new List<int> { 1, 2, 3 },       // Product E (same as A)
            };

            // Identify which machine pairs are used most frequently
            var bottlenecks = DistanceCalculator.IdentifyBottleneckPaths(routings);

            Console.WriteLine("Most Frequently Used Paths:");
            Console.WriteLine("From → To | Frequency");
            Console.WriteLine("----------------------");

            foreach (var kvp in bottlenecks.Take(5))
            {
                Console.WriteLine($"  {kvp.Key.from} → {kvp.Key.to}   |    {kvp.Value} times");
            }

            Console.WriteLine("\n💡 Recommendation: Place frequently connected machines closer together");
            Console.WriteLine();
        }

        /// <summary>
        /// Example 8: Export distance matrix to CSV
        /// </summary>
        public static void Example8_ExportCSV()
        {
            Console.WriteLine("Example 8: Export Distance Matrix to CSV");
            Console.WriteLine("========================================");

            // Sample machines
            var machines = new List<MachineLocation>
            {
                new MachineLocation { MachineId = 1, XCoordinate = 100, YCoordinate = 100 },
                new MachineLocation { MachineId = 2, XCoordinate = 300, YCoordinate = 100 },
            };

            // Build distance matrix
            var distances = DistanceCalculator.BuildDistanceMatrix(machines);

            // Machine names
            var names = new Dictionary<int, string>
            {
                { 1, "CNC Mill" },
                { 2, "Lathe" }
            };

            // Generate CSV
            string csv = DistanceCalculator.GenerateDistanceMatrixCSV(distances, names);

            Console.WriteLine("Generated CSV:");
            Console.WriteLine(csv);

            Console.WriteLine("💾 Save this to a file for analysis in Excel or other tools");
            Console.WriteLine();
        }
    }

    #region Usage in Main Application

    /// <summary>
    /// How to use in your simulation engine
    /// </summary>
    public class SimulationIntegrationExample
    {
        private List<MachineDistance> _distanceMatrix;

        public void Initialize()
        {
            // Load distance matrix from database or ViewModel
            _distanceMatrix = LoadDistanceMatrixFromDatabase();
        }

        public void ProcessPartMovement(int partId, int fromMachineId, int toMachineId)
        {
            // Get travel time from distance matrix
            double travelTime = DistanceCalculator.GetTravelTime(
                _distanceMatrix, 
                fromMachineId, 
                toMachineId
            );

            // Add material handling event
            Console.WriteLine($"Part {partId} moving from Machine {fromMachineId} to {toMachineId}");
            Console.WriteLine($"Travel time: {travelTime:F2} seconds");

            // Schedule arrival event in your simulation
            // ScheduleEvent(new PartArrivalEvent { ... });
        }

        private List<MachineDistance> LoadDistanceMatrixFromDatabase()
        {
            // This would connect to your actual database
            // For example purposes, returning empty list
            return new List<MachineDistance>();
        }
    }

    #endregion
}
