using Microsoft.AspNetCore.SignalR;
using ThesisNest.Services;
using System;
using System.Threading.Tasks;

namespace ThesisNest.Hubs
{
    public class ChatHub : Hub
    {
        private readonly BackgroundOpenAIQueue _queue;
        public ChatHub(BackgroundOpenAIQueue queue) => _queue = queue;

        public Task SendMessage(string user, string message)
        {
            var ts = DateTime.UtcNow;
            var connId = Context.ConnectionId; // SignalR এ এটা non-null ই থাকে

            _queue.QueueMessage(new QueuedMessage
            {
                User = user,
                Message = message,
                ConnectionId = connId,
                Timestamp = ts
            });

            // আর এখানে Clients.Caller.ReceiveMessage পাঠাবো না (double message এড়াতে)
            return Task.CompletedTask;
        }
    }
}
