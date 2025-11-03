using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    [Table("machine_locations")]
    public class MachineLocation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("machine_id")]
        public int MachineId { get; set; }

        [Column("x_coordinate")]
        public double XCoordinate { get; set; }

        [Column("y_coordinate")]
        public double YCoordinate { get; set; }

        [Column("rotation")]
        public int Rotation { get; set; }

        [Column("floor_number")]
        public int FloorNumber { get; set; }

        [Column("department")]
        [MaxLength(50)]
        public string Department { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("MachineId")]
        public virtual Machine Machine { get; set; }
    }
}
