using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Thesiss.Models.modell;


namespace WebApplication1.Controllers.controllerss
{
    [Route("Error")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

       

        [Route("")]
        public IActionResult Index()
        {
            // Default Problem Details
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;

            var model = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                Message = exception?.Message ?? "An unexpected error occurred.",
                StackTrace = exception?.StackTrace,
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            _logger.LogError(exception, "Unhandled exception occurred.");

            return View("Error", model);
        }

     



        [Route("{statusCode:int}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var model = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                StatusCode = statusCode
            };

            switch (statusCode)
            {
                case 404:
                    model.Message = "Sorry, the page you are looking for could not be found.";
                    break;
                case 401:
                    model.Message = "You are not authorized to access this resource.";
                    break;
                case 403:
                    model.Message = "Forbidden: You don’t have permission to view this page.";
                    break;
                default:
                    model.Message = "Oops! Something went wrong.";
                    break;
            }

            _logger.LogWarning("HTTP {StatusCode} error occurred.", statusCode);

            return View("Error", model);
        }




        [Route("Details")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Details()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = feature?.Error;

            var model = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                Message = exception?.Message,
                StackTrace = exception?.StackTrace,
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            return View("ErrorDetails", model);
        }
    }
}
