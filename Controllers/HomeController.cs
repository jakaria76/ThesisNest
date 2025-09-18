using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels.Home;

namespace ThesisNest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;

        public HomeController(ApplicationDbContext db, UserManager<ApplicationUser> um)
        {
            _db = db;
            _um = um;
        }

        // ================= HOME / DASHBOARD =================
        public async Task<IActionResult> Index()
        {
            var vm = new HomeVm();

            // Get current user
            var user = await _um.GetUserAsync(User);
            vm.UserFullName = user?.FullName ?? user?.UserName;

            // Load FAQs
            vm.FAQs = await _db.FAQs.AsNoTracking()
                .Select(f => new FAQ { Id = f.Id, Question = f.Question, Answer = f.Answer })
                .ToListAsync();

            if (user == null)
            {
                // Guest view
                vm.Notices = GetPublicNotices();
                vm.Deadlines = GetPublicDeadlines();
                return View(vm);
            }

            // Roles
            var roles = await _um.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");
            bool isTeacher = roles.Contains("Teacher");
            bool isStudent = roles.Contains("Student");

            vm.Role = isAdmin ? "Admin" : isTeacher ? "Teacher" : isStudent ? "Student" : "User";

            vm.Notices = GetNoticesForAll();
            vm.Deadlines = GetDeadlinesForAll();

            // ================= Student =================
            if (isStudent)
            {
                var sp = await _db.StudentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                vm.StudentProfileId = sp?.Id;

                if (sp != null)
                {
                    var thesisCount = await _db.Theses
                        .AsNoTracking()
                        .CountAsync(t => t.StudentProfileId == sp.Id);

                    var rawSubmissionCount = await _db.ThesisSubmissions
                        .AsNoTracking()
                        .CountAsync(ts => ts.StudentId == user.Id);

                    vm.MyTotalSubmissions = thesisCount > 0 ? thesisCount : rawSubmissionCount;

                    var latestThesis = await _db.Theses
                        .AsNoTracking()
                        .Where(t => t.StudentProfileId == sp.Id)
                        .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (latestThesis != null)
                    {
                        vm.MyProposalStatus = latestThesis.Status.ToString();
                    }
                    else
                    {
                        var latestSubmission = await _db.ThesisSubmissions
                            .AsNoTracking()
                            .Where(ts => ts.StudentId == user.Id)
                            .OrderByDescending(ts => ts.SubmissionDate)
                            .FirstOrDefaultAsync();

                        vm.MyProposalStatus = string.IsNullOrWhiteSpace(latestSubmission?.Status)
                            ? "Submitted"
                            : latestSubmission!.Status!;
                    }

                    vm.MyFeedbackCount = await _db.Theses
                        .AsNoTracking()
                        .Where(t => t.StudentProfileId == sp.Id)
                        .SelectMany(t => t.Feedbacks)
                        .CountAsync();

                    // ------------- FIX: bring to memory, then convert to UTC DateTime -------------
                    var recentFeedbackRaw = await _db.ThesisFeedbacks
                        .AsNoTracking()
                        .Where(f => f.Thesis.StudentProfileId == sp.Id)
                        .OrderByDescending(f => f.CreatedAt)
                        .Take(5)
                        .Select(f => new { f.Message, f.CreatedAt })
                        .ToListAsync();

                    vm.MyRecentActivities = recentFeedbackRaw
                        .Select(f => new ActivityItem(
                            "New feedback",
                            f.Message,
                            AsUtc(f.CreatedAt),                                  // ✅ DateTimeOffset → DateTime(UTC)
                            Url.Action("Index", "StudentThesis")
                        ))
                        .ToList();
                }
            }

            // ================= Teacher =================
            if (isTeacher)
            {
                var tpId = await _db.TeacherProfiles
                    .AsNoTracking()
                    .Where(t => t.UserId == user.Id)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync();

                vm.TeacherProfileId = tpId;

                if (tpId.HasValue)
                {
                    var teacherId = tpId.Value;

                    vm.PendingReviews = await _db.Theses
                        .AsNoTracking()
                        .CountAsync(t => t.TeacherProfileId == teacherId &&
                                         t.Status == ThesisStatus.Pending);

                    vm.CompletedCount = await _db.Theses
                        .AsNoTracking()
                        .CountAsync(t => t.TeacherProfileId == teacherId &&
                                         t.Status == ThesisStatus.Accept);

                    vm.OngoingCount = vm.PendingReviews;

                    // ------------- FIX: bring to memory, then convert to UTC DateTime -------------
                    var latestVersions = await _db.ThesisVersions
                        .AsNoTracking()
                        .Where(v => v.Thesis.TeacherProfileId == teacherId)
                        .OrderByDescending(v => v.CreatedAt)
                        .Take(5)
                        .Select(v => new
                        {
                            ThesisTitle = v.Thesis.Title,
                            v.FileName,
                            v.CreatedAt,
                            v.Id
                        })
                        .ToListAsync();

                    vm.TeacherRecent = latestVersions
                        .Select(x => new ActivityItem(
                            "New submission",
                            (x.ThesisTitle ?? "Untitled") +
                                (string.IsNullOrWhiteSpace(x.FileName) ? "" : $" — {x.FileName}"),
                            AsUtc(x.CreatedAt),                                  // ✅ safe conversion
                            Url.Action("PreviewVersion", "ThesisReview", new { id = x.Id })
                        ))
                        .ToList();
                }
            }

            // ================= Admin =================
            if (isAdmin)
            {
                vm.TotalStudents = await _db.StudentProfiles.CountAsync();
                vm.TotalTeachers = await _db.TeacherProfiles.CountAsync();

                var weekAgo = DateTime.UtcNow.AddDays(-7);
                vm.ProposalsThisWeek = await _db.ThesisVersions.CountAsync(v => v.CreatedAt >= weekAgo);

                // ------------- FIX: bring to memory, then convert to UTC DateTime -------------
                var siteRecentRaw = await _db.ThesisVersions
                    .OrderByDescending(v => v.Id)
                    .Take(7)
                    .Select(v => new { v.FileName, v.CreatedAt })
                    .ToListAsync();

                vm.SiteRecent = siteRecentRaw
                    .Select(v => new ActivityItem(
                        "Uploaded proposal",
                        v.FileName,
                        AsUtc(v.CreatedAt),                                      // ✅ safe conversion
                        Url.Action("Index", "AdminProposal")
                    ))
                    .ToList();
            }

            return View(vm);
        }

        // ================= PRIVACY =================
        public IActionResult Privacy() => View();

        // ===== Helper methods =====
        private List<NoticeItem> GetPublicNotices() => new()
        {
            new("Welcome to ThesisNest", Url.Action("Login", "Account"), null)
        };

        private List<NoticeItem> GetNoticesForAll() => GetPublicNotices();
        private List<DeadlineItem> GetPublicDeadlines() => new();
        private List<DeadlineItem> GetDeadlinesForAll() => new();

        // ====== DateTime helpers (always return UTC DateTime) ======
        private static DateTime AsUtc(DateTime dt) =>
            dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        private static DateTime AsUtc(DateTimeOffset dto) => dto.UtcDateTime;
    }
}
