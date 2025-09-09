//using Microsoft.AspNetCore.Mvc;
//using System.Security.Cryptography;
//using System.Text;
//using ThesisNest.Data;
//using ThesisNest.Models;
//using ThesisNest.Models.Model;



//    public class AccountController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public AccountController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // -------------------------
//        //  Registration - GET
//        // -------------------------
//        [HttpGet]
//        public IActionResult Register()
//        {
//            return View();
//        }

//        // -------------------------
//        //  Registration - POST
//        // -------------------------
//        [HttpPost]
//        public IActionResult Register(User model)
//        {
//            if (ModelState.IsValid)
//            {
//                // Password Hashing
//                model.PasswordHash = HashPassword(model.PasswordHash);

//                // Supervisor will not auto approve
//                if (model.Role == "Supervisor")
//                {
//                    model.IsApproved = false; // Must wait for Admin approval
//                }

//            // Save user to DB
//            //Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ApplicationUser> entityEntry = _context.Users.Add(model);
//            //    _context.SaveChanges();

//                return RedirectToAction("Login");
//            }
//            // If model state is invalid, re-display the form with validation errors
//            return View(model);
//        }

//        // -------------------------
//        //  Login - GET
//        // -------------------------
//        [HttpGet]
//        public IActionResult Login()
//        {
//            return View();
//        }

//        // -------------------------
//        //  Login - POST
//        // -------------------------
//        [HttpPost]
//        public IActionResult Login(string email, string password)
//        {
//            string hashedPassword = HashPassword(password);
//            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);

//            if (user == null)
//            {
//                ViewBag.Error = "Invalid Email or Password";
//                return View();
//            }

//            // Supervisor approval check
//            if (user.Role == "Supervisor" && !user.IsApproved)
//            {
//                ViewBag.Error = "Your account is waiting for Admin approval.";
//                return View();
//            }

//            // Session Management
//            HttpContext.Session.SetString("UserId", user.Id.ToString());
//            HttpContext.Session.SetString("UserRole", user.Role);
//            HttpContext.Session.SetString("UserName", user.FullName);

//            // Redirect based on role
//            if (user.Role == "Student")
//            {
//                return RedirectToAction("Index", "Student");
//            }
//            else if (user.Role == "Supervisor")
//            {
//                return RedirectToAction("Index", "Supervisor");
//            }
//            else if (user.Role == "Admin")
//            {
//                return RedirectToAction("Index", "Admin");
//            }
//            // if role is unrecognized, redirect to login
//            return RedirectToAction("Login");
//        }

//        // -------------------------
//        //  LogOut
//        // -------------------------
//        public IActionResult Logout()
//        {
//            HttpContext.Session.Clear();
//            return RedirectToAction("Login");
//        }

//        // -------------------------
//        // Password Hash Method
//        // -------------------------
//        private string HashPassword(string password)
//        {
//            using (var sha256 = SHA256.Create())
//            {
//                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
//                var builder = new StringBuilder();
//                foreach (var b in bytes)
//                {
//                    builder.Append(b.ToString("x2"));
//                }
//                return builder.ToString();
//            }
//        }
//    }

