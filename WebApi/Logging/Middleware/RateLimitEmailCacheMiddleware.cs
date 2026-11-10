using System.Text.Json;

namespace WebApi.Logging.Middleware
{
    /// <summary>
    /// Middleware that extracts and caches the email from login/register requests
    /// so it can be used by the rate limit logger when a request is rejected.
    /// 
    /// This runs VERY EARLY in the pipeline, before rate limiting, to ensure
    /// the body can be buffered and read.
    /// </summary>
    public class RateLimitEmailCacheMiddleware
    {
        private readonly RequestDelegate _next;

        public RateLimitEmailCacheMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process auth endpoints with JSON bodies
            if ((context.Request.Path.StartsWithSegments("/api/auth/login") ||
                 context.Request.Path.StartsWithSegments("/api/auth/register")) &&
                context.Request.ContentType?.Contains("application/json") == true &&
                context.Request.ContentLength > 0)
            {
                try
                {
                    // Enable buffering so body can be read multiple times
                    context.Request.EnableBuffering();

                    // Read the body
                    var currentPosition = context.Request.Body.Position;
                    context.Request.Body.Position = 0;

                    using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                    {
                        var body = await reader.ReadToEndAsync();
                        context.Request.Body.Position = currentPosition;

                        // Parse JSON and extract email
                        try
                        {
                            using (var doc = JsonDocument.Parse(body))
                            {
                                if (doc.RootElement.TryGetProperty("email", out var emailElement))
                                {
                                    var email = emailElement.GetString();
                                    if (!string.IsNullOrEmpty(email))
                                    {
                                        // Store in context items for rate limit logger to access
                                        context.Items["RateLimitEmail"] = email;
                                    }
                                }
                            }
                        }
                        catch { /* Ignore JSON parsing errors */ }
                    }
                }
                catch { /* Ignore any buffering/reading errors */ }
            }

            await _next(context);
        }
    }
}
