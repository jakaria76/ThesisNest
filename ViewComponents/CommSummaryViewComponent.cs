using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels;

namespace ThesisNest.ViewComponents
{
    public class CommSummaryViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _ctx;
        private readonly UserManager<ApplicationUser> _um;

        public CommSummaryViewComponent(ApplicationDbContext ctx, UserManager<ApplicationUser> um)
        {
            _ctx = ctx; _um = um;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return View(new CommSummaryVm());

            var me = await _um.GetUserAsync(HttpContext.User);
            var teacher = await _ctx.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == me!.Id);
            if (teacher == null) return View(new CommSummaryVm());

            var threads = await _ctx.CommunicationThreads
                .Include(t => t.Student)
                .Where(t => t.TeacherProfileId == teacher.Id && t.IsEnabled)
                .ToListAsync();

            var vm = new CommSummaryVm
            {
                ActiveThreads = threads.Count,
                Threads = threads
                    .Select(t => (t.StudentProfileId, t.Student.FullName ?? "Student"))
                    .ToList()
            };

            var studentUserIds = threads.Select(t => t.Student.UserId).ToHashSet();

            // SMS (chat) — কতজন student পাঠিয়েছে (distinct students)
            vm.SmsStudents = await _ctx.Messages
                .Where(m => threads.Select(t => t.Id).Contains(m.ThreadId)
                            && studentUserIds.Contains(m.SenderUserId))
                .Select(m => m.Thread.StudentProfileId)
                .Distinct()
                .CountAsync();

            // Calls — student শুরু করেছে এমন distinct student count
            vm.AudioStudents = await _ctx.CallSessions
                .Where(c => threads.Select(t => t.Id).Contains(c.ThreadId)
                            && c.Type == CommunicationType.Audio
                            && studentUserIds.Contains(c.StartedByUserId))
                .Select(c => c.Thread.StudentProfileId)
                .Distinct()
                .CountAsync();

            vm.VideoStudents = await _ctx.CallSessions
                .Where(c => threads.Select(t => t.Id).Contains(c.ThreadId)
                            && c.Type == CommunicationType.Video
                            && studentUserIds.Contains(c.StartedByUserId))
                .Select(c => c.Thread.StudentProfileId)
                .Distinct()
                .CountAsync();

            return View(vm);
        }
    }
}
