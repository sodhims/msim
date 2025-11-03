using System;
using System.Collections.Generic;
using System.Linq;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.WPF.Utilities
{
    /// <summary>
    /// Utility class for calculating distances and travel times between machines
    /// Supports multiple distance calculation methods
    /// </summary>
    public static class DistanceCalculator
    {
        #region Constants

        /// <summary>
        /// Conversion factor: 1 pixel = 0.1 meters (10 pixels per meter)
        /// </summary>
        public const double PIXELS_TO_METERS = 0.1;

        /// <summary>
        /// Default material handling speed in meters per second
        /// </summary>
        public const double DEFAULT_TRAVEL_SPEED = 1.0; // 1 m/s

        /// <summary>
        /// Congestion factor to account for obstacles and traffic
        /// </summary>
        public const double CONGESTION_FACTOR = 1.15;

        #endregion

        #region Distance Calculation Methods

        /// <summary>
        /// Calculate Euclidean (straight-line) distance between two points
        /// </summary>
        /// <param name="x1">X coordinate of first point (pixels)</param>
        /// <param name="y1">Y coordinate of first point (pixels)</param>
        /// <param name="x2">X coordinate of second point (pixels)</param>
        /// <param name="y2">Y coordinate of second point (pixels)</param>
        /// <returns>Distance in meters</returns>
        public static double CalculateEuclideanDistance(double x1, double y1, double x2, double y2)
        {
            double distancePixels = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return distancePixels * PIXELS_TO_METERS;
        }

        /// <summary>
        /// Calculate Manhattan (rectilinear) distance between two points
        /// Better for factory floors with grid-based movement
        /// </summary>
        /// <param name="x1">X coordinate of first point (pixels)</param>
        /// <param name="y1">Y coordinate of first point (pixels)</param>
        /// <param name="x2">X coordinate of second point (pixels)</param>
        /// <param name="y2">Y coordinate of second point (pixels)</param>
        /// <returns>Distance in meters</returns>
        public static double CalculateManhattanDistance(double x1, double y1, double x2, double y2)
        {
            double distancePixels = Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
            return distancePixels * PIXELS_TO_METERS;
        }

        /// <summary>
        /// Calculate distance considering congestion and obstacles
        /// </summary>
        /// <param name="x1">X coordinate of first point (pixels)</param>
        /// <param name="y1">Y coordinate of first point (pixels)</param>
        /// <param name="x2">X coordinate of second point (pixels)</param>
        /// <param name="y2">Y coordinate of second point (pixels)</param>
        /// <param name="useManhattan">Use Manhattan distance instead of Euclidean</param>
        /// <returns>Adjusted distance in meters</returns>
        public static double CalculateAdjustedDistance(double x1, double y1, double x2, double y2, bool useManhattan = false)
        {
            double baseDistance = useManhattan
                ? CalculateManhattanDistance(x1, y1, x2, y2)
                : CalculateEuclideanDistance(x1, y1, x2, y2);

            return baseDistance * CONGESTION_FACTOR;
        }

        #endregion

        #region Travel Time Calculations

        /// <summary>
        /// Calculate travel time between two machines
        /// </summary>
        /// <param name="distanceMeters">Distance in meters</param>
        /// <param name="speedMps">Speed in meters per second (default: 1.0 m/s)</param>
        /// <returns>Travel time in seconds</returns>
        public static double CalculateTravelTime(double distanceMeters, double speedMps = DEFAULT_TRAVEL_SPEED)
        {
            if (speedMps <= 0)
                throw new ArgumentException("Speed must be positive", nameof(speedMps));

            return distanceMeters / speedMps;
        }

        /// <summary>
        /// Calculate travel time with acceleration and deceleration
        /// More realistic for AGVs and material handling equipment
        /// </summary>
        /// <param name="distanceMeters">Distance in meters</param>
        /// <param name="maxSpeedMps">Maximum speed in m/s</param>
        /// <param name="accelerationMps2">Acceleration in m/s²</param>
        /// <returns>Travel time in seconds</returns>
        public static double CalculateTravelTimeWithAcceleration(
            double distanceMeters,
            double maxSpeedMps = 1.5,
            double accelerationMps2 = 0.5)
        {
            // Time to reach max speed
            double timeToMaxSpeed = maxSpeedMps / accelerationMps2;

            // Distance covered during acceleration
            double accelDistance = 0.5 * accelerationMps2 * Math.Pow(timeToMaxSpeed, 2);

            // If distance is short, we never reach max speed
            if (2 * accelDistance >= distanceMeters)
            {
                // Accelerate to midpoint, then decelerate
                double midpointSpeed = Math.Sqrt(accelerationMps2 * distanceMeters);
                return 2 * midpointSpeed / accelerationMps2;
            }
            else
            {
                // Accelerate, cruise at max speed, decelerate
                double cruiseDistance = distanceMeters - 2 * accelDistance;
                double cruiseTime = cruiseDistance / maxSpeedMps;
                return 2 * timeToMaxSpeed + cruiseTime;
            }
        }

        #endregion

        #region Distance Matrix Operations

        /// <summary>
        /// Build complete distance matrix for all machines
        /// </summary>
        /// <param name="machines">List of machine locations</param>
        /// <param name="useManhattan">Use Manhattan distance instead of Euclidean</param>
        /// <param name="includeReverse">Include reverse direction (A->B and B->A)</param>
        /// <returns>List of distance records</returns>
        public static List<MachineDistance> BuildDistanceMatrix(
            IEnumerable<MachineLocation> machines,
            bool useManhattan = false,
            bool includeReverse = true)
        {
            var distances = new List<MachineDistance>();
            var machineList = machines.ToList();

            for (int i = 0; i < machineList.Count; i++)
            {
                for (int j = i + 1; j < machineList.Count; j++)
                {
                    var from = machineList[i];
                    var to = machineList[j];

                    double distance = useManhattan
                        ? CalculateManhattanDistance(from.XCoordinate, from.YCoordinate, to.XCoordinate, to.YCoordinate)
                        : CalculateEuclideanDistance(from.XCoordinate, from.YCoordinate, to.XCoordinate, to.YCoordinate);

                    double travelTime = CalculateTravelTime(distance);

                    // Add forward direction
                    distances.Add(new MachineDistance
                    {
                        FromMachineId = from.MachineId,
                        ToMachineId = to.MachineId,
                        DistanceMeters = Math.Round(distance, 2),
                        TravelTimeSeconds = Math.Round(travelTime, 2),
                        UpdatedAt = DateTime.Now
                    });

                    // Add reverse direction if requested
                    if (includeReverse)
                    {
                        distances.Add(new MachineDistance
                        {
                            FromMachineId = to.MachineId,
                            ToMachineId = from.MachineId,
                            DistanceMeters = Math.Round(distance, 2),
                            TravelTimeSeconds = Math.Round(travelTime, 2),
                            UpdatedAt = DateTime.Now
                        });
                    }
                }
            }

            return distances;
        }

        /// <summary>
        /// Get distance between two specific machines from matrix
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="fromMachineId">Source machine ID</param>
        /// <param name="toMachineId">Destination machine ID</param>
        /// <returns>Distance in meters, or -1 if not found</returns>
        public static double GetDistance(List<MachineDistance> distances, int fromMachineId, int toMachineId)
        {
            var distance = distances.FirstOrDefault(d =>
                d.FromMachineId == fromMachineId && d.ToMachineId == toMachineId);

            return distance?.DistanceMeters ?? -1;
        }

        /// <summary>
        /// Get travel time between two specific machines from matrix
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="fromMachineId">Source machine ID</param>
        /// <param name="toMachineId">Destination machine ID</param>
        /// <returns>Travel time in seconds, or -1 if not found</returns>
        public static double GetTravelTime(List<MachineDistance> distances, int fromMachineId, int toMachineId)
        {
            var distance = distances.FirstOrDefault(d =>
                d.FromMachineId == fromMachineId && d.ToMachineId == toMachineId);

            return distance?.TravelTimeSeconds ?? -1;
        }

        #endregion

        #region Layout Analysis

        /// <summary>
        /// Calculate total distance for a given part routing
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="routing">Sequence of machine IDs representing part routing</param>
        /// <returns>Total distance in meters</returns>
        public static double CalculateRoutingDistance(List<MachineDistance> distances, List<int> routing)
        {
            if (routing == null || routing.Count < 2)
                return 0;

            double totalDistance = 0;
            for (int i = 0; i < routing.Count - 1; i++)
            {
                totalDistance += GetDistance(distances, routing[i], routing[i + 1]);
            }

            return totalDistance;
        }

        /// <summary>
        /// Calculate average distance between all machine pairs
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <returns>Average distance in meters</returns>
        public static double CalculateAverageDistance(List<MachineDistance> distances)
        {
            if (distances == null || distances.Count == 0)
                return 0;

            // Only count unique pairs (not both A->B and B->A)
            var uniqueDistances = distances
                .GroupBy(d => new {
                    Min = Math.Min(d.FromMachineId, d.ToMachineId),
                    Max = Math.Max(d.FromMachineId, d.ToMachineId)
                })
                .Select(g => g.First())
                .ToList();

            return uniqueDistances.Average(d => d.DistanceMeters);
        }

        /// <summary>
        /// Find closest machine to a given machine
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="machineId">Source machine ID</param>
        /// <returns>ID of closest machine, or -1 if not found</returns>
        public static int FindClosestMachine(List<MachineDistance> distances, int machineId)
        {
            var closest = distances
                .Where(d => d.FromMachineId == machineId)
                .OrderBy(d => d.DistanceMeters)
                .FirstOrDefault();

            return closest?.ToMachineId ?? -1;
        }

        /// <summary>
        /// Calculate layout compactness score (0-100, higher is better)
        /// Based on inverse of average distance
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <returns>Compactness score</returns>
        public static double CalculateLayoutCompactness(List<MachineDistance> distances)
        {
            double avgDistance = CalculateAverageDistance(distances);

            if (avgDistance == 0)
                return 100;

            // Normalize to 0-100 scale (assuming 50m is "spread out")
            return Math.Max(0, Math.Min(100, 100 * (1 - avgDistance / 50.0)));
        }

        #endregion

        #region Optimization Helpers

        /// <summary>
        /// Evaluate layout efficiency for given product routings
        /// Lower score is better
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="productRoutings">List of product routings (each routing is a list of machine IDs)</param>
        /// <returns>Total weighted distance score</returns>
        public static double EvaluateLayoutEfficiency(
            List<MachineDistance> distances,
            List<List<int>> productRoutings)
        {
            double totalScore = 0;

            foreach (var routing in productRoutings)
            {
                totalScore += CalculateRoutingDistance(distances, routing);
            }

            return totalScore;
        }

        /// <summary>
        /// Identify bottleneck paths (most frequently used machine pairs)
        /// </summary>
        /// <param name="productRoutings">List of product routings</param>
        /// <returns>Dictionary of machine pairs and their frequency</returns>
        public static Dictionary<(int from, int to), int> IdentifyBottleneckPaths(List<List<int>> productRoutings)
        {
            var pathFrequency = new Dictionary<(int from, int to), int>();

            foreach (var routing in productRoutings)
            {
                for (int i = 0; i < routing.Count - 1; i++)
                {
                    var path = (routing[i], routing[i + 1]);

                    if (pathFrequency.ContainsKey(path))
                        pathFrequency[path]++;
                    else
                        pathFrequency[path] = 1;
                }
            }

            return pathFrequency.OrderByDescending(kvp => kvp.Value)
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #endregion

        #region Export/Reporting

        /// <summary>
        /// Generate distance matrix report as CSV string
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <param name="machineNames">Dictionary mapping machine IDs to names</param>
        /// <returns>CSV formatted string</returns>
        public static string GenerateDistanceMatrixCSV(
            List<MachineDistance> distances,
            Dictionary<int, string> machineNames)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("From Machine,To Machine,Distance (m),Travel Time (s)");

            foreach (var distance in distances.OrderBy(d => d.FromMachineId).ThenBy(d => d.ToMachineId))
            {
                string fromName = machineNames.ContainsKey(distance.FromMachineId)
                    ? machineNames[distance.FromMachineId]
                    : $"Machine {distance.FromMachineId}";

                string toName = machineNames.ContainsKey(distance.ToMachineId)
                    ? machineNames[distance.ToMachineId]
                    : $"Machine {distance.ToMachineId}";

                csv.AppendLine($"{fromName},{toName},{distance.DistanceMeters:F2},{distance.TravelTimeSeconds:F2}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// Generate layout statistics summary
        /// </summary>
        /// <param name="distances">Distance matrix</param>
        /// <returns>Statistics object</returns>
        public static LayoutStatistics CalculateStatistics(List<MachineDistance> distances)
        {
            if (distances == null || distances.Count == 0)
                return new LayoutStatistics();

            var uniqueDistances = distances
                .GroupBy(d => new {
                    Min = Math.Min(d.FromMachineId, d.ToMachineId),
                    Max = Math.Max(d.FromMachineId, d.ToMachineId)
                })
                .Select(g => g.First())
                .Select(d => d.DistanceMeters)
                .ToList();

            return new LayoutStatistics
            {
                TotalPairs = uniqueDistances.Count,
                MinimumDistance = uniqueDistances.Min(),
                MaximumDistance = uniqueDistances.Max(),
                AverageDistance = uniqueDistances.Average(),
                MedianDistance = CalculateMedian(uniqueDistances),
                StandardDeviation = CalculateStandardDeviation(uniqueDistances),
                CompactnessScore = CalculateLayoutCompactness(distances)
            };
        }

        private static double CalculateMedian(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            else
                return sorted[count / 2];
        }

        private static double CalculateStandardDeviation(List<double> values)
        {
            double avg = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumOfSquares / values.Count);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Layout statistics container
    /// </summary>
    public class LayoutStatistics
    {
        public int TotalPairs { get; set; }
        public double MinimumDistance { get; set; }
        public double MaximumDistance { get; set; }
        public double AverageDistance { get; set; }
        public double MedianDistance { get; set; }
        public double StandardDeviation { get; set; }
        public double CompactnessScore { get; set; }

        public override string ToString()
        {
            return $"Layout Statistics:\n" +
                   $"  Machine Pairs: {TotalPairs}\n" +
                   $"  Min Distance: {MinimumDistance:F2} m\n" +
                   $"  Max Distance: {MaximumDistance:F2} m\n" +
                   $"  Avg Distance: {AverageDistance:F2} m\n" +
                   $"  Median Distance: {MedianDistance:F2} m\n" +
                   $"  Std Deviation: {StandardDeviation:F2} m\n" +
                   $"  Compactness: {CompactnessScore:F1}/100";
        }
    }

    #endregion
}
