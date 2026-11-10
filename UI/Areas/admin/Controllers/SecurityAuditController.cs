using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI.Constants;
using UI.Services;
using Business.Services.Logging;
using UI.Helpers;
using UI.Models;

namespace UI.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin controller for managing and reviewing security audit logs.
    /// Provides paginated views for suspicious activities, rate limit breaches, failed logins, etc.
    /// Only accessible to Admin role.
    /// </summary>
    [Area("admin")]
    [Authorize(Roles = RoleNames.Admin)]
    [Route("admin/security-audit")]
    public class SecurityAuditController : Controller
    {
        private readonly SecurityAuditService _securityAuditService;
        private readonly ILogger<SecurityAuditController> _logger;
        private const int DefaultPageSize = 20;

        public SecurityAuditController(
            SecurityAuditService securityAuditService,
            ILogger<SecurityAuditController> logger)
        {
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        /// <summary>
        /// Security audit dashboard showing overview and aggregated event patterns.
        /// </summary>
        [HttpGet("")]
        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _securityAuditService.GetSecurityStatsAsync();
                var aggregatedEvents = await _securityAuditService.GetAggregatedEventsAsync(
                    hoursBack: 24,
                    limit: 50);

                ViewBag.Stats = stats;
                ViewBag.AggregatedEvents = aggregatedEvents;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading security audit dashboard");
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading security audit data.";
                return RedirectToAction("Index", "Home", new { area = "admin" });
            }
        }

        /// <summary>
        /// View all unreviewed suspicious events with pagination.
        /// </summary>
        [HttpGet("unreviewed")]
        public async Task<IActionResult> Unreviewed(int pageNumber = 1, int pageSize = DefaultPageSize)
        {
            try
            {
                var pagedEvents = await _securityAuditService.GetUnreviewedEventsPagedAsync(
                    pageNumber, pageSize);
                var paginationInfo = pagedEvents.ToPaginationInfo();

                ViewBag.PaginationInfo = paginationInfo;
                ViewBag.PagedEvents = pagedEvents.Items;
                ViewBag.ViewTitle = "Unreviewed Events";

                return View("EventList", pagedEvents.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading unreviewed events");
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading events.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// View all critical security events with pagination.
        /// </summary>
        [HttpGet("critical")]
        public async Task<IActionResult> Critical(int pageNumber = 1, int pageSize = DefaultPageSize)
        {
            try
            {
                var pagedEvents = await _securityAuditService.GetCriticalEventsPagedAsync(
                    pageNumber, pageSize);
                var paginationInfo = pagedEvents.ToPaginationInfo();

                ViewBag.PaginationInfo = paginationInfo;
                ViewBag.PagedEvents = pagedEvents.Items;
                ViewBag.ViewTitle = "Critical Events";

                return View("EventList", pagedEvents.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading critical events");
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading events.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// View events by type with pagination.
        /// </summary>
        [HttpGet("by-type/{eventType}")]
        public async Task<IActionResult> ByType(
            string eventType,
            int pageNumber = 1,
            int pageSize = DefaultPageSize)
        {
            try
            {
                var pagedEvents = await _securityAuditService.GetEventsByTypePagedAsync(
                    eventType, pageNumber, pageSize);
                var paginationInfo = pagedEvents.ToPaginationInfo();

                ViewBag.PaginationInfo = paginationInfo;
                ViewBag.PagedEvents = pagedEvents.Items;
                ViewBag.ViewTitle = $"Events: {eventType}";
                ViewBag.FilterType = "Type";
                ViewBag.FilterValue = eventType;

                return View("EventList", pagedEvents.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading events by type: {EventType}", eventType);
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading events.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// View events by IP address with pagination.
        /// </summary>
        [HttpGet("by-ip/{ipAddress}")]
        public async Task<IActionResult> ByIp(
            string ipAddress,
            int pageNumber = 1,
            int pageSize = DefaultPageSize)
        {
            try
            {
                var pagedEvents = await _securityAuditService.GetEventsByIpPagedAsync(
                    ipAddress, pageNumber, pageSize);
                var paginationInfo = pagedEvents.ToPaginationInfo();

                ViewBag.PaginationInfo = paginationInfo;
                ViewBag.PagedEvents = pagedEvents.Items;
                ViewBag.ViewTitle = $"Events from IP: {ipAddress}";
                ViewBag.FilterType = "IP Address";
                ViewBag.FilterValue = ipAddress;

                return View("EventList", pagedEvents.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading events by IP: {IpAddress}", ipAddress);
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading events.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// View events by user email with pagination.
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> ByEmail(
            string email,
            int pageNumber = 1,
            int pageSize = DefaultPageSize)
        {
            try
            {
                var pagedEvents = await _securityAuditService.GetEventsByEmailPagedAsync(
                    email, pageNumber, pageSize);
                var paginationInfo = pagedEvents.ToPaginationInfo();

                ViewBag.PaginationInfo = paginationInfo;
                ViewBag.PagedEvents = pagedEvents.Items;
                ViewBag.ViewTitle = $"Events for: {email}";
                ViewBag.FilterType = "Email";
                ViewBag.FilterValue = email;

                return View("EventList", pagedEvents.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error loading events by email: {Email}", email);
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error loading events.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Mark a security event as reviewed by admin.
        /// </summary>
        [HttpPost("mark-reviewed/{eventId}")]
        public async Task<IActionResult> MarkReviewed(Guid eventId, string adminNotes = "")
        {
            try
            {
                var success = await _securityAuditService.MarkAsReviewedAsync(eventId, adminNotes);

                if (success)
                {
                    TempData["AlertType"] = "success";
                    TempData["AlertMessage"] = "✅ Event marked as reviewed.";
                }
                else
                {
                    TempData["AlertType"] = "warning";
                    TempData["AlertMessage"] = "⚠️ Could not find event to review.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔴 Error marking event as reviewed: {EventId}", eventId);
                TempData["AlertType"] = "error";
                TempData["AlertMessage"] = "Error processing request.";
                return RedirectToAction("Index");
            }
        }
    }
}
























//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using UI.Constants;
//using UI.Services;
//using Business.Services.Logging;

//namespace UI.Areas.Admin.Controllers
//{
//    /// <summary>
//    /// Admin controller for managing and reviewing security audit logs.
//    /// Provides dashboard views for suspicious activities, rate limit breaches, failed logins, etc.
//    /// Only accessible to Admin role.
//    /// </summary>
//    [Area("admin")]
//    [Authorize(Roles = RoleNames.Admin)]
//    [Route("admin/security-audit")]
//    public class SecurityAuditController : Controller
//    {
//        private readonly SecurityAuditService _securityAuditService;
//        private readonly ILogger<SecurityAuditController> _logger;

//        public SecurityAuditController(
//            SecurityAuditService securityAuditService,
//            ILogger<SecurityAuditController> logger)
//        {
//            _securityAuditService = securityAuditService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Security audit dashboard showing overview and recent suspicious activities.
//        /// </summary>
//        [HttpGet("")]
//        [HttpGet("index")]
//        public async Task<IActionResult> Index()
//        {
//            try
//            {
//                var stats = await _securityAuditService.GetSecurityStatsAsync();
//                var unreviewedEvents = await _securityAuditService.GetUnreviewedEventsAsync(10);
//                var criticalEvents = await _securityAuditService.GetCriticalEventsAsync(5);

//                ViewBag.Stats = stats;
//                ViewBag.UnreviewedEvents = unreviewedEvents;
//                ViewBag.CriticalEvents = criticalEvents;

//                return View();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading security audit dashboard");
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading security audit data.";
//                return RedirectToAction("Index", "Home", new { area = "admin" });
//            }
//        }

//        /// <summary>
//        /// View all unreviewed suspicious events.
//        /// </summary>
//        [HttpGet("unreviewed")]
//        public async Task<IActionResult> Unreviewed(int limit = 100)
//        {
//            try
//            {
//                var events = await _securityAuditService.GetUnreviewedEventsAsync(limit);
//                return View("EventList", events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading unreviewed events");
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading events.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// View all critical security events.
//        /// </summary>
//        [HttpGet("critical")]
//        public async Task<IActionResult> Critical(int limit = 100)
//        {
//            try
//            {
//                var events = await _securityAuditService.GetCriticalEventsAsync(limit);
//                return View("EventList", events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading critical events");
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading events.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// View events by type (rate limit, login failures, token reuse, etc).
//        /// </summary>
//        [HttpGet("by-type/{eventType}")]
//        public async Task<IActionResult> ByType(string eventType, int limit = 100)
//        {
//            try
//            {
//                var events = await _securityAuditService.GetEventsByTypeAsync(eventType, limit);
//                ViewBag.EventType = eventType;
//                return View("EventList", events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading events by type: {EventType}", eventType);
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading events.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// View all suspicious events from a specific IP address.
//        /// </summary>
//        [HttpGet("by-ip/{ipAddress}")]
//        public async Task<IActionResult> ByIP(string ipAddress, int limit = 100)
//        {
//            try
//            {
//                var events = await _securityAuditService.GetEventsByIpAsync(ipAddress, limit);
//                ViewBag.IPAddress = ipAddress;
//                return View("EventList", events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading events by IP: {IpAddress}", ipAddress);
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading events.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// View all suspicious events for a specific user.
//        /// </summary>
//        [HttpGet("by-user/{userId}")]
//        public async Task<IActionResult> ByUser(string userId, int limit = 100)
//        {
//            try
//            {
//                var events = await _securityAuditService.GetEventsByUserAsync(userId, limit);
//                ViewBag.UserId = userId;
//                return View("EventList", events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error loading events by user: {UserId}", userId);
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error loading events.";
//                return RedirectToAction("Index");
//            }
//        }

//        /// <summary>
//        /// Mark a security event as reviewed and add admin notes.
//        /// </summary>
//        [HttpPost("mark-reviewed/{eventId}")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> MarkReviewed(Guid eventId, string adminNotes)
//        {

//            try
//            {
//                var success = await _securityAuditService.MarkAsReviewedAsync(eventId, adminNotes);

//                if (success)
//                {
//                    TempData["AlertType"] = "success";
//                    TempData["AlertMessage"] = "Event marked as reviewed.";
//                }
//                else
//                {
//                    TempData["AlertType"] = "error";
//                    TempData["AlertMessage"] = "Event not found.";
//                }

//                return RedirectToAction("Index");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "🔴 Error marking event as reviewed: {EventId}", eventId);
//                TempData["AlertType"] = "error";
//                TempData["AlertMessage"] = "Error updating event.";
//                return RedirectToAction("Index");
//            }
//        }
//    }
//}
