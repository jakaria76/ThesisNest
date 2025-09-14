// Controllers/GroqDiagController.cs
using Microsoft.AspNetCore.Mvc;
using ThesisNest.Services;

namespace ThesisNest.Controllers
{
    [ApiController]
    [Route("diag/groq")]
    public class GroqDiagController : ControllerBase
    {
        [HttpGet("status")]
        public async Task<IActionResult> Status([FromServices] GroqService svc)
        {
            var r = await svc.AskAsync("Say 'pong' only.");
            if (!r.Ok)
                return Content($"[Groq {r.Status}] {r.Reason}\nURL: {r.Url}\n{r.ErrorBody}", "text/plain");

            return Content($"OK: {r.Text}", "text/plain");
        }
    }
}
