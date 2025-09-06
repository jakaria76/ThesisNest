using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;
using Microsoft.Extensions.Options;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GoogleMapsOptions _maps;

        private const long MaxPhotoBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        public TeacherProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IOptions<GoogleMapsOptions> mapsOptions)
        {
            _context = context;
            _userManager = userManager;
            _maps = mapsOptions.Value;

        }

        // ========= INDEX =========
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.TeacherProfiles
                .Include(p => p.Educations)
                .Include(p => p.Achievements)
                .Include(p => p.Publications)
                .Include(p => p.Theses)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null) return RedirectToAction(nameof(Create));

            ViewData["Departments"] = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync();
            ViewBag.GoogleMapsApiKey = _maps.ApiKey ?? "";

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
                Phone = user.PhoneNumber,
                Educations = new List<TeacherEducation>(),
                Achievements = new List<TeacherAchievement>(),
                Publications = new List<TeacherPublication>()
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

            // Photo
            if (ProfileImageFile is { Length: > 0 })
            {
                var (ok, msg) = await TryBindPhotoAsync(model, ProfileImageFile);
                if (!ok) ModelState.AddModelError("ProfileImage", msg!);
            }

            // Normalize child collections
            NormalizeChildrenForCreate(model);

            // Remove server-managed props
            ModelState.Remove(nameof(TeacherProfile.UserId));
            ModelState.Remove(nameof(TeacherProfile.CreatedAt));
            ModelState.Remove(nameof(TeacherProfile.UpdatedAt));
            ModelState.Remove(nameof(TeacherProfile.Slug));

            if (!ModelState.IsValid) return View(model);

            // EF will insert children (FK set automatically because they're attached to model)
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
                .Include(p => p.Educations)
                .Include(p => p.Achievements)
                .Include(p => p.Publications)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null) return RedirectToAction(nameof(Create));
            return View(profile);
        }

        /// <summary>
        /// Edit POST: একসাথে প্রোফাইল + Educations + Achievements + Publications upsert করে।
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherProfile model, IFormFile? ProfileImageFile, bool removePhoto = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Load existing with children (tracked)
            var profile = await _context.TeacherProfiles
                .Include(p => p.Educations)
                .Include(p => p.Achievements)
                .Include(p => p.Publications)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Id == model.Id);

            if (profile == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            // Remove server-managed props from modelstate
            ModelState.Remove(nameof(TeacherProfile.UserId));
            ModelState.Remove(nameof(TeacherProfile.CreatedAt));
            ModelState.Remove(nameof(TeacherProfile.UpdatedAt));
            ModelState.Remove(nameof(TeacherProfile.Slug));
            ModelState.Remove(nameof(TeacherProfile.RowVersion));

            if (!ModelState.IsValid) return View(profile);

            // Basic fields
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

            // Photo
            if (removePhoto)
            {
                profile.ProfileImage = null;
                profile.ProfileImageContentType = null;
                profile.ProfileImageFileName = null;
            }
            else if (ProfileImageFile is { Length: > 0 })
            {
                var (ok, msg) = await TryBindPhotoAsync(profile, ProfileImageFile);
                if (!ok)
                {
                    ModelState.AddModelError("ProfileImage", msg!);
                    return View(profile);
                }
            }

            // Incoming lists never null
            var incomingEducations = model.Educations ?? new List<TeacherEducation>();
            var incomingAchievements = model.Achievements ?? new List<TeacherAchievement>();
            var incomingPublications = model.Publications ?? new List<TeacherPublication>();

            // Upsert Educations
            UpsertCollection(
                incomingEducations,
                profile.Educations,
                (src, dest) =>
                {
                    dest.Degree = src.Degree;
                    dest.Institution = src.Institution;
                    dest.BoardOrUniversity = src.BoardOrUniversity;
                    dest.FieldOfStudy = src.FieldOfStudy;
                    dest.PassingYear = src.PassingYear;
                    dest.Result = src.Result;
                    dest.Country = src.Country;
                });

            // Upsert Achievements
            UpsertCollection(
                incomingAchievements,
                profile.Achievements,
                (src, dest) =>
                {
                    dest.Title = src.Title;
                    dest.Issuer = src.Issuer;
                    dest.IssuedOn = src.IssuedOn;
                    dest.Description = src.Description;
                    dest.Url = src.Url;
                });

            // Upsert Publications
            UpsertCollection(
                incomingPublications,
                profile.Publications,
                (src, dest) =>
                {
                    dest.Title = src.Title;
                    dest.VenueType = src.VenueType;
                    dest.VenueName = src.VenueName;
                    dest.Year = src.Year;
                    dest.Volume = src.Volume;
                    dest.Issue = src.Issue;
                    dest.Pages = src.Pages;
                    dest.DOI = src.DOI;
                    dest.Url = src.Url;
                    dest.CoAuthors = src.CoAuthors;
                    dest.Abstract = src.Abstract;
                });

            // Ensure FK for all children (especially newly added)
            foreach (var e in profile.Educations) e.TeacherProfileId = profile.Id;
            foreach (var a in profile.Achievements) a.TeacherProfileId = profile.Id;
            foreach (var p in profile.Publications) p.TeacherProfileId = profile.Id;

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
                .Include(t => t.Educations)
                .Include(t => t.Achievements)
                .Include(t => t.Publications)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (profile == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Theses.RemoveRange(profile.Theses);
            _context.TeacherEducations.RemoveRange(profile.Educations);
            _context.TeacherAchievements.RemoveRange(profile.Achievements);
            _context.TeacherPublications.RemoveRange(profile.Publications);
            _context.TeacherProfiles.Remove(profile);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile deleted successfully!";
            return RedirectToAction(nameof(Create));
        }

        // ========= PHOTO =========
        [HttpGet]
        [AllowAnonymous] // public details view-এর জন্য দরকার হলে
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
                .Include(t => t.Educations)
                .Include(t => t.Achievements)
                .Include(t => t.Publications)
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

        private static void NormalizeChildrenForCreate(TeacherProfile model)
        {
            var now = DateTime.UtcNow;

            foreach (var e in model.Educations ?? Enumerable.Empty<TeacherEducation>())
            {
                e.Id = 0;
                e.CreatedAt = now;
                e.UpdatedAt = now;
            }

            foreach (var a in model.Achievements ?? Enumerable.Empty<TeacherAchievement>())
            {
                a.Id = 0;
                a.CreatedAt = now;
                a.UpdatedAt = now;
            }

            foreach (var p in model.Publications ?? Enumerable.Empty<TeacherPublication>())
            {
                p.Id = 0;
                p.CreatedAt = now;
                p.UpdatedAt = now;
            }
        }

        /// <summary>
        /// Generic upsert for one-to-many child collections.
        /// - Adds new items where Id == 0
        /// - Updates matched items by Id
        /// - Deletes items removed from the form
        /// </summary>
        private static void UpsertCollection<TChild>(
            IEnumerable<TChild> incoming,
            ICollection<TChild> existing,
            Action<TChild, TChild> mapFields
        ) where TChild : class
        {
            int GetId(TChild x) => (int)(typeof(TChild).GetProperty("Id")!.GetValue(x) ?? 0);

            var now = DateTime.UtcNow;

            // UPDATE + ADD
            foreach (var src in incoming)
            {
                var id = GetId(src);
                if (id == 0)
                {
                    // NEW
                    typeof(TChild).GetProperty("CreatedAt")?.SetValue(src, now);
                    typeof(TChild).GetProperty("UpdatedAt")?.SetValue(src, now);
                    existing.Add(src);
                }
                else
                {
                    // UPDATE
                    var dest = existing.FirstOrDefault(e => GetId(e) == id);
                    if (dest != null)
                    {
                        mapFields(src, dest);
                        typeof(TChild).GetProperty("UpdatedAt")?.SetValue(dest, now);
                    }
                }
            }

            // DELETE missing
            var incomingIds = incoming.Select(GetId).ToHashSet();
            var toRemove = existing.Where(e => !incomingIds.Contains(GetId(e))).ToList();
            foreach (var r in toRemove) existing.Remove(r);
        }

        // ========= EDUCATION (optional granular endpoints) =========
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(TeacherEducation model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var profile = await _context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (profile == null) return RedirectToAction(nameof(Create));

            model.Id = 0;
            model.TeacherProfileId = profile.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid education data.";
                return RedirectToAction(nameof(Index));
            }

            _context.TeacherEducations.Add(model);
            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Education added.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Duplicate degree for this profile (each degree can be added once).";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditEducation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var edu = await _context.TeacherEducations
                .Include(e => e.TeacherProfile)
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherProfile.UserId == user.Id);

            if (edu == null) return NotFound();
            return View("EducationEdit", edu);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEducation(TeacherEducation model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var edu = await _context.TeacherEducations
                .Include(e => e.TeacherProfile)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.TeacherProfile.UserId == user.Id);

            if (edu == null) return NotFound();
            if (!ModelState.IsValid) return View("EducationEdit", model);

            edu.Degree = model.Degree;
            edu.Institution = model.Institution;
            edu.BoardOrUniversity = model.BoardOrUniversity;
            edu.FieldOfStudy = model.FieldOfStudy;
            edu.PassingYear = model.PassingYear;
            edu.Result = model.Result;
            edu.Country = model.Country;
            edu.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Education updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Duplicate degree for this profile.";
                return View("EducationEdit", edu);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var edu = await _context.TeacherEducations
                .Include(e => e.TeacherProfile)
                .FirstOrDefaultAsync(e => e.Id == id && e.TeacherProfile.UserId == user.Id);

            if (edu == null) return NotFound();

            _context.TeacherEducations.Remove(edu);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Education deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ========= ACHIEVEMENTS (optional granular endpoints) =========
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAchievement(TeacherAchievement model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var profile = await _context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (profile == null) return RedirectToAction(nameof(Create));

            model.Id = 0;
            model.TeacherProfileId = profile.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid achievement data.";
                return RedirectToAction(nameof(Index));
            }

            _context.TeacherAchievements.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Achievement added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditAchievement(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherAchievements
                .Include(a => a.TeacherProfile)
                .FirstOrDefaultAsync(a => a.Id == id && a.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();
            return View("AchievementEdit", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAchievement(TeacherAchievement model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherAchievements
                .Include(a => a.TeacherProfile)
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();
            if (!ModelState.IsValid) return View("AchievementEdit", model);

            item.Title = model.Title;
            item.Issuer = model.Issuer;
            item.IssuedOn = model.IssuedOn;
            item.Description = model.Description;
            item.Url = model.Url;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Achievement updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAchievement(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherAchievements
                .Include(a => a.TeacherProfile)
                .FirstOrDefaultAsync(a => a.Id == id && a.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();

            _context.TeacherAchievements.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Achievement deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ========= PUBLICATIONS (optional granular endpoints) =========
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPublication(TeacherPublication model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var profile = await _context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (profile == null) return RedirectToAction(nameof(Create));

            model.Id = 0;
            model.TeacherProfileId = profile.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid publication data.";
                return RedirectToAction(nameof(Index));
            }

            _context.TeacherPublications.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Publication added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditPublication(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherPublications
                .Include(p => p.TeacherProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();
            return View("PublicationEdit", item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPublication(TeacherPublication model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherPublications
                .Include(p => p.TeacherProfile)
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();
            if (!ModelState.IsValid) return View("PublicationEdit", model);

            item.Title = model.Title;
            item.VenueType = model.VenueType;
            item.VenueName = model.VenueName;
            item.Year = model.Year;
            item.Volume = model.Volume;
            item.Issue = model.Issue;
            item.Pages = model.Pages;
            item.DOI = model.DOI;
            item.Url = model.Url;
            item.CoAuthors = model.CoAuthors;
            item.Abstract = model.Abstract;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Publication updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublication(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var item = await _context.TeacherPublications
                .Include(p => p.TeacherProfile)
                .FirstOrDefaultAsync(p => p.Id == id && p.TeacherProfile.UserId == user.Id);

            if (item == null) return NotFound();

            _context.TeacherPublications.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Publication deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
