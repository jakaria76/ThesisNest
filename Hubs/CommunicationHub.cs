using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Hubs
{
    [Authorize]
    public class CommunicationHub : Hub
    {
        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<ApplicationUser> _um;

        // simple per-connection rate-limit for chat (1 msg/sec)
        private static readonly ConcurrentDictionary<string, DateTime> _rl = new();

        public CommunicationHub(ApplicationDbContext ctx, UserManager<ApplicationUser> um)
        {
            _ctx = ctx; _um = um;
        }

        private async Task<bool> IsParticipantAsync(int threadId, string userId, bool mustBeEnabled = true)
        {
            return await _ctx.CommunicationThreads
                .Include(t => t.Teacher).Include(t => t.Student)
                .AnyAsync(t => t.Id == threadId
                               && (!mustBeEnabled || t.IsEnabled)
                               && (t.Teacher.UserId == userId || t.Student.UserId == userId));
        }

        public async Task JoinThread(int threadId)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");

            var allowed = await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true);
            if (!allowed) throw new HubException("Not a participant or not enabled.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"thread-{threadId}");
        }

        // =======================
        // Chat
        // =======================
        public async Task SendMessage(int threadId, string text)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");

            // membership (enabled thread) check
            if (!await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true))
                throw new HubException("Not allowed.");

            // simple rate-limit: 1 msg/sec per connection
            var key = Context.ConnectionId;
            var now = DateTime.UtcNow;
            if (_rl.TryGetValue(key, out var last) && (now - last).TotalSeconds < 1)
                return;
            _rl[key] = now;

            text = (text ?? string.Empty).Trim();
            if (text.Length == 0) return;
            if (text.Length > 2000) text = text.Substring(0, 2000);

            var msg = new ThesisNest.Models.Message
            {
                ThreadId = threadId,
                SenderUserId = me.Id,
                Text = text,
                SentAt = DateTime.UtcNow
            };

            _ctx.Messages.Add(msg);
            await _ctx.SaveChangesAsync();

            var payload = new
            {
                id = msg.Id,
                threadId,
                text = msg.Text,
                sentAt = msg.SentAt,
                senderUserId = msg.SenderUserId,
                senderName = me.FullName ?? me.UserName ?? "User"
            };

            await Clients.Group($"thread-{threadId}").SendAsync("receiveMessage", payload);
        }

        // =======================
        // WebRTC signaling (with membership checks)
        // =======================
        public async Task SendOffer(int threadId, object offer)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");
            if (!await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true))
                throw new HubException("Not allowed.");
            await Clients.Group($"thread-{threadId}").SendAsync("receiveOffer", offer);
        }

        public async Task SendAnswer(int threadId, object answer)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");
            if (!await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true))
                throw new HubException("Not allowed.");
            await Clients.Group($"thread-{threadId}").SendAsync("receiveAnswer", answer);
        }

        public async Task SendIceCandidate(int threadId, object candidate)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");
            if (!await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true))
                throw new HubException("Not allowed.");
            await Clients.Group($"thread-{threadId}").SendAsync("receiveIceCandidate", candidate);
        }

        // =======================
        // Call lifecycle
        // =======================
        public async Task StartCall(int threadId, CommunicationType type)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");

            if (!await IsParticipantAsync(threadId, me.Id, mustBeEnabled: true))
                throw new HubException("Not allowed.");

            var call = new CallSession { ThreadId = threadId, Type = type, StartedByUserId = me.Id };
            _ctx.CallSessions.Add(call);
            await _ctx.SaveChangesAsync();

            await Clients.Group($"thread-{threadId}")
                .SendAsync("callStarted", new { threadId, type, callId = call.Id, startedAt = call.StartedAt });
        }

        public async Task EndCall(int callId)
        {
            var me = await _um.GetUserAsync(Context.User);
            if (me == null) throw new HubException("Unauthorized.");

            // load call + thread and verify membership before ending
            var call = await _ctx.CallSessions
                .Include(c => c.Thread).ThenInclude(t => t.Teacher)
                .Include(c => c.Thread).ThenInclude(t => t.Student)
                .FirstOrDefaultAsync(c => c.Id == callId);

            if (call == null) return;

            var allowed = call.Thread != null
                          && (call.Thread.Teacher.UserId == me.Id || call.Thread.Student.UserId == me.Id);
            if (!allowed) throw new HubException("Not allowed.");

            call.EndedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            await Clients.Group($"thread-{call.ThreadId}")
                .SendAsync("callEnded", new { callId = call.Id, endedAt = call.EndedAt });
        }
    }
}
