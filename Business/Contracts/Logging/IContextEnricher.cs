using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Business.Contracts.Logging
{
    /// <summary>
    /// Contract for HTTP context enrichment in middleware.
    /// Extracts contextual information (email, user claims, etc.) and makes it available
    /// to downstream components (filters, logging, etc.).
    /// 
    /// SOLID Principle: Interface Segregation - Minimal, focused middleware contract.
    /// Used for email cache extraction in rate limiting scenarios.
    /// </summary>
    public interface IContextEnricher
    {
        /// <summary>
        /// Enriches the HTTP context with extracted contextual information.
        /// Called during middleware pipeline to setup context for logging/filters.
        /// </summary>
        /// <param name="context">HTTP context to enrich</param>
        /// <returns>Async task representing enrichment operation</returns>
        Task EnrichContextAsync(HttpContext context);
    }
}
