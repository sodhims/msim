using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    [Table("machines")]
    public class Machine
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("machine_type")]
        [MaxLength(50)]
        public string MachineType { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; }

        [Column("buffer_capacity")]
        public int BufferCapacity { get; set; }

        [Column("processing_rate")]
        public double ProcessingRate { get; set; }
    }
}
