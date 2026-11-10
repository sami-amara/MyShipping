namespace Business.Contracts.Logging
{
    /// <summary>
    /// Interface for centralized security event logging.
    /// Defines contract for logging authentication, token, password, and account management events.
    /// Implemented by SecurityEventLoggerBase in Business.Services.
    /// </summary>
    public interface ISecurityEventLogger
    {
        // ═══════════════════════════════════════════════════════════════════════════════════
        // 🔐 LOGIN EVENTS
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Logs a successful login event.
        /// </summary>
        void LogSuccessfulLogin(string userId, string email);

        /// <summary>
        /// Logs a failed login attempt.
        /// </summary>
        /// 
        Task LogFailedLogin(string email, string reason);

        /// <summary>
        /// Logs when a user account is locked due to failed login attempts.
        /// </summary>
        Task LogAccountLockout(string userId, string email, DateTimeOffset? lockoutEnd);

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 📝 REGISTRATION EVENTS
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Logs a successful user registration.
        /// </summary>
        void LogRegistrationSuccess(string userId, string email);

        /// <summary>
        /// Logs a failed registration attempt.
        /// </summary>
        void LogRegistrationFailed(string email, string reason);

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 🔑 TOKEN MANAGEMENT EVENTS
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Logs token rotation (refresh token used to generate new access token).
        /// </summary>
        void LogTokenRotation(string userId);

        /// <summary>
        /// Logs suspicious token reuse detected (same token used multiple times).
        /// </summary>
        Task LogTokenReuseDetected(string userId, string token);

        /// <summary>
        /// Logs a failed token refresh attempt.
        /// </summary>
        void LogTokenRefreshFailed(string reason);

        /// <summary>
        /// Logs when all tokens are revoked (logout).
        /// </summary>
        void LogAllTokensRevoked(string userId, string reason);

        // ═══════════════════════════════════════════════════════════════════════════════════
        // 🔐 PASSWORD MANAGEMENT EVENTS
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Logs a successful password reset.
        /// </summary>
        void LogPasswordResetSuccess(string userId, string email);

        /// <summary>
        /// Logs a failed password reset attempt.
        /// </summary>
        void LogPasswordResetFailed(string email, string reason);

        /// <summary>
        /// Logs when a password reset request is initiated (email sent).
        /// </summary>
        void LogPasswordResetRequested(string email);

        // ═══════════════════════════════════════════════════════════════════════════════════
        // ✉️ EMAIL & ACCOUNT EVENTS
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Logs successful email address confirmation.
        /// </summary>
        void LogEmailConfirmed(string userId, string email);

        /// <summary>
        /// Logs user logout.
        /// </summary>
        void LogLogout(string userId);

        /// <summary>
        /// Logs a successful password change by authenticated user.
        /// </summary>
        void LogPasswordChangeSuccess(string userId, string email);

        /// <summary>
        /// Logs a failed password change attempt.
        /// </summary>
        void LogPasswordChangeFailed(string email, string reason);

        /// <summary>
        /// Logs a failed email confirmation attempt.
        /// </summary>
        void LogEmailConfirmationFailed(string userId, string reason);

        Task LogShipmentStatusChanged(
            Guid shipmentId,
            string status,
            string userId,
            string? trackingNumber = null,
            string? reason = null,
            string? carrierId = null,
            string severity = "Information");


        // Existing methods (LoginSuccess, LoginFailed, etc.)

        // New method for security-relevant exceptions
        void LogSecurityException(
                string correlationId,
                Exception exception,
                string? userId,
                string? email,
                string? ipAddress,
                string? path);
        
    }
}
