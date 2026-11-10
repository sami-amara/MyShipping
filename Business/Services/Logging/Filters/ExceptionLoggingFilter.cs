using Business.Contracts;
using Business.Contracts.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog.Context;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;

namespace Business.Services.Logging.Filters
{
    /// <summary>
    /// Professional exception logging MVC filter for centralized exception handling.
    /// Routes security exceptions to security.txt and app exceptions to app.txt.
    /// 
    /// Features:
    /// - Single Responsibility: Logs exceptions only, doesn't handle business logic
    /// - Dependency Inversion: Depends on ILogger abstract interface
    /// - Automatic correlation ID tracking for distributed debugging
    /// - User identity and email extraction from claims/context
    /// - Exception categorization for pattern analysis
    /// - RFC 7807 ProblemDetails response generation
    /// - Inner exception traversal for root cause analysis
    /// - Structured logging with Serilog for machine parsing
    /// - SECURITY-FOCUSED: Security exceptions logged to security.txt only
    /// - NON-SECURITY: Application exceptions logged to app.txt only
    /// </summary>
    public class ExceptionLoggingFilter : IExceptionFilter
    {
        private readonly ILogger<ExceptionLoggingFilter> _logger;
        private readonly IUserService _userService;
        private readonly ISecurityEventLogger _securityEventLogger;

        public ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger, IUserService userService,
            ISecurityEventLogger securityEventLogger)
        {
            _logger = logger;
            _userService = userService;
            _securityEventLogger = securityEventLogger;
        }

        /// <summary>
        /// MVC filter invoked when an exception occurs in the request pipeline.
        /// Routes security exceptions to security.txt and app exceptions to app.txt.
        /// </summary>
        public void OnException(ExceptionContext context)
        {
            if (context?.Exception == null)
                return;

            try
            {
                var exception = context.Exception;
                var httpContext = context.HttpContext;
                var request = httpContext.Request;

                var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
                    ? id?.ToString()
                    : "unknown";

                var userId = "anonymous";
                try { userId = _userService.GetLoggedInUser().ToString(); }
                catch { /* not authenticated */ }

                var email = httpContext.User?.FindFirst("email")?.Value
                    ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? "unknown";

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = request.Headers["User-Agent"].ToString() ?? "unknown";

                var exceptionDetails = BuildExceptionDetails(exception);
                var category = CategorizeException(exception);
                var logLevel = DetermineLogLevel(category);
                var isSecurityException = IsSecurityRelevant(exception);

                using (LogContext.PushProperty("CorrelationId", correlationId))
                using (LogContext.PushProperty("UserId", userId))
                using (LogContext.PushProperty("Email", email))
                using (LogContext.PushProperty("IpAddress", ipAddress))
                using (LogContext.PushProperty("UserAgent", userAgent))
                using (LogContext.PushProperty("EventType", "ExceptionOccurred"))
                using (LogContext.PushProperty("ExceptionCategory", category.ToString()))
                {
                    // ✅ SECURITY EXCEPTIONS → security.txt ONLY
                    if (isSecurityException)
                    {
                        _securityEventLogger.LogSecurityException(
                            correlationId,
                            exception,
                            userId,
                            email,
                            ipAddress,
                            request.Path
                        );
                    }
                    // ✅ NON-SECURITY EXCEPTIONS → app.txt ONLY
                    else
                    {
                        switch (logLevel)
                        {
                            case LogLevel.Critical:
                                _logger.LogCritical(exception,
                                    "CRITICAL EXCEPTION | {Method} {Path} | CorrelationId: {CorrelationId} | User: {UserId} | Message: {ExceptionMessage}",
                                    request.Method, request.Path, correlationId, userId, exception.Message);
                                break;
                            case LogLevel.Error:
                                _logger.LogError(exception,
                                    "Error in {Method} {Path} | Correlation: {CorrelationId} | User: {UserId} | Email: {Email} | Message: {ExceptionMessage}",
                                    request.Method, request.Path, correlationId, userId, email, exception.Message);
                                break;
                            case LogLevel.Warning:
                                _logger.LogWarning(exception,
                                    "Warning in {Method} {Path} | Correlation: {CorrelationId} | Message: {ExceptionMessage}",
                                    request.Method, request.Path, correlationId, exception.Message);
                                break;
                        }
                    }
                }

                var problemDetails = CreateProblemDetails(exception, correlationId, category, context);
                context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
                context.ExceptionHandled = true;
            }
            catch
            {
                context.ExceptionHandled = false;
            }
        }

        /// <summary>
        /// Builds detailed exception information object with all nested exceptions.
        /// Recursively handles inner exceptions for root cause analysis.
        /// </summary>
        private Dictionary<string, object> BuildExceptionDetails(Exception exception)
        {
            var details = new Dictionary<string, object>
            {
                { "Type", exception.GetType().FullName ?? "Unknown" },
                { "Message", exception.Message },
                { "StackTrace", exception.StackTrace ?? "N/A" },
                { "HResult", exception.HResult },
                { "Category", CategorizeException(exception).ToString() }
            };

            if (exception.InnerException != null)
            {
                details["InnerException"] = BuildExceptionDetails(exception.InnerException);
            }

            return details;
        }

        /// <summary>
        /// Categorizes exception into business-friendly categories.
        /// Used for pattern analysis, alerting, and monitoring.
        /// </summary>
        private ExceptionCategory CategorizeException(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException or ArgumentException
                    => ExceptionCategory.ValidationException,
                InvalidOperationException or NotImplementedException
                    => ExceptionCategory.BusinessLogicException,
                TimeoutException or System.Net.Http.HttpRequestException
                    => ExceptionCategory.InfrastructureException,
                UnauthorizedAccessException
                    => ExceptionCategory.SecurityException,
                KeyNotFoundException
                    => ExceptionCategory.DataAccessException,
                System.IO.IOException
                    => ExceptionCategory.InfrastructureException,
                OutOfMemoryException or StackOverflowException
                    => ExceptionCategory.InfrastructureException,
                SystemException when exception.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
                    => ExceptionCategory.DataAccessException,
                _ => ExceptionCategory.UnknownException
            };
        }

        /// <summary>
        /// Determines appropriate logging level based on exception category.
        /// Critical: Out of memory, Stack overflow → Critical
        /// Error: Database, business logic failures → Error
        /// Warning: Validation, not implemented → Warning
        /// </summary>
        private LogLevel DetermineLogLevel(ExceptionCategory category)
        {
            return category switch
            {
                ExceptionCategory.SecurityException => LogLevel.Warning,
                ExceptionCategory.ValidationException => LogLevel.Warning,
                ExceptionCategory.BusinessLogicException => LogLevel.Error,
                ExceptionCategory.InfrastructureException => LogLevel.Error,
                ExceptionCategory.DataAccessException => LogLevel.Error,
                _ => LogLevel.Error
            };
        }

        /// <summary>
        /// Creates RFC 7807 ProblemDetails response object.
        /// Standardized error response format for REST APIs.
        /// </summary>
        private Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
            Exception exception,
            string correlationId,
            ExceptionCategory category,
            ExceptionContext context)
        {
            var statusCode = DetermineStatusCode(category);

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Type = $"https://api.myshipping.com/errors/{category}",
                Title = "An error occurred processing your request",
                Detail = exception.Message,
                Status = statusCode,
                Instance = context.HttpContext.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    { "traceId", correlationId },
                    { "timestamp", DateTime.UtcNow.ToString("O") },
                    { "category", category.ToString() }
                }
            };

            return problemDetails;
        }

        /// <summary>
        /// Determines HTTP status code based on exception category.
        /// Follows REST conventions and HTTP semantics.
        /// </summary>
        private int DetermineStatusCode(ExceptionCategory category)
        {
            return category switch
            {
                ExceptionCategory.ValidationException => 400,
                ExceptionCategory.SecurityException => 401,
                ExceptionCategory.DataAccessException => 404,
                ExceptionCategory.BusinessLogicException => 400,
                ExceptionCategory.InfrastructureException => 503,
                _ => 500
            };
        }

        /// <summary>
        /// SINGLE SOURCE OF TRUTH: Determines if an exception is security-related.
        /// Returns true → goes to security.txt ONLY
        /// Returns false → goes to app.txt ONLY
        /// </summary>
        private bool IsSecurityRelevant(Exception ex)
        {
            return ex is AuthenticationException
                || ex is SecurityTokenException
                || ex is UnauthorizedAccessException
                || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
        }
    }
}
























//using Business.Contracts;
//using Business.Contracts.Logging;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using Serilog.Context;
//using Serilog.Events;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Authentication;

//namespace Business.Logging.Filters
//{
//    /// <summary>
//    /// Professional exception logging MVC filter for centralized exception handling.
//    /// Routes security exceptions to security.txt and app exceptions to app.txt.
//    /// </summary>
//    public class ExceptionLoggingFilter : IExceptionFilter
//    {
//        private readonly ILogger<ExceptionLoggingFilter> _logger;
//        private readonly IUserService _userService;
//        private readonly ISecurityEventLogger _securityEventLogger;

//        public ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger, IUserService userService,
//            ISecurityEventLogger securityEventLogger)
//        {
//            _logger = logger;
//            _userService = userService;
//            _securityEventLogger = securityEventLogger;
//        }

//        public void OnException(ExceptionContext context)
//        {
//            if (context?.Exception == null)
//                return;

//            try
//            {
//                var exception = context.Exception;
//                var httpContext = context.HttpContext;
//                var request = httpContext.Request;

//                var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
//                    ? id?.ToString()
//                    : "unknown";

//                var userId = "anonymous";
//                try { userId = _userService.GetLoggedInUser().ToString(); }
//                catch { /* not authenticated */ }

//                var email = httpContext.User?.FindFirst("email")?.Value
//                    ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
//                    ?? "unknown";

//                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//                var userAgent = request.Headers["User-Agent"].ToString() ?? "unknown";

//                var exceptionDetails = BuildExceptionDetails(exception);
//                var category = CategorizeException(exception);
//                var logLevel = DetermineLogLevel(category);
//                var isSecurityException = IsSecurityRelevant(exception);

//                using (LogContext.PushProperty("CorrelationId", correlationId))
//                using (LogContext.PushProperty("UserId", userId))
//                using (LogContext.PushProperty("Email", email))
//                using (LogContext.PushProperty("IpAddress", ipAddress))
//                using (LogContext.PushProperty("UserAgent", userAgent))
//                using (LogContext.PushProperty("EventType", "ExceptionOccurred"))
//                // ✅ KEY FIX: Add ExceptionCategory ALWAYS so filters can see it
//                using (LogContext.PushProperty("ExceptionCategory", category.ToString()))
//                {
//                    // ✅ SECURITY EXCEPTIONS → security.txt ONLY
//                    if (isSecurityException)
//                    {
//                        _securityEventLogger.LogSecurityException(
//                            correlationId,
//                            exception,
//                            userId,
//                            email,
//                            ipAddress,
//                            request.Path
//                        );
//                    }
//                    // ✅ NON-SECURITY EXCEPTIONS → app.txt ONLY
//                    else
//                    {
//                        switch (logLevel)
//                        {
//                            case LogLevel.Critical:
//                                _logger.LogCritical(exception,
//                                    "CRITICAL EXCEPTION | {Method} {Path} | CorrelationId: {CorrelationId} | User: {UserId} | Message: {ExceptionMessage}",
//                                    request.Method, request.Path, correlationId, userId, exception.Message);
//                                break;
//                            case LogLevel.Error:
//                                _logger.LogError(exception,
//                                    "Error in {Method} {Path} | Correlation: {CorrelationId} | User: {UserId} | Email: {Email} | Message: {ExceptionMessage}",
//                                    request.Method, request.Path, correlationId, userId, email, exception.Message);
//                                break;
//                            case LogLevel.Warning:
//                                _logger.LogWarning(exception,
//                                    "Warning in {Method} {Path} | Correlation: {CorrelationId} | Message: {ExceptionMessage}",
//                                    request.Method, request.Path, correlationId, exception.Message);
//                                break;
//                        }
//                    }
//                }

//                var problemDetails = CreateProblemDetails(exception, correlationId, category, context);
//                context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
//                context.ExceptionHandled = true;
//            }
//            catch
//            {
//                context.ExceptionHandled = false;
//            }
//        }

//        private Dictionary<string, object> BuildExceptionDetails(Exception exception)
//        {
//            var details = new Dictionary<string, object>
//            {
//                { "Type", exception.GetType().FullName ?? "Unknown" },
//                { "Message", exception.Message },
//                { "StackTrace", exception.StackTrace ?? "N/A" },
//                { "HResult", exception.HResult },
//                { "Category", CategorizeException(exception).ToString() }
//            };

//            if (exception.InnerException != null)
//            {
//                details["InnerException"] = BuildExceptionDetails(exception.InnerException);
//            }

//            return details;
//        }

//        private ExceptionCategory CategorizeException(Exception exception)
//        {
//            return exception switch
//            {
//                ArgumentNullException or ArgumentException
//                    => ExceptionCategory.ValidationException,
//                InvalidOperationException or NotImplementedException
//                    => ExceptionCategory.BusinessLogicException,
//                TimeoutException or System.Net.Http.HttpRequestException
//                    => ExceptionCategory.InfrastructureException,
//                UnauthorizedAccessException
//                    => ExceptionCategory.SecurityException,
//                KeyNotFoundException
//                    => ExceptionCategory.DataAccessException,
//                System.IO.IOException
//                    => ExceptionCategory.InfrastructureException,
//                OutOfMemoryException or StackOverflowException
//                    => ExceptionCategory.InfrastructureException,
//                SystemException when exception.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
//                    => ExceptionCategory.DataAccessException,
//                _ => ExceptionCategory.UnknownException
//            };
//        }

//        private LogLevel DetermineLogLevel(ExceptionCategory category)
//        {
//            return category switch
//            {
//                ExceptionCategory.SecurityException => LogLevel.Warning,
//                ExceptionCategory.ValidationException => LogLevel.Warning,
//                ExceptionCategory.BusinessLogicException => LogLevel.Error,
//                ExceptionCategory.InfrastructureException => LogLevel.Error,
//                ExceptionCategory.DataAccessException => LogLevel.Error,
//                _ => LogLevel.Error
//            };
//        }

//        private Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
//            Exception exception,
//            string correlationId,
//            ExceptionCategory category,
//            ExceptionContext context)
//        {
//            var statusCode = DetermineStatusCode(category);

//            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
//            {
//                Type = $"https://api.myshipping.com/errors/{category}",
//                Title = "An error occurred processing your request",
//                Detail = exception.Message,
//                Status = statusCode,
//                Instance = context.HttpContext.Request.Path,
//                Extensions = new Dictionary<string, object?>
//                {
//                    { "traceId", correlationId },
//                    { "timestamp", DateTime.UtcNow.ToString("O") },
//                    { "category", category.ToString() }
//                }
//            };

//            return problemDetails;
//        }

//        private int DetermineStatusCode(ExceptionCategory category)
//        {
//            return category switch
//            {
//                ExceptionCategory.ValidationException => 400,
//                ExceptionCategory.SecurityException => 401,
//                ExceptionCategory.DataAccessException => 404,
//                ExceptionCategory.BusinessLogicException => 400,
//                ExceptionCategory.InfrastructureException => 503,
//                _ => 500
//            };
//        }

//        /// <summary>
//        /// SINGLE SOURCE OF TRUTH: Determines if an exception is security-related.
//        /// Returns true → goes to security.txt ONLY
//        /// Returns false → goes to app.txt ONLY
//        /// </summary>
//        private bool IsSecurityRelevant(Exception ex)
//        {
//            return ex is AuthenticationException
//                || ex is SecurityTokenException
//                || ex is UnauthorizedAccessException
//                || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
//                || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
//                || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
//                || ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
//                || ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
//        }
//    }
//}











////using Business.Contracts;
////using Business.Contracts.Logging;
////using Microsoft.AspNetCore.Mvc;
////using Microsoft.AspNetCore.Mvc.Filters;
////using Microsoft.Extensions.Logging;
////using Microsoft.IdentityModel.Tokens;
////using Serilog.Context;
////using Serilog.Events;
////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Security.Authentication;

////namespace Business.Logging.Filters
////{
////    /// <summary>
////    /// Professional exception logging MVC filter for centralized exception handling.
////    /// Captures all exceptions with full diagnostic context and logs them via Serilog.
////    /// 
////    /// ORIGINALLY FROM: WebApi.Filters.ExceptionLoggingFilter
////    /// MOVED TO: Business.Filters.ExceptionLoggingFilter (shared between WebApi & UI)
////    /// 
////    /// SOLID Compliance:
////    /// - Single Responsibility: Logs exceptions only, doesn't handle business logic
////    /// - Open/Closed: Extends IExceptionFilter without modifying contracts
////    /// - Dependency Inversion: Depends on ILogger abstract interface
////    /// - Interface Segregation: Implements minimal IExceptionFilter interface
////    /// 
////    /// Features:
////    /// - Automatic correlation ID tracking for distributed debugging
////    /// - User identity and email extraction from claims/context
////    /// - Exception categorization for pattern analysis
////    /// - RFC 7807 ProblemDetails response generation
////    /// - Inner exception traversal for root cause analysis
////    /// - Structured logging with Serilog for machine parsing
////    /// - SECURITY-FOCUSED: Security exceptions logged to security.txt only
////    /// - NON-SECURITY: Application exceptions logged to app.txt only
////    /// </summary>
////    public class ExceptionLoggingFilter : IExceptionFilter
////    {
////        private readonly ILogger<ExceptionLoggingFilter> _logger;

////        private readonly IUserService _userService;

////        private readonly ISecurityEventLogger _securityEventLogger;
////        public ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger, IUserService userService,
////            ISecurityEventLogger securityEventLogger)
////        {
////            _logger = logger;
////            _userService = userService;
////            _securityEventLogger = securityEventLogger;
////        }

////        /// <summary>
////        /// MVC filter invoked when an exception occurs in the request pipeline.
////        /// Routes security exceptions to security.txt and app exceptions to app.txt.
////        /// </summary>
////        public void OnException(ExceptionContext context)
////        {
////            if (context?.Exception == null)
////                return;

////            try
////            {
////                var exception = context.Exception;
////                var httpContext = context.HttpContext;
////                var request = httpContext.Request;

////                var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
////                    ? id?.ToString()
////                    : "unknown";

////                var userId = "anonymous";
////                try { userId = _userService.GetLoggedInUser().ToString(); }
////                catch { /* not authenticated */ }

////                var email = httpContext.User?.FindFirst("email")?.Value
////                    ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
////                    ?? "unknown";

////                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
////                var userAgent = request.Headers["User-Agent"].ToString() ?? "unknown";

////                var exceptionDetails = BuildExceptionDetails(exception);
////                var category = CategorizeException(exception);
////                var logLevel = DetermineLogLevel(category);
////                var isSecurityException = IsSecurityRelevant(exception);

////                using (LogContext.PushProperty("CorrelationId", correlationId))
////                using (LogContext.PushProperty("UserId", userId))
////                using (LogContext.PushProperty("Email", email))
////                using (LogContext.PushProperty("IpAddress", ipAddress))
////                using (LogContext.PushProperty("UserAgent", userAgent))
////                using (LogContext.PushProperty("EventType", "ExceptionOccurred"))
////                using (LogContext.PushProperty("ExceptionCategory", category.ToString()))
////                {
////                    // ✅ SECURITY EXCEPTIONS → security.txt ONLY
////                    if (isSecurityException)
////                    {
////                        _securityEventLogger.LogSecurityException(
////                            correlationId,
////                            exception,
////                            userId,
////                            email,
////                            ipAddress,
////                            request.Path
////                        );
////                    }
////                    // ✅ NON-SECURITY EXCEPTIONS → app.txt ONLY
////                    else
////                    {
////                        switch (logLevel)
////                        {
////                            case LogLevel.Critical:
////                                _logger.LogCritical(exception,
////                                    "CRITICAL EXCEPTION | {Method} {Path} | CorrelationId: {CorrelationId} | User: {UserId} | Message: {ExceptionMessage}",
////                                    request.Method, request.Path, correlationId, userId, exception.Message);
////                                break;
////                            case LogLevel.Error:
////                                _logger.LogError(exception,
////                                    "Error in {Method} {Path} | Correlation: {CorrelationId} | User: {UserId} | Email: {Email} | Message: {ExceptionMessage}",
////                                    request.Method, request.Path, correlationId, userId, email, exception.Message);
////                                break;
////                            case LogLevel.Warning:
////                                _logger.LogWarning(exception,
////                                    "Warning in {Method} {Path} | Correlation: {CorrelationId} | Message: {ExceptionMessage}",
////                                    request.Method, request.Path, correlationId, exception.Message);
////                                break;
////                        }
////                    }
////                }

////                var problemDetails = CreateProblemDetails(exception, correlationId, category, context);
////                context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
////                context.ExceptionHandled = true;
////            }
////            catch
////            {
////                context.ExceptionHandled = false;
////            }
////        }


////        /// <summary>
////        /// Builds detailed exception information object with all nested exceptions.
////        /// Recursively handles inner exceptions for root cause analysis.
////        /// </summary>
////        private Dictionary<string, object> BuildExceptionDetails(Exception exception)
////        {
////            var details = new Dictionary<string, object>
////            {
////                { "Type", exception.GetType().FullName ?? "Unknown" },
////                { "Message", exception.Message },
////                { "StackTrace", exception.StackTrace ?? "N/A" },
////                { "HResult", exception.HResult },
////                { "Category", CategorizeException(exception).ToString() }
////            };

////            // Recursively add inner exception details
////            if (exception.InnerException != null)
////            {
////                details["InnerException"] = BuildExceptionDetails(exception.InnerException);
////            }

////            return details;
////        }

////        /// <summary>
////        /// Categorizes exception into business-friendly categories.
////        /// Used for pattern analysis, alerting, and monitoring.
////        /// </summary>
////        private ExceptionCategory CategorizeException(Exception exception)
////        {
////            return exception switch
////            {
////                ArgumentNullException or ArgumentException
////                    => ExceptionCategory.ValidationException,

////                InvalidOperationException or NotImplementedException
////                    => ExceptionCategory.BusinessLogicException,

////                TimeoutException or System.Net.Http.HttpRequestException
////                    => ExceptionCategory.InfrastructureException,

////                UnauthorizedAccessException
////                    => ExceptionCategory.SecurityException,

////                KeyNotFoundException
////                    => ExceptionCategory.DataAccessException,

////                System.IO.IOException
////                    => ExceptionCategory.InfrastructureException,

////                OutOfMemoryException or StackOverflowException
////                    => ExceptionCategory.InfrastructureException,

////                SystemException when exception.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
////                    => ExceptionCategory.DataAccessException,

////                _ => ExceptionCategory.UnknownException
////            };
////        }

////        /// <summary>
////        /// Determines appropriate logging level based on exception category.
////        /// Critical: Out of memory, Stack overflow → Critical
////        /// Error: Database, business logic failures → Error
////        /// Warning: Validation, not implemented → Warning
////        /// </summary>
////        private LogLevel DetermineLogLevel(ExceptionCategory category)
////        {
////            return category switch
////            {
////                ExceptionCategory.SecurityException => LogLevel.Warning,
////                ExceptionCategory.ValidationException => LogLevel.Warning,
////                ExceptionCategory.BusinessLogicException => LogLevel.Error,
////                ExceptionCategory.InfrastructureException => LogLevel.Error,
////                ExceptionCategory.DataAccessException => LogLevel.Error,
////                _ => LogLevel.Error
////            };
////        }

////        /// <summary>
////        /// Creates RFC 7807 ProblemDetails response object.
////        /// Standardized error response format for REST APIs.
////        /// </summary>
////        private Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
////            Exception exception,
////            string correlationId,
////            ExceptionCategory category,
////            ExceptionContext context)
////        {
////            var statusCode = DetermineStatusCode(category);

////            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
////            {
////                Type = $"https://api.myshipping.com/errors/{category}",
////                Title = "An error occurred processing your request",
////                Detail = exception.Message,
////                Status = statusCode,
////                Instance = context.HttpContext.Request.Path,
////                Extensions = new Dictionary<string, object?>
////                {
////                    { "traceId", correlationId },
////                    { "timestamp", DateTime.UtcNow.ToString("O") },
////                    { "category", category.ToString() }
////                }
////            };

////            return problemDetails;
////        }

////        /// <summary>
////        /// Determines HTTP status code based on exception category.
////        /// Follows REST conventions and HTTP semantics.
////        /// </summary>
////        private int DetermineStatusCode(ExceptionCategory category)
////        {
////            return category switch
////            {
////                ExceptionCategory.ValidationException => 400,       // Bad Request
////                ExceptionCategory.SecurityException => 401,         // Unauthorized
////                ExceptionCategory.DataAccessException => 404,       // Not Found
////                ExceptionCategory.BusinessLogicException => 400,    // Bad Request
////                ExceptionCategory.InfrastructureException => 503,   // Service Unavailable
////                _ => 500                                            // Internal Server Error
////            };
////        }

////        /// <summary>
////        /// SINGLE SOURCE OF TRUTH for security classification.
////        /// Determines if an exception is security-related and should be logged to security.txt.
////        /// Returns true: log to security.txt ONLY
////        /// Returns false: log to app.txt ONLY
////        /// </summary>
////        private bool IsSecurityRelevant(Exception ex)
////        {
////            // Security-related exceptions
////            return ex is AuthenticationException
////                || ex is SecurityTokenException
////                || ex is UnauthorizedAccessException
////                || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
////                || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
////                || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
////                || ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
////                || ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
////        }
////    }
////}






















//////using Business.Contracts;
//////using Business.Contracts.Logging;
//////using Microsoft.AspNetCore.Mvc;
//////using Microsoft.AspNetCore.Mvc.Filters;
//////using Microsoft.Extensions.Logging;
//////using Microsoft.IdentityModel.Tokens;
//////using Serilog.Context;
//////using Serilog.Events;
//////using System;
//////using System.Collections.Generic;
//////using System.Linq;
//////using System.Security.Authentication;

//////namespace Business.Logging.Filters
//////{
//////    /// <summary>
//////    /// Professional exception logging MVC filter for centralized exception handling.
//////    /// Captures all exceptions with full diagnostic context and logs them via Serilog.
//////    /// 
//////    /// ORIGINALLY FROM: WebApi.Filters.ExceptionLoggingFilter
//////    /// MOVED TO: Business.Filters.ExceptionLoggingFilter (shared between WebApi & UI)
//////    /// 
//////    /// SOLID Compliance:
//////    /// - Single Responsibility: Logs exceptions only, doesn't handle business logic
//////    /// - Open/Closed: Extends IExceptionFilter without modifying contracts
//////    /// - Dependency Inversion: Depends on ILogger abstract interface
//////    /// - Interface Segregation: Implements minimal IExceptionFilter interface
//////    /// 
//////    /// Features:
//////    /// - Automatic correlation ID tracking for distributed debugging
//////    /// - User identity and email extraction from claims/context
//////    /// - Exception categorization for pattern analysis
//////    /// - RFC 7807 ProblemDetails response generation
//////    /// - Inner exception traversal for root cause analysis
//////    /// - Structured logging with Serilog for machine parsing
//////    /// </summary>
//////    public class ExceptionLoggingFilter : IExceptionFilter
//////    {
//////        private readonly ILogger<ExceptionLoggingFilter> _logger;

//////        private readonly IUserService _userService;

//////        private readonly ISecurityEventLogger _securityEventLogger;
//////        public ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger, IUserService userService, 
//////            ISecurityEventLogger securityEventLogger)
//////        {
//////            _logger = logger;
//////            _userService = userService;
//////            _securityEventLogger = securityEventLogger;
//////        }


//////        /// <summary>
//////        /// MVC filter invoked when an exception occurs in the request pipeline.
//////        /// Routes security exceptions to security.txt and app exceptions to app.txt.
//////        /// </summary>
//////        public void OnException(ExceptionContext context)
//////        {
//////            if (context?.Exception == null)
//////                return;

//////            try
//////            {
//////                var exception = context.Exception;
//////                var httpContext = context.HttpContext;
//////                var request = httpContext.Request;

//////                var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
//////                    ? id?.ToString()
//////                    : "unknown";

//////                var userId = "anonymous";
//////                try { userId = _userService.GetLoggedInUser().ToString(); }
//////                catch { /* not authenticated */ }

//////                var email = httpContext.User?.FindFirst("email")?.Value
//////                    ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
//////                    ?? "unknown";

//////                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//////                var userAgent = request.Headers["User-Agent"].ToString() ?? "unknown";

//////                var exceptionDetails = BuildExceptionDetails(exception);
//////                var category = CategorizeException(exception);
//////                var logLevel = DetermineLogLevel(category);
//////                var isSecurityException = IsSecurityRelevant(exception);

//////                using (LogContext.PushProperty("CorrelationId", correlationId))
//////                using (LogContext.PushProperty("UserId", userId))
//////                using (LogContext.PushProperty("Email", email))
//////                using (LogContext.PushProperty("IpAddress", ipAddress))
//////                using (LogContext.PushProperty("UserAgent", userAgent))
//////                using (LogContext.PushProperty("EventType", "ExceptionOccurred"))
//////                using (LogContext.PushProperty("ExceptionCategory", category.ToString()))
//////                {
//////                    // ✅ SECURITY EXCEPTIONS → security.txt ONLY
//////                    if (isSecurityException)
//////                    {
//////                        _securityEventLogger.LogSecurityException(
//////                            correlationId,
//////                            exception,
//////                            userId,
//////                            email,
//////                            ipAddress,
//////                            request.Path
//////                        );
//////                    }
//////                    // ✅ NON-SECURITY EXCEPTIONS → app.txt ONLY
//////                    else
//////                    {
//////                        switch (logLevel)
//////                        {
//////                            case LogLevel.Critical:
//////                                _logger.LogCritical(exception,
//////                                    "CRITICAL EXCEPTION | {Method} {Path} | CorrelationId: {CorrelationId} | User: {UserId} | Message: {ExceptionMessage}",
//////                                    request.Method, request.Path, correlationId, userId, exception.Message);
//////                                break;
//////                            case LogLevel.Error:
//////                                _logger.LogError(exception,
//////                                    "Error in {Method} {Path} | Correlation: {CorrelationId} | User: {UserId} | Email: {Email} | Message: {ExceptionMessage}",
//////                                    request.Method, request.Path, correlationId, userId, email, exception.Message);
//////                                break;
//////                            case LogLevel.Warning:
//////                                _logger.LogWarning(exception,
//////                                    "Warning in {Method} {Path} | Correlation: {CorrelationId} | Message: {ExceptionMessage}",
//////                                    request.Method, request.Path, correlationId, exception.Message);
//////                                break;
//////                        }
//////                    }
//////                }

//////                var problemDetails = CreateProblemDetails(exception, correlationId, category, context);
//////                context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
//////                context.ExceptionHandled = true;
//////            }
//////            catch
//////            {
//////                context.ExceptionHandled = false;
//////            }
//////        }



//////        ///// <summary>
//////        ///// MVC filter invoked when an exception occurs in the request pipeline.
//////        ///// Logs exception context and generates appropriate HTTP response.
//////        ///// </summary>
//////        ///// 
//////        //public void OnException(ExceptionContext context)
//////        //{
//////        //    if (context?.Exception == null)
//////        //        return;

//////        //    try
//////        //    {
//////        //        var exception = context.Exception;
//////        //        var httpContext = context.HttpContext;
//////        //        var request = httpContext.Request;

//////        //        var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
//////        //            ? id?.ToString()
//////        //            : "unknown";

//////        //        var userId = "anonymous";
//////        //        try { userId = _userService.GetLoggedInUser().ToString(); }
//////        //        catch { /* not authenticated */ }

//////        //        var email = httpContext.User?.FindFirst("email")?.Value
//////        //            ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
//////        //            ?? "unknown";

//////        //        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//////        //        var userAgent = request.Headers["User-Agent"].ToString() ?? "unknown";

//////        //        var exceptionDetails = BuildExceptionDetails(exception);
//////        //        var category = CategorizeException(exception);
//////        //        var logLevel = DetermineLogLevel(category);

//////        //        using (LogContext.PushProperty("CorrelationId", correlationId))
//////        //        using (LogContext.PushProperty("UserId", userId))
//////        //        using (LogContext.PushProperty("Email", email))
//////        //        using (LogContext.PushProperty("IpAddress", ipAddress))
//////        //        using (LogContext.PushProperty("UserAgent", userAgent))
//////        //        using (LogContext.PushProperty("EventType", "ExceptionOccurred"))
//////        //        using (LogContext.PushProperty("ExceptionCategory", category.ToString()))
//////        //        {
//////        //            // Always log to app.txt
//////        //            // Always log to app.txt unless it's security relevant
//////        //            if (!IsSecurityRelevant(exception))
//////        //            {
//////        //                switch (logLevel)
//////        //                {
//////        //                    case LogLevel.Critical:
//////        //                        _logger.LogCritical(exception,
//////        //                            "CRITICAL EXCEPTION | {Method} {Path} | CorrelationId: {CorrelationId} | User: {UserId} | Message: {ExceptionMessage}",
//////        //                            request.Method, request.Path, correlationId, userId, exception.Message);
//////        //                        break;
//////        //                    case LogLevel.Error:
//////        //                        _logger.LogError(exception,
//////        //                            "Error in {Method} {Path} | Correlation: {CorrelationId} | User: {UserId} | Email: {Email} | Message: {ExceptionMessage}",
//////        //                            request.Method, request.Path, correlationId, userId, email, exception.Message);
//////        //                        break;
//////        //                    case LogLevel.Warning:
//////        //                        _logger.LogWarning(exception,
//////        //                            "Warning in {Method} {Path} | Correlation: {CorrelationId} | Message: {ExceptionMessage}",
//////        //                            request.Method, request.Path, correlationId, exception.Message);
//////        //                        break;
//////        //                }
//////        //            }

//////        //            // ✅ Only log to security.txt if relevant
//////        //            if (IsSecurityRelevant(exception))
//////        //            {
//////        //                _securityEventLogger.LogSecurityException(
//////        //                    correlationId,
//////        //                    exception,
//////        //                    userId,
//////        //                    email,
//////        //                    ipAddress,
//////        //                    request.Path
//////        //                );
//////        //            }
//////        //        }

//////        //        var problemDetails = CreateProblemDetails(exception, correlationId, category, context);
//////        //        context.Result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
//////        //        context.ExceptionHandled = true;
//////        //    }
//////        //    catch (Exception filterException)
//////        //    {
//////        //        _logger.LogError(filterException, "Failed to log exception properly in filter");
//////        //        context.ExceptionHandled = false;
//////        //    }
//////        //}


//////        /// <summary>
//////        /// Builds detailed exception information object with all nested exceptions.
//////        /// Recursively handles inner exceptions for root cause analysis.
//////        /// </summary>
//////        private Dictionary<string, object> BuildExceptionDetails(Exception exception)
//////        {
//////            var details = new Dictionary<string, object>
//////            {
//////                { "Type", exception.GetType().FullName ?? "Unknown" },
//////                { "Message", exception.Message },
//////                { "StackTrace", exception.StackTrace ?? "N/A" },
//////                { "HResult", exception.HResult },
//////                { "Category", CategorizeException(exception).ToString() }
//////            };

//////            // Recursively add inner exception details
//////            if (exception.InnerException != null)
//////            {
//////                details["InnerException"] = BuildExceptionDetails(exception.InnerException);
//////            }

//////            return details;
//////        }

//////        /// <summary>
//////        /// Categorizes exception into business-friendly categories.
//////        /// Used for pattern analysis, alerting, and monitoring.
//////        /// </summary>
//////        private ExceptionCategory CategorizeException(Exception exception)
//////        {
//////            return exception switch
//////            {
//////                ArgumentNullException or ArgumentException
//////                    => ExceptionCategory.ValidationException,

//////                InvalidOperationException or NotImplementedException
//////                    => ExceptionCategory.BusinessLogicException,

//////                TimeoutException or System.Net.Http.HttpRequestException
//////                    => ExceptionCategory.InfrastructureException,

//////                UnauthorizedAccessException
//////                    => ExceptionCategory.SecurityException,

//////                KeyNotFoundException
//////                    => ExceptionCategory.DataAccessException,

//////                System.IO.IOException
//////                    => ExceptionCategory.InfrastructureException,

//////                OutOfMemoryException or StackOverflowException
//////                    => ExceptionCategory.InfrastructureException,

//////                SystemException when exception.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
//////                    => ExceptionCategory.DataAccessException,

//////                _ => ExceptionCategory.UnknownException
//////            };
//////        }

//////        /// <summary>
//////        /// Determines appropriate logging level based on exception category.
//////        /// Critical: Out of memory, Stack overflow → Critical
//////        /// Error: Database, business logic failures → Error
//////        /// Warning: Validation, not implemented → Warning
//////        /// </summary>
//////        private LogLevel DetermineLogLevel(ExceptionCategory category)
//////        {
//////            return category switch
//////            {
//////                ExceptionCategory.SecurityException => LogLevel.Warning,
//////                ExceptionCategory.ValidationException => LogLevel.Warning,
//////                ExceptionCategory.BusinessLogicException => LogLevel.Error,
//////                ExceptionCategory.InfrastructureException => LogLevel.Error,
//////                ExceptionCategory.DataAccessException => LogLevel.Error,
//////                _ => LogLevel.Error
//////            };
//////        }

//////        /// <summary>
//////        /// Creates RFC 7807 ProblemDetails response object.
//////        /// Standardized error response format for REST APIs.
//////        /// </summary>
//////        private Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
//////            Exception exception,
//////            string correlationId,
//////            ExceptionCategory category,
//////            ExceptionContext context)
//////        {
//////            var statusCode = DetermineStatusCode(category);

//////            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
//////            {
//////                Type = $"https://api.myshipping.com/errors/{category}",
//////                Title = "An error occurred processing your request",
//////                Detail = exception.Message,
//////                Status = statusCode,
//////                Instance = context.HttpContext.Request.Path,
//////                Extensions = new Dictionary<string, object?>
//////                {
//////                    { "traceId", correlationId },
//////                    { "timestamp", DateTime.UtcNow.ToString("O") },
//////                    { "category", category.ToString() }
//////                }
//////            };

//////            return problemDetails;
//////        }

//////        /// <summary>
//////        /// Determines HTTP status code based on exception category.
//////        /// Follows REST conventions and HTTP semantics.
//////        /// </summary>
//////        private int DetermineStatusCode(ExceptionCategory category)
//////        {
//////            return category switch
//////            {
//////                ExceptionCategory.ValidationException => 400,       // Bad Request
//////                ExceptionCategory.SecurityException => 401,         // Unauthorized
//////                ExceptionCategory.DataAccessException => 404,       // Not Found
//////                ExceptionCategory.BusinessLogicException => 400,    // Bad Request
//////                ExceptionCategory.InfrastructureException => 503,   // Service Unavailable
//////                _ => 500                                            // Internal Server Error
//////            };
//////        }

//////        /// <summary>
//////        /// SINGLE SOURCE OF TRUTH for security classification.
//////        /// Determines if an exception is security-related and should be logged to security.txt.
//////        /// Returns true: log to security.txt ONLY
//////        /// Returns false: log to app.txt ONLY
//////        /// </summary>
//////        private bool IsSecurityRelevant(Exception ex)
//////        {
//////            // Security-related exceptions
//////            return ex is AuthenticationException
//////                || ex is SecurityTokenException
//////                || ex is UnauthorizedAccessException
//////                || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
//////                || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
//////                || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
//////                || ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
//////                || ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
//////        }
//////        //private bool IsSecurityRelevant(Exception ex)
//////        //{
//////        //    // Example: only security-related exceptions
//////        //    return ex is AuthenticationException
//////        //        || ex is SecurityTokenException
//////        //        || ex is UnauthorizedAccessException
//////        //        || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
//////        //        || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase);
//////        //}
//////    }
//////}
