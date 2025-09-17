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
    public class StudentProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const long MaxPhotoBytes = 2 * 1024 * 1024; // 2MB
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        public StudentProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= INDEX / VIEW PROFILE =================
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (profile == null) return RedirectToAction(nameof(Create));
            return View(profile);
        }

        // ================= CREATE =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            if (await _context.StudentProfiles.AnyAsync(p => p.UserId == currentUser.Id))
                return RedirectToAction(nameof(Edit));

            var vm = new StudentProfile
            {
                UserId = currentUser.Id,
                FullName = currentUser.FullName ?? currentUser.UserName ?? "",
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentProfile model, IFormFile? ProfileImageFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            if (await _context.StudentProfiles.AnyAsync(p => p.UserId == currentUser.Id))
                return RedirectToAction(nameof(Edit));

            model.UserId = currentUser.Id;

            if (ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                var (ok, msg) = await TryBindPhotoAsync(model, ProfileImageFile);
                if (!ok) ModelState.AddModelError("ProfileImage", msg!);
            }

            if (!ModelState.IsValid) return View(model);

            _context.StudentProfiles.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            if (profile == null) return RedirectToAction(nameof(Create));
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentProfile model, IFormFile? ProfileImageFile, bool removePhoto = false)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id && p.Id == model.Id);

            if (profile == null)
            {
                ModelState.AddModelError(string.Empty, "Profile not found.");
                return View(model);
            }

            // Update fields
            profile.FullName = model.FullName;
            profile.DateOfBirth = model.DateOfBirth;
            profile.Gender = model.Gender;
            profile.PhoneNumber = model.PhoneNumber;
            profile.Email = model.Email;
            profile.Address = model.Address;
            profile.University = model.University;
            profile.Department = model.Department;
            profile.Semester = model.Semester;
            profile.StudentId = model.StudentId;
            profile.GPA = model.GPA;
            profile.ThesisTitle = model.ThesisTitle;
            profile.Supervisor = model.Supervisor;
            profile.ThesisStatus = model.ThesisStatus;
            profile.SubmissionDate = model.SubmissionDate;
            profile.Feedback = model.Feedback;
            profile.Skills = model.Skills;
            profile.Achievements = model.Achievements;
            profile.LinkedIn = model.LinkedIn;
            profile.GitHub = model.GitHub;
            profile.Portfolio = model.Portfolio;

            // Handle photo
            if (removePhoto)
            {
                profile.ProfileImage = null;
                profile.ProfileImageContentType = null;
            }
            else if (ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                var (ok, msg) = await TryBindPhotoAsync(profile, ProfileImageFile);
                if (!ok)
                {
                    ModelState.AddModelError("ProfileImage", msg!);
                    return View(model);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return NotFound();

            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            if (profile == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.StudentProfiles.Remove(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile deleted.";
            return RedirectToAction(nameof(Create));
        }

        // ================= PHOTO =================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Photo(int id)
        {
            var profile = await _context.StudentProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile == null || profile.ProfileImage == null || string.IsNullOrEmpty(profile.ProfileImageContentType))
            {
                var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/default-user.png");
                var defaultBytes = await System.IO.File.ReadAllBytesAsync(defaultPath);
                return File(defaultBytes, "image/png");
            }

            return File(profile.ProfileImage, profile.ProfileImageContentType);
        }

        // ================= HELPERS =================
        private static async Task<(bool ok, string? message)> TryBindPhotoAsync(StudentProfile target, IFormFile file)
        {
            if (!AllowedContentTypes.Contains(file.ContentType))
                return (false, "Only JPEG/PNG/WebP images are allowed.");

            if (file.Length > MaxPhotoBytes)
                return (false, $"File too large. Max {MaxPhotoBytes / (1024 * 1024)}MB allowed.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            target.ProfileImage = ms.ToArray();
            target.ProfileImageContentType = file.ContentType;
            return (true, null);
        }

        // ================= VIEW ALL SUPERVISORS =================
        [AllowAnonymous]
        public async Task<IActionResult> AllSupervisors()
        {
            var teachers = await _context.TeacherProfiles
                .Include(t => t.Theses)
                .AsNoTracking()
                .ToListAsync();

            var model = teachers.Select(t => new TeacherViewModel
            {
                Id = t.Id,
                FullName = t.FullName,
                Department = t.Department,
                Designation = t.Designation,
                Bio = t.Bio,
                AvailableSlots = t.AvailableSlots,
                ProfileImageUrl = Url.Action("Photo", "TeacherProfile", new { id = t.Id })
            }).ToList();

            return View(model);
        }
    }
}
