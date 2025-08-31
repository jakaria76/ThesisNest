using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentThesisController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentThesisController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ===== Helpers =====
        private async Task<int?> GetMyStudentProfileIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            var sp = await _db.StudentProfiles.AsNoTracking()
                         .FirstOrDefaultAsync(s => s.UserId == user.Id);
            return sp?.Id;
        }

        private static bool IsAllowedFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext is ".pdf" or ".docx";
        }

        // ===== My Submissions (Index) =====
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            var query = _db.Theses.AsNoTracking()
                .Include(t => t.Department)
                .Where(t => t.StudentProfileId == myId.Value)
                .OrderByDescending(t => t.CreatedAt);

            var items = await query
                .Select(t => new ThesisListItemVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Department = t.Department.Name,
                    ProposalStatus = t.ProposalStatus,
                    CurrentVersionNo = t.CurrentVersionNo,
                    CreatedAtStr = t.CreatedAt.ToLocalTime().ToString("dd MMM yyyy")
                })
                .ToListAsync();

            var vm = new ThesisIndexVm
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = items.Count
            };

            return View(vm); // Views/StudentThesis/Index.cshtml
        }

        // ===== Create (GET) =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ThesisCreateVm
            {
                Departments = await _db.Departments.OrderBy(d => d.Name)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = d.Id.ToString(), Text = d.Name }).ToListAsync(),

                // NOTE: তুমি যদি Supervisor টেক্সট ফিল্ড ব্যবহার করো, নিচের লাইনটা দরকার নেই।
                Supervisors = await _db.TeacherProfiles.OrderBy(t => t.FullName)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = t.Id.ToString(), Text = t.FullName }).ToListAsync()
            };
            return View(vm); // Views/StudentThesis/Create.cshtml
        }

        // ===== Create (POST) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Create(ThesisCreateVm vm, string submit)
        {
            // File validation
            if (vm.File == null)
                ModelState.AddModelError(nameof(vm.File), "File is required.");
            else
            {
                if (!IsAllowedFile(vm.File.FileName))
                    ModelState.AddModelError(nameof(vm.File), "Only PDF or DOCX is allowed.");
                if (vm.File.Length > 20 * 1024 * 1024)
                    ModelState.AddModelError(nameof(vm.File), "Max file size is 20 MB.");
            }

            if (!ModelState.IsValid)
            {
                vm.Departments = await _db.Departments.OrderBy(d => d.Name)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = d.Id.ToString(), Text = d.Name }).ToListAsync();

                vm.Supervisors = await _db.TeacherProfiles.OrderBy(t => t.FullName)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = t.Id.ToString(), Text = t.FullName }).ToListAsync();

                return View(vm);
            }

            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            // Read file bytes
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await vm.File.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            // Create Thesis
            var thesis = new Thesis
            {
                Title = vm.Title,
                Abstract = vm.Abstract,
                DepartmentId = vm.DepartmentId,
                StudentProfileId = myId.Value,
                TeacherProfileId = vm.TeacherProfileId, // যদি তুমি টেক্সট ফিল্ডে আইডি ইনপুট নাও
                Keywords = vm.Keywords,
                ProposalStatus = submit == "submit" ? ProposalStatus.Submitted : ProposalStatus.Draft,
                Status = ThesisStatus.Proposed,
                CreatedAt = DateTime.UtcNow,
                CurrentVersionNo = 1
            };
            _db.Theses.Add(thesis);
            await _db.SaveChangesAsync();

            // First Version
            var version = new ThesisVersion
            {
                ThesisId = thesis.Id,
                VersionNo = 1,
                FileData = bytes,
                FileName = vm.File.FileName,
                ContentType = vm.File.ContentType ?? "application/octet-stream",
                FileSize = vm.File.Length,
                Comment = vm.Note,
                CommentByStudentProfileId = myId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _db.ThesisVersions.Add(version);
            await _db.SaveChangesAsync();

            TempData["Success"] = thesis.ProposalStatus == ProposalStatus.Submitted
                ? "Proposal submitted successfully!"
                : "Draft saved successfully!";

            // 👉 Redirect to My Submissions
            return RedirectToAction(nameof(Index));
        }

        // ===== Details (optional but handy) =====
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            var thesis = await _db.Theses
                .Include(t => t.Department)
                .Include(t => t.Supervisor)
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentProfileId == myId.Value);

            if (thesis == null) return NotFound();

            var versions = await _db.ThesisVersions
                .Where(v => v.ThesisId == id)
                .OrderByDescending(v => v.VersionNo)
                .ToListAsync();

            var feedbacks = await _db.ThesisFeedbacks
                .Where(f => f.ThesisId == id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var vm = new ThesisDetailsVm
            {
                Thesis = thesis,
                Versions = versions,
                Feedbacks = feedbacks
            };

            return View(vm); // Views/StudentThesis/Details.cshtml
        }

        // (bonus) UploadVersion / Acknowledge/Preview/Download আগে দিয়েছিলাম—ইচ্ছা করলে যুক্ত করে রেখো
    }
}
