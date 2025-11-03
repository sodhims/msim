using System;
using System.Collections.Generic;
using System.Linq;
using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Database.Models;
using Machine = ManufacturingSimulation.Core.Models.Machine;
namespace ManufacturingSimulation.Bridge
{
    /// <summary>
    /// Converts MES database entities to Simulation engine objects
    /// </summary>
    public class MesToSimulationMapper
    {
        /// <summary>
        /// Convert MES WorkCenter to Simulation Machine
        /// </summary>
        public Machine MapToMachine(WorkCenter workCenter)
        {
            if (workCenter == null)
                throw new ArgumentNullException(nameof(workCenter));

            // Create machine with work center ID and name
            return new Machine(
                workCenter.WorkCenterId,
                workCenter.WorkCenterName
            );
        }

        /// <summary>
        /// Calculate buffer capacity based on work center properties
        /// </summary>
        public int CalculateBufferCapacity(WorkCenter workCenter)
        {
            // Default buffer size
            int bufferSize = 10;

            // Adjust based on capacity if available
            if (workCenter.CapacityUnitsPerHour.HasValue && workCenter.CapacityUnitsPerHour > 0)
            {
                // Higher capacity machines can handle larger buffers
                bufferSize = Math.Max(5, Math.Min(20, (int)(workCenter.CapacityUnitsPerHour.Value / 5)));
            }

            // Bottlenecks need larger buffers
            if (workCenter.IsBottleneck)
            {
                bufferSize = Math.Max(bufferSize, 15);
            }

            return bufferSize;
        }

public SimulationResult MapToResult(
    int runId,
    SimulationStatistics stats,
    double simulationDurationHours)
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

public List<SimulationWorkCenterResult> MapToWorkCenterResults(
    int runId,
    SimulationStatistics stats)
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
            AvgWaitTimeHours = 0, // Not available in stats
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

        /// <summary>
        /// Convert production order to simulation parts
        /// Returns list of parts (one per quantity)
        /// </summary>
        public List<Part> MapToParts(ProductionOrder order, double arrivalTime = 0.0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var parts = new List<Part>();

            // Get routing from product
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

                parts.Add(part);
            }

            return parts;
        }

        /// <summary>
        /// Convert due date to simulation time (hours from start)
        /// </summary>
        private double CalculateDueDate(ProductionOrder order, double arrivalTime)
        {
            if (!order.DueDate.HasValue)
            {
                // No due date - give generous deadline
                return arrivalTime + 1000.0;
            }

            // Calculate hours from now to due date
            var hoursUntilDue = (order.DueDate.Value - DateTime.Now).TotalHours;

            // Convert to simulation time
            return arrivalTime + Math.Max(hoursUntilDue, 24.0); // Minimum 24 hours
        }

        /// <summary>
        /// Estimate processing time for a routing operation
        /// </summary>
        public double EstimateProcessingTime(Routing routing)
        {
            if (routing == null)
                return 5.0; // Default

            double processingTime = 0.0;

            // Add setup time (converted to same units)
            if (routing.SetupTimeMinutes.HasValue)
            {
                processingTime += routing.SetupTimeMinutes.Value;
            }

            // Add cycle time
            if (routing.CycleTimeMinutes.HasValue)
            {
                processingTime += routing.CycleTimeMinutes.Value;
            }

            // Default if nothing specified
            if (processingTime == 0)
            {
                processingTime = 5.0;
            }

            return processingTime;
        }

        /// <summary>
        /// Calculate mean processing time for a work center
        /// Based on all routings using that work center
        /// </summary>
        public double CalculateMeanProcessingTime(
            WorkCenter workCenter,
            List<Routing> allRoutings)
        {
            var wcRoutings = allRoutings
                .Where(r => r.WorkCenterId == workCenter.WorkCenterId)
                .ToList();

            if (!wcRoutings.Any())
                return 5.0; // Default

            var processingTimes = wcRoutings
                .Select(r => EstimateProcessingTime(r))
                .ToList();

            return processingTimes.Average();
        }

        /// <summary>
        /// Convert simulation statistics to database result
        /// </summary>

        /// <summary>
        /// Create order predictions based on simulation results
        /// </summary>
        public List<SimulationOrderPrediction> MapToOrderPredictions(
            int runId,
            List<ProductionOrder> orders,
            SimulationStatistics stats)
        {
            var predictions = new List<SimulationOrderPrediction>();

            foreach (var order in orders)
            {
                // Calculate prediction based on throughput and order position
                double predictedFlowTime = stats.AverageFlowTime / 60.0; // Convert to hours

                var prediction = new SimulationOrderPrediction
                {
                    RunId = runId,
                    ProductionOrderId = order.OrderId,
                    PredictedFlowTimeHours = predictedFlowTime,
                    PredictedCompletionTime = predictedFlowTime, // Simplified
                    Completed = stats.TotalPartsCompleted >= order.Quantity,
                    PartsCompleted = Math.Min(order.Quantity, stats.TotalPartsCompleted)
                };

                predictions.Add(prediction);
            }

            return predictions;
        }
    }
}
