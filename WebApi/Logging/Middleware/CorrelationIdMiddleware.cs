using Serilog.Context;

namespace WebApi.Logging.Middleware
{
    /// <summary>
    /// Middleware to handle X-Correlation-ID for distributed tracing.
    /// Generates a new correlation ID if not present, adds it to response headers,
    /// and enriches Serilog context for all log entries in the request.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private const string CorrelationIdProperty = "CorrelationId";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Extract or generate Correlation ID
            var correlationId = ExtractOrGenerateCorrelationId(context);

            // Push to Serilog LogContext so it's included in all logs for this request
            using (LogContext.PushProperty(CorrelationIdProperty, correlationId))
            {
                // Add to items so it's accessible throughout the request pipeline
                context.Items[CorrelationIdProperty] = correlationId;

                // Add response header with correlation ID
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                    {
                        context.Response.Headers.Append(CorrelationIdHeader, correlationId);
                    }
                    return Task.CompletedTask;
                });

                //_logger.LogDebug("Request started - CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                //    correlationId, context.Request.Path, context.Request.Method);

                await _next(context);

                //_logger.LogDebug("Request completed - CorrelationId: {CorrelationId}, StatusCode: {StatusCode}",
                //    correlationId, context.Response.StatusCode);
            }
        }

        /// <summary>
        /// Extracts X-Correlation-ID from request header or generates a new one if not present.
        /// </summary>
        private string ExtractOrGenerateCorrelationId(HttpContext context)
        {
            const string correlationIdHeader = "X-Correlation-ID";

            if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationIdValue))
            {
                var correlationId = correlationIdValue.ToString();
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    return correlationId;
                }
            }

            // Generate new correlation ID if not present
            var newCorrelationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[correlationIdHeader] = newCorrelationId;
            return newCorrelationId;
        }
    }

    /// <summary>
    /// Extension method to easily add CorrelationIdMiddleware to the middleware pipeline.
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
