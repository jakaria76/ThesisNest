using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFAQController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminFAQController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var faqs = _context.FAQs.OrderByDescending(f => f.CreatedAt).ToList();
            return View(faqs);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FAQ model)
        {
            if (ModelState.IsValid)
            {
                _context.FAQs.Add(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var faq = _context.FAQs.Find(id);
            if (faq == null) return NotFound();
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(FAQ model)
        {
            if (ModelState.IsValid)
            {
                _context.FAQs.Update(model);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var faq = _context.FAQs.Find(id);
            if (faq != null)
            {
                _context.FAQs.Remove(faq);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }

}
