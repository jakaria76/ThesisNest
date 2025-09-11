//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels;
using ThesisNest.ViewModels;

namespace ThesisNest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            if (User.Identity?.IsAuthenticated ?? false)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, "Student"))
                    {
                        vm.StudentProfile = await _context.StudentProfiles.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == user.Id);

                        vm.ThesisUpload = new ThesisCreateVm
                        {
                            Departments = await _context.Departments.AsNoTracking()
                                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToListAsync(),
                            Supervisors = await _context.TeacherProfiles.AsNoTracking()
                                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.FullName }).ToListAsync(),
                            IsDeclared = true
                        };

                        vm.Tasks.Add("Submit draft proposal");
                        vm.Tasks.Add("Meet with supervisor");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                    {
                        vm.TeacherProfile = await _context.TeacherProfiles.AsNoTracking()
                            .Include(t => t.Theses)
                            .FirstOrDefaultAsync(t => t.UserId == user.Id);

                        vm.Tasks.Add("Review student proposals");
                        vm.Tasks.Add("Update research blog");
                    }
                }
            }

            vm.CollaborationLinks.AddRange(new[]
            {
                new DashboardLink { Title = "GOOGLE SCHOLAR", Url = "https://scholar.google.com/" },
                new DashboardLink { Title = "READ a PAPER", Url = "https://dspace.mit.edu/bitstream/handle/1721.1/120609/1088413444-MIT.pdf" }
            });

            return View(vm);
        }
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
