using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThesisNest.Data;
using ThesisNest.Models.modell;

namespace ThesisNest.Controllers.controllerss
{
    public class UIController : Controller
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly ILogger<UIController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UIController(ApplicationDbContext context, ILogger<UIController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: UI/Index
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var users = _context.UIModels.AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    //users = users.Where(u => u.Name.Contains(searchString) || u.Email.Contains(searchString));
                }

                var count = await users.CountAsync();
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);

                var model = await users
                    //.OrderByDescending(static u => u.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.SearchString = searchString;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index fetch error.");
                // Safe fallback: empty list
                return View(Array.Empty<UIModel>());
            }
        }

        // GET: UI/Details/5
        //public IActionResult Details(int? id)
        //{
        //    if (id == null) return NotFound();

        //    try
        //    {
        //        //var user = await _context.UIModels.FirstOrDefaultAsync(u => u.Id == id);
        //        //if (user == null) return NotFound();
        //        //return View(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Details fetch error.");
        //        return View(new UIModel()); // Safe placeholder
        //    }
        //}

        // GET: UI/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UI/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UIModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (Request.Form.Files.Count > 0)
                    {
                        var file = Request.Form.Files[0];
                        var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploads, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        model.ProfilePicture = "/uploads/" + fileName;
                    }

                    _context.Add(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create error.");
                ModelState.AddModelError("", "Create failed, safe fallback.");
                return View(model);
            }
        }

        // GET: UI/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null) return NotFound();

        //    try
        //    {
        //        //var user = await _context.UIModels.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        //        //if (user == null) return NotFound();
        //        //return View(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Edit fetch error.");
        //        return View(new UIModel());
        //    }
        //}

        // POST: UI/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UIModel model)
        {
            if (id != model.Id) return BadRequest();

            try
            {
                if (ModelState.IsValid)
                {
                    if (Request.Form.Files.Count > 0)
                    {
                        var file = Request.Form.Files[0];
                        var uploads = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploads, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        model.ProfilePicture = "/uploads/" + fileName;
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit error.");
                ModelState.AddModelError("", "Edit failed, safe fallback.");
                return View(model);
            }
        }

        // GET: UI/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var user = (id);
                if (user == null) return NotFound();
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete fetch error.");
                return View(new UIModel());
            }
        }

        // POST: UI/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = (id);
                if (user != null)
                {
                    if (true)
                    {
                        
                    }

                    //_context.UIModels.Remove(user);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete error.");
                return RedirectToAction(nameof(Index));
            }
        }

        private bool UIModelExists(int id)
        {
            try
            {
                return false;
            }
            catch
            {
                return false; // Safe fallback
            }
        }

        // Toggle Active Status
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var user = (id);
                if (user == null) return Json(new { success = false });

                //user.IsActive = !user.IsActive;
                _context.Update(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleActive error.");
                return Json(new { success = false });
            }
        }

        // Bulk Delete
        [HttpPost]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            try
            {
                var users = _context.UIModels;
                //_context.UIModels.RemoveRange(users);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkDelete error.");
                return Json(new { success = false });
            }
        }

        // Search Autocomplete
        //[HttpGet]
        //public async Task<IActionResult> Search(string term)
        //{
        //    try
        //    {
        //        var users = await _context.UIModels
        //            .Where(u)
        //            .Select(u => new { u.Id, u.Name })
        //            .Take(10)
        //            .ToListAsync();

        //        return Json(users);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Search error.");
        //        return Json(new { success = false });
        //    }
        //}

        // Export CSV
        //public async Task<IActionResult> ExportCsv()
        //{
        //    try
        //    {
        //        var users = await _context.UIModels.ToListAsync();
        //        var csv = "Id,Name,Email,Role,Status,CreatedAt\n";
        //        foreach (var u in users)
        //        {
        //            csv += $"{u.Id},{u.Name},{u.Email},{u.Role},{u.IsActive},{u.CreatedAt:u}\n";
        //        }

        //        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        //        return File(bytes, "text/csv", "users.csv");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "ExportCsv error.");
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        // Profile
        //public async Task<IActionResult> Profile()
        //{
        //    try
        //    {
        //        var user = await _context.UIModels.FindAsync();
        //        if (user == null) return NotFound();
        //        return View(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Profile fetch error.");
        //        return View(new UIModel());
        //    }
        //}
    }
}
