using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProposalController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminProposalController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============== LIST ==============
        // সব প্রস্তাবের (ThesisVersions) তালিকা; সার্চ/ফিল্টার সাপোর্টেড
        public async Task<IActionResult> Index(string? q, int? deptId, int page = 1, int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);

            var query = _db.ThesisVersions
                .Include(v => v.Thesis)
                    .ThenInclude(t => t.Department)
                .Include(v => v.Thesis)
                    .ThenInclude(t => t.Student)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(v =>
                    v.Thesis.Title.ToLower().Contains(term) ||
                    (v.Thesis.Student != null && v.Thesis.Student.FullName.ToLower().Contains(term)) ||
                    (v.Thesis.Student != null && v.Thesis.Student.Email!.ToLower().Contains(term))
                );
            }

            if (deptId.HasValue && deptId.Value > 0)
                query = query.Where(v => v.Thesis.DepartmentId == deptId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.CreatedAt) // ধরে নিচ্ছি ThesisVersion-এ CreatedAt আছে; না থাকলে Id desc দিন
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.Departments = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();

            return View(items);
        }

        // ============== DOWNLOAD ==============
        [HttpGet]
        public async Task<IActionResult> Download(int versionId)
        {
            var version = await _db.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null) return NotFound();
            if (version.FileData == null || string.IsNullOrEmpty(version.ContentType))
                return NotFound();

            return File(version.FileData, version.ContentType, version.FileName);
        }

        // ============== PREVIEW (PDF/DOC/DOCX) ==============
        [HttpGet]
        public async Task<IActionResult> Preview(int versionId)
        {
            var version = await _db.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null || version.FileData == null)
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction(nameof(Index));
            }

            string fileType;
            if (version.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileType = "pdf";
                string base64String = Convert.ToBase64String(version.FileData);
                ViewBag.FileUrl = $"data:application/pdf;base64,{base64String}";
            }
            else if (version.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
                     version.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                fileType = "doc";
                ViewBag.FileUrl = Url.Action(nameof(RenderFileInline), new { versionId = version.Id });
            }
            else
            {
                fileType = "other";
            }

            ViewBag.PreviewType = fileType;
            ViewBag.ThesisId = version.ThesisId;
            ViewBag.ThesisVersionId = version.Id;

            return View("Preview");
        }

        // ============== INLINE RENDER (DOC/DOCX) ==============
        [HttpGet]
        public async Task<IActionResult> RenderFileInline(int versionId)
        {
            var version = await _db.ThesisVersions
                .Include(v => v.Thesis)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null || version.FileData == null)
                return NotFound();

            // ThesisReviewController-এর মত range processing দিচ্ছি যাতে ব্রাউজারে লোড হয়।:contentReference[oaicite:2]{index=2}
            return File(version.FileData, version.ContentType, version.FileName, enableRangeProcessing: true);
        }
    }
}
