using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Per-work-center simulation results
    /// Shows performance and bottleneck analysis for each machine
    /// </summary>
    public class SimulationWorkCenterResult
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("run_id")]
        public int RunId { get; set; }

        [Required]
        [Column("work_center_id")]
        public int WorkCenterId { get; set; }

        [Column("utilization_percent")]
        public double UtilizationPercent { get; set; }

        [Column("parts_processed")]
        public int PartsProcessed { get; set; }

        [Column("avg_queue_size")]
        public double AvgQueueSize { get; set; }

        [Column("max_queue_size")]
        public int MaxQueueSize { get; set; }

        [Column("avg_wait_time_hours")]
        public double AvgWaitTimeHours { get; set; }

        [Column("is_bottleneck")]
        public bool IsBottleneck { get; set; } = false;

        // Navigation properties
        public SimulationRun Run { get; set; }
        public WorkCenter WorkCenter { get; set; }
    }
}
