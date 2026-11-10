using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Business.Contracts.Logging
{
    /// <summary>
    /// Contract for centralized exception logging.
    /// Provides uniform exception categorization, logging, and problem details generation
    /// across all application projects (WebApi, UI, etc.).
    /// 
    /// SOLID Principle: Dependency Inversion - Depend on abstraction, not concrete implementations.
    /// Enables runtime switching between different exception logging strategies.
    /// </summary>
    public interface IExceptionLogger
    {
        /// <summary>
        /// Logs an exception with full context and generates appropriate HTTP response.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">HTTP context providing request/response details</param>
        /// <returns>Structured problem details for HTTP response (RFC 7231 compliant)</returns>
        Task<ProblemDetails> LogExceptionAsync(Exception exception, HttpContext context);

        /// <summary>
        /// Categorizes an exception to determine appropriate handling strategy.
        /// </summary>
        /// <param name="exception">The exception to categorize</param>
        /// <returns>Exception category (SecurityException, ValidationException, InfrastructureException, etc.)</returns>
        ExceptionCategory CategorizeException(Exception exception);

        /// <summary>
        /// Determines appropriate log level for an exception.
        /// Security exceptions = Warning, Infrastructure = Error, Validation = Information, etc.
        /// </summary>
        LogLevel DetermineLogLevel(ExceptionCategory category);
    }

    /// <summary>
    /// Categorizes exceptions for appropriate handling and logging.
    /// </summary>
    public enum ExceptionCategory
    {
        SecurityException,
        ValidationException,
        InfrastructureException,
        DataAccessException,
        BusinessLogicException,
        UnknownException
    }

    /// <summary>
    /// RFC 7807 Problem Details response object for standardized error responses.
    /// Provides machine-readable and human-readable error information.
    /// </summary>
    public class ProblemDetails
    {
        public string Type { get; set; } = "about:blank";
        public string Title { get; set; } = "An error occurred";
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public Dictionary<string, object>? Extensions { get; set; }
        public string? TraceId { get; set; }
    }
}
