using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ManufacturingSimulation.Database.Models;

namespace ManufacturingSimulation.Database.Repositories
{
    /// <summary>
    /// Repository for loading MES data and saving simulation results
    /// Handles all database operations for simulation integration
    /// </summary>
    public class SimulationRepository
    {
        private readonly MesDbContext _context;

        public SimulationRepository(MesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Load MES Data

        /// <summary>
        /// Load all active work centers for a student
        /// </summary>
        public List<WorkCenter> GetWorkCenters(int studentId)
        {
            return _context.WorkCenters
                .Where(wc => wc.StudentId == studentId)
                .OrderBy(wc => wc.WorkCenterCode)
                .ToList();
        }

        /// <summary>
        /// Load specific work center by ID
        /// </summary>
        public WorkCenter GetWorkCenter(int workCenterId)
        {
            return _context.WorkCenters
                .FirstOrDefault(wc => wc.WorkCenterId == workCenterId);
        }

        /// <summary>
        /// Load production orders for simulation
        /// </summary>
        public List<ProductionOrder> GetProductionOrders(int studentId, List<int> orderIds)
        {
            return _context.ProductionOrders
                .Include(po => po.Product)
                    .ThenInclude(p => p.Routings)
                        .ThenInclude(r => r.WorkCenter)
                .Where(po => po.StudentId == studentId && orderIds.Contains(po.OrderId))
                .ToList();
        }

        /// <summary>
        /// Load all production orders for a student
        /// </summary>
        public List<ProductionOrder> GetAllProductionOrders(int studentId)
        {
            return _context.ProductionOrders
                .Include(po => po.Product)
                .Where(po => po.StudentId == studentId)
                .OrderBy(po => po.Priority)
                .ThenBy(po => po.DueDate)
                .ToList();
        }

        /// <summary>
        /// Load routings for a product
        /// </summary>
        public List<Routing> GetRoutings(int productId)
        {
            return _context.Routings
                .Include(r => r.WorkCenter)
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.OperationSeq)
                .ToList();
        }

        #endregion

        #region Scenario Management

        /// <summary>
        /// Create a new simulation scenario
        /// </summary>
        public int CreateScenario(SimulationScenario scenario)
        {
            _context.SimulationScenarios.Add(scenario);
            _context.SaveChanges();
            return scenario.ScenarioId;
        }

        /// <summary>
        /// Get a simulation scenario by ID
        /// </summary>
        public SimulationScenario GetScenario(int scenarioId)
        {
            return _context.SimulationScenarios
                .FirstOrDefault(s => s.ScenarioId == scenarioId);
        }

        /// <summary>
        /// Get all scenarios for a student
        /// </summary>
        public List<SimulationScenario> GetScenarios(int studentId)
        {
            return _context.SimulationScenarios
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.CreatedDate)
                .ToList();
        }

        /// <summary>
        /// Delete a scenario and all its runs
        /// </summary>
        public void DeleteScenario(int scenarioId)
        {
            var scenario = _context.SimulationScenarios
                .Include(s => s.Runs)
                .FirstOrDefault(s => s.ScenarioId == scenarioId);

            if (scenario != null)
            {
                _context.SimulationScenarios.Remove(scenario);
                _context.SaveChanges();
            }
        }

        #endregion

        #region Simulation Execution

        /// <summary>
        /// Create a new simulation run
        /// </summary>
        public int CreateRun(int scenarioId, int studentId, string configJson = null)
        {
            var run = new SimulationRun
            {
                ScenarioId = scenarioId,
                StudentId = studentId,
                Status = "Running",
                ConfigJson = configJson,
                RunDate = DateTime.UtcNow
            };

            _context.SimulationRuns.Add(run);
            _context.SaveChanges();
            return run.RunId;
        }

        /// <summary>
        /// Update run status
        /// </summary>
        public void UpdateRunStatus(int runId, string status, double? durationSeconds = null, string errorMessage = null)
        {
            var run = _context.SimulationRuns.Find(runId);
            if (run != null)
            {
                run.Status = status;
                run.DurationSeconds = durationSeconds;
                run.ErrorMessage = errorMessage;
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Save overall simulation results
        /// </summary>
        public void SaveResults(SimulationResult result)
        {
            _context.SimulationResults.Add(result);
            _context.SaveChanges();
        }

        /// <summary>
        /// Save work center results
        /// </summary>
        public void SaveWorkCenterResults(List<SimulationWorkCenterResult> results)
        {
            _context.SimulationWorkCenterResults.AddRange(results);
            _context.SaveChanges();
        }

        /// <summary>
        /// Save order predictions
        /// </summary>
        public void SaveOrderPredictions(List<SimulationOrderPrediction> predictions)
        {
            _context.SimulationOrderPredictions.AddRange(predictions);
            _context.SaveChanges();
        }

        #endregion

        #region Query Results

        /// <summary>
        /// Get simulation run with all results
        /// </summary>
        public SimulationRun GetRunWithResults(int runId)
        {
            return _context.SimulationRuns
                .Include(r => r.Result)
                .Include(r => r.Scenario)
                .FirstOrDefault(r => r.RunId == runId);
        }

        /// <summary>
        /// Get work center results for a run
        /// </summary>
        public List<SimulationWorkCenterResult> GetWorkCenterResults(int runId)
        {
            return _context.SimulationWorkCenterResults
                .Include(wcr => wcr.WorkCenter)
                .Where(wcr => wcr.RunId == runId)
                .OrderByDescending(wcr => wcr.UtilizationPercent)
                .ToList();
        }

        /// <summary>
        /// Get order predictions for a run
        /// </summary>
        public List<SimulationOrderPrediction> GetOrderPredictions(int runId)
        {
            return _context.SimulationOrderPredictions
                .Include(op => op.ProductionOrder)
                    .ThenInclude(po => po.Product)
                .Where(op => op.RunId == runId)
                .OrderBy(op => op.PredictedCompletionTime)
                .ToList();
        }

        /// <summary>
        /// Get all runs for a scenario
        /// </summary>
        public List<SimulationRun> GetRunsForScenario(int scenarioId)
        {
            return _context.SimulationRuns
                .Include(r => r.Result)
                .Where(r => r.ScenarioId == scenarioId)
                .OrderByDescending(r => r.RunDate)
                .ToList();
        }

        /// <summary>
        /// Get bottleneck work center from a run
        /// </summary>
        public WorkCenter GetBottleneck(int runId)
        {
            var bottleneck = _context.SimulationWorkCenterResults
                .Include(wcr => wcr.WorkCenter)
                .Where(wcr => wcr.RunId == runId)
                .OrderByDescending(wcr => wcr.UtilizationPercent)
                .FirstOrDefault();

            return bottleneck?.WorkCenter;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get student's simulation statistics
        /// </summary>
        public (int TotalRuns, int CompletedRuns, DateTime? LastRun) GetStudentStats(int studentId)
        {
            var runs = _context.SimulationRuns
                .Where(r => r.StudentId == studentId)
                .ToList();

            return (
                TotalRuns: runs.Count,
                CompletedRuns: runs.Count(r => r.Status == "Completed"),
                LastRun: runs.Any() ? runs.Max(r => r.RunDate) : (DateTime?)null
            );
        }

        #endregion
    }
}
