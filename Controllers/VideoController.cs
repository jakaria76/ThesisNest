using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Data;
using ThesisNest.Models;

namespace ThesisNest.Controllers;

[Authorize]
public class VideoController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _um;
    private readonly IWebHostEnvironment _env;

    public VideoController(ApplicationDbContext ctx, UserManager<ApplicationUser> um, IWebHostEnvironment env)
    {
        _ctx = ctx;
        _um = um;
        _env = env;
    }

    // =========================
    // Room by threadId (both roles)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Room(int threadId)
    {
        var me = await _um.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var thread = await _ctx.CommunicationThreads
            .Include(t => t.Teacher)
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == threadId && t.IsEnabled);

        if (thread is null)
        {
            TempData["Error"] = "Communication not enabled or thread not found.";
            return RedirectToAction("Index", "Home");
        }

        // membership check
        if (thread.Teacher.UserId != me.Id && thread.Student.UserId != me.Id)
            return Forbid();

        var vm = new VideoRoomVm
        {
            ThreadId = thread.Id,
            TeacherName = thread.Teacher?.FullName ?? "Teacher",
            StudentName = thread.Student?.FullName ?? "Student",
            IsTeacher = thread.Teacher?.UserId == me.Id
        };

        return View(vm);
    }

    // =========================
    // Student → open own enabled room
    // =========================
    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> MyRoom()
    {
        var me = await _um.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var student = await _ctx.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == me.Id);

        if (student is null) return Forbid();

        var thread = await _ctx.CommunicationThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.StudentProfileId == student.Id && t.IsEnabled);

        if (thread is null)
        {
            TempData["Error"] = "No enabled communication yet.";
            return RedirectToAction("Index", "StudentThesis");
        }

        return RedirectToAction(nameof(Room), new { threadId = thread.Id });
    }

    // =========================
    // Teacher/Admin → open room with specific student
    // =========================
    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet]
    public async Task<IActionResult> WithStudent(int studentProfileId)
    {
        var me = await _um.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var isAdmin = await _um.IsInRoleAsync(me, "Admin");

        int? myTeacherId = null;
        if (!isAdmin)
        {
            myTeacherId = await _ctx.TeacherProfiles
                .Where(t => t.UserId == me.Id)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();

            if (myTeacherId is null) return Forbid();
        }

        var q = _ctx.CommunicationThreads
            .AsNoTracking()
            .Where(t => t.StudentProfileId == studentProfileId && t.IsEnabled);

        if (!isAdmin)
            q = q.Where(t => t.TeacherProfileId == myTeacherId!.Value);

        var thread = await q.FirstOrDefaultAsync();

        if (thread is null)
        {
            TempData["Error"] = "No enabled communication with that student.";
            return RedirectToAction("Index", "ThesisReview");
        }

        return RedirectToAction(nameof(Room), new { threadId = thread.Id });
    }

    // =========================
    // Messages history (JSON) – used by comm.js → loadMessages()
    // =========================
    [HttpGet]
    public async Task<IActionResult> Messages(int threadId, int take = 50)
    {
        var me = await _um.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var allowed = await _ctx.CommunicationThreads
            .Include(t => t.Teacher)
            .Include(t => t.Student)
            .AnyAsync(t => t.Id == threadId
                        && t.IsEnabled
                        && (t.Teacher.UserId == me.Id || t.Student.UserId == me.Id));

        if (!allowed) return Forbid();

        take = Math.Clamp(take, 10, 200);

        var items = await _ctx.Messages
            .AsNoTracking()
            .Where(m => m.ThreadId == threadId)
            .OrderByDescending(m => m.SentAt)   // newest first
            .Take(take)
            .Select(m => new
            {
                id = m.Id,
                threadId = m.ThreadId,
                text = m.Text,
                sentAt = m.SentAt,
                senderUserId = m.SenderUserId
            })
            .ToListAsync();

        items.Reverse(); // oldest → newest for UI

        return Json(new { items });
    }

    // =========================
    // AJAX Upload (image/audio/video/docs/zip)
    // =========================
    [HttpPost]
    [IgnoreAntiforgeryToken]                  // সহজ AJAX-এর জন্য; চাইলে নীচের নোট দেখো
    [RequestSizeLimit(100_000_000)]           // ~100 MB
    public async Task<IActionResult> Upload(int threadId, IFormFile file, int? durationMs, int? width, int? height)
    {
        var me = await _um.GetUserAsync(User);
        if (me is null) return Unauthorized();

        var ok = await _ctx.CommunicationThreads
            .Include(t => t.Teacher).Include(t => t.Student)
            .AnyAsync(t => t.Id == threadId && t.IsEnabled &&
                           (t.Teacher.UserId == me.Id || t.Student.UserId == me.Id));
        if (!ok) return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file." });

        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        var allowedPrefixes = new[]
        {
            "image/","audio/","video/",
            "application/pdf","text/plain",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/zip"
        };
        if (!allowedPrefixes.Any(p => contentType.StartsWith(p)))
            return BadRequest(new { error = "File type not allowed." });

        // Save under wwwroot/uploads/threads/{threadId}/
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var saveDir = Path.Combine(webRoot, "uploads", "threads", threadId.ToString());
        Directory.CreateDirectory(saveDir);

        var ext = Path.GetExtension(file.FileName);
        var newName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(saveDir, newName);

        await using (var fs = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(fs);
        }

        var publicUrl = $"/uploads/threads/{threadId}/{newName}";

        return Json(new
        {
            ok = true,
            url = publicUrl,
            originalName = file.FileName,
            size = file.Length,
            contentType,
            durationMs,
            width,
            height
        });
    }
}
