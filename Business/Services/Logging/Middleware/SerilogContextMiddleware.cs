using Business.Contracts.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.Logging.Middleware
{


    public class SerilogContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Resolve scoped service per request
            var enricher = context.RequestServices.GetRequiredService<IContextEnricher>();
            await enricher.EnrichContextAsync(context);

            // Push values into Serilog
            var correlationId = HttpContextEnricher.GetCorrelationId(context);
            var userId = HttpContextEnricher.GetUserId(context);

            using (LogContext.PushProperty("CorrelationId", correlationId ?? "N/A"))
            using (LogContext.PushProperty("UserId", userId ?? "Anonymous"))
            {
                await _next(context);
            }
        }
    }
}
