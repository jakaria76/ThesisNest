using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class ThesisReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ThesisReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ===== List all theses =====
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            var thesesQuery = _context.Theses
                .Include(t => t.Student)
                .Include(t => t.Department)
                .Include(t => t.Versions)
                .Include(t => t.Feedbacks)
                    .ThenInclude(f => f.GivenBy)
                .AsNoTracking();

            if (!isAdmin)
            {
                int? teacherId = await _context.TeacherProfiles
                    .Where(p => p.UserId == user.Id)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync();

                if (teacherId == null) return Forbid();

                thesesQuery = thesesQuery.Where(t => t.TeacherProfileId == teacherId.Value);
            }

            var theses = await thesesQuery.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(theses);
        }

        // ===== Download File =====
        [HttpGet]
        public async Task<IActionResult> DownloadVersion(int versionId)
        {
            var version = await _context.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            int? teacherId = await _context.TeacherProfiles
                .Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (!isAdmin && (teacherId == null || version.Thesis.TeacherProfileId != teacherId.Value))
                return Forbid();

            return File(version.FileData, version.ContentType, version.FileName);
        }

        // ===== Preview File =====
        [HttpGet]
        public async Task<IActionResult> PreviewVersion(int versionId)
        {
            var version = await _context.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null || version.FileData == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index");
            }

            string fileType;
            if (version.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileType = "pdf";
                string base64String = Convert.ToBase64String(version.FileData);
                ViewBag.FileUrl = $"data:application/pdf;base64,{base64String}";
            }
            else if (version.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
                     version.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                fileType = "doc";
                ViewBag.FileUrl = Url.Action("RenderFileInline", "ThesisReview", new { versionId = version.Id });
            }
            else
            {
                fileType = "other";
            }

            ViewBag.PreviewType = fileType;
            ViewBag.ThesisId = version.ThesisId;
            ViewBag.ThesisVersionId = version.Id;

            return View("Preview");
        }

        // ===== Render DOC/DOCX inline =====
        [HttpGet]
        public async Task<IActionResult> RenderFileInline(int versionId)
        {
            var version = await _context.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null || version.FileData == null)
                return NotFound();

            return File(version.FileData, version.ContentType, version.FileName, enableRangeProcessing: true);
        }

        // ===== Update Thesis Status =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int thesisId, ThesisStatus status)
        {
            var thesis = await _context.Theses.FindAsync(thesisId);
            if (thesis == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            int? teacherId = await _context.TeacherProfiles
                .Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (!isAdmin && (teacherId == null || thesis.TeacherProfileId != teacherId.Value))
                return Forbid();

            thesis.Status = status;
            thesis.CompletedAt = status == ThesisStatus.Completed ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thesis status updated!";
            return RedirectToAction(nameof(Index));
        }

        // ===== Add Feedback =====
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeedback(int thesisId, string message, bool requestChanges = false)
        {
            var thesis = await _context.Theses.FindAsync(thesisId);
            if (thesis == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            int? teacherId = await _context.TeacherProfiles
                .Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (!isAdmin && (teacherId == null || thesis.TeacherProfileId != teacherId.Value))
                return Forbid();

            var feedback = new ThesisFeedback
            {
                ThesisId = thesisId,
                GivenByTeacherProfileId = teacherId.Value,
                Message = message,
                IsChangeRequested = requestChanges
            };

            _context.ThesisFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Feedback submitted!";
            return RedirectToAction(nameof(Index));
        }
        
    }
}
