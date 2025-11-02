using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a simulation scenario (what-if analysis)
    /// Students create scenarios to test different production plans
    /// </summary>
    public class SimulationScenario
    {
        [Key]
        [Column("scenario_id")]
        public int ScenarioId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("scenario_name")]
        public string ScenarioName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("base_date")]
        public DateTime? BaseDate { get; set; }

        [Column("simulation_duration_hours")]
        public double SimulationDurationHours { get; set; } = 168.0; // 1 week default

        [Column("random_seed")]
        public int RandomSeed { get; set; } = 42;

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Student Student { get; set; }
        public ICollection<SimulationRun> Runs { get; set; } = new List<SimulationRun>();
        
        // Which orders are included in this scenario
        [NotMapped]
        public List<int> OrderIds { get; set; } = new List<int>();
    }
}
