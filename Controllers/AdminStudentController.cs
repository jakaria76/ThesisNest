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
    public class AdminStudentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _um;
        private readonly RoleManager<IdentityRole> _rm;

        public AdminStudentController(ApplicationDbContext db, UserManager<ApplicationUser> um, RoleManager<IdentityRole> rm)
        {
            _db = db; _um = um; _rm = rm;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var items = await _db.StudentProfiles
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .ToListAsync();
            return View(items);
        }

        // CREATE (GET)
        [HttpGet]
        public IActionResult Create() => View(new CreateStudentVm());

        // CREATE (POST) — নতুন ApplicationUser + StudentProfile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (!await _rm.RoleExistsAsync("Student"))
                await _rm.CreateAsync(new IdentityRole("Student"));

            var user = await _um.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = vm.Email,
                    UserName = vm.Email,
                    FullName = vm.FullName,
                    PhoneNumber = vm.PhoneNumber,
                    EmailConfirmed = true
                };
                var createRes = await _um.CreateAsync(user, vm.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var e in createRes.Errors) ModelState.AddModelError("", e.Description);
                    return View(vm);
                }
            }

            if (!await _um.IsInRoleAsync(user, "Student"))
                await _um.AddToRoleAsync(user, "Student");

            if (await _db.StudentProfiles.AnyAsync(p => p.UserId == user.Id))
            {
                ModelState.AddModelError("", "This user already has a Student profile.");
                return View(vm);
            }

            var profile = new StudentProfile
            {
                UserId = user.Id,
                FullName = vm.FullName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                University = vm.University,
                Department = vm.Department,
                StudentId = vm.StudentId,
                Semester = vm.Semester,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.StudentProfiles.Add(profile);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Student created.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var profile = await _db.StudentProfiles.FindAsync(id);
            if (profile == null) return NotFound();
            return View(profile);
        }

        // EDIT (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentProfile model)
        {
            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == model.Id);
            if (profile == null) return NotFound();

            profile.FullName = model.FullName;
            profile.Email = model.Email;
            profile.PhoneNumber = model.PhoneNumber;
            profile.University = model.University;
            profile.Department = model.Department;
            profile.StudentId = model.StudentId;
            profile.Semester = model.Semester;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Student updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE শুধু প্রোফাইল (User ডিলিট করে না)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == id);
            if (profile == null) return NotFound();

            // Thread Restrict → আগে কাটো :contentReference[oaicite:8]{index=8}
            var threads = await _db.CommunicationThreads
                .Where(th => th.StudentProfileId == profile.Id)
                .ToListAsync();
            _db.CommunicationThreads.RemoveRange(threads);

            // Thesis.StudentProfileId nullable, কিন্তু Restrict—তাই null করে দাও। :contentReference[oaicite:9]{index=9}
            var theses = await _db.Theses.Where(t => t.StudentProfileId == profile.Id).ToListAsync();
            foreach (var th in theses) th.StudentProfileId = null;

            _db.StudentProfiles.Remove(profile);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Student profile deleted.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE ACCOUNT — সবশেষে ApplicationUser ডিলিট (Permanent)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var profile = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == id);
            if (profile == null)
            {
                TempData["Error"] = "Student profile not found.";
                return RedirectToAction(nameof(Index));
            }

            var threads = await _db.CommunicationThreads
                .Where(th => th.StudentProfileId == profile.Id)
                .ToListAsync();
            _db.CommunicationThreads.RemoveRange(threads);

            var theses = await _db.Theses.Where(t => t.StudentProfileId == profile.Id).ToListAsync();
            foreach (var th in theses) th.StudentProfileId = null;

            _db.StudentProfiles.Remove(profile);
            await _db.SaveChangesAsync(); // FK updates first

            var user = await _um.FindByIdAsync(profile.UserId);
            if (user != null)
            {
                var res = await _um.DeleteAsync(user);
                if (!res.Succeeded)
                {
                    TempData["Error"] = "User deletion failed, but student profile was removed.";
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = "Student account deleted permanently.";
            return RedirectToAction(nameof(Index));
        }
    }
}
