
using Business.Contracts;
using Business.Contracts.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace Business.Services.Logging
{
    public class RateLimitEventLogger : IRateLimitEventLogger
    {
        private readonly ILogger<RateLimitEventLogger> _logger;
        private readonly IServiceProvider? _serviceProvider;
        private readonly IUIUserService _userService;
        protected readonly IHttpContextAccessor? _httpContextAccessor;

        public RateLimitEventLogger(
            ILogger<RateLimitEventLogger> logger,
            IServiceProvider? serviceProvider = null,
            IUIUserService userService = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogRateLimitBreachAsync(
            string clientIdentifier,
            string endpoint,
            int requestsCount,
            string limitWindow,
            string? email = null,
            string? correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(clientIdentifier))
                throw new ArgumentException("Client identifier required", nameof(clientIdentifier));

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint required", nameof(endpoint));

            try
            {
                // Push contextual properties
                using (LogContext.PushProperty("EventType", "RATE_LIMIT_BREACH"))
                using (LogContext.PushProperty("ClientIdentifier", clientIdentifier))
                using (LogContext.PushProperty("Endpoint", endpoint))
                using (LogContext.PushProperty("RequestsCount", requestsCount))
                using (LogContext.PushProperty("LimitWindow", limitWindow))
                using (LogContext.PushProperty("Email", email ?? "unknown"))
                using (LogContext.PushProperty("CorrelationId", correlationId ?? "unknown"))
                {
                    _logger.LogWarning(
                        "⚠️ Rate limit breach detected | Email: {Email} | Client: {ClientIdentifier} | Endpoint: {Endpoint} | Requests: {RequestsCount} | Window: {LimitWindow} | CorrelationId: {CorrelationId}",
                        email ?? "unknown",
                        clientIdentifier,
                        endpoint,
                        requestsCount,
                        limitWindow,
                        correlationId ?? "unknown");
                }

                // Persist to database safely
                if (_serviceProvider != null)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var auditService = scope.ServiceProvider.GetService<SecurityAuditService>();
                        if (auditService != null)
                        {
                            await auditService.LogSecurityEventAsync(
                                eventType: "RATE_LIMIT_BREACH",
                                severity: "Warning",
                                userId: null,
                                email: email,
                                ipAddress: clientIdentifier,
                                endpoint: endpoint,
                                description: $"Rate limit breach: {requestsCount} requests in {limitWindow}",
                                metadata: $"requestsCount:{requestsCount}|window:{limitWindow}|correlationId:{correlationId ?? "unknown"}",
                                correlationId: correlationId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist rate limit breach to database");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging rate limit breach: {ex.Message}");
            }
        }

        public async Task LogSuspiciousActivityAsync(
            string clientIdentifier,
            string suspiciousPattern,
            string[] affectedEndpoints)
        {
            if (string.IsNullOrWhiteSpace(clientIdentifier))
                throw new ArgumentException("Client identifier required", nameof(clientIdentifier));

            if (string.IsNullOrWhiteSpace(suspiciousPattern))
                throw new ArgumentException("Pattern description required", nameof(suspiciousPattern));

            if (affectedEndpoints == null || affectedEndpoints.Length == 0)
                throw new ArgumentException("At least one endpoint required", nameof(affectedEndpoints));

            try
            {
                var endpointsList = string.Join(", ", affectedEndpoints);

                using (LogContext.PushProperty("EventType", "SUSPICIOUS_ACTIVITY"))
                using (LogContext.PushProperty("ClientIdentifier", clientIdentifier))
                using (LogContext.PushProperty("SuspiciousPattern", suspiciousPattern))
                using (LogContext.PushProperty("AffectedEndpoints", endpointsList))
                {
                    _logger.LogError(
                        "🚨 SUSPICIOUS_ACTIVITY | Pattern: {Pattern} | Client: {ClientIdentifier} | Endpoints: {Endpoints}",
                        suspiciousPattern,
                        clientIdentifier,
                        endpointsList);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging suspicious activity: {ex.Message}");
            }
        }

        public async Task LogRateLimitRecoveryAsync(string clientIdentifier, DateTime recoveryTime)
        {
            if (string.IsNullOrWhiteSpace(clientIdentifier))
                throw new ArgumentException("Client identifier required", nameof(clientIdentifier));

            try
            {
                using (LogContext.PushProperty("EventType", "RATE_LIMIT_RECOVERY"))
                using (LogContext.PushProperty("ClientIdentifier", clientIdentifier))
                using (LogContext.PushProperty("RecoveryTime", recoveryTime))
                {
                    _logger.LogInformation(
                        "✅ RATE_LIMIT_RECOVERY | Client: {ClientIdentifier} | RecoveredAt: {RecoveryTime}",
                        clientIdentifier,
                        recoveryTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging rate limit recovery: {ex.Message}");
            }
        }

        protected string GetCorrelationId()
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context?.Items.TryGetValue("CorrelationId", out var id) == true)
                {
                    return id?.ToString() ?? "unknown";
                }
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}



























