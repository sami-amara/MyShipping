using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer;
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

                _logger.LogInformation(
                    "✅ Audit log recorded: Action={Action}, UserId={UserId}, InitiatedBy={InitiatedBy}, Timestamp={Timestamp}",
                    action, userId, initiatedBy, auditLog.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error recording audit log for user {UserId}", userId);
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
        /// Retrieves all deactivated users with their deactivation details.
        /// Used by Admin Dashboard.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetDeactivatedUsersAsync()
        {
            var context = _context as ShippingContext;

            return await context.Users
                .Where(u => u.IsDeactivated)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.DeactivatedAt,
                    LastAction = context.AccountAuditLogs
                        .Where(a => a.UserId == u.Id)
                        .OrderByDescending(a => a.Timestamp)
                        .FirstOrDefault().Timestamp
                })
                .OrderByDescending(u => u.DeactivatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves audit logs with user information for admin viewing.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsWithUserInfoAsync(int limit = 100)
        {
            var context = _context as ShippingContext;

            return await _context.AccountAuditLogs
                .Join(
                    context.Users,
                    audit => audit.UserId,
                    user => user.Id,
                    (audit, user) => new
                    {
                        audit.Id,
                        audit.Action,
                        audit.Reason,
                        audit.InitiatedBy,
                        audit.IpAddress,
                        audit.Timestamp,
                        UserEmail = user.Email,
                        UserName = $"{user.FirstName} {user.LastName}",
                        UserIsDeactivated = user.IsDeactivated
                    }
                )
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Searches audit logs by user email.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsByUserEmailAsync(string email, int limit = 100)
        {
            var context = _context as ShippingContext;

            return await _context.AccountAuditLogs
                .Where(a => a.InitiatedBy == email || a.UserId == email)
                .Join(
                    context.Users,
                    audit => audit.UserId,
                    user => user.Id,
                    (audit, user) => new
                    {
                        audit.Id,
                        audit.Action,
                        audit.Reason,
                        audit.InitiatedBy,
                        audit.IpAddress,
                        audit.Timestamp,
                        UserEmail = user.Email,
                        UserName = $"{user.FirstName} {user.LastName}"
                    }
                )
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Searches audit logs by action type with filters.
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetAuditLogsByActionWithDetailsAsync(string action, int limit = 100)
        {
            var context = _context as ShippingContext;

            return await _context.AccountAuditLogs
                .Where(a => a.Action == action)
                .Join(
                    context.Users,
                    audit => audit.UserId,
                    user => user.Id,
                    (audit, user) => new
                    {
                        audit.Id,
                        audit.Action,
                        audit.Reason,
                        audit.InitiatedBy,
                        audit.IpAddress,
                        audit.Timestamp,
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = $"{user.FirstName} {user.LastName}",
                        UserIsDeactivated = user.IsDeactivated
                    }
                )
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToListAsync();
        }



    }
}
