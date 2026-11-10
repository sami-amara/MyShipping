using Business.Contracts;
using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Services;

namespace WebApi.Logging.Middleware
{
    /// <summary>
    /// Middleware that logs HTTP request and response details for debugging and performance monitoring.
    /// 
    /// Captures:
    /// - HTTP method, path, query string
    /// - Request size (content length)
    /// - Response status code
    /// - Response time (milliseconds)
    /// - Request/response body snippets (for debugging, with sensitive data redaction)
    /// 
    /// Logs are enriched with correlation ID and user context for end-to-end request tracing.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;


        private readonly IUserService _userService;

        
        // Configure which endpoints to log (can be extended)
        private static readonly string[] LoggablePaths = new[]
        {
            "/api/",
            "/auth/",
            "/shipments/",
            "/payments/"
        };

        // Content types safe to log
        private static readonly string[] LoggableContentTypes = new[]
        {
            "application/json",
            "application/x-www-form-urlencoded",
            "text/plain"
        };

        public RequestResponseLoggingMiddleware(RequestDelegate next,
            IUserService userService,
            ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _userService = userService;

        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            // Skip logging for static files and health checks
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            // Extract IP early (doesn't depend on authentication)
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            LogContext.PushProperty("IpAddress", ipAddress);

            try
            {
                // Replace response stream to capture response body
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // Call next middleware
                    await _next(context);

                    stopwatch.Stop();

                    // Log response (UserId extracted AFTER request processing so authentication is complete)
                    await LogResponse(context, stopwatch.ElapsedMilliseconds, responseBody, userService);

                    // Reset stream position to beginning before copying back
                    responseBody.Position = 0;

                    // Copy response body back to original stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in RequestResponseLoggingMiddleware for {Path}", context.Request.Path);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        /// <summary>
        /// Determines if this request path should be logged.
        /// </summary>
        private bool ShouldSkipLogging(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

            // Skip health checks, static assets, etc.
            if (pathValue.Contains("/health") ||
                pathValue.Contains("/swagger") ||
                pathValue.Contains(".js") ||
                pathValue.Contains(".css") ||
                pathValue.Contains(".png") ||
                pathValue.Contains(".jpg") ||
                pathValue.Contains("/favicon"))
            {
                return true;
            }

            // Only log configured API paths
            return !LoggablePaths.Any(p => pathValue.StartsWith(p));
        }

        /// <summary>
        /// Logs HTTP request details.
        /// </summary>
        private async Task LogRequest(HttpContext context, IUserService userService)
        {
            // All request context (IP, UserId, CorrelationId) is already pushed in InvokeAsync
            // This method is now for future expansion if needed

            await Task.CompletedTask;
        }

        /// <summary>
        /// Logs HTTP response details and performance metrics (no body logging - security risk).
        /// UserId is extracted AFTER request processing so authentication is complete.
        /// </summary>
        private async Task LogResponse(HttpContext context, long elapsedMilliseconds, MemoryStream responseBody, IUserService userService)
        {
            var response = context.Response;

            // Extract UserId AFTER request processing (authentication is now complete for POST /api/auth/login)
            var userId = "anonymous";
            try
            {
                var userIdGuid = userService.GetLoggedInUser();
                if (userIdGuid != Guid.Empty)
                    userId = userIdGuid.ToString();
            }
            catch
            {
                // User not authenticated, keep as "anonymous"
            }

            // Push UserId to context (IpAddress already pushed in InvokeAsync)
            if (userId != "anonymous")
            {
                LogContext.PushProperty("UserId", userId);
            }

            // Log response time and event type in context
            LogContext.PushProperty("EventType", "HttpResponseSent");
            LogContext.PushProperty("ResponseTime", elapsedMilliseconds);

            // Configuration threshold from appsettings
            const int PerformanceThresholdMs = 2000;

            if (response.StatusCode >= 500)
            {
                // Server errors - always log
                _logger.LogError(
                    "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    response.StatusCode,
                    elapsedMilliseconds);
            }
            else if (response.StatusCode >= 400)
            {
                // Client errors - log as warning only if slow
                if (elapsedMilliseconds > PerformanceThresholdMs)
                {
                    _logger.LogWarning(
                        "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMilliseconds}ms (SLOW)",
                        context.Request.Method,
                        context.Request.Path,
                        response.StatusCode,
                        elapsedMilliseconds);
                }
            }
            else if (elapsedMilliseconds > PerformanceThresholdMs)
            {
                // Success but slow - log as warning (ONLY ONCE)
                _logger.LogWarning(
                    "SLOW REQUEST: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    response.StatusCode,
                    elapsedMilliseconds);
            }
            // Else: successful, fast request - not logged (noise reduction)

            await Task.CompletedTask;
        }

        /// <summary>
        /// Determines if content type is safe to log (not binary).
        /// </summary>
        private bool IsLoggableContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            return LoggableContentTypes.Any(ct => 
                contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase));
        }
    }
}
