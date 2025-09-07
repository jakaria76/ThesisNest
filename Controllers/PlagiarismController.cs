using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThesisNest.Data;
using ThesisNest.Models;
using ThesisNest.Services;
using ThesisNest.ViewModels;

namespace ThesisNest.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class PlagiarismController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileTextExtractor _extractor;
        private readonly GoogleSearchService _google;
        private readonly SimilarityService _sim;

        public PlagiarismController(
            ApplicationDbContext db,
            IFileTextExtractor extractor,
            GoogleSearchService google,
            SimilarityService sim)
        {
            _db = db;
            _extractor = extractor;
            _google = google;
            _sim = sim;
        }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAndCheck()
        {
            var file = Request.Form.Files.FirstOrDefault();
            string text;
            string filename = "DirectText";

            if (file != null && file.Length > 0)
            {
                filename = file.FileName;
                text = await _extractor.ExtractTextAsync(file);
            }
            else
            {
                text = Request.Form["plainText"].FirstOrDefault() ?? "";
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["Error"] = "No text or file provided.";
                return RedirectToAction(nameof(Index));
            }

            var doc = new PlagiarismDocument
            {
                FileName = filename,
                TextContent = text,
                UploadedAt = DateTime.UtcNow
            };
            _db.PlagiarismDocuments.Add(doc);
            await _db.SaveChangesAsync();

            // Local similarity against previously stored docs
            var others = await _db.PlagiarismDocuments.Where(d => d.Id != doc.Id).ToListAsync();
            float localMaxSim = 0f;
            int localMatchedId = 0;
            foreach (var o in others)
            {
                var s = _sim.ComputeCosineSimilarity(doc.TextContent, o.TextContent);
                if (s > localMaxSim) { localMaxSim = s; localMatchedId = o.Id; }
            }

            // Web search: sample long sentences (to save quota)
            var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+")
                                 .Select(s => s.Trim())
                                 .Where(s => s.Length > 40)
                                 .OrderByDescending(s => s.Length)
                                 .Take(8)
                                 .ToList();

            var webMatches = new System.Collections.Generic.List<WebMatch>();
            foreach (var s in sentences)
            {
                var items = await _google.SearchAsync(s, num: 3);
                foreach (var it in items)
                {
                    var score = _sim.ComputeCosineSimilarity(s, it.snippet) * 100f;
                    if (score > 65f) // threshold
                    {
                        webMatches.Add(new WebMatch { Sentence = s, Url = it.link, Snippet = it.snippet, Score = score });
                    }
                }
            }

            float webAvg = webMatches.Count > 0 ? webMatches.Average(m => m.Score / 100f) : 0f;
            float combined = (0.6f * localMaxSim + 0.4f * webAvg) * 100f;

            doc.CombinedScore = combined;
            await _db.SaveChangesAsync();

            var vm = new PlagiarismResultViewModel
            {
                DocumentId = doc.Id,
                FileName = doc.FileName,
                LocalBestMatchDocumentId = localMatchedId,
                LocalMaxSimilarity = localMaxSim * 100f,
                WebMatches = webMatches,
                CombinedScore = combined
            };

            return View("Result", vm);
        }
    }
}
