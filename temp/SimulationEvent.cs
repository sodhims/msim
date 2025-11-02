using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Individual simulation event - logged during simulation execution
    /// </summary>
    [Table("simulation_events")]
    public class SimulationEvent
    {
        [Key]
        [Column("event_id")]
        public int EventId { get; set; }

        [Required]
        [Column("run_id")]
        public int RunId { get; set; }

        [Required]
        [Column("event_time")]
        public double EventTime { get; set; }

        [Required]
        [Column("event_type")]
        [MaxLength(50)]
        public string EventType { get; set; }

        [Column("part_id")]
        [MaxLength(100)]
        public string PartId { get; set; }

        [Column("order_number")]
        [MaxLength(100)]
        public string OrderNumber { get; set; }

        [Column("machine_name")]
        [MaxLength(200)]
        public string MachineName { get; set; }

        [Column("queue_size")]
        public int QueueSize { get; set; }

        [Column("details")]
        public string Details { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public SimulationRun Run { get; set; }
    }
}
