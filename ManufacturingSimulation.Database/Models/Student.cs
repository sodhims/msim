using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingSimulation.Database.Models
{
    /// <summary>
    /// Represents a student/user in the MES training system
    /// </summary>
    public class Student
    {
        [Key]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Required]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Required]
        [Column("full_name")]
        public string FullName { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("role")]
        public string Role { get; set; } = "Student";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }
    }
}
