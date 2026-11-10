using Business.Contracts;
using Business.Services.Logging.SecurityLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApi.Logging.SecurityLogger
{
    /// <summary>
    /// WebApi implementation of SecurityEventLoggerBase.
    /// Thin wrapper that inherits all security event logging methods from the shared base class.
    /// Registered in DI container for use throughout WebApi.
    /// </summary>
   

    public class SecurityEventLogger : SecurityEventLoggerBase
    {
        public SecurityEventLogger(
            ILogger<SecurityEventLogger> logger,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)  // 🆕 Third parameter for database persistence
            : base(logger, httpContextAccessor, serviceProvider)  // 🆕 Pass all three to base class
        {
        }
    }
}
