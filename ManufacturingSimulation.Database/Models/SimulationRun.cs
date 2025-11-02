using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a single execution of a simulation scenario
    /// Tracks when simulation was run and its status
    /// </summary>
    public class SimulationRun
    {
        [Key]
        [Column("run_id")]
        public int RunId { get; set; }

        [Required]
        [Column("scenario_id")]
        public int ScenarioId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("run_date")]
        public DateTime RunDate { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed

        [Column("duration_seconds")]
        public double? DurationSeconds { get; set; }

        [Column("config_json")]
        public string? ConfigJson { get; set; } // Snapshot of configuration used

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public SimulationScenario Scenario { get; set; }
        public Student Student { get; set; }
        public SimulationResult Result { get; set; }
    }
}
