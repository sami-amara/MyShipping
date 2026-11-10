using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.DbContext;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UI.Services
{
    /// <summary>
    /// Service for logging and querying account audit events.
    /// Provides audit trail for GDPR compliance and security monitoring.
    /// </summary>
    public class AccountAuditService
    {
        private readonly ShippingContext _context;
        private readonly ILogger<AccountAuditService> _logger;

        public AccountAuditService(ShippingContext context,
            ILogger<AccountAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Records an account action in the audit log.
        /// </summary>
        public async Task LogAccountActionAsync(string userId, string action, string initiatedBy,
            string? reason = null, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AccountAuditLog
                {
                    UserId = userId,
                    Action = action,
                    InitiatedBy = initiatedBy,
                    Reason = reason,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.AccountAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                //_logger.LogInformation(
                //    "✅ Audit log recorded: Action={Action}, UserId={UserId}, InitiatedBy={InitiatedBy}, Timestamp={Timestamp}",
                //    action, userId, initiatedBy, auditLog.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error recording audit log for user {UserId}", userId);
                // Don't throw - audit logging should not break the main operation
            }
        }

        /// <summary>
        /// Retrieves audit logs for a specific user.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetUserAuditLogsAsync(string userId, int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves audit logs for a specific action across all users.
        /// Useful for admin monitoring and security analysis.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetAuditLogsByActionAsync(string action, int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.Action == action)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all deactivation events.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetDeactivationLogsAsync(int limit = 100)
        {
            return await GetAuditLogsByActionAsync("Deactivated", limit);
        }

        /// <summary>
        /// Retrieves all account reactivation events.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetReactivationLogsAsync(int limit = 100)
        {
            return await GetAuditLogsByActionAsync("Reactivated", limit);
        }

        /// <summary>
        /// Retrieves all failed login attempt logs.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetFailedLoginLogsAsync(int limit = 100)
        {
            return await GetAuditLogsByActionAsync("FailedLoginAttempt_InvalidPassword", limit);
        }

        /// <summary>
        /// Retrieves all account lockout events.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetAccountLockoutLogsAsync(int limit = 100)
        {
            return await GetAuditLogsByActionAsync("FailedLoginAttempt_LockedOut", limit);
        }

        /// <summary>
        /// Retrieves all password change events.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetPasswordChangeLogsAsync(int limit = 100)
        {
            return await GetAuditLogsByActionAsync("PasswordChanged", limit);
        }

        /// <summary>
        /// Retrieves audit logs from a specific time period.
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetAuditLogsByTimeRangeAsync(
            DateTime startTime, DateTime endTime, int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.Timestamp >= startTime && a.Timestamp <= endTime)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }



        /// <summary>
        /// Gets all deactivated users (for admin dashboard).
        /// </summary>
        public async Task<IEnumerable<AccountAuditLog>> GetDeactivatedUsersAsync(int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.Action == "Deactivated")
                .OrderByDescending(a => a.Timestamp)
                .Distinct()
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Gets audit logs filtered by user email (with user details).
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsByUserEmailAsync(string email, int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.InitiatedBy.Contains(email))
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.Action,
                    a.Reason,
                    a.InitiatedBy,
                    a.IpAddress,
                    a.Timestamp,
                    UserEmail = a.User != null ? a.User.Email : "Unknown"
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets audit logs by action with detailed user information.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsByActionWithDetailsAsync(string action, int limit = 100)
        {
            return await _context.AccountAuditLogs
                .Where(a => a.Action == action)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.Action,
                    a.Reason,
                    a.InitiatedBy,
                    a.IpAddress,
                    a.Timestamp,
                    UserEmail = a.User != null ? a.User.Email : "Unknown",
                    UserPhoneNumber = a.User != null ? a.User.PhoneNumber : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets all audit logs with user information included.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsWithUserInfoAsync(int limit = 100)
        {
            return await _context.AccountAuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.Action,
                    a.Reason,
                    a.InitiatedBy,
                    a.IpAddress,
                    a.Timestamp,
                    UserEmail = a.User != null ? a.User.Email : "Unknown",
                    UserPhoneNumber = a.User != null ? a.User.PhoneNumber : null,
                    UserIsDeactivated = a.User != null ? a.User.IsDeactivated : false
                })
                .ToListAsync();
        }





        /// <summary>
        /// Gets audit statistics for dashboard.
        /// </summary>
        public async Task<AccountAuditStats> GetAuditStatsAsync(int hoursBack = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

            var recentAudits = await _context.AccountAuditLogs
                .Where(a => a.Timestamp >= cutoffTime)
                .ToListAsync();

            return new AccountAuditStats
            {
                TotalEvents = recentAudits.Count,
                Deactivations = recentAudits.Count(a => a.Action == "Deactivated"),
                Reactivations = recentAudits.Count(a => a.Action == "Reactivated"),
                FailedLogins = recentAudits.Count(a => a.Action.Contains("FailedLoginAttempt")),
                PasswordChanges = recentAudits.Count(a => a.Action == "PasswordChanged"),
                AccountLockouts = recentAudits.Count(a => a.Action.Contains("LockedOut")),
                UniqueUsersAffected = recentAudits.Select(a => a.UserId).Distinct().Count()
            };
        }
    }

    /// <summary>
    /// Account audit statistics for admin dashboard.
    /// </summary>
    public class AccountAuditStats
    {
        public int TotalEvents { get; set; }
        public int Deactivations { get; set; }
        public int Reactivations { get; set; }
        public int FailedLogins { get; set; }
        public int PasswordChanges { get; set; }
        public int AccountLockouts { get; set; }
        public int UniqueUsersAffected { get; set; }
    }
}
