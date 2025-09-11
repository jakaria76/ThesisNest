using Microsoft.AspNetCore.Mvc;
using ThesisNest.Data;
using ThesisNest.Models.model;

namespace ThesisNest.Controllers.controller
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private object setting;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------
        // Dashboard
        // -------------------------
        public IActionResult Index()
        {
            return View();
        }

        // -------------------------
        // Profile Management
        // -------------------------

        // GET: Profile Page
        public IActionResult Profile()
        {
            var adminIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            int adminId = int.Parse(adminIdStr);
            var admin = _context.Users.FirstOrDefault();

            if (admin == null)
                return NotFound();

            return View(admin);
        }

        // POST: Update Profile
        [HttpPost]
        public IActionResult Profile(User model)
        {
            // Try to get id from hidden input
            var adminId = model.Id;

            // Fallback: if hidden input blank, get from session
            if (adminId == 0)
            {
                var adminIdStr = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(adminIdStr))
                {
                    adminId = int.Parse(adminIdStr);
                }
            }

            var admin = _context.Users.FirstOrDefault();
            if (admin == null)
                return NotFound();

            // Update profile fields
            admin.FullName = model.FullName;
            admin.Email = model.Email;
            //admin.ContactNumber = model.ContactNumber;

            _context.Users.Update(admin);
            _context.SaveChanges();

            ViewBag.Message = "Profile updated successfully!";
            return View(admin);
        }

        // -------------------------
        // User Management
        // -------------------------

        // List of Users
        public IActionResult ManageUsers()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // GET: Create User form
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: Create User
        [HttpPost]
        public IActionResult CreateUser(User model)
        {
            if (ModelState.IsValid)
            {
                // Supervisor → Pending approval
                model.IsApproved = model.Role == "Supervisor" ? false : true;

                //_context.Users.Add(model);
                _context.SaveChanges();

                return RedirectToAction("ManageUsers");
            }
            return View(model);
        }

        // GET: Edit Role
        public IActionResult EditRole(int id)
        {
            var user = _context.Users.FirstOrDefault();
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Edit Role
        [HttpPost]
        public IActionResult EditRole(User model)
        {
            var user = _context.Users.FirstOrDefault();
            if (user == null) return NotFound();

            //user.Role = model.Role;
            _context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        // Approve Supervisor
        public IActionResult ApproveSupervisor(int id)
        {
           var user = _context.Users.FirstOrDefault();
            if (user == null) return NotFound();

            user.IsApproved = true;
            _context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        // Reject (Delete) Supervisor
        public IActionResult RejectSupervisor(int id)
        {
            //var user = _context.Users.FirstOrDefault(u => u.Id == id && u.Role == "Supervisor");
            //if (user == null) return NotFound();

            //_context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("ManageUsers");
        }


        // ============================
        // Department Management
        // ============================

        // Department List

        public IActionResult Departments()
        {
            var departments = _context.Departments.ToList();
            return View(departments);
        }

        // GET: Add Department
        public IActionResult AddDepartment()
        {
            return View();
        }

        // POST: Add Department
        [HttpPost]
        public IActionResult AddDepartment(Department model)
        {
            if (ModelState.IsValid)
            {
                //_context.Departments.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Departments");
            }
            return View(model);
        }

        // GET: Edit Department
        public IActionResult EditDepartment(int id)
        {
            var dept = _context.Departments.FirstOrDefault(d => d.Id == id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        // POST: Edit Department
        [HttpPost]
        public IActionResult EditDepartment(Department mod)
        {
            if (ModelState.IsValid)
            {
                //_context.Departments.Update(mod);
                _context.SaveChanges();
                return RedirectToAction("Departments");
            }
            return View(mod);
        }

        // Delete Department
        public IActionResult DeleteDepartment(int id)
        {
            var dept = _context.Departments.FirstOrDefault(d => d.Id == id);
            if (dept == null) return NotFound();

            _context.Departments.Remove(dept);
            _context.SaveChanges();

            return RedirectToAction("Departments");
        }


        // -------------------------
        // Reports
        // -------------------------

        public IActionResult Reports()
        {
            // Department-wise Thesis Count
            var deptReports = _context.Departments
                .Select(d => new DepartmentReport
                {
                    DepartmentName = d.Name,
                    ThesisCount = _context.Theses.Count(t => t.DepartmentId == d.Id)
                })
                .ToList();


            return View();
        }




        // -------------------------
        //  Settings
        // -------------------------

        public IActionResult Settings()
        {
            
            return View();
        }

        // POST: Update Setting
        [HttpPost]
        public IActionResult UpdateSetting(int id, string value)
        {
            
            if (setting != null)
            {
                setting = value;
                _context.SaveChanges();
            }
            return RedirectToAction("Settings");
        }
    }
}
