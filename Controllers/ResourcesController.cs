using Microsoft.AspNetCore.Mvc;

namespace ThesisNest.Controllers
{
    public class ResourcesController : Controller
    {
        public IActionResult Guidelines()
        {
            return View();
        }
    }
}
