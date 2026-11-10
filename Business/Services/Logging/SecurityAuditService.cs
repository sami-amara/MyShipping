
using DataAccessLayer.DbContext;
using DataAccessLayer.Extensions;
using DataAccessLayer.Model;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Logging
{
    /// <summary>
    /// Service for managing security audit logs with pagination, aggregation, and filtering.
    /// Provides queries for admin dashboard to review security incidents with drill-down capabilities.
    /// </summary>
    public class SecurityAuditService
    {
        private readonly ShippingContext _context;
        private readonly ILogger<SecurityAuditService> _logger;

        /// <summary>
        /// Shipment lifecycle event types that represent normal workflow progression
        /// (not security incidents). These are excluded from Security Audit listings
        /// and remain visible only in the Shipment Audit Timeline feature.
        /// </summary>
        //private static readonly HashSet<string> ExcludedShipmentLifecycleEventTypes = new(StringComparer.OrdinalIgnoreCase)
        private static readonly HashSet<string> NormalShipmentLifecycleEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            "SHIPMENT_CREATED",
            "SHIPMENT_UPDATED",
            "SHIPMENT_APPROVED",
            "SHIPMENT_READYFORSHIPPING",
            "SHIPMENT_SHIPPED",
            "SHIPMENT_DELIVERED"
        };

        public SecurityAuditService(ShippingContext context, ILogger<SecurityAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Filters out normal shipment lifecycle events (Created, Updated, Approved,
        /// ReadyForShipping, Shipped, Delivered) from Security Audit queries, since these
        /// are routine workflow events and not security incidents. Shipment exception events
        /// (Cancelled, Returned, Refunded, Deleted) remain visible since they may require
        /// investigation or review.
        /// </summary>
        private static IQueryable<SecurityAuditLog> ExcludeShipmentLifecycleEvents(IQueryable<SecurityAuditLog> query)
        {
            return query.Where(e => !NormalShipmentLifecycleEvents.Contains(e.EventType));
        }

        // ═══════════════════════════════════════════════════════════════
        // PAGINATED QUERY METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets paginated unreviewed security events.
        /// </summary>
        public async Task<PagedResult<SecurityAuditLog>> GetUnreviewedEventsPagedAsync(
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                            
                var query = ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => !e.IsReviewed)
                    .OrderByDescending(e => e.Timestamp);

                return await query.ToPagedResultAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting paginated unreviewed events");
                return new PagedResult<SecurityAuditLog>
                {
                    Items = new List<SecurityAuditLog>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// Gets paginated critical security events.
        /// </summary>
        public async Task<PagedResult<SecurityAuditLog>> GetCriticalEventsPagedAsync(
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var query = ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => e.Severity == "Critical")
                    .OrderByDescending(e => e.Timestamp);

                return await query.ToPagedResultAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting paginated critical events");
                return new PagedResult<SecurityAuditLog>
                {
                    Items = new List<SecurityAuditLog>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// Gets paginated events by event type.
        /// </summary>
        public async Task<PagedResult<SecurityAuditLog>> GetEventsByTypePagedAsync(
            string eventType,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var query = ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => e.EventType == eventType)
                    .OrderByDescending(e => e.Timestamp);

                return await query.ToPagedResultAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting paginated events by type: {EventType}", eventType);
                return new PagedResult<SecurityAuditLog>
                {
                    Items = new List<SecurityAuditLog>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// Gets paginated events by IP address.
        /// </summary>
        public async Task<PagedResult<SecurityAuditLog>> GetEventsByIpPagedAsync(
            string ipAddress,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var query = ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => e.IpAddress == ipAddress)
                    .OrderByDescending(e => e.Timestamp);

                return await query.ToPagedResultAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting paginated events by IP: {IpAddress}", ipAddress);
                return new PagedResult<SecurityAuditLog>
                {
                    Items = new List<SecurityAuditLog>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// Gets paginated events by user email.
        /// </summary>
        public async Task<PagedResult<SecurityAuditLog>> GetEventsByEmailPagedAsync(
            string email,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var query = ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => e.Email == email)
                    .OrderByDescending(e => e.Timestamp);

                return await query.ToPagedResultAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting paginated events by email: {Email}", email);
                return new PagedResult<SecurityAuditLog>
                {
                    Items = new List<SecurityAuditLog>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // AGGREGATED QUERY METHODS (For Dashboard Summary)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets aggregated event counts grouped by EventType, Email, and IpAddress.
        /// Used for dashboard Event Breakdown section to show patterns instead of raw logs.
        /// </summary>
        public async Task<List<AggregatedSecurityEvent>> GetAggregatedEventsAsync(
            int hoursBack = 24,
            int limit = 50)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

                var aggregated = await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                    .Where(e => e.Timestamp >= cutoffTime)
                    .GroupBy(e => new { e.EventType, e.Email, e.IpAddress })
                    .Select(g => new AggregatedSecurityEvent
                    {
                        EventType = g.Key.EventType,
                        Email = g.Key.Email ?? "unknown",
                        IpAddress = g.Key.IpAddress ?? "unknown",
                        AttemptCount = g.Count(),
                        FirstAttempt = g.Min(e => e.Timestamp),
                        LastAttempt = g.Max(e => e.Timestamp),
                        Severity = g.First().Severity,
                        IsReviewed = g.All(e => e.IsReviewed)
                    })
                    .OrderByDescending(a => a.AttemptCount)
                    .ThenByDescending(a => a.LastAttempt)
                    .Take(limit)
                    .ToListAsync();

                return aggregated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error getting aggregated events");
                return new List<AggregatedSecurityEvent>();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ORIGINAL NON-PAGINATED METHODS (For compatibility)
        // ═══════════════════════════════════════════════════════════════

        public async Task LogSecurityEventAsync(
            string eventType,
            string severity,
            string? userId,
            string? email,
            string? ipAddress,
            string? endpoint,
            string? description,
            string? metadata = null,
            string? correlationId = null,
            Guid? shipmentId = null,
            string? trackingNumber = null,
            string? shipmentEventType = null,
            string? reason = null)
        {
            try
            {
                var securityLog = new SecurityAuditLog
                {
                    EventType = eventType,
                    Severity = severity,
                    UserId = userId,

                    ShipmentId = shipmentId,
                    TrackingNumber = trackingNumber,
                    ShipmentEventType = shipmentEventType,
                    Reason = reason,


                    Email = email,
                    IpAddress = ipAddress,
                    Endpoint = endpoint,
                    Description = description,
                    Metadata = metadata,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityAuditLogs.Add(securityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error recording security audit log: {EventType}", eventType);
            }
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetUnreviewedEventsAsync(int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => !e.IsReviewed)
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetCriticalEventsAsync(int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.Severity == "Critical")
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByTypeAsync(
            string eventType, int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.EventType == eventType)
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetHighRiskEventsAsync(int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.Severity == "Critical" || e.Severity == "High")
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReviewedAsync(Guid eventId, string adminNotes)
        {
            try
            {
                var securityEvent = await _context.SecurityAuditLogs.FindAsync(eventId);
                if (securityEvent == null)
                    return false;

                securityEvent.IsReviewed = true;
                securityEvent.ReviewedAt = DateTime.UtcNow;
                securityEvent.AdminNotes = adminNotes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error marking security event as reviewed: {EventId}", eventId);
                return false;
            }
        }

        public async Task<SecurityAuditStats> GetSecurityStatsAsync(int hoursBack = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

            var recentEvents = await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.Timestamp >= cutoffTime)
                .ToListAsync();

            return new SecurityAuditStats
            {
                TotalEvents = recentEvents.Count,
                CriticalEvents = recentEvents.Count(e => e.Severity == "Critical"),
                HighRiskEvents = recentEvents.Count(e => e.Severity == "High"),
                UnreviewedEvents = recentEvents.Count(e => !e.IsReviewed),
                RateLimitBreaches = recentEvents.Count(e => e.EventType == "RATE_LIMIT_BREACH"),
                FailedLogins = recentEvents.Count(e => e.EventType == "LOGIN_FAILED"),
                TokenReuseEvents = recentEvents.Count(e => e.EventType == "TOKEN_REUSE_DETECTED"),
                AccountLockouts = recentEvents.Count(e => e.EventType == "ACCOUNT_LOCKED"),
                UniqueSuspiciousIPs = recentEvents
                    .Where(e => !string.IsNullOrEmpty(e.IpAddress))
                    .Select(e => e.IpAddress)
                    .Distinct()
                    .Count()
            };
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByIpAsync(
            string ipAddress, int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.IpAddress == ipAddress)
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByEmailAsync(
            string email, int limit = 100)
        {
            return await ExcludeShipmentLifecycleEvents(_context.SecurityAuditLogs)
                .Where(e => e.Email == email)
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Security audit statistics for dashboard.
    /// </summary>
    public class SecurityAuditStats
    {
        public int TotalEvents { get; set; }
        public int CriticalEvents { get; set; }
        public int HighRiskEvents { get; set; }
        public int UnreviewedEvents { get; set; }
        public int RateLimitBreaches { get; set; }
        public int FailedLogins { get; set; }
        public int TokenReuseEvents { get; set; }
        public int AccountLockouts { get; set; }
        public int UniqueSuspiciousIPs { get; set; }
    }

    /// <summary>
    /// Aggregated security event for dashboard summary.
    /// Groups multiple events by type, email, and IP for pattern analysis.
    /// </summary>
    public class AggregatedSecurityEvent
    {
        public string EventType { get; set; }
        public string Email { get; set; }
        public string IpAddress { get; set; }
        public int AttemptCount { get; set; }
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
        public string Severity { get; set; }
        public bool IsReviewed { get; set; }

        /// <summary>
        /// Gets badge color based on event severity.
        /// </summary>
        public string GetSeverityBadgeClass() => Severity switch
        {
            "Critical" => "bg-danger",
            "High" => "bg-warning",
            "Medium" => "bg-info",
            _ => "bg-secondary"
        };

        /// <summary>
        /// Gets event type icon for UI display.
        /// </summary>
        public string GetEventIcon() => EventType switch
        {
            "RATE_LIMIT_BREACH" => "⚠️",
            "LOGIN_FAILED" => "🔐",
            "TOKEN_REUSE_DETECTED" => "🚨",
            "ACCOUNT_LOCKED" => "🔒",
            "ACCOUNT_DEACTIVATED" => "❌",
            _ => "📊"
        };
    }
}
























//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using DataAccessLayer.DbContext;
//using DataAccessLayer.Models;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace Business.Services.Logging
//{
//    /// <summary>
//    /// Service for managing security audit logs and suspicious activity tracking.
//    /// Provides queries for admin dashboard to review security incidents.
//    /// 
//    /// SECURITY FOCUS:
//    /// - Rate limit breaches (brute force prevention)
//    /// - Failed login attempts (authentication attacks)
//    /// - Account lockouts (automated response to attacks)
//    /// - Token reuse (account compromise detection)
//    /// - Unusual activity patterns (anomaly detection)
//    /// </summary>
//    public class SecurityAuditService
//    {

//        private readonly ShippingContext _context;
//        private readonly ILogger<SecurityAuditService> _logger;

//        public SecurityAuditService(ShippingContext context, ILogger<SecurityAuditService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Records a security event in the audit log.
//        /// Called by security loggers to store suspicious/security events in database.
//        /// </summary>
//        public async Task LogSecurityEventAsync(
//            string eventType,
//            string severity,
//            string? userId,
//            string? email,
//            string? ipAddress,
//            string? endpoint,
//            string? description,
//            string? metadata = null,
//            string? correlationId = null)
//        {
//            try
//            {
//                var securityLog = new SecurityAuditLog
//                {
//                    EventType = eventType,
//                    Severity = severity,
//                    UserId = userId,
//                    Email = email,
//                    IpAddress = ipAddress,
//                    Endpoint = endpoint,
//                    Description = description,
//                    Metadata = metadata,
//                    CorrelationId = correlationId,
//                    Timestamp = DateTime.UtcNow
//                };

//                _context.SecurityAuditLogs.Add(securityLog);
//                await _context.SaveChangesAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error recording security audit log: {EventType}", eventType);
//                // Don't throw - security logging should not break main operation
//            }
//        }

//        /// <summary>
//        /// Gets all unreviewed security events (for admin dashboard alerts).
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetUnreviewedEventsAsync(int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => !e.IsReviewed)
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets critical security events (potential breaches).
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetCriticalEventsAsync(int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.Severity == "CRITICAL")
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets events by type (e.g., "RATE_LIMIT_BREACH", "TOKEN_REUSE", "LOGIN_FAILED").
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByTypeAsync(
//            string eventType, int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.EventType == eventType)
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets events for a specific IP address (detect coordinated attacks).
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByIpAsync(
//            string ipAddress, int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.IpAddress == ipAddress)
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets events for a specific user (account compromise investigation).
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByUserAsync(
//            string userId, int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.UserId == userId)
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets events from a specific time period.
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetEventsByTimeRangeAsync(
//            DateTime startTime, DateTime endTime, int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Gets high-risk events: CRITICAL and HIGH severity.
//        /// </summary>
//        public async Task<IEnumerable<SecurityAuditLog>> GetHighRiskEventsAsync(int limit = 100)
//        {
//            return await _context.SecurityAuditLogs
//                .Where(e => e.Severity == "CRITICAL" || e.Severity == "HIGH")
//                .OrderByDescending(e => e.Timestamp)
//                .Take(limit)
//                .ToListAsync();
//        }

//        /// <summary>
//        /// Marks an event as reviewed and adds admin notes.
//        /// </summary>
//        public async Task<bool> MarkAsReviewedAsync(Guid eventId, string adminNotes)
//        {
//            try
//            {
//                var securityEvent = await _context.SecurityAuditLogs.FindAsync(eventId);
//                if (securityEvent == null)
//                    return false;

//                securityEvent.IsReviewed = true;
//                securityEvent.ReviewedAt = DateTime.UtcNow;
//                securityEvent.AdminNotes = adminNotes;

//                await _context.SaveChangesAsync();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error marking security event as reviewed: {EventId}", eventId);
//                return false;
//            }
//        }



//        /// <summary>
//        /// Gets security statistics for dashboard.
//        /// </summary>
//        public async Task<SecurityAuditStats> GetSecurityStatsAsync(int hoursBack = 24)
//        {
//            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

//            var recentEvents = await _context.SecurityAuditLogs
//                .Where(e => e.Timestamp >= cutoffTime)
//                .ToListAsync();

//            return new SecurityAuditStats
//            {
//                TotalEvents = recentEvents.Count,
//                CriticalEvents = recentEvents.Count(e => e.Severity == "CRITICAL"),
//                HighRiskEvents = recentEvents.Count(e => e.Severity == "HIGH"),
//                UnreviewedEvents = recentEvents.Count(e => !e.IsReviewed),
//                RateLimitBreaches = recentEvents.Count(e => e.EventType == "RATE_LIMIT_BREACH"),
//                FailedLogins = recentEvents.Count(e => e.EventType == "LOGIN_FAILED"),
//                TokenReuseEvents = recentEvents.Count(e => e.EventType == "TOKEN_REUSE"),
//                AccountLockouts = recentEvents.Count(e => e.EventType == "ACCOUNT_LOCKOUT"),
//                UniqueSuspiciousIPs = recentEvents
//                    .Where(e => !string.IsNullOrEmpty(e.IpAddress))
//                    .Select(e => e.IpAddress)
//                    .Distinct()
//                    .Count()
//            };
//        }
//    }

//    /// <summary>
//    /// Security audit statistics for dashboard.
//    /// </summary>
//    public class SecurityAuditStats
//    {
//        public int TotalEvents { get; set; }
//        public int CriticalEvents { get; set; }
//        public int HighRiskEvents { get; set; }
//        public int UnreviewedEvents { get; set; }
//        public int RateLimitBreaches { get; set; }
//        public int FailedLogins { get; set; }
//        public int TokenReuseEvents { get; set; }
//        public int AccountLockouts { get; set; }
//        public int UniqueSuspiciousIPs { get; set; }
//    }
//}



