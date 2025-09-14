using System;
using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string User { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool FromBot { get; set; } = false;
    }
}
