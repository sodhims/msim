using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Predicted completion time for each production order
    /// Helps students see when orders will finish
    /// </summary>
    public class SimulationOrderPrediction
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("run_id")]
        public int RunId { get; set; }

        [Required]
        [Column("production_order_id")]
        public int ProductionOrderId { get; set; }

        [Column("predicted_completion_time")]
        public double? PredictedCompletionTime { get; set; } // Hours from start

        [Column("predicted_flow_time_hours")]
        public double? PredictedFlowTimeHours { get; set; }

        [Column("completed")]
        public bool Completed { get; set; } = false;

        [Column("parts_completed")]
        public int PartsCompleted { get; set; }

        // Navigation properties
        public SimulationRun Run { get; set; }
        public ProductionOrder ProductionOrder { get; set; }
    }
}
