using ManufacturingSimulation.Core;
using ManufacturingSimulation.Core.Configuration;
using ManufacturingSimulation.Core.Models;
using ManufacturingSimulation.Database;
using ManufacturingSimulation.Database.Models;
using ManufacturingSimulation.Database.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace ManufacturingSimulation.Bridge
{
    /// <summary>
    /// Main service for running simulations
    /// Orchestrates loading MES data, running simulation, and saving results
    /// </summary>
    public class SimulationService
    {
        private readonly SimulationRepository _repository;
        private readonly MesToSimulationMapper _mapper;
        private readonly MesDbContext _db;

        public SimulationService(MesDbContext context)
        {
            _db = context;
            _repository = new SimulationRepository(context);
            _mapper = new MesToSimulationMapper(42);
        }

        public SimulationService(SimulationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = new MesToSimulationMapper();
        }

        /// <summary>
        /// Run a simulation scenario
        /// </summary>
        public SimulationRunResult RunScenario(int scenarioId, int studentId, List<int> orderIds, List<MachineConfiguration> machineConfigs = null)
        {
            var stopwatch = Stopwatch.StartNew();
            int runId = 0;

            try
            {
                // 1. Load scenario configuration
                var scenario = _repository.GetScenario(scenarioId);
                if (scenario == null)
                    throw new InvalidOperationException($"Scenario {scenarioId} not found");

                // 2. Create simulation run record
                var config = JsonSerializer.Serialize(new
                {
                    ScenarioId = scenarioId,
                    OrderIds = orderIds,
                    Duration = scenario.SimulationDurationHours,
                    RandomSeed = scenario.RandomSeed
                });

                runId = _repository.CreateRun(scenarioId, studentId, config);

                // 3. Load MES data
                var workCenters = _repository.GetWorkCenters(studentId);
                var orders = _repository.GetProductionOrders(studentId, orderIds);

                if (!workCenters.Any())
                    throw new InvalidOperationException("No work centers defined for this student");

                if (!orders.Any())
                    throw new InvalidOperationException("No production orders found");

                // 4. Build simulation engine
                var engine = new SimulationEngine(scenario.RandomSeed);
                var logger = new SimulationEventLogger(runId, _db);
                engine.SetEventLogger(logger);

                // 5. Add machines - USE UI CONFIG IF PROVIDED
                if (machineConfigs != null && machineConfigs.Any())
                {
                    foreach (var machineConfig in machineConfigs)
                    {
                        var wc = workCenters.FirstOrDefault(w => w.WorkCenterName == machineConfig.Name);
                        if (wc != null)
                        {
                            var machine = _mapper.MapToMachine(wc);
                            engine.AddMachine(machine, machineConfig.BufferCapacity);
                        }
                    }
                }
                else
                {
                    // Fallback to database values
                    foreach (var wc in workCenters)
                    {
                        var machine = _mapper.MapToMachine(wc);
                        int bufferCapacity = _mapper.CalculateBufferCapacity(wc);
                        engine.AddMachine(machine, bufferCapacity);
                    }
                }

                // 6. Schedule parts (from production orders)
                double currentTime = 0.0;
                var allParts = new List<Part>();

                foreach (var order in orders.OrderBy(o => o.Priority))
                {
                    var parts = _mapper.MapToParts(order, currentTime);
                    foreach (var part in parts)
                    {
                        engine.SchedulePartArrival(part, part.ArrivalTime);
                        allParts.Add(part);
                    }

                    // Small delay between orders
                    currentTime += 0.5;
                }

                // 7. Run simulation
                double durationMinutes = scenario.SimulationDurationHours * 60.0;
                engine.RunUntil(durationMinutes);

                // 8. Get statistics
                var stats = engine.GetStatistics();

                // 9. Save results
                SaveResults(runId, stats, orders, scenario.SimulationDurationHours);

                // 10. Update run status
                stopwatch.Stop();
                _repository.UpdateRunStatus(runId, "Completed", stopwatch.Elapsed.TotalSeconds);

                // 11. Return results
                return new SimulationRunResult
                {
                    RunId = runId,
                    Success = true,
                    Statistics = stats,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                    BottleneckWorkCenter = _repository.GetBottleneck(runId)
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (runId > 0)
                {
                    _repository.UpdateRunStatus(runId, "Failed",
                        stopwatch.Elapsed.TotalSeconds, ex.Message);
                }

                return new SimulationRunResult
                {
                    RunId = runId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds
                };
            }
        }

        /// <summary>
        /// Save all simulation results to database
        /// </summary>
        private void SaveResults(
            int runId,
            SimulationStatistics stats,
            List<ProductionOrder> orders,
            double simulationDurationHours)
        {
            // Save overall results
            var result = _mapper.MapToResult(runId, stats, simulationDurationHours);
            _repository.SaveResults(result);

            // Save work center results
            var wcResults = _mapper.MapToWorkCenterResults(runId, stats);
            _repository.SaveWorkCenterResults(wcResults);

            // Save order predictions
            var orderPredictions = _mapper.MapToOrderPredictions(runId, orders, stats);
            _repository.SaveOrderPredictions(orderPredictions);
        }

        /// <summary>
        /// Quick simulation run with default parameters
        /// </summary>
        public SimulationRunResult QuickRun(
            int studentId,
            List<int> orderIds,
            double durationHours,
            int randomSeed,
            string dispatchRule = "FIFO",
            List<MachineConfiguration> machineConfigs = null)
        {
            var scenario = new SimulationScenario
            {
                StudentId = studentId,
                ScenarioName = $"Quick Run {DateTime.Now:yyyy-MM-dd HH:mm} | {dispatchRule}",
                SimulationDurationHours = durationHours,
                RandomSeed = randomSeed
            };
            int scenarioId = _repository.CreateScenario(scenario);
            return RunScenario(scenarioId, studentId, orderIds, machineConfigs);
        }

        /// <summary>
        /// Get results for a completed run
        /// </summary>
        public SimulationRunSummary GetRunSummary(int runId)
        {
            var run = _repository.GetRunWithResults(runId);
            if (run == null)
                return null;

            var wcResults = _repository.GetWorkCenterResults(runId);
            var orderPredictions = _repository.GetOrderPredictions(runId);
            var bottleneck = _repository.GetBottleneck(runId);

            return new SimulationRunSummary
            {
                RunId = runId,
                ScenarioName = run.Scenario?.ScenarioName,
                RunDate = run.RunDate,
                Status = run.Status,
                Result = run.Result,
                WorkCenterResults = wcResults,
                OrderPredictions = orderPredictions,
                BottleneckWorkCenter = bottleneck
            };
        }

        /// <summary>
        /// Compare multiple simulation runs
        /// </summary>
        public SimulationComparison CompareRuns(List<int> runIds)
        {
            var summaries = runIds
                .Select(id => GetRunSummary(id))
                .Where(s => s != null)
                .ToList();

            return new SimulationComparison
            {
                Runs = summaries,
                BestThroughput = summaries.OrderByDescending(s => s.Result?.Throughput).FirstOrDefault(),
                BestFlowTime = summaries.OrderBy(s => s.Result?.AvgFlowTimeHours).FirstOrDefault(),
                BestUtilization = summaries.OrderByDescending(s => s.Result?.OverallUtilizationPercent).FirstOrDefault()
            };
        }

        /// <summary>
        /// Get all scenarios for a student
        /// </summary>
        public List<SimulationScenario> GetStudentScenarios(int studentId)
        {
            return _repository.GetScenarios(studentId);
        }

        /// <summary>
        /// Get all runs for a scenario
        /// </summary>
        public List<SimulationRun> GetScenarioRuns(int scenarioId)
        {
            return _repository.GetRunsForScenario(scenarioId);
        }
    }

    #region Result Classes

    /// <summary>
    /// Result of running a simulation
    /// </summary>
    public class SimulationRunResult
    {
        public int RunId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public double DurationSeconds { get; set; }
        public SimulationStatistics Statistics { get; set; }
        public WorkCenter BottleneckWorkCenter { get; set; }
    }

    /// <summary>
    /// Complete summary of a simulation run
    /// </summary>
    public class SimulationRunSummary
    {
        public int RunId { get; set; }
        public string ScenarioName { get; set; }
        public DateTime RunDate { get; set; }
        public string Status { get; set; }
        public SimulationResult Result { get; set; }
        public List<SimulationWorkCenterResult> WorkCenterResults { get; set; }
        public List<SimulationOrderPrediction> OrderPredictions { get; set; }
        public WorkCenter BottleneckWorkCenter { get; set; }
    }

    /// <summary>
    /// Comparison of multiple simulation runs
    /// </summary>
    public class SimulationComparison
    {
        public List<SimulationRunSummary> Runs { get; set; }
        public SimulationRunSummary BestThroughput { get; set; }
        public SimulationRunSummary BestFlowTime { get; set; }
        public SimulationRunSummary BestUtilization { get; set; }
    }

    #endregion
}
