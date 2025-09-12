using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels.Admin;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTeacherController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly RoleManager<IdentityRole> _rm;

        public AdminTeacherController(ApplicationDbContext db, UserManager<ApplicationUser> um, RoleManager<IdentityRole> rm)
        {
            _db = db; _um = um; _rm = rm;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var items = await _db.TeacherProfiles
                .AsNoTracking()
                .OrderBy(t => t.FullName)
                .ToListAsync();
            return View(items);
        }

        // CREATE (GET)
        [HttpGet]
        public IActionResult Create() => View(new CreateTeacherVm());

        // CREATE (POST) — নতুন ApplicationUser + TeacherProfile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTeacherVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (!await _rm.RoleExistsAsync("Teacher"))
                await _rm.CreateAsync(new IdentityRole("Teacher"));

            var user = await _um.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = vm.Email,
                    UserName = vm.Email,
                    FullName = vm.FullName,
                    PhoneNumber = vm.Phone,
                    EmailConfirmed = true
                };
                var createRes = await _um.CreateAsync(user, vm.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var e in createRes.Errors) ModelState.AddModelError("", e.Description);
                    return View(vm);
                }
            }

            if (!await _um.IsInRoleAsync(user, "Teacher"))
                await _um.AddToRoleAsync(user, "Teacher");

            if (await _db.TeacherProfiles.AnyAsync(t => t.UserId == user.Id))
            {
                ModelState.AddModelError("", "This user already has a Teacher profile.");
                return View(vm);
            }

            var profile = new TeacherProfile
            {
                UserId = user.Id,
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                Designation = vm.Designation,
                Department = vm.Department,
                OfficeLocation = vm.OfficeLocation,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Slug = await MakeUniqueSlugAsync(ToSlug(vm.FullName))
            };

            _db.TeacherProfiles.Add(profile);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher created.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _db.TeacherProfiles.FindAsync(id);
            if (profile == null) return NotFound();
            return View(profile);
        }

        // EDIT (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherProfile model)
        {
            var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.Id == model.Id);
            if (profile == null) return NotFound();

            profile.FullName = model.FullName;
            profile.Email = model.Email;
            profile.Phone = model.Phone;
            profile.Designation = model.Designation;
            profile.Department = model.Department;
            profile.OfficeLocation = model.OfficeLocation;
            profile.Bio = model.Bio;
            profile.ResearchSummary = model.ResearchSummary;
            profile.Latitude = model.Latitude;
            profile.Longitude = model.Longitude;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Teacher updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE শুধু প্রোফাইল + তার children (User অ্যাকাউন্ট ডিলিট করে না)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await _db.TeacherProfiles
                .Include(t => t.Theses)
                .Include(t => t.Educations)
                .Include(t => t.Achievements)
                .Include(t => t.Publications)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (profile == null) return NotFound();

            // Restrict FKs: আগে সম্পর্কগুলো কাটো। Thesis → Versions/Feedbacks Cascade. :contentReference[oaicite:4]{index=4}
            _db.Theses.RemoveRange(profile.Theses);
            _db.TeacherEducations.RemoveRange(profile.Educations);
            _db.TeacherAchievements.RemoveRange(profile.Achievements);
            _db.TeacherPublications.RemoveRange(profile.Publications);

            // Threads আগে কাটতে হবে (Restrict to Teacher/Student) :contentReference[oaicite:5]{index=5}
            var threads = await _db.CommunicationThreads
                .Where(th => th.TeacherProfileId == profile.Id)
                .ToListAsync();
            _db.CommunicationThreads.RemoveRange(threads); // Messages/Calls cascade

            _db.TeacherProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Teacher profile deleted.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE ACCOUNT — সবশেষে ApplicationUser ডিলিট (Permanent)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var profile = await _db.TeacherProfiles
                .Include(t => t.Theses).ThenInclude(th => th.Versions)
                .Include(t => t.Theses).ThenInclude(th => th.Feedbacks)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (profile == null)
            {
                TempData["Error"] = "Teacher profile not found.";
                return RedirectToAction(nameof(Index));
            }

            var threads = await _db.CommunicationThreads
                .Where(th => th.TeacherProfileId == profile.Id)
                .ToListAsync();
            _db.CommunicationThreads.RemoveRange(threads);

            _db.Theses.RemoveRange(profile.Theses);
            _db.TeacherProfiles.Remove(profile);

            await _db.SaveChangesAsync(); // FK clear

            var user = await _um.FindByIdAsync(profile.UserId);
            if (user != null)
            {
                var res = await _um.DeleteAsync(user);
                if (!res.Succeeded)
                {
                    TempData["Error"] = "User deletion failed, but profile data was removed.";
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = "Teacher account deleted permanently.";
            return RedirectToAction(nameof(Index));
        }

        // Helpers (slug)
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
            var slug = baseSlug; var i = 2;
            while (true)
            {
                var exists = await _db.TeacherProfiles
                    .AnyAsync(t => t.Slug == slug && (!excludeId.HasValue || t.Id != excludeId.Value));
                if (!exists) return slug;
                slug = $"{baseSlug}-{i++}";
            }
        }
    }
}
