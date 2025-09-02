using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const long MaxPhotoBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        public TeacherProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========= INDEX =========
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.TeacherProfiles
                .Include(p => p.Theses)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null) return RedirectToAction(nameof(Create));

            ViewData["Departments"] = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(profile);
        }

        // ========= CREATE =========
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (await _context.TeacherProfiles.AnyAsync(t => t.UserId == user.Id))
                return RedirectToAction(nameof(Edit));

            return View(new TeacherProfile
            {
                FullName = user.FullName ?? user.UserName ?? "",
                Email = user.Email,
                Phone = user.PhoneNumber
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeacherProfile model, IFormFile? ProfileImageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            if (await _context.TeacherProfiles.AnyAsync(t => t.UserId == user.Id))
                return RedirectToAction(nameof(Edit));

            model.UserId = user.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.Slug = await MakeUniqueSlugAsync(ToSlug(model.FullName));

            if (ProfileImageFile is { Length: > 0 })
            {
                var (ok, msg) = await TryBindPhotoAsync(model, ProfileImageFile);
                if (!ok) ModelState.AddModelError("ProfileImage", msg!);
            }

            ModelState.Remove(nameof(TeacherProfile.UserId));
            ModelState.Remove(nameof(TeacherProfile.CreatedAt));
            ModelState.Remove(nameof(TeacherProfile.UpdatedAt));
            ModelState.Remove(nameof(TeacherProfile.Slug));

            if (!ModelState.IsValid) return View(model);

            _context.TeacherProfiles.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========= EDIT =========
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.TeacherProfiles
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null) return RedirectToAction(nameof(Create));
            return View(profile);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherProfile model, IFormFile? ProfileImageFile, bool removePhoto = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.TeacherProfiles
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Id == model.Id);
            if (profile == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove(nameof(TeacherProfile.UserId));
            ModelState.Remove(nameof(TeacherProfile.CreatedAt));
            ModelState.Remove(nameof(TeacherProfile.UpdatedAt));
            ModelState.Remove(nameof(TeacherProfile.Slug));
            ModelState.Remove(nameof(TeacherProfile.RowVersion));

            if (!ModelState.IsValid) return View(profile);

            profile.FullName = model.FullName;
            profile.Designation = model.Designation;
            profile.Department = model.Department;
            profile.OfficeLocation = model.OfficeLocation;
            profile.Email = model.Email;
            profile.Phone = model.Phone;
            profile.IsPublicEmail = model.IsPublicEmail;
            profile.IsPublicPhone = model.IsPublicPhone;
            profile.Bio = model.Bio;
            profile.ResearchSummary = model.ResearchSummary;
            profile.Latitude = model.Latitude;
            profile.Longitude = model.Longitude;

            if (removePhoto)
            {
                profile.ProfileImage = null;
                profile.ProfileImageContentType = null;
                profile.ProfileImageFileName = null;
            }
            else if (ProfileImageFile is { Length: > 0 })
            {
                var (ok, msg) = await TryBindPhotoAsync(profile, ProfileImageFile);
                if (!ok) ModelState.AddModelError("ProfileImage", msg!);
            }

            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========= DELETE =========
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.TeacherProfiles
                .Include(t => t.Theses)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Theses.RemoveRange(profile.Theses);
            _context.TeacherProfiles.Remove(profile);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile deleted successfully!";
            return RedirectToAction(nameof(Create));
        }

        // ========= PHOTO =========
        [HttpGet]
        public async Task<IActionResult> Photo(int id)
        {
            var profile = await _context.TeacherProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (profile == null || profile.ProfileImage == null || string.IsNullOrEmpty(profile.ProfileImageContentType))
                return File(System.IO.File.ReadAllBytes("wwwroot/images/default-user.png"), "image/png");

            return File(profile.ProfileImage, profile.ProfileImageContentType);
        }

        // ========= DETAILS =========
        [Authorize(Roles = "Teacher,Admin,Student")]
        public async Task<IActionResult> Details(int id)
        {
            var profile = await _context.TeacherProfiles
                .Include(t => t.Theses)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (profile == null) return NotFound();

            ViewBag.CanEdit = User.IsInRole("Teacher") || User.IsInRole("Admin");
            return View("TeacherDetails", profile);
        }

        // ========= HELPERS =========
        private static string ToSlug(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("n")[..8];

            var s = text.Trim().ToLowerInvariant();
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') sb.Append('-');
            }

            var slug = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
        }

        private async Task<string> MakeUniqueSlugAsync(string baseSlug, int? excludeId = null)
        {
            var slug = baseSlug;
            var i = 2;
            while (true)
            {
                var exists = await _context.TeacherProfiles
                    .AnyAsync(t => t.Slug == slug && (!excludeId.HasValue || t.Id != excludeId.Value));
                if (!exists) return slug;
                slug = $"{baseSlug}-{i++}";
            }
        }

        private static async Task<(bool ok, string? msg)> TryBindPhotoAsync(TeacherProfile target, IFormFile file)
        {
            if (!AllowedContentTypes.Contains(file.ContentType))
                return (false, "Only JPEG/PNG/WebP allowed.");
            if (file.Length > MaxPhotoBytes)
                return (false, "Max 2MB image allowed.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            target.ProfileImage = ms.ToArray();
            target.ProfileImageContentType = file.ContentType;
            target.ProfileImageFileName = file.FileName;
            return (true, null);
        }
        

    }
}
