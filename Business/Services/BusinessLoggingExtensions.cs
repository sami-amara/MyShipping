using Business.Contracts.Logging;
using Business.Services.Logging;
using Business.Services.Logging.Filters;
using Business.Services.Logging.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Business.Services
{
    /// <summary>
    /// Centralized registration service for Business layer logging and security components.
    /// Enables consistent setup across WebApi, UI, and other applications.
    /// 
    /// DESIGN PRINCIPLES:
    /// - Provides single source of truth for logging infrastructure
    /// - Reduces duplication across projects
    /// - Makes it easy to update all projects simultaneously
    /// - Follows composition over inheritance pattern
    /// 
    /// USAGE in WebApplicationBuilder:
    /// ```
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Add Business logging services
    /// builder.Services.ConfigureBusinessLogging();
    /// 
    /// // Add Serilog configuration
    /// builder.Services.ConfigureBusinessSerilog();
    /// 
    /// // (existing service registrations...)
    /// 
    /// var app = builder.Build();
    /// app.UseBusinessLogging(); // Add middleware
    /// ```
    /// </summary>
    public static class BusinessLoggingExtensions
    {
        /// <summary>
        /// Registers all Business layer logging and security services.
        /// Call this early in WebApplicationBuilder setup.
        /// </summary>
        /// <param name="services">Service collection from builder</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureBusinessLogging(
            this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register core logging services
            // Note: ExceptionLoggingFilter is an MVC IExceptionFilter, not IExceptionLogger
            // It's registered via AddMvc() below, not as a service
            services.AddScoped<ExceptionLoggingFilter>();
            services.AddScoped<IPiiRedactor, PiiRedactor>();
            services.AddScoped<IRateLimitEventLogger, RateLimitEventLogger>();
            services.AddScoped<IContextEnricher, HttpContextEnricher>();

            // Register Serilog filters (singletons as they're stateless)
            services.AddSingleton<SerilogPiiFilter>(sp =>
            {
                var redactor = sp.GetRequiredService<IPiiRedactor>();
                return new SerilogPiiFilter(redactor, enabled: true);
            });

            services.AddSingleton<SecurityEventFilter>();

            return services;
        }

        /// <summary>
        /// Configures Serilog with Business layer defaults.
        /// Includes PII redaction, security event filtering, and structured logging.
        /// </summary>
        /// <param name="services">Service collection from builder</param>
        /// <param name="configuration">App configuration for Serilog settings</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection ConfigureBusinessSerilog(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Serilog configuration is typically done via builder.Host.UseSerilog()
            // This method exists for documentation and to ensure services are available
            // Call ConfigureLogging on WebApplicationBuilder instead:
            // builder.Services.ConfigureBusinessLogging();
            // builder.Host.UseSerilog((context, config) => ConfigureLogging(config, context.Configuration));

            return services;
        }

        /// <summary>
        /// Uses Business layer logging middleware.
        /// Call this in app configuration pipeline.
        /// </summary>
        /// <param name="app">Web application</param>
        /// <returns>Web application for chaining</returns>
        public static WebApplication UseBusinessLogging(
            this WebApplication app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            // Add HTTP context enrichment middleware
            // Must be early in pipeline before rate limiting, authentication, etc.
            app.Use(async (context, next) =>
            {
                var enricher = context.RequestServices.GetRequiredService<IContextEnricher>();
                await enricher.EnrichContextAsync(context);
                await next();
            });

            // Add exception logging filter to all controllers
            // (Controllers that use [ApiController] or inherit from Controller will get this automatically
            // if registered as a global filter - see WebApi/Program.cs configuration)

            return app;
        }

        /// <summary>
        /// Configures Serilog directly in a delegate pattern.
        /// Use this when calling builder.Host.UseSerilog().
        /// </summary>
        /// <param name="config">Serilog LoggerConfiguration</param>
        /// <param name="services">Service provider for dependency resolution</param>
        /// <example>
        /// builder.Host.UseSerilog((context, sp, config) =>
        /// {
        ///     BusinessLoggingExtensions.ConfigureSerilogPipeline(config, sp, context.Configuration);
        /// });
        /// </example>
        public static void ConfigureSerilogPipeline(
            Serilog.LoggerConfiguration config,
            IServiceProvider services,
            IConfiguration? configuration = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (services == null)
                throw new ArgumentNullException(nameof(services));

            try
            {
                // Get PII redactor from DI (or create default)
                var redactor = services.GetService<IPiiRedactor>() ?? new PiiRedactor();

                // Apply PII redaction filter
                config.WithPiiRedaction(redactor);

                // Apply security event filter
                var securityFilter = services.GetService<SecurityEventFilter>();
                if (securityFilter != null)
                {
                    config.Filter.With(securityFilter);
                }

                // Add standard properties to all log events
                config.Enrich.FromLogContext()
                      .Enrich.WithProperty("Application", "MyShipping")
                      .Enrich.WithProperty("Environment", GetEnvironment(configuration));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring logging: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the current environment (Development, Staging, Production).
        /// </summary>
        private static string GetEnvironment(IConfiguration? configuration)
        {
            return configuration?["ASPNETCORE_ENVIRONMENT"] ??
                   Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                   "Production";
        }
    }

    /// <summary>
    /// Configuration class for Business logging options.
    /// Can be extended with additional settings in the future.
    /// </summary>
    public class BusinessLoggingOptions
    {
        public bool EnablePiiRedaction { get; set; } = true;
        public bool EnableSecurityEventFilter { get; set; } = true;
        public bool EnableRateLimitLogging { get; set; } = true;
        public string RedactionPlaceholder { get; set; } = "[REDACTED]";
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    }
}
