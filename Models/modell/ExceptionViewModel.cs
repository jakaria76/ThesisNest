using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Thesiss.Models.modell
{
    [Route("Exception")]
    public class ExceptionController : Controller
    {
        private readonly ILogger<ExceptionController> _logger;

        public ExceptionController(ILogger<ExceptionController> logger)
        {
            _logger = logger;
        }
 


        [Route("")]
        public IActionResult Index()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;

            //var model = new ExceptionViewModel
            //{
            //    RequestId = HttpContext.TraceIdentifier,
            //    StatusCode = (int)HttpStatusCode.InternalServerError,
            //    Message = exception?.Message ?? "An unexpected error occurred.",
            //    StackTrace = exception?.StackTrace
            //};

            _logger.LogError(exception, "Unhandled exception occurred");

            return View("Exception");
        }


        //[Route("{statusCode:int}")]
        //public IActionResult Status(int statusCode)
        //{
        //    var model = new ExceptionViewModel
        //    {
        //        RequestId = HttpContext.TraceIdentifier,
        //        StatusCode = statusCode
        //    };

        //    switch (statusCode)
        //    {
        //        case 404:
        //            model.Message = "The page you are looking for was not found.";
        //            break;
        //        case 401:
        //            model.Message = "Unauthorized: You need to log in.";
        //            break;
        //        case 403:
        //            model.Message = "Forbidden: You don’t have permission to view this page.";
        //            break;
        //        default:
        //            model.Message = "Oops! Something went wrong.";
        //            break;
        //    }

        //    _logger.LogWarning("Status code {StatusCode} occurred.", statusCode);

        //    return View("Exception", model);
        //}
    }
}
