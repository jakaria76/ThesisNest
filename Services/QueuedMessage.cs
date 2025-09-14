using System;

namespace ThesisNest.Services
{
    public class QueuedMessage
    {
        public string User { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ConnectionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
