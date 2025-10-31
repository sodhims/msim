using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ManufacturingSimulation
{
    /// <summary>
    /// Represents a single task/operation in the Gantt chart
    /// </summary>
    public class GanttTask
    {
        public int PartId { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration => EndTime - StartTime;
        public string Operation { get; set; }
        public string Status { get; set; }

        public override string ToString()
        {
            return $"Part {PartId} on {MachineName}: {StartTime:F2}-{EndTime:F2}";
        }
    }

    /// <summary>
    /// Represents part routing information
    /// </summary>
    public class PartRoute
    {
        public int PartId { get; set; }
        public double ArrivalTime { get; set; }
        public double CompletionTime { get; set; }
        public double TotalTime => CompletionTime - ArrivalTime;
        public List<int> MachineSequence { get; set; } = new List<int>();
        public List<GanttTask> Tasks { get; set; } = new List<GanttTask>();
    }

    /// <summary>
    /// Main data structure for Gantt chart visualization
    /// </summary>
    public class GanttChartData
    {
        public List<GanttTask> Tasks { get; set; } = new List<GanttTask>();
        public List<PartRoute> Routes { get; set; } = new List<PartRoute>();
        public List<int> MachineIds { get; set; } = new List<int>();
        public List<int> PartIds { get; set; } = new List<int>();
        public double MinTime { get; set; }
        public double MaxTime { get; set; }
        public string DataSource { get; set; }

        /// <summary>
        /// Load Gantt chart data from CSV log files
        /// </summary>
        public static GanttChartData LoadFromCsvLogs(string partsLogPath, string eventsLogPath = null, string routingLogPath = null)
        {
            var data = new GanttChartData
            {
                DataSource = Path.GetFileName(partsLogPath)
            };

            // Read parts log - this is the primary source
            if (File.Exists(partsLogPath))
            {
                data.LoadPartsLog(partsLogPath);
            }
            else
            {
                throw new FileNotFoundException($"Parts log file not found: {partsLogPath}");
            }

            // Read routing log if available for additional details
            if (!string.IsNullOrEmpty(routingLogPath) && File.Exists(routingLogPath))
            {
                data.LoadRoutingLog(routingLogPath);
            }

            // Read events log if available for additional context
            if (!string.IsNullOrEmpty(eventsLogPath) && File.Exists(eventsLogPath))
            {
                data.LoadEventsLog(eventsLogPath);
            }

            data.CalculateTimeRange();
            data.ExtractMachineAndPartIds();
            data.BuildRoutes();

            return data;
        }

        private void LoadPartsLog(string path)
        {
            using var reader = new StreamReader(path);
            
            // Skip header
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 5) continue;

                try
                {
                    var task = new GanttTask
                    {
                        PartId = int.Parse(parts[0].Trim()),
                        MachineId = int.Parse(parts[1].Trim()),
                        MachineName = $"Machine {parts[1].Trim()}",
                        StartTime = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        EndTime = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                        Operation = parts.Length > 5 ? parts[5].Trim() : "Process",
                        Status = parts.Length > 4 ? parts[4].Trim() : "Completed"
                    };

                    Tasks.Add(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing parts log line: {line}. Error: {ex.Message}");
                }
            }
        }

        private void LoadRoutingLog(string path)
        {
            // Routing log format: PartId, Step, MachineId, ArrivalTime, StartTime, CompletionTime
            using var reader = new StreamReader(path);
            
            // Skip header
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 6) continue;

                try
                {
                    int partId = int.Parse(parts[0].Trim());
                    int machineId = int.Parse(parts[2].Trim());
                    
                    // Find or create route
                    var route = Routes.FirstOrDefault(r => r.PartId == partId);
                    if (route == null)
                    {
                        route = new PartRoute { PartId = partId };
                        Routes.Add(route);
                    }

                    if (!route.MachineSequence.Contains(machineId))
                    {
                        route.MachineSequence.Add(machineId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing routing log line: {line}. Error: {ex.Message}");
                }
            }
        }

        private void LoadEventsLog(string path)
        {
            // Events log can provide additional context (optional enhancement)
            // Format: Time, EventType, MachineId, PartId, Details
            // This is kept for future expansion
        }

        private void CalculateTimeRange()
        {
            if (Tasks.Count == 0)
            {
                MinTime = 0;
                MaxTime = 0;
                return;
            }

            MinTime = Tasks.Min(t => t.StartTime);
            MaxTime = Tasks.Max(t => t.EndTime);
        }

        private void ExtractMachineAndPartIds()
        {
            MachineIds = Tasks.Select(t => t.MachineId).Distinct().OrderBy(id => id).ToList();
            PartIds = Tasks.Select(t => t.PartId).Distinct().OrderBy(id => id).ToList();
        }

        private void BuildRoutes()
        {
            // Group tasks by part ID to build routes
            var tasksByPart = Tasks.GroupBy(t => t.PartId);

            foreach (var partGroup in tasksByPart)
            {
                var route = Routes.FirstOrDefault(r => r.PartId == partGroup.Key);
                if (route == null)
                {
                    route = new PartRoute { PartId = partGroup.Key };
                    Routes.Add(route);
                }

                // Sort tasks by start time
                var sortedTasks = partGroup.OrderBy(t => t.StartTime).ToList();
                route.Tasks = sortedTasks;

                if (sortedTasks.Count > 0)
                {
                    route.ArrivalTime = sortedTasks.First().StartTime;
                    route.CompletionTime = sortedTasks.Last().EndTime;
                }

                // Build machine sequence if not already loaded
                if (route.MachineSequence.Count == 0)
                {
                    route.MachineSequence = sortedTasks.Select(t => t.MachineId).ToList();
                }
            }
        }

        /// <summary>
        /// Get tasks for a specific machine
        /// </summary>
        public List<GanttTask> GetTasksForMachine(int machineId)
        {
            return Tasks.Where(t => t.MachineId == machineId)
                       .OrderBy(t => t.StartTime)
                       .ToList();
        }

        /// <summary>
        /// Get tasks for a specific part
        /// </summary>
        public List<GanttTask> GetTasksForPart(int partId)
        {
            return Tasks.Where(t => t.PartId == partId)
                       .OrderBy(t => t.StartTime)
                       .ToList();
        }

        /// <summary>
        /// Get route information for a specific part
        /// </summary>
        public PartRoute GetRoute(int partId)
        {
            return Routes.FirstOrDefault(r => r.PartId == partId);
        }

        /// <summary>
        /// Calculate machine utilization
        /// </summary>
        public Dictionary<int, double> CalculateMachineUtilization()
        {
            var utilization = new Dictionary<int, double>();
            double totalTime = MaxTime - MinTime;

            if (totalTime <= 0) return utilization;

            foreach (var machineId in MachineIds)
            {
                var machineTasks = GetTasksForMachine(machineId);
                double busyTime = machineTasks.Sum(t => t.Duration);
                utilization[machineId] = busyTime / totalTime;
            }

            return utilization;
        }

        /// <summary>
        /// Get statistics summary
        /// </summary>
        public GanttStatistics GetStatistics()
        {
            return new GanttStatistics
            {
                TotalParts = PartIds.Count,
                TotalMachines = MachineIds.Count,
                TotalTasks = Tasks.Count,
                SimulationDuration = MaxTime - MinTime,
                AverageTaskDuration = Tasks.Count > 0 ? Tasks.Average(t => t.Duration) : 0,
                MachineUtilization = CalculateMachineUtilization(),
                AverageFlowTime = Routes.Count > 0 ? Routes.Average(r => r.TotalTime) : 0
            };
        }
    }

    /// <summary>
    /// Statistics summary for the Gantt chart data
    /// </summary>
    public class GanttStatistics
    {
        public int TotalParts { get; set; }
        public int TotalMachines { get; set; }
        public int TotalTasks { get; set; }
        public double SimulationDuration { get; set; }
        public double AverageTaskDuration { get; set; }
        public double AverageFlowTime { get; set; }
        public Dictionary<int, double> MachineUtilization { get; set; }

        public override string ToString()
        {
            return $"Parts: {TotalParts}, Machines: {TotalMachines}, Tasks: {TotalTasks}\n" +
                   $"Duration: {SimulationDuration:F2}, Avg Task: {AverageTaskDuration:F2}\n" +
                   $"Avg Flow Time: {AverageFlowTime:F2}";
        }
    }
}
