using System;
using DataAccessLayer.UserModels;

namespace DataAccessLayer.Models
{
    /// <summary>
    /// Security audit log for tracking suspicious and security-related activities.
    /// Captures: rate limit breaches, failed logins with lockout, token reuse, brute force attempts, etc.
    /// Used for security incident analysis, threat detection, and compliance reporting.
    /// 
    /// NOTE: Different from AccountAuditLog which tracks account lifecycle (deactivation, password change, etc.)
    /// SecurityAuditLog focuses on SUSPICIOUS ACTIVITIES and SECURITY EVENTS.
    /// </summary>
    public class SecurityAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of security event: "LOGIN_FAILED", "RATE_LIMIT_BREACH", "TOKEN_REUSE", "ACCOUNT_LOCKOUT", etc.
        /// </summary>
        public string EventType { get; set; }
       
        /// <summary>
        /// Severity level of the event: "LOW", "MEDIUM", "HIGH", "CRITICAL"
        /// Examples:
        ///   - LOW: Single failed login attempt
        ///   - MEDIUM: Rate limit breach
        ///   - HIGH: Account lockout after multiple failed attempts
        ///   - CRITICAL: Token reuse detected (possible account compromise)
        /// </summary>
        public string Severity { get; set; } // "LOW", "MEDIUM", "HIGH", "CRITICAL"

        /// <summary>
        /// User ID if event is related to a specific user account (can be null for IP-based events).
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Email of the affected/targeted user or admin (masked in logs for privacy).
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// IP address of the request source. Critical for rate limit and brute force detection.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Endpoint/path that was targeted: /Account/Login, /api/auth/login, /Account/Register, etc.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Detailed description of what happened and why it was flagged.
        /// Examples:
        ///   - "Invalid password attempt #3 of 5"
        ///   - "Rate limit exceeded: 11 requests in 5 minutes"
        ///   - "Token reuse detected for same user within 2 minutes"
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Additional metadata as JSON for flexible storage (request id, user agent, correlation id, etc).
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Whether this event has been reviewed/acknowledged by an admin.
        /// </summary>
        public bool IsReviewed { get; set; } = false;

        /// <summary>
        /// Admin notes about the incident (investigation notes, actions taken, etc).
        /// </summary>
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Timestamp when this security event occurred (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when admin reviewed this event (nullable, null until reviewed).
        /// </summary>
        /// 

        /// <summary>
        /// Optional: ShipmentId for shipment-specific audit events.
        /// Useful for timeline filtering and workflow tracking.
        /// </summary>
        public Guid? ShipmentId { get; set; }

        /// <summary>
        /// Shipment tracking number (denormalized for quick display in timeline).
        /// </summary>
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Shipment state event type: "Created", "Updated", "Approved", "ReadyForShip", "Shipped", "Delivered", "Returned", "Refunded", "Cancelled", "Deleted"
        /// </summary>
        public string? ShipmentEventType { get; set; }

        /// <summary>
        /// Reason for state change (e.g., cancellation reason, return reason, refund reason).
        /// </summary>
        public string? Reason { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? CorrelationId { get; set; }

        // 🔑 Foreign key reflection
        public virtual ApplicationUser? User { get; set; }
        
    }
}




