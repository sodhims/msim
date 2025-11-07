using System;
using System.Collections.Generic;

namespace ManufacturingSimulation.Bridge.Models
{
    /// <summary>
    /// Data model for Gantt chart visualization
    /// </summary>
    public class GanttData
    {
        public int RunId { get; set; }
        public DateTime SimulationDate { get; set; }
        public List<GanttTask> Tasks { get; set; } = new List<GanttTask>();
        public List<string> UniqueMachines { get; set; } = new List<string>();
        public double Makespan { get; set; }
    }

    /// <summary>
    /// Represents a single task on the Gantt chart
    /// </summary>
    public class GanttTask
    {
        public string PartId { get; set; }
        public string MachineName { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        
        /// <summary>
        /// Calculated duration of the task
        /// </summary>
        public double Duration => EndTime - StartTime;
    }
}
