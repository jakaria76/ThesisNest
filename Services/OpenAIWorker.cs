using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThesisNest.Data;
using ThesisNest.Hubs;
using ThesisNest.Models;

namespace ThesisNest.Services
{
    public class OpenAIWorker : BackgroundService
    {
        private readonly BackgroundOpenAIQueue _queue;
        private readonly IHubContext<ChatHub> _hub;
        private readonly ILogger<OpenAIWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public OpenAIWorker(
            BackgroundOpenAIQueue queue,
            IHubContext<ChatHub> hub,
            ILogger<OpenAIWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _queue = queue;
            _hub = hub;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OpenAIWorker (Groq mode) started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                QueuedMessage? item = null;

                try
                {
                    item = await _queue.DequeueAsync(stoppingToken);
                    if (item == null) continue;

                    var hasClient = !string.IsNullOrEmpty(item.ConnectionId);

                    if (hasClient)
                    {
                        try
                        {
                            await _hub.Clients.Client(item.ConnectionId!)
                                .SendAsync("BotTyping", true, cancellationToken: stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "BotTyping(true) send failed (ignored).");
                        }
                    }

                    // ----------------------------
                    // LLM reply via GroqService
                    // ----------------------------
                    string reply;
                    var ts = DateTime.UtcNow;

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var llm = scope.ServiceProvider.GetRequiredService<GroqService>();

                        var r = await llm.AskAsync(item.Message, stoppingToken);

                        reply = r.Ok
                            ? (string.IsNullOrWhiteSpace(r.Text) ? "(empty)" : r.Text)
                            : $"[Groq {r.Status} {r.Reason}] {(r.ErrorBody ?? "")}\nURL: {r.Url}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Groq call failed.");
                        reply = "দুঃখিত, এখন বট উত্তর দিতে পারছে না। একটু পর চেষ্টা করুন।";
                    }

                    // ----------------------------
                    // SignalR: send message to client(s)
                    // ----------------------------
                    try
                    {
                        if (hasClient)
                        {
                            await _hub.Clients.Client(item.ConnectionId!)
                                .SendAsync("ReceiveMessage", "Bot", reply, ts, cancellationToken: stoppingToken);
                        }
                        else
                        {
                            await _hub.Clients.All
                                .SendAsync("ReceiveMessage", "Bot", reply, ts, cancellationToken: stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send bot message via SignalR.");
                    }

                    // ----------------------------
                    // Save chat message to DB
                    // ----------------------------
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        db.ChatMessages.Add(new ChatMessage
                        {
                            User = "Bot",
                            Message = reply,
                            Timestamp = ts,
                            FromBot = true
                        });
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to save bot chat message (non-fatal).");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop error");
                    await Task.Delay(1000, stoppingToken);
                }
                finally
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item?.ConnectionId))
                        {
                            await _hub.Clients.Client(item!.ConnectionId!)
                                .SendAsync("BotTyping", false, cancellationToken: stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "BotTyping(false) send failed (ignored).");
                    }
                }
            }

            _logger.LogInformation("OpenAIWorker (Groq mode) stopped.");
        }
    }
}
