using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ThreadId { get; set; }
        public CommunicationThread Thread { get; set; } = default!;

        [Required, MaxLength(4000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string SenderUserId { get; set; } = string.Empty;
        public ApplicationUser? Sender { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
