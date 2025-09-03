using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _environment;

        public StudentThesisController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _db = db;
            _userManager = userManager;
            _environment = environment;
        }

        // ----------------- HELPERS -----------------
        private async Task<int> GetOrCreateStudentProfileIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) throw new Exception("User not found");

            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (profile != null) return profile.Id;

            profile = new StudentProfile
            {
                UserId = user.Id,
                FullName = user.FullName ?? user.UserName ?? "Unknown",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.StudentProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile.Id;
        }

        private static bool IsAllowedFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext is ".pdf" or ".doc" or ".docx";
        }

        // ----------------- INDEX -----------------
        public async Task<IActionResult> Index()
        {
            int myId = await GetOrCreateStudentProfileIdAsync();

            var items = await _db.Theses
                .Include(t => t.Department)
                .Include(t => t.Supervisor)
                .Where(t => t.StudentProfileId == myId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ThesisIndexItemVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Department = t.Department != null ? t.Department.Name : "N/A",
                    TeacherId = t.TeacherProfileId,
                    TeacherFullName = t.Supervisor != null ? t.Supervisor.FullName : "N/A",
                    ProposalStatus = t.ProposalStatus,
                    TeacherStatus = t.Status,
                    CurrentVersionNo = t.CurrentVersionNo,
                    CreatedAtStr = t.CreatedAt.ToLocalTime().ToString("dd MMM yyyy")
                })
                .ToListAsync();

            var vm = new ThesisIndexVm
            {
                Items = items,
                Page = 1,
                PageSize = 10,
                TotalCount = items.Count
            };

            return View(vm);
        }

        // ----------------- DETAILS -----------------
        public async Task<IActionResult> Details(int id)
        {
            int myId = await GetOrCreateStudentProfileIdAsync();

            var thesis = await _db.Theses
                .Include(t => t.Department)
                .Include(t => t.Supervisor)
                .FirstOrDefaultAsync(t => t.Id == id && t.StudentProfileId == myId);

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

        // ----------------- CREATE -----------------
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ThesisCreateVm
            {
                Departments = await _db.Departments.Select(d =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync(),

                Supervisors = await _db.TeacherProfiles.Select(t =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.FullName
                    }).ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThesisCreateVm model, string submit)
        {
            if (!ModelState.IsValid) return View(model);

            int studentId = await GetOrCreateStudentProfileIdAsync();

            if (model.File == null || !IsAllowedFile(model.File.FileName))
            {
                ModelState.AddModelError("File", "Invalid file type.");
                return View(model);
            }

            if (model.File.Length > 20 * 1024 * 1024)
            {
                ModelState.AddModelError("File", "File too large (max 20MB).");
                return View(model);
            }

            using var ms = new MemoryStream();
            await model.File.CopyToAsync(ms);

            var thesis = new Thesis
            {
                Title = model.Title,
                Abstract = model.Abstract,
                StudentProfileId = studentId,
                DepartmentId = model.DepartmentId,
                TeacherProfileId = model.TeacherProfileId,
                Keywords = model.Keywords,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProposalStatus = submit == "submit" ? ProposalStatus.Submitted : ProposalStatus.Draft
            };

            _db.Theses.Add(thesis);
            await _db.SaveChangesAsync();

            var version = new ThesisVersion
            {
                ThesisId = thesis.Id,
                VersionNo = 1,
                FileName = model.File.FileName,
                ContentType = model.File.ContentType,
                FileData = ms.ToArray(),
                CreatedAt = DateTime.UtcNow
            };
            _db.ThesisVersions.Add(version);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Proposal uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------- PREVIEW VERSION -----------------
        [HttpGet]
        public async Task<IActionResult> PreviewVersion(int id)
        {
            var version = await _db.ThesisVersions.FindAsync(id);
            if (version == null || version.FileData == null)
                return NotFound();

            string fileName = version.FileName;
            string contentType = version.ContentType ?? "application/octet-stream";

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(version.FileData, contentType, fileName, enableRangeProcessing: true);
            }

            if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
            {
                var absoluteUrl = Url.Action("DownloadVersion", "StudentThesis", new { id = version.Id }, HttpContext.Request.Scheme);
                return Redirect($"https://view.officeapps.live.com/op/embed.aspx?src={Uri.EscapeDataString(absoluteUrl)}");
            }

            return File(version.FileData, contentType, fileName);
        }

        // ----------------- DOWNLOAD VERSION -----------------
        [HttpGet]
        public async Task<IActionResult> DownloadVersion(int id)
        {
            var version = await _db.ThesisVersions.FindAsync(id);
            if (version == null || version.FileData == null) return NotFound();

            return File(version.FileData, version.ContentType ?? "application/octet-stream", version.FileName);
        }

        // ----------------- TEACHER DETAILS -----------------
        public async Task<IActionResult> TeacherDetails(int id)
        {
            var teacher = await _db.TeacherProfiles
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null) return NotFound();

            var vm = new TeacherProfileVm
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Email = teacher.Email,
                Department = teacher.Department ?? "N/A",
                Phone = teacher.Phone,
                Office = teacher.OfficeLocation, // <-- match your model
                ProfileImageUrl = teacher.ProfileImage != null
        ? Url.Action("Photo", "TeacherProfile", new { id = teacher.Id })
        : "https://via.placeholder.com/240x240.png?text=User"
            };



            return View(vm);
        }

        // ----------------- TEACHER PHOTO -----------------
        public IActionResult Photo(int id)
        {
            var teacher = _db.TeacherProfiles.FirstOrDefault(t => t.Id == id);
            if (teacher == null || teacher.ProfileImage == null) return NotFound();

            return File(teacher.ProfileImage, "image/png"); // adjust type if needed
        }
    }
}
