using Microsoft.AspNetCore.Http;

namespace UI.Services
{
    /// <summary>
    /// Centralized security event logging service for authentication and authorization events
    /// </summary>
    public class SecurityEventLogger
    {
        private readonly ILogger<SecurityEventLogger> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityEventLogger(ILogger<SecurityEventLogger> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetClientIp()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        // ✅ LOGIN EVENTS
        public void LogSuccessfulLogin(string userId, string email)
        {
            _logger.LogInformation(
                "🔐 LOGIN_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP} | UserAgent: {UserAgent}",
                email, userId, GetClientIp(), GetUserAgent());
        }

        public void LogFailedLogin(string email, string reason)
        {
            _logger.LogWarning(
                "⚠️ LOGIN_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP} | UserAgent: {UserAgent}",
                email, reason, GetClientIp(), GetUserAgent());
        }

        public void LogAccountLockout(string userId, string email, DateTimeOffset? lockoutEnd)
        {
            _logger.LogWarning(
                "🔒 ACCOUNT_LOCKED | User: {Email} | UserId: {UserId} | LockedUntil: {LockoutEnd} | IP: {IP}",
                email, userId, lockoutEnd, GetClientIp());
        }

        // ✅ REGISTRATION EVENTS
        public void LogRegistrationSuccess(string userId, string email)
        {
            _logger.LogInformation(
                "✅ REGISTRATION_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP}",
                email, userId, GetClientIp());
        }

        public void LogRegistrationFailed(string email, string reason)
        {
            _logger.LogWarning(
                "❌ REGISTRATION_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                email, reason, GetClientIp());
        }

        // ✅ TOKEN EVENTS
        public void LogTokenRotation(string userId)
        {
            _logger.LogInformation(
                "🔄 TOKEN_ROTATED | UserId: {UserId} | IP: {IP}",
                userId, GetClientIp());
        }

        public void LogTokenReuseDetected(string userId, string token)
        {
            _logger.LogError(
                "🚨 SECURITY_ALERT: TOKEN_REUSE_DETECTED | UserId: {UserId} | TokenPreview: {TokenPreview} | IP: {IP} | UserAgent: {UserAgent}",
                userId, token.Substring(0, Math.Min(20, token.Length)), GetClientIp(), GetUserAgent());
        }

        public void LogAllTokensRevoked(string userId, string reason)
        {
            _logger.LogWarning(
                "🔒 ALL_TOKENS_REVOKED | UserId: {UserId} | Reason: {Reason} | IP: {IP}",
                userId, reason, GetClientIp());
        }

        public void LogTokenRefreshFailed(string reason)
        {
            _logger.LogWarning(
                "⚠️ TOKEN_REFRESH_FAILED | Reason: {Reason} | IP: {IP}",
                reason, GetClientIp());
        }

        // ✅ PASSWORD EVENTS
        public void LogPasswordResetRequested(string email)
        {
            _logger.LogInformation(
                "🔑 PASSWORD_RESET_REQUESTED | Email: {Email} | IP: {IP}",
                email, GetClientIp());
        }

        public void LogPasswordResetSuccess(string userId, string email)
        {
            _logger.LogInformation(
                "✅ PASSWORD_RESET_SUCCESS | User: {Email} | UserId: {UserId} | IP: {IP}",
                email, userId, GetClientIp());
        }

        public void LogPasswordResetFailed(string email, string reason)
        {
            _logger.LogWarning(
                "❌ PASSWORD_RESET_FAILED | Email: {Email} | Reason: {Reason} | IP: {IP}",
                email, reason, GetClientIp());
        }

        // ✅ EMAIL CONFIRMATION EVENTS
        public void LogEmailConfirmationSent(string email)
        {
            _logger.LogInformation(
                "📧 EMAIL_CONFIRMATION_SENT | Email: {Email} | IP: {IP}",
                email, GetClientIp());
        }

        public void LogEmailConfirmed(string userId, string email)
        {
            _logger.LogInformation(
                "✅ EMAIL_CONFIRMED | User: {Email} | UserId: {UserId} | IP: {IP}",
                email, userId, GetClientIp());
        }

        // ✅ LOGOUT EVENTS
        public void LogLogout(string userId)
        {
            _logger.LogInformation(
                "🔓 LOGOUT | UserId: {UserId} | IP: {IP}",
                userId, GetClientIp());
        }
    }
}
