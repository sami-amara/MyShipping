using Business.Contracts.Logging;
using Business.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Stripe;
using System.Net.NetworkInformation;


namespace Business.Services.Logging.SecurityLogger
{
    /// <summary>
    /// Abstract base class for centralized security event logging.
    /// Consolidates security event logging logic for WebApi and UI projects.
    /// 
    /// Provides:
    /// - Login/Registration events
    /// - Token lifecycle events
    /// - Password management events
    /// - Email confirmation events
    /// - Logout/Account management events
    /// - IP address and User-Agent enrichment
    /// - Correlation ID integration
    /// </summary>
    public abstract class SecurityEventLoggerBase : ISecurityEventLogger
    {
        protected readonly ILogger<SecurityEventLoggerBase> _logger;
        protected readonly IHttpContextAccessor? _httpContextAccessor;
        protected readonly IServiceProvider? _serviceProvider;


        protected SecurityEventLoggerBase(ILogger<SecurityEventLoggerBase> logger, 
            IHttpContextAccessor? httpContextAccessor = null,
            IServiceProvider? serviceProvider = null)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }


        

        /// <summary>
        /// Extracts the client IP address from the current HTTP context.
        /// </summary>
        protected string GetClientIp()
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                return context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Extracts the User-Agent from the current HTTP context.
        /// </summary>
        protected string GetUserAgent()
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                return context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Extracts the correlation ID from the current HTTP context or LogContext.
        /// </summary>
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


        // Add this helper method after the constructor
        protected async Task QueueAuditLogAsync(
            string eventType,
            string severity,
            string? userId,
            string? email,
            string? description = null,
            string? metadata = null,
            string? correlationId = null,
            Guid? shipmentId = null,
            string? trackingNumber = null,
            string? shipmentEventType = null,
            string? reason = null)
        {
            // Check if service provider is null - if so, exit gracefully
            if (_serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ QueueAuditLogAsync: ServiceProvider is null, skipping audit queue for {eventType}");
                return;
            }

            try
            {
                // Try to resolve background task queue from DI
                var taskQueue = _serviceProvider.GetService<SecurityAuditTaskQueue>();

                if (taskQueue == null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ QueueAuditLogAsync: SecurityAuditTaskQueue not registered in DI, skipping background audit for {eventType}");
                    return;
                }


                await taskQueue.QueueAuditLogAsync(
                eventType: eventType,
                severity: severity,
                userId: userId,
                email: email,

                shipmentId: shipmentId,
                trackingNumber: trackingNumber,
                shipmentEventType: shipmentEventType,
                reason: reason,

                ipAddress: GetClientIp(),
                endpoint: null,

                description: description,

                metadata: metadata,

                correlationId: correlationId);
                System.Diagnostics.Debug.WriteLine($"✅ QueueAuditLogAsync: Queued {eventType} for background processing");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - security logging should not break main operation
                System.Diagnostics.Debug.WriteLine($"❌ QueueAuditLogAsync: Error queuing {eventType}: {ex.Message}");
                _logger?.LogError(ex, "Failed to queue security event: {EventType}", eventType);
            }
        }

        


        // ═══════════════════════════════════════════════════════════════
        // ✅ LOGIN EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogSuccessfulLogin(string userId, string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "LOGIN_SUCCESS");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("UserAgent", GetUserAgent());

                _logger.LogInformation("LOGIN_SUCCESS From LoogSuccessFulLogin FOR | UserId: {UserId} | Email: {Email} | IP: {IP}",
                    userId, email, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging successful login: {ex.Message}"); }
        }

       

        // LogFailedLogin
        public virtual async Task LogFailedLogin(string email, string reason)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "LOGIN_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);
                LogContext.PushProperty("UserAgent", GetUserAgent());

                _logger.LogWarning("LOGIN_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                    email, reason, GetClientIp());

                // 🆕 Queue to background service
                await QueueAuditLogAsync("LOGIN_FAILED", "Warning", null, email, reason,
                    metadata: $"reason:{reason}", correlationId: GetCorrelationId());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging failed login: {ex.Message}");
            }
        }

        // LogAccountLockout
        public virtual async Task LogAccountLockout(string userId, string email, DateTimeOffset? lockoutEnd)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "ACCOUNT_LOCKED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("LockoutEnd", lockoutEnd);

                _logger.LogWarning("🔒 ACCOUNT_LOCKED | User: {Email} | UserId: {UserId} | LockedUntil: {LockoutEnd} | IP: {IP}",
                    email, userId, lockoutEnd, GetClientIp());

                // 🆕 Queue to background service
                await QueueAuditLogAsync("ACCOUNT_LOCKED", "Critical", userId, email,
                    $"Account locked until {lockoutEnd}",
                    metadata: $"lockoutEnd:{lockoutEnd}",
                    correlationId: GetCorrelationId());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging account lockout: {ex.Message}");
            }
        }

        // LogTokenReuseDetected
        public virtual async Task LogTokenReuseDetected(string userId, string token)
        {
            try
            {
                var tokenPreview = string.IsNullOrEmpty(token) ? "N/A" : token.Substring(0, Math.Min(20, token.Length));
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("EventType", "TOKEN_REUSE_DETECTED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("TokenPreview", tokenPreview);

                _logger.LogError(
                    "🚨 SECURITY_ALERT: TOKEN_REUSE_DETECTED | UserId: {UserId} | TokenPreview: {TokenPreview} | IP: {IP} | UserAgent: {UserAgent}",
                    userId, tokenPreview, GetClientIp(), GetUserAgent());

                // 🆕 Queue to background service
                await QueueAuditLogAsync("TOKEN_REUSE_DETECTED", "Critical", userId, null,
                    "Token reuse detected - possible account compromise",
                    metadata: $"tokenPreview:{tokenPreview}|userAgent:{GetUserAgent()}",
                    correlationId: GetCorrelationId());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging token reuse: {ex.Message}");
            }
        }


 
        // ══════════════════════════════════════════════════════════════
        // ✅ REGISTRATION EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogRegistrationSuccess(string userId, string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "REGISTRATION_SUCCESS");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("✅ REGISTRATION_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP}",
                    email, userId, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging registration success: {ex.Message}"); }
        }

        public virtual void LogRegistrationFailed(string email, string reason)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "REGISTRATION_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);

                _logger.LogWarning("❌ REGISTRATION_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                    email, reason, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging registration failed: {ex.Message}"); }
        }

        

        // ═══════════════════════════════════════════════════════════════
        // ✅ TOKEN EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogTokenRotation(string userId)
        {
            try
            {
                // 🔧 Push all properties to LogContext FIRST
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("EventType", "TOKEN_ROTATED");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogDebug(
                    "🔄 TOKEN_ROTATED | UserId: {UserId} | IP: {IP}",
                    userId, GetClientIp());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging token rotation: {ex.Message}");
            }
        }
      

        public virtual void LogAllTokensRevoked(string userId, string reason)
        {
            try
            {
                // 🔧 Push all properties to LogContext FIRST
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("EventType", "ALL_TOKENS_REVOKED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("RevocationReason", reason);

                _logger.LogWarning(
                    "🔒 ALL_TOKENS_REVOKED | UserId: {UserId} | Reason: {Reason} | IP: {IP}",
                    userId, reason, GetClientIp());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging tokens revoked: {ex.Message}");
            }
        }

        public virtual void LogTokenRefreshFailed(string reason)
        {
            try
            {
                // 🔧 Push all properties to LogContext FIRST
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("EventType", "TOKEN_REFRESH_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);

                _logger.LogWarning(
                    "⚠️ TOKEN_REFRESH_FAILED | Reason: {Reason} | IP: {IP}",
                    reason, GetClientIp());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging token refresh failed: {ex.Message}");
            }
        }



   
        // ═══════════════════════════════════════════════════════════════
        // ✅ PASSWORD EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogPasswordResetRequested(string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "PASSWORD_RESET_REQUESTED");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("🔑 PASSWORD_RESET_REQUESTED | Email: {Email} | IP: {IP}",
                    email, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging password reset requested: {ex.Message}"); }
        }

        public virtual void LogPasswordResetSuccess(string userId, string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "PASSWORD_RESET_SUCCESS");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("✅ PASSWORD_RESET_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP}",
                    email, userId, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging password reset success: {ex.Message}"); }
        }

        public virtual void LogPasswordResetFailed(string email, string reason)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "PASSWORD_RESET_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);

                _logger.LogWarning("❌ PASSWORD_RESET_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                    email, reason, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging password reset failed: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════
        // ✅ EMAIL CONFIRMATION EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogEmailConfirmationSent(string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "EMAIL_CONFIRMATION_SENT");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("📧 EMAIL_CONFIRMATION_SENT | Email: {Email} | IP: {IP}",
                    email, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging email confirmation sent: {ex.Message}"); }
        }

        public virtual void LogEmailConfirmed(string userId, string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "EMAIL_CONFIRMED");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("✅ EMAIL_CONFIRMED | User: {Email} | UserId: {UserId} | IP: {IP}",
                    email, userId, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging email confirmed: {ex.Message}"); }


        }
     
        // ═══════════════════════════════════════════════════════════════
        // ✅ LOGOUT EVENTS
        // ═══════════════════════════════════════════════════════════════

        public virtual void LogLogout(string userId)
        {
            try
            {
                // 🔧 Push all properties to LogContext FIRST
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("EventType", "LOGOUT");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("🔓 LOGOUT | UserId: {UserId} | IP: {IP}",userId, GetClientIp());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging logout: {ex.Message}");
            }
        }


        /// <summary>
        /// Logs a successful password change by authenticated user.
        /// </summary>
        public virtual void LogPasswordChangeSuccess(string userId, string email)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "PASSWORD_CHANGE_SUCCESS");
                LogContext.PushProperty("IpAddress", GetClientIp());

                _logger.LogInformation("✅ PASSWORD_CHANGE_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP}",
                    email, userId, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging password change success: {ex.Message}"); }
        }

        /// <summary>
        /// Logs a failed password change attempt.
        /// </summary>
        public virtual void LogPasswordChangeFailed(string email, string reason)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("Email", email);
                LogContext.PushProperty("EventType", "PASSWORD_CHANGE_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);

                _logger.LogWarning("❌ PASSWORD_CHANGE_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                    email, reason, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging password change failed: {ex.Message}"); }
        }

        /// <summary>
        /// Logs a failed email confirmation attempt.
        /// </summary>
        public virtual void LogEmailConfirmationFailed(string userId, string reason)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", GetCorrelationId());
                LogContext.PushProperty("UserId", userId);
                LogContext.PushProperty("EventType", "EMAIL_CONFIRMATION_FAILED");
                LogContext.PushProperty("IpAddress", GetClientIp());
                LogContext.PushProperty("FailureReason", reason);

                _logger.LogWarning("❌ EMAIL_CONFIRMATION_FAILED | UserId: {UserId} | Reason: {Reason} | IP: {IP}",
                    userId, reason, GetClientIp());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error logging email confirmation failed: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════
        // ✅ SHIPMENT AUDIT EVENTS
        // ═══════════════════════════════════════════════════════════════


        public virtual async Task LogShipmentStatusChanged(
            Guid shipmentId,
            string status,
            string userId,
            string? trackingNumber = null,
            string? reason = null,
            string? carrierId = null,
            string severity = "Information")
        {
            try
            {
                var correlationId = GetCorrelationId();
                var ip = GetClientIp();
                var userAgent = GetUserAgent();

                var eventType = $"SHIPMENT_{status.ToUpper()}";

                LogContext.PushProperty("CorrelationId", correlationId);
                LogContext.PushProperty("UserId", userId);

                LogContext.PushProperty("ShipmentId", shipmentId);
                LogContext.PushProperty("TrackingNumber", trackingNumber);
                LogContext.PushProperty("ShipmentEventType", status);

               LogContext.PushProperty("EventType", eventType);

                LogContext.PushProperty("IpAddress", ip);
                LogContext.PushProperty("UserAgent", userAgent);

                if (!string.IsNullOrWhiteSpace(reason))
                    LogContext.PushProperty("Reason", reason);

                if (!string.IsNullOrWhiteSpace(carrierId))
                    LogContext.PushProperty("CarrierId", carrierId);

                _logger.LogInformation(
                    "Shipment status changed. ShipmentId={ShipmentId}, Tracking={TrackingNumber}, " +
                    "Status={Status}, UserId={UserId}, Reason={Reason}",
                    shipmentId,
                    trackingNumber,
                    status,
                    userId,
                    reason ?? "N/A");

                await QueueAuditLogAsync(
                    eventType: eventType,
                    severity: severity,
                    userId: userId,
                    email: null,

                    shipmentId: shipmentId,
                    trackingNumber: trackingNumber,
                    shipmentEventType: status,
                    reason: reason,
                    description: GetShipmentDescription(status),
                    metadata:
                        $"carrierId:{carrierId}|userAgent:{userAgent}",
                    correlationId: correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to log shipment status change for shipment {ShipmentId}",
                    shipmentId);
            }
        }

        private static string GetShipmentDescription(string status)
        {
            return status switch
            {
                "Created" => "Shipment created.",
                "Updated" => "Shipment details edited or updated.",
                "Approved" => "Shipment approved.",
                "ReadyForShipping" => "Shipment marked ready for shipping.",
                "Shipped" => "Shipment handed over to carrier.",
                "Delivered" => "Shipment delivered to recipient.",
                "Returned" => "Shipment returned by customer or carrier.",
                "Refunded" => "Shipment refund processed.",
                "Cancelled" => "Shipment cancelled.",
                "Deleted" => "Shipment deleted.",
                _ => $"Shipment status changed to {status}"
            };
        }



        public virtual void LogSecurityException(
            string correlationId,
            Exception exception,
            string? userId,
            string? email,
            string? ipAddress,
            string? path)
        {
            try
            {
                LogContext.PushProperty("CorrelationId", correlationId);
                LogContext.PushProperty("UserId", userId ?? "unknown");
                LogContext.PushProperty("Email", email ?? "unknown");
                LogContext.PushProperty("IpAddress", ipAddress ?? "unknown");
                LogContext.PushProperty("Path", path ?? "unknown");
                LogContext.PushProperty("EventType", "SECURITY_EXCEPTION");

                _logger.LogError(
                    exception,
                    "SECURITY_EXCEPTION | Path={Path} | User={UserId} | Email={Email} | IP={IpAddress} | CorrelationId={CorrelationId} | Message={Message}",
                    path,
                    userId,
                    email,
                    ipAddress,
                    correlationId,
                    exception.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging security exception: {ex.Message}");
            }
        }


    }
}
