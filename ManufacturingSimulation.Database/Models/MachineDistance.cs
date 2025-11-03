using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    [Table("machine_distances")]
    public class MachineDistance
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("from_machine_id")]
        public int FromMachineId { get; set; }

        [Required]
        [Column("to_machine_id")]
        public int ToMachineId { get; set; }

        [Column("distance_meters")]
        public double DistanceMeters { get; set; }

        [Column("travel_time_seconds")]
        public double TravelTimeSeconds { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
