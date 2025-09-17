using System;
using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Answer { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
