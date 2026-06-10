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
    }
}
