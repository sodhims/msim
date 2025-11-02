using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Overall simulation results for a run
    /// High-level performance metrics
    /// </summary>
    public class SimulationResult
    {
        [Key]
        [Column("result_id")]
        public int ResultId { get; set; }

        [Required]
        [Column("run_id")]
        public int RunId { get; set; }

        [Column("simulated_time_hours")]
        public double SimulatedTimeHours { get; set; }

        [Column("throughput")]
        public double Throughput { get; set; } // Parts per hour

        [Column("avg_flow_time_hours")]
        public double AvgFlowTimeHours { get; set; }

        [Column("avg_wip")]
        public double AvgWip { get; set; }

        [Column("total_parts_arrived")]
        public int TotalPartsArrived { get; set; }

        [Column("total_parts_completed")]
        public int TotalPartsCompleted { get; set; }

        [Column("overall_utilization_percent")]
        public double OverallUtilizationPercent { get; set; }

        // Navigation property
        public SimulationRun Run { get; set; }
    }
}
