using Microsoft.AspNetCore.SignalR;
using ThesisNest.Services;

namespace ThesisNest.Hubs
{
    public class ChatHub : Hub
    {
        private readonly BackgroundOpenAIQueue _queue;

        public ChatHub(BackgroundOpenAIQueue queue)
        {
            _queue = queue;

            // Subscribe to bot responses
            _queue.BotResponded += async (sender, e) =>
            {
                await Clients.Client(e.ConnectionId)
                             .SendAsync("ReceiveBotMessage", e.Response);
            };
        }

        public Task SendMessage(string user, string message)
        {
            var ts = DateTime.UtcNow;
            var connId = Context.ConnectionId;

            _queue.QueueMessage(new QueuedMessage
            {
                User = user,
                Message = message,
                ConnectionId = connId,
                Timestamp = ts
            });

            return Task.CompletedTask;
        }
    }
}
