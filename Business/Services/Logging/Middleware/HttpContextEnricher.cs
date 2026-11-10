using Business.Contracts.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Logging.Middleware
{
    /// <summary>
    /// Professional middleware for enriching HTTP context with request metadata.
    /// Extracts email, user claims, and other contextual data early in pipeline.
    /// 
    /// ORIGINALLY FROM: WebApi.Middleware.RateLimitEmailCacheMiddleware
    /// MOVED TO: Business.Middleware.HttpContextEnricher (shared)
    /// 
    /// Features:
    /// - Buffers request body safely for multi-read scenarios
    /// - Extracts email from JSON login/register payloads
    /// - Caches contextual data in HttpContext.Items for downstream components
    /// - Handles edge cases (empty body, invalid JSON, etc.)
    /// - Zero breaking changes to request pipeline
    /// 
    /// Usage:
    /// 1. Call early in middleware pipeline (before rate limiting)
    /// 2. Downstream components access data via context.Items["...KeyConstants"]
    /// 3. Works with any endpoint, not just auth
    /// 
    /// SOLID Compliance:
    /// - Single Responsibility: Only enriches context, no validation/logging
    /// - Dependency Inversion: Implements IContextEnricher contract
    /// - Composable: Can be used in multiple applications
    /// </summary>
    public class HttpContextEnricher : IContextEnricher
    {
        private const string RateLimitEmailKey = "RateLimitEmail";
        private const string CorrelationIdKey = "CorrelationId";
        private const string ClientIpKey = "ClientIp";
        private const string UserIdKey = "UserId";



        ///// <summary>
        ///// Standard JSON property names to extract from request bodies
        ///// </summary>
        private static readonly string[] EmailPropertyNames = { "email", "Email", "user_email", "UserEmail" };

      
    


        /// <summary>
        /// Enriches HTTP context with extracted request metadata.
        /// Called early in pipeline to prepare contextual data.
        /// </summary>
        public async Task EnrichContextAsync(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                // Extract and cache correlation ID
                ExtractCorrelationId(context);

                // Extract and cache client IP
                ExtractClientIp(context);

                // Extract and cache email from JSON body (for auth endpoints)
                await ExtractEmailFromBodyAsync(context);

                // Extract user claims if authenticated
                ExtractUserClaims(context);
            }
            catch (Exception ex)
            {
                // Fail silently - enrichment should not break the application
                System.Diagnostics.Debug.WriteLine($"Error enriching context: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts correlation ID from headers or generates one.
        /// Used for distributed tracing across services.
        /// </summary>
        private static void ExtractCorrelationId(HttpContext context)
        {
            // Check for existing correlation ID from headers
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) ||
                context.Request.Headers.TryGetValue("X-Request-Id", out correlationId) ||
                context.Request.Headers.TryGetValue("Correlation-Id", out correlationId))
            {
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    context.Items[CorrelationIdKey] = correlationId.ToString();
                    return;
                }
            }

            // Generate new correlation ID if none provided
            context.Items[CorrelationIdKey] = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Extracts client IP address from request.
        /// Handles X-Forwarded-For for proxies/load balancers.
        /// </summary>
        private static void ExtractClientIp(HttpContext context)
        {
            string? clientIp;

            // Check for X-Forwarded-For header (proxy/load balancer)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ips = forwardedFor.ToString().Split(',');
                clientIp = ips[0].Trim();
            }
            else
            {
                // Use direct connection IP
                clientIp = context.Connection.RemoteIpAddress?.ToString();
            }

            if (!string.IsNullOrEmpty(clientIp))
            {
                context.Items[ClientIpKey] = clientIp;
            }
        }

        /// <summary>
        /// Extracts email from request body for auth endpoints.
        /// Safely buffers body to avoid breaking downstream components.
        /// </summary>
        private static async Task ExtractEmailFromBodyAsync(HttpContext context)
        {
            // Only process POST/PUT with JSON body
            if (!context.Request.ContentType?.Contains("application/json") == true ||
                context.Request.ContentLength <= 0)
            {
                return;
            }

            try
            {
                // Enable request body buffering for multi-read support
                context.Request.EnableBuffering();

                // Save current position
                var originalPosition = context.Request.Body.Position;

                // Reset to start for reading
                context.Request.Body.Position = 0;

                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();

                    // Restore position for downstream components
                    context.Request.Body.Position = originalPosition;

                    // Parse JSON and extract email
                    if (!string.IsNullOrEmpty(body))
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(body))
                            {
                                var email = ExtractEmailProperty(doc.RootElement);
                                if (!string.IsNullOrEmpty(email))
                                {
                                    context.Items[RateLimitEmailKey] = email;
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Invalid JSON - skip enrichment
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail gracefully - if body buffering fails, continue
                System.Diagnostics.Debug.WriteLine($"Error extracting email from body: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts email property from JSON element.
        /// Checks multiple common property name variations.
        /// </summary>
        private static string? ExtractEmailProperty(JsonElement rootElement)
        {
            foreach (var propertyName in EmailPropertyNames)
            {
                if (rootElement.TryGetProperty(propertyName, out var emailElement))
                {
                    return emailElement.GetString();
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts user claims from authenticated request.
        /// Stores user ID from claims for downstream logging.
        /// </summary>
        private static void ExtractUserClaims(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // Extract user ID from standard claims
            var userIdClaim = context.User.FindFirst("sub") ??
                             context.User.FindFirst("oid") ??
                             context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
            {
                context.Items[UserIdKey] = userIdClaim.Value;
            }
        }

        /// <summary>
        /// Gets extracted email from context.
        /// Used by downstream components (logging, rate limiting, etc.).
        /// </summary>
        public static string? GetEmail(HttpContext context)
        {
            return context?.Items.TryGetValue(RateLimitEmailKey, out var email) == true
                ? email?.ToString()
                : null;
        }

        /// <summary>
        /// Gets extracted correlation ID from context.
        /// </summary>
        public static string? GetCorrelationId(HttpContext context)
        {
            return context?.Items.TryGetValue(CorrelationIdKey, out var id) == true
                ? id?.ToString()
                : null;
        }

        /// <summary>
        /// Gets extracted client IP from context.
        /// </summary>
        public static string? GetClientIp(HttpContext context)
        {
            return context?.Items.TryGetValue(ClientIpKey, out var ip) == true
                ? ip?.ToString()
                : null;
        }

        /// <summary>
        /// Gets extracted user ID from context.
        /// </summary>
        public static string? GetUserId(HttpContext context)
        {
            return context?.Items.TryGetValue(UserIdKey, out var userId) == true
                ? userId?.ToString()
                : null;
        }
    }
}
