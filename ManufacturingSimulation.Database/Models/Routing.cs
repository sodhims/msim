using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a routing step (operation) for a product
    /// Defines the sequence of work centers a product goes through
    /// </summary>
    public class Routing
    {
        [Key]
        [Column("routing_id")]
        public int RoutingId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("operation_seq")]
        public int OperationSeq { get; set; }

        [Required]
        [Column("work_center_id")]
        public int WorkCenterId { get; set; }

        [Column("operation_name")]
        public string? OperationName { get; set; }

        [Column("setup_time_minutes")]
        public int? SetupTimeMinutes { get; set; }

        [Column("cycle_time_minutes")]
        public double? CycleTimeMinutes { get; set; }

        [Column("labor_hours")]
        public double? LaborHours { get; set; }

        [Column("setup_time_mean")]
        public double? SetupTimeMean { get; set; }

        [Column("setup_time_std_dev")]
        public double? SetupTimeStdDev { get; set; }

        [Column("setup_time_distribution")]
        public string? SetupTimeDistribution { get; set; } = "Normal";

        [Column("cycle_time_std_dev")]
        public double? CycleTimeStdDev { get; set; }

        [Column("cycle_time_distribution")]
        public string? CycleTimeDistribution { get; set; } = "Normal";

        [Column("batch_size")]
        public int? BatchSize { get; set; } = 1;

        [Column("is_buffer_only")]
        public bool? IsBufferOnly { get; set; } = false;

        // Navigation properties
        public Student Student { get; set; }
        public Product Product { get; set; }
        public WorkCenter WorkCenter { get; set; }
    }
}
