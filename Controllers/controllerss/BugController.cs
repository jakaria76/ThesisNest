using Microsoft.AspNetCore.Mvc;


namespace Thesiss.Controllers
{
    /*
    
    public class BugController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BugController()
        {
            //_context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var bugs = await _context.Bugs.ToListAsync();
            return View(bugs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var bug = await _context.Bugs.FindAsync(id);
            if (bug == null)
            {
                return NotFound();
            }
            return View(bug);
        }

        
        public IActionResult Create()
        {
            return View();
        }


         [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bug bug)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bug);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bug);
        }

        // Bug edit (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var bug = await _context.Bugs.FindAsync(id);
            if (bug == null)
            {
                return NotFound();
            }
            return View(bug);
        }

        // Bug edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bug bug)
        {
            if (id != bug.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                //try
                //{
                //    _context.Update(bug);
                //    await _context.SaveChangesAsync();
                //}
                //catch (DbUpdateConcurrencyException)
                //{
                //    if (!_context.Bugs.Any(b => b.Id == bug.Id))
                //        return NotFound();
                //    throw;
                //}
                //return RedirectToAction(nameof(Index));
            }
            return View(bug);
        }

        // Bug delete (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var bug = await _context.Bugs.FindAsync(id);
            if (bug == null) return NotFound();
            return View(bug);
        }

        // Bug delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bug = await _context.Bugs.FindAsync(id);
            if (bug != null)
            {
                _context.Bugs.Remove(bug);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
    */
}
