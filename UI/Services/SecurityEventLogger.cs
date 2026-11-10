using Business.Contracts;
using Business.Services.Logging.SecurityLogger;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace UI.Services
{
    /// <summary>
    /// UI implementation of SecurityEventLoggerBase.
    /// Thin wrapper that inherits all security event logging methods from the shared base class.
    /// Registered in DI container for use throughout UI.
    /// 
    /// Constructor Parameters:
    /// - logger: For writing to Serilog (file logs)
    /// - httpContextAccessor: For accessing HttpContext to extract IP, UserAgent, CorrelationId
    /// - serviceProvider: For resolving SecurityAuditService to persist events to database
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

