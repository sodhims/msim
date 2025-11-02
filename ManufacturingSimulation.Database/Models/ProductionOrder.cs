using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a production order (work order)
    /// NOTE: Add this table to your MES database schema if not present
    /// </summary>
    public class ProductionOrder
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("order_number")]
        public string OrderNumber { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("priority")]
        public int Priority { get; set; } = 1;

        [Column("status")]
        public string Status { get; set; } = "Planned"; // Planned, Released, InProgress, Completed

        [Column("release_date")]
        public DateTime? ReleaseDate { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("completion_date")]
        public DateTime? CompletionDate { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Student Student { get; set; }
        public Product Product { get; set; }
    }
}
