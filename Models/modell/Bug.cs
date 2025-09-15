using System;
using System.ComponentModel.DataAnnotations;

namespace Thesiss.Models.modell
{
    public class Bug
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Severity { get; set; }   // Low, Medium, High

        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open / InProgress / Closed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
