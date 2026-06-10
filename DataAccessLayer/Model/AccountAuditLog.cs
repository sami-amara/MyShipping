using System;
using DataAccessLayer.UserModels;

namespace DataAccessLayer.Models
{
    /// <summary>
    /// Audit log for tracking account lifecycle events (deactivation, reactivation, lockout, etc.).
    /// Used for GDPR compliance, user privacy tracking, and security monitoring.
    /// </summary>
    public class AccountAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User ID whose account was affected.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Description of the action performed.
        /// Examples: "Deactivated", "Reactivated", "Locked", "Unlocked", "PasswordChanged"
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Detailed reason or notes about the action (optional).
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Email of the user/admin who initiated this action.
        /// For user-initiated actions, this is the user's own email.
        /// For admin actions, this is the admin's email.
        /// </summary>
        public string InitiatedBy { get; set; }

        /// <summary>
        /// IP address of the request (optional, for security tracking).
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Timestamp when this action occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // ✅ Foreign key navigation
        public virtual ApplicationUser User { get; set; }
    }
}
