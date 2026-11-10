using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services.Shipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Constants;
using UI.Helpers;
using UI.Models;

namespace UI.Areas.admin.Controllers
{


    [Area("admin")]

    [Route("admin/[controller]")]

    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]

    public class AuditTimelineController : Controller
    {
        private readonly IShipmentAuditService _auditService;
        private readonly ILogger<AuditTimelineController> _logger;

        public AuditTimelineController(
            IShipmentAuditService auditService,
            ILogger<AuditTimelineController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Serves the Razor view for the Shipment Audit Timeline page, using server-side
        /// paging via the shared PaginationHelper (same pattern as Admin/Shipments/List).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string q = null, int page = 1, int pageSize = 10)
        {
            var paged = await _auditService.SearchAuditTimelineAsync(q, page, pageSize);
            var pager = paged.ToPaginationInfo(windowSize: 7);

            var vm = new AuditTimelineIndexViewModel
            {
                Paged = paged,
                Pager = pager,
                Search = q ?? string.Empty
            };

            return View(vm);
        }

        [HttpGet("shipment/{shipmentIdOrTracking}")]
        public async Task<IActionResult> GetShipmentTimeline(string shipmentIdOrTracking)
        {
            if (string.IsNullOrWhiteSpace(shipmentIdOrTracking))
                return BadRequest(new { error = "Shipment ID is required" });

            try
            {
                IEnumerable<ShipmentTimelineEventDto> timeline;
                if (Guid.TryParse(shipmentIdOrTracking, out var shipmentId))
                {
                    timeline = await _auditService.GetShipmentTimelineAsync(shipmentId);
                }
                else
                {
                    _logger.LogInformation($"Tracking number search requested: {shipmentIdOrTracking}");
                    timeline = await _auditService.GetShipmentTimelineByTrackingAsync(shipmentIdOrTracking);
                }

                if (timeline == null || !timeline.Any())
                    return NotFound(new { error = "Shipment not found" });

                return Ok(timeline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shipment timeline");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserShipmentEvents(string userId, [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { error = "User ID is required" });

            try
            {
                if (pageSize <= 0 || pageSize > 500)
                    pageSize = 50;

                var events = await _auditService.GetUserShipmentEventsAsync(userId, pageSize);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user shipment events");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        [HttpGet("user-search/{userSearch}")]
        public async Task<IActionResult> SearchUserShipmentEvents(string userSearch, [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(userSearch))
                return BadRequest(new { error = "User search term is required" });

            try
            {
                if (pageSize <= 0 || pageSize > 500)
                    pageSize = 50;

                var events = await _auditService.SearchUserShipmentEventsAsync(userSearch, pageSize);

                if (events == null || !events.Any())
                    return NotFound(new { error = "No shipment events found for this user" });

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching user shipment events");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }
    }
}










































//using Business.Contracts.Shipment;
//using Business.Services.Shipment;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Threading.Tasks;

//namespace UI.Controllers
//{
//    [Authorize(Roles = "Admin")]
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AuditTimelineController : ControllerBase
//    {
//        private readonly IShipmentAuditService _auditService;
//        private readonly ILogger<AuditTimelineController> _logger;

//        public AuditTimelineController(
//            IShipmentAuditService auditService,
//            ILogger<AuditTimelineController> logger)
//        {
//            _auditService = auditService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Get timeline events for a specific shipment by ShipmentId or TrackingNumber
//        /// </summary>
//        [HttpGet("shipment/{shipmentIdOrTracking}")]
//        public async Task<IActionResult> GetShipmentTimeline(string shipmentIdOrTracking)
//        {
//            if (string.IsNullOrWhiteSpace(shipmentIdOrTracking))
//                return BadRequest(new { error = "Shipment ID or tracking number is required" });

//            try
//            {
//                // Try parsing as GUID first
//                if (Guid.TryParse(shipmentIdOrTracking, out var shipmentId))
//                {
//                    var timeline = await _auditService.GetShipmentTimelineAsync(shipmentId);
//                    return Ok(timeline);
//                }
//                else
//                {
//                    // TODO: If tracking number is provided, query shipment table to get ShipmentId
//                    // For now, return empty result
//                    _logger.LogInformation($"Tracking number search requested: {shipmentIdOrTracking}");
//                    return NotFound(new { error = "Shipment not found with the provided tracking number" });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving shipment timeline");
//                return StatusCode(500, new { error = "An error occurred while retrieving the timeline" });
//            }
//        }

//        /// <summary>
//        /// Get shipment events created/modified by a specific user
//        /// </summary>
//        [HttpGet("user/{userId}")]
//        public async Task<IActionResult> GetUserShipmentEvents(string userId, [FromQuery] int pageSize = 50)
//        {
//            if (string.IsNullOrWhiteSpace(userId))
//                return BadRequest(new { error = "User ID is required" });

//            try
//            {
//                if (pageSize <= 0 || pageSize > 500)
//                    pageSize = 50;

//                var events = await _auditService.GetUserShipmentEventsAsync(userId, pageSize);
//                return Ok(events);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving user shipment events");
//                return StatusCode(500, new { error = "An error occurred while retrieving user events" });
//            }
//        }
//    }
//}




























////using Business.Contracts.Shipment;
////using Business.Services.Shipment;
////using Microsoft.AspNetCore.Authorization;
////using Microsoft.AspNetCore.Mvc;
////using System;
////using System.Threading.Tasks;


////namespace UI.Controllers
////{
////    [Authorize(Roles = "Admin")]
////    [Route("api/[controller]")]
////    [ApiController]
////    public class AuditTimelineController : ControllerBase
////    {
////        private readonly IShipmentAuditService _auditService;

////        public AuditTimelineController(IShipmentAuditService auditService)
////        {
////            _auditService = auditService;
////        }

////        /// <summary>
////        /// Get timeline events for a specific shipment
////        /// </summary>
////        [HttpGet("shipment/{shipmentId}")]
////        public async Task<IActionResult> GetShipmentTimeline(Guid shipmentId)
////        {
////            if (shipmentId == Guid.Empty)
////                return BadRequest(new { error = "Invalid shipment ID" });

////            var timeline = await _auditService.GetShipmentTimelineAsync(shipmentId);
////            return Ok(timeline);
////        }

////        /// <summary>
////        /// Get shipment events created/modified by a specific user
////        /// </summary>
////        [HttpGet("user/{userId}")]
////        public async Task<IActionResult> GetUserShipmentEvents(string userId, [FromQuery] int pageSize = 50)
////        {
////            if (string.IsNullOrWhiteSpace(userId))
////                return BadRequest(new { error = "Invalid user ID" });

////            var events = await _auditService.GetUserShipmentEventsAsync(userId, pageSize);
////            return Ok(events);
////        }
////    }
////}