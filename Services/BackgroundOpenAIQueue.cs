using System.Collections.Concurrent;

namespace ThesisNest.Services
{
    public class BackgroundOpenAIQueue
    {
        private readonly ConcurrentQueue<QueuedMessage> _messages = new();
        private readonly SemaphoreSlim _signal = new(0);

        public event EventHandler<BotResponseEventArgs>? BotResponded;

        public void QueueMessage(QueuedMessage msg)
        {
            _messages.Enqueue(msg);
            _signal.Release();
        }

        public async Task<QueuedMessage> DequeueAsync(CancellationToken token)
        {
            await _signal.WaitAsync(token);
            _messages.TryDequeue(out var msg);
            return msg!;
        }

        public void RaiseBotResponse(string connectionId, string response)
        {
            BotResponded?.Invoke(this, new BotResponseEventArgs
            {
                ConnectionId = connectionId,
                Response = response
            });
        }
    }
}
