using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a component/material used in products
    /// </summary>
    public class Component
    {
        [Key]
        [Column("component_id")]
        public int ComponentId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("component_number")]
        public string ComponentNumber { get; set; }

        [Required]
        [Column("component_name")]
        public string ComponentName { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("unit_cost")]
        public double? UnitCost { get; set; }

        [Column("unit_of_measure")]
        public string UnitOfMeasure { get; set; } = "EA";

        [Column("lead_time_days")]
        public int? LeadTimeDays { get; set; }

        [Column("is_subcontracted")]
        public bool IsSubcontracted { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        public Student Student { get; set; }
    }
}
