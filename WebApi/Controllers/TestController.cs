using Business.Contracts.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApi.Logging.SecurityLogger;

namespace WebApi.Controllers
{
    public class TestController : Controller
    {

        private readonly ISecurityEventLogger _securityLogger;

        public TestController(ISecurityEventLogger securityLogger)
        {
            _securityLogger = securityLogger;
        }


        [HttpGet("test-token-reuse")]
        [AllowAnonymous]
        public async Task<IActionResult> TestTokenReuse()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _securityLogger.LogTokenReuseDetected(
                    userId,
                    token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.TEST_TOKEN_REUSE_DETECTION"
                );

                return Ok(new
                {
                    Success = true,
                    Message = "TOKEN_REUSE_DETECTED event logged successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }


        //[HttpPost("tokenreuse")]
        //public async Task<IActionResult> SimulateTokenReuse()
        //{
        //    // Call the logger base method, which internally queues the audit log
        //    await _securityLogger.LogTokenReuseDetected(
        //        userId: "test-user",
        //        token: "FAKE-TOKEN-FOR-TESTING"
        //    );

        //    return Ok(new { message = "Simulated TokenReuse event logged and queued." });
        //}
    }
}
