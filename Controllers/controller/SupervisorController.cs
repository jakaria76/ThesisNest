using Microsoft.AspNetCore.Mvc;

namespace ThesisNest.Controllers
{
    public class SupervisorController : Controller
    {
        public IActionResult Index()
        {
            //return View();

            // For simplicity, just return a plain content for now
            return Content("Welcome to Supervisor Dashboard");
        }
    }
}
