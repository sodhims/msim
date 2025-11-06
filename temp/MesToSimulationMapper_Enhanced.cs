using System;
using System.Collections.Generic;
using System.Linq;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Core.Distributions;
using ManufacturingSimulation.Database.Models;
using Machine = ManufacturingSimulation.Core.Models.Machine;

namespace ManufacturingSimulation.Bridge
{
    /// <summary>
    /// Converts MES database entities to Simulation engine objects
    /// ENHANCED: Now reads routing timing data and distributions
    /// </summary>
    public class MesToSimulationMapper
    {
        private readonly Random _random;

        public MesToSimulationMapper(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public Machine MapToMachine(WorkCenter workCenter)
        {
            if (workCenter == null)
                throw new ArgumentNullException(nameof(workCenter));

            return new Machine(
                workCenter.WorkCenterId,
                workCenter.WorkCenterName
            );
        }

        public int CalculateBufferCapacity(WorkCenter workCenter)
        {
            // Use configured buffer capacity if available
            if (workCenter.BufferCapacity.HasValue && workCenter.BufferCapacity.Value > 0)
            {
                return workCenter.BufferCapacity.Value;
            }

            // Default buffer size
            int bufferSize = 10;

            // Adjust based on capacity if available
            if (workCenter.CapacityUnitsPerHour.HasValue && workCenter.CapacityUnitsPerHour > 0)
            {
                bufferSize = Math.Max(5, Math.Min(20, (int)(workCenter.CapacityUnitsPerHour.Value / 5)));
            }

            // Bottlenecks need larger buffers
            if (workCenter.IsBottleneck)
            {
                bufferSize = Math.Max(bufferSize, 15);
            }

            return bufferSize;
        }

        /// <summary>
        /// Convert production order to simulation parts WITH routing timing data
        /// </summary>
        public List<Part> MapToParts(ProductionOrder order, double arrivalTime = 0.0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var parts = new List<Part>();

            // Get routing from product (ordered by operation sequence)
            var routings = order.Product.Routings
                .OrderBy(r => r.OperationSeq)
                .ToList();

            if (!routings.Any())
            {
                throw new InvalidOperationException(
                    $"Product {order.Product.ProductName} has no routing defined");
            }

            // Create route (list of work center IDs)
            var route = routings.Select(r => r.WorkCenterId).ToList();

            // Create operation timings from routing data
            var operationTimings = CreateOperationTimings(routings);

            // Calculate due date in simulation time
            double dueDate = CalculateDueDate(order, arrivalTime);

            // Create one part per quantity
            for (int i = 0; i < order.Quantity; i++)
            {
                var part = new Part(
                    id: $"{order.OrderNumber}-{i + 1}",
                    route: new List<int>(route),
                    arrivalTime: arrivalTime + (i * 0.1), // Small gap between parts
                    priority: order.Priority,
                    dueDate: dueDate
                );

                // Attach operation timings to the part
                part.OperationTimings = new List<OperationTiming>(operationTimings);
                
                // Set initial operation timing
                part.SetCurrentOperationTiming();

                parts.Add(part);
            }

            return parts;
        }

        /// <summary>
        /// NEW: Create operation timings from routing data
        /// Samples from distributions if specified
        /// </summary>
        private List<OperationTiming> CreateOperationTimings(List<Routing> routings)
        {
            var timings = new List<OperationTiming>();

            foreach (var routing in routings)
            {
                double setupTime = SampleSetupTime(routing);
                double cycleTime = SampleCycleTime(routing);
                int batchSize = routing.BatchSize ?? 1;
                bool isBufferOnly = routing.IsBufferOnly ?? false;

                var timing = new OperationTiming(
                    machineId: routing.WorkCenterId,
                    setupTime: setupTime,
                    cycleTime: cycleTime,
                    batchSize: batchSize,
                    isBufferOnly: isBufferOnly,
                    distributionType: routing.CycleTimeDistribution ?? "Constant"
                );

                timings.Add(timing);
            }

            return timings;
        }

        /// <summary>
        /// Sample setup time from routing data
        /// </summary>
        private double SampleSetupTime(Routing routing)
        {
            // If buffer-only, setup time is 0
            if (routing.IsBufferOnly == true)
                return 0;

            // If distribution parameters are specified, use them
            if (routing.SetupTimeMean.HasValue)
            {
                string distType = routing.SetupTimeDistribution ?? "Normal";
                double mean = routing.SetupTimeMean.Value;
                double stdDev = routing.SetupTimeStdDev ?? 0;

                return SampleFromDistribution(distType, mean, stdDev);
            }

            // Fallback to fixed setup time from minutes field
            if (routing.SetupTimeMinutes.HasValue)
            {
                return routing.SetupTimeMinutes.Value;
            }

            // Default
            return 5.0;
        }

        /// <summary>
        /// Sample cycle time from routing data
        /// </summary>
        private double SampleCycleTime(Routing routing)
        {
            // If buffer-only, cycle time is 0
            if (routing.IsBufferOnly == true)
                return 0;

            // If distribution parameters are specified, use them
            if (routing.CycleTimeMinutes.HasValue)
            {
                string distType = routing.CycleTimeDistribution ?? "Normal";
                double mean = routing.CycleTimeMinutes.Value;
                double stdDev = routing.CycleTimeStdDev ?? 0;

                return SampleFromDistribution(distType, mean, stdDev);
            }

            // Default
            return 10.0;
        }

        /// <summary>
        /// Sample a value from the specified distribution
        /// </summary>
        private double SampleFromDistribution(string distributionType, double param1, double param2)
        {
            switch (distributionType?.ToLower())
            {
                case "normal":
                    // Normal distribution: param1 = mean, param2 = stdDev
                    if (param2 > 0)
                    {
                        return Math.Max(0.1, SampleNormal(param1, param2));
                    }
                    return param1; // Constant if no std dev

                case "uniform":
                    // Uniform distribution: param1 = min, param2 = max
                    if (param2 > param1)
                    {
                        return param1 + (_random.NextDouble() * (param2 - param1));
                    }
                    return param1;

                case "exponential":
                    // Exponential distribution: param1 = lambda (rate)
                    return -Math.Log(1.0 - _random.NextDouble()) / param1;

                case "constant":
                default:
                    // Constant (deterministic)
                    return param1;
            }
        }

        /// <summary>
        /// Sample from normal distribution using Box-Muller transform
        /// </summary>
        private double SampleNormal(double mean, double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private double CalculateDueDate(ProductionOrder order, double arrivalTime)
        {
            if (!order.DueDate.HasValue)
            {
                return arrivalTime + 1000.0;
            }

            var hoursUntilDue = (order.DueDate.Value - DateTime.Now).TotalHours;
            return arrivalTime + Math.Max(hoursUntilDue, 24.0);
        }

        public double EstimateProcessingTime(Routing routing)
        {
            if (routing == null)
                return 5.0;

            double processingTime = 0.0;

            if (routing.SetupTimeMinutes.HasValue)
            {
                processingTime += routing.SetupTimeMinutes.Value;
            }

            if (routing.CycleTimeMinutes.HasValue)
            {
                processingTime += routing.CycleTimeMinutes.Value;
            }

            if (processingTime == 0)
            {
                processingTime = 5.0;
            }

            return processingTime;
        }

        public double CalculateMeanProcessingTime(WorkCenter workCenter, List<Routing> allRoutings)
        {
            var wcRoutings = allRoutings
                .Where(r => r.WorkCenterId == workCenter.WorkCenterId)
                .ToList();

            if (!wcRoutings.Any())
                return 5.0;

            var processingTimes = wcRoutings
                .Select(r => EstimateProcessingTime(r))
                .ToList();

            return processingTimes.Average();
        }

        public SimulationResult MapToResult(int runId, SimulationStatistics stats, double simulationDurationHours)
        {
            return new SimulationResult
            {
                RunId = runId,
                SimulatedTimeHours = simulationDurationHours,
                Throughput = stats.Throughput,
                AvgFlowTimeHours = stats.AverageFlowTime / 60.0,
                AvgWip = stats.CurrentWIP,
                TotalPartsArrived = stats.TotalPartsArrived,
                TotalPartsCompleted = stats.TotalPartsCompleted,
                OverallUtilizationPercent = stats.MachineStats.Any() 
                    ? stats.MachineStats.Values.Average(m => m.Utilization) 
                    : 0
            };
        }

        public List<SimulationWorkCenterResult> MapToWorkCenterResults(int runId, SimulationStatistics stats)
        {
            var results = new List<SimulationWorkCenterResult>();

            foreach (var kvp in stats.MachineStats)
            {
                var machineStats = kvp.Value;

                var result = new SimulationWorkCenterResult
                {
                    RunId = runId,
                    WorkCenterId = kvp.Key,
                    UtilizationPercent = machineStats.Utilization,
                    PartsProcessed = machineStats.PartsProcessed,
                    AvgQueueSize = machineStats.AverageBufferCount,
                    MaxQueueSize = machineStats.CurrentBufferCount,
                    AvgWaitTimeHours = 0,
                    IsBottleneck = false
                };

                results.Add(result);
            }

            if (results.Any())
            {
                var bottleneck = results.OrderByDescending(r => r.UtilizationPercent).First();
                bottleneck.IsBottleneck = true;
            }

            return results;
        }

        public List<SimulationOrderPrediction> MapToOrderPredictions(
            int runId,
            List<ProductionOrder> orders,
            SimulationStatistics stats)
        {
            var predictions = new List<SimulationOrderPrediction>();

            foreach (var order in orders)
            {
                double predictedFlowTime = stats.AverageFlowTime / 60.0;

                var prediction = new SimulationOrderPrediction
                {
                    RunId = runId,
                    ProductionOrderId = order.OrderId,
                    PredictedFlowTimeHours = predictedFlowTime,
                    PredictedCompletionTime = predictedFlowTime,
                    Completed = stats.TotalPartsCompleted >= order.Quantity,
                    PartsCompleted = Math.Min(order.Quantity, stats.TotalPartsCompleted)
                };

                predictions.Add(prediction);
            }

            return predictions;
        }
    }
}
