//using Microsoft.AspNetCore.Mvc;

//namespace UI.Controllers
//{
//    public class ErrorController : Controller
//    {
//        public IActionResult Index()
//        {
//            return View();
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Models;

namespace UI.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        // GET: /Error
        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index(int? statusCode = null)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            statusCode ??= HttpContext.Response.StatusCode;

            _logger.LogError("Error page displayed. RequestId: {RequestId}, StatusCode: {StatusCode}, Path: {Path}",
                requestId, statusCode, HttpContext.Request.Path);

            var model = new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = statusCode,
                ErrorMessage = statusCode switch
                {
                    400 => "Bad Request - The request could not be understood by the server.",
                    401 => "Unauthorized - You need to log in to access this resource.",
                    403 => "Forbidden - You don't have permission to access this resource.",
                    404 => "Not Found - The page you're looking for doesn't exist.",
                    500 => "Internal Server Error - Something went wrong on our end.",
                    503 => "Service Unavailable - The service is temporarily unavailable.",
                    _ => "An unexpected error occurred."
                }
            };

            return View(model);
        }

        // GET: /Error/{statusCode}
        [Route("Error/{statusCode}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCodeError(int statusCode)
        {
            return Index(statusCode);
        }
    }
}