using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels.Home;

namespace ThesisNest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db,
            UserManager<ApplicationUser> um)
        {
            _logger = logger;
            _db = db;
            _um = um;
        }

        // =========================
        // Role-aware Home (uses HomeVm)
        // =========================
        public async Task<IActionResult> Index()
        {
            var vm = new HomeVm();
            var user = await _um.GetUserAsync(User);
            vm.UserFullName = user?.FullName ?? user?.UserName;

            if (user == null)
            {
                vm.Notices = GetPublicNotices();
                vm.Deadlines = GetPublicDeadlines();
                return View(vm);
            }

            var roles = await _um.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");
            bool isTeacher = roles.Contains("Teacher");
            bool isStudent = roles.Contains("Student");

            vm.Role = isAdmin ? "Admin" : isTeacher ? "Teacher" : isStudent ? "Student" : "User";
            vm.Notices = GetNoticesForAll();
            vm.Deadlines = GetDeadlinesForAll();

            // ----- Student block -----
            if (isStudent)
            {
                var sp = await _db.StudentProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                vm.StudentProfileId = sp?.Id;

                if (sp != null)
                {
                    vm.MyTotalSubmissions = await _db.ThesisVersions
                        .CountAsync(v => v.Thesis.StudentProfileId == sp.Id);

                    var myThesis = await _db.Theses
                        .Include(t => t.Feedbacks)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.StudentProfileId == sp.Id);

                    vm.MyProposalStatus = myThesis?.Status.ToString();
                    vm.MyFeedbackCount = myThesis?.Feedbacks?.Count ?? 0;

                    vm.MyRecentActivities = await _db.ThesisFeedbacks
                        .Where(f => f.Thesis.StudentProfileId == sp.Id)
                        .OrderByDescending(f => f.CreatedAt).Take(5)
                        .Select(f => new ActivityItem(
                            "New feedback",
                            f.Message,
                            f.CreatedAt,
                            Url.Action("Index", "StudentThesis")
                        )).ToListAsync();
                }
            }

            // ----- Teacher block -----
            if (isTeacher)
            {
                var tpId = await _db.TeacherProfiles
                    .Where(t => t.UserId == user.Id)
                    .Select(t => (int?)t.Id).FirstOrDefaultAsync();
                vm.TeacherProfileId = tpId;

                if (tpId != null)
                {
                    vm.PendingReviews = await _db.Theses
                        .CountAsync(t => t.TeacherProfileId == tpId && t.Status == ThesisStatus.Pending);

                    vm.OngoingCount = await _db.Theses
                        .CountAsync(t => t.TeacherProfileId == tpId && t.Status == ThesisStatus.Pending);

                    vm.CompletedCount = await _db.Theses
                        .CountAsync(t => t.TeacherProfileId == tpId && t.Status == ThesisStatus.Accept);

                    vm.TeacherRecent = await _db.ThesisVersions
                        .Where(v => v.Thesis.TeacherProfileId == tpId)
                        .OrderByDescending(v => v.Id).Take(5)
                        .Select(v => new ActivityItem(
                            "New submission",
                            v.FileName,
                            v.CreatedAt,
                            Url.Action("Index", "ThesisReview")
                        )).ToListAsync();
                }
            }

            // ----- Admin block -----
            if (isAdmin)
            {
                vm.TotalStudents = await _db.StudentProfiles.CountAsync();
                vm.TotalTeachers = await _db.TeacherProfiles.CountAsync();

                var weekAgo = DateTime.UtcNow.AddDays(-7);
                vm.ProposalsThisWeek = await _db.ThesisVersions.CountAsync(v => v.CreatedAt >= weekAgo);

                vm.SiteRecent = await _db.ThesisVersions
                    .OrderByDescending(v => v.Id).Take(7)
                    .Select(v => new ActivityItem(
                        "Uploaded proposal",
                        v.FileName,
                        v.CreatedAt,
                        Url.Action("Index", "AdminProposal")
                    )).ToListAsync();
            }

            return View(vm);
        }

        // =========================
        // Privacy (Admin/Teacher only)
        // =========================
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // Error
        // =========================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ===== Notices/Deadlines helpers (replace with DB later) =====
        private List<NoticeItem> GetPublicNotices() => new()
        {
            new("Welcome to ThesisNest", Url.Action("Login","Account"), null)
        };
        private List<NoticeItem> GetNoticesForAll() => GetPublicNotices();

        private List<DeadlineItem> GetPublicDeadlines() => new();
        private List<DeadlineItem> GetDeadlinesForAll() => new();
    }
}
