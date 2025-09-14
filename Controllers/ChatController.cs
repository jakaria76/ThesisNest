using Microsoft.AspNetCore.Mvc;

namespace ThesisNest.Controllers
{
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Chatbot";
            return View();
        }
    }
}