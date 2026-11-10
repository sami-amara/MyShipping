using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class DiagnosticsController : ControllerBase
    {
        // GET api/diagnostics/whoami
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                Authenticated = User?.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User?.Identity?.AuthenticationType,
                Name = User?.Identity?.Name,
                Claims = User?.Claims.Select(c => new { c.Type, c.Value }).ToArray()
            });
        }

        /// <summary>
        /// Deliberately throws an exception for testing logging functionality.
        /// 
        /// This endpoint throws an InvalidOperationException to verify that:
        /// 1. Exceptions are logged by the ExceptionLoggingFilter
        /// 2. Events are captured in the security log file
        /// 3. Exception details are persisted to the database (if configured)
        /// 4. Correlation IDs and user context are properly enriched in logs
        /// 
        /// GET api/diagnostics/throw-exception
        /// </summary>
        [HttpGet("throw-exception")]
        public IActionResult ThrowException()
        {
            throw new InvalidOperationException(
                "This is a deliberate test exception to verify exception logging is working correctly. " +
                "Check the security log file and database for this exception entry with the current correlation ID."
            );
        }

        /// <summary>
        /// Throws a null reference exception for testing edge cases in logging.
        /// 
        /// GET api/diagnostics/throw-null-reference
        /// </summary>
        [HttpGet("throw-null-reference")]
        public IActionResult ThrowNullReferenceException()
        {
            object? nullObject = null;
            // Deliberately cause a null reference exception
            _ = nullObject.ToString();
            return Ok("This line will never execute");
        }

        [HttpGet("Test-Security-Exception")]
      
        public IActionResult TestSecurityException()
        {
            // This simulates a deliberate security-related failure
            throw new UnauthorizedAccessException(
                "🧪 TEST SECURITY EXCEPTION: Unauthorized access attempt detected. " +
                "This should be logged into security.txt and the database, " +
                "but not into app.txt as a normal exception.");
        }
    }
}
