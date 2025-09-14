using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ThesisNest.Services
{
    public class BackgroundOpenAIQueue
    {
        private readonly ConcurrentQueue<QueuedMessage> _messages = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void QueueMessage(QueuedMessage message)
        {
            _messages.Enqueue(message);
            _signal.Release();
        }

        public async Task<QueuedMessage> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _messages.TryDequeue(out var message);
            return message!;
        }
    }
}
