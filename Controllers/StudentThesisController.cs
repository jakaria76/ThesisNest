using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            var items = await _db.Theses
                .AsNoTracking()
                .Include(t => t.Department)
                .Include(t => t.Supervisor)
                .Where(t => t.StudentProfileId == myId.Value)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ThesisIndexItemVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Department = t.Department.Name,
                    TeacherId = t.TeacherProfileId,
                    TeacherFullName = t.Supervisor.FullName,
                    ProposalStatus = t.ProposalStatus,
                    TeacherStatus = t.Status,
                    CurrentVersionNo = t.CurrentVersionNo,
                    CreatedAtStr = t.CreatedAt.ToLocalTime().ToString("dd MMM yyyy")
                }).ToListAsync();

            var vm = new ThesisIndexVm
            {
                Items = items,
                Page = 1,
                PageSize = 10,
                TotalCount = items.Count
            };

            return View(vm);
        }

        // ===== CREATE GET =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ThesisCreateVm
            {
                Departments = await _db.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = d.Id.ToString(), Text = d.Name }).ToListAsync(),

                Supervisors = await _db.TeacherProfiles
                    .OrderBy(t => t.FullName)
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    { Value = t.Id.ToString(), Text = t.FullName }).ToListAsync()
            };

            return View(vm);
        }

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Create(ThesisCreateVm vm, string submit)
        {
            if (vm.File == null)
                ModelState.AddModelError(nameof(vm.File), "File is required.");
            else
            {
                if (!IsAllowedFile(vm.File.FileName))
                    ModelState.AddModelError(nameof(vm.File), "Only PDF or DOCX allowed.");
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

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await vm.File.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var thesis = new Thesis
            {
                Title = vm.Title,
                Abstract = vm.Abstract,
                DepartmentId = vm.DepartmentId,
                StudentProfileId = myId.Value,
                TeacherProfileId = vm.TeacherProfileId,
                Keywords = vm.Keywords,
                ProposalStatus = submit == "submit" ? ProposalStatus.Submitted : ProposalStatus.Draft,
                Status = ThesisStatus.Proposed,
                CreatedAt = DateTime.UtcNow,
                CurrentVersionNo = 1
            };

            _db.Theses.Add(thesis);
            await _db.SaveChangesAsync();

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

            return RedirectToAction(nameof(Index));
        }

        // ===== DETAILS =====
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

            return View(vm);
        }

        // ===== TEACHER DETAILS =====
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> TeacherDetails(int id)
        {
            var teacher = await _db.TeacherProfiles
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            ViewBag.CanEdit = false; // Students cannot edit/delete
            return View("TeacherDetails", teacher);
        }

        // ===== TEACHER PHOTO =====
        [HttpGet]
        public async Task<IActionResult> Photo(int id)
        {
            var profile = await _db.TeacherProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (profile == null || profile.ProfileImage == null || string.IsNullOrEmpty(profile.ProfileImageContentType))
                return NotFound();

            return File(profile.ProfileImage, profile.ProfileImageContentType);
        }

        // ===== DOWNLOAD/UPLOAD VERSION =====
        [HttpGet]
        public async Task<IActionResult> DownloadVersion(int id)
        {
            var version = await _db.ThesisVersions.FindAsync(id);
            if (version == null || version.FileData == null) return NotFound();

            return File(version.FileData, version.ContentType, version.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> UploadVersion(int thesisId, IFormFile? file, string? note, string? submit,
            string title, string @abstract, string? keywords)
        {
            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            var thesis = await _db.Theses
                .Include(t => t.Versions)
                .FirstOrDefaultAsync(t => t.Id == thesisId && t.StudentProfileId == myId.Value);

            if (thesis == null) return NotFound();

            thesis.Title = title;
            thesis.Abstract = @abstract;
            thesis.Keywords = keywords;
            thesis.UpdatedAt = DateTime.UtcNow;
            _db.Theses.Update(thesis);
            await _db.SaveChangesAsync();

            if (file != null)
            {
                if (!IsAllowedFile(file.FileName))
                {
                    TempData["Error"] = "Only PDF or DOCX allowed.";
                    return RedirectToAction("Details", new { id = thesisId });
                }

                if (file.Length > 20 * 1024 * 1024)
                {
                    TempData["Error"] = "Max file size is 20 MB.";
                    return RedirectToAction("Details", new { id = thesisId });
                }

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                var newVersionNo = thesis.CurrentVersionNo + 1;

                var version = new ThesisVersion
                {
                    ThesisId = thesis.Id,
                    VersionNo = newVersionNo,
                    FileData = bytes,
                    FileName = file.FileName,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    FileSize = file.Length,
                    Comment = note,
                    CommentByStudentProfileId = myId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ThesisVersions.Add(version);
                thesis.CurrentVersionNo = newVersionNo;
                _db.Theses.Update(thesis);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = submit == "submit" ? "Version submitted successfully!" : "Changes saved!";
            return RedirectToAction("Details", new { id = thesisId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcknowledgeFeedback(int id)
        {
            var myId = await GetMyStudentProfileIdAsync();
            if (myId == null) return Forbid();

            var feedback = await _db.ThesisFeedbacks
                .Include(f => f.Thesis)
                .FirstOrDefaultAsync(f => f.Id == id && f.Thesis.StudentProfileId == myId.Value);

            if (feedback == null) return NotFound();

            feedback.AcknowledgedAt = DateTime.UtcNow;
            _db.ThesisFeedbacks.Update(feedback);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Feedback acknowledged successfully!";
            return RedirectToAction(nameof(Details), new { id = feedback.ThesisId });
        }
    }
}
