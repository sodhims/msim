using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a work center (machine/workstation) in the manufacturing system
    /// Maps to work_centers table in MES database
    /// </summary>
    public class WorkCenter
    {
        [Key]
        [Column("work_center_id")]
        public int WorkCenterId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("work_center_code")]
        public string WorkCenterCode { get; set; }

        [Required]
        [Column("work_center_name")]
        public string WorkCenterName { get; set; }

        [Column("wc_type_id")]
        public int? WcTypeId { get; set; }

        [Column("capacity_units_per_hour")]
        public double? CapacityUnitsPerHour { get; set; }

        [Column("operating_cost_per_hour")]
        public double? OperatingCostPerHour { get; set; }

        [Column("setup_time_minutes")]
        public int SetupTimeMinutes { get; set; } = 15;

        [Column("availability_percent")]
        public double AvailabilityPercent { get; set; } = 90.0;

        [Column("investment_cost")]
        public double? InvestmentCost { get; set; }

        [Column("depreciation_years")]
        public int? DepreciationYears { get; set; }

        [Column("requires_operator")]
        public bool RequiresOperator { get; set; } = false;

        [Column("is_bottleneck")]
        public bool IsBottleneck { get; set; } = false;

        // Navigation property
        public Student Student { get; set; }
    }
}
