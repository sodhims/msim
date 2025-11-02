using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a finished product in the manufacturing system
    /// </summary>
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("product_number")]
        public string ProductNumber { get; set; }

        [Required]
        [Column("product_name")]
        public string ProductName { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("standard_cost")]
        public double? StandardCost { get; set; }

        [Column("selling_price")]
        public double? SellingPrice { get; set; }

        [Column("bom_levels")]
        public int? BomLevels { get; set; }

        [Column("assembly_time_minutes")]
        public int? AssemblyTimeMinutes { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Student Student { get; set; }
        public ICollection<Routing> Routings { get; set; } = new List<Routing>();
    }
}
