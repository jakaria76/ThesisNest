namespace ThesisNest.Services
{
    public class QueuedMessage
    {
        public string User { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class BotResponseEventArgs : EventArgs
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
    }
}
