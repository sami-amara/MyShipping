using AutoMapper;
using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Helpers;
using Business.Services;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Exceptions;
using DataAccessLayer.Model;
using Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using UI.Constants;
using UI.Helpers;
using UI.Models;
using UI.Services;
using UI.Controllers;


namespace UI.Areas.admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]
    //[Authorize(Roles = "Admin,Reviewer,Operation,OperationManager")]
    //[Authorize]
    public class ShipmentsController : Controller
    {

       
        IShipmentQuery _shipmentQuery;
        private readonly ILogger _logger;
        private readonly IUserService _userService;
        public ShipmentsController( ILogger<HomeController> logger,
              IUserService userService,
        IShipmentQuery shipmentQuery)
        {
            
            _logger = logger;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
           
        }

        public IActionResult Create() => View();

        /// <summary>
        /// Displays a paged, filterable list of shipments for admin users.
        /// Implements role-based filtering: Admin sees all, Reviewer sees Created+Updated, Operation sees Approved, etc.
        /// Supports searching by sender/receiver name or tracking number, filtering by status and payment status.
        /// </summary>
        /// <param name="q">Search term to filter by sender/receiver name or tracking number (optional)</param>
        /// <param name="status">Shipment status filter (int enum value - optional, user can select from dropdown)</param>
        /// <param name="isPaid">Payment status filter: true=paid, false=unpaid, null=all (optional)</param>
        /// <param name="page">Current page number (1-based, default=1)</param>
        /// <param name="pageSize">Number of items per page (default=10)</param>
        /// <param name="sortBy">Column to sort by (default="CreatedDate")</param>
        /// <param name="sortDir">Sort direction: "asc" or "desc" (default="desc")</param>
        /// <returns>View with ShipmentsIndexViewModel containing paged results and pagination info</returns>
        public async Task<IActionResult> List(string q = null, int? status = null, bool? isPaid = null, int page = 1, int pageSize = 10,
            string sortBy = "CreatedDate", string sortDir = "desc")
        {
            try
            {
                // ✅ ROLE-BASED FILTERING LOGIC
                // Priority: User-provided status > Role-based default status
                List<ShipmentStatusEnum>? statusFilter = null;

                if (status.HasValue && Enum.IsDefined(typeof(ShipmentStatusEnum), status.Value))
                {
                    // ✅ User explicitly selected a status from dropdown - this takes priority over role defaults
                    statusFilter = new List<ShipmentStatusEnum> { (ShipmentStatusEnum)status.Value };
                }
                else if (User.IsInRole(RoleNames.Admin) || User.IsInRole("Administrator"))
                {
                    // ✅ Admin role: Show ALL shipments regardless of status
                    statusFilter = null;  // null = no status filter
                }
                else if (User.IsInRole(RoleNames.Reviewer))
                {
                    // ✅ Reviewer role: Show Created AND Updated shipments (they need to approve both)
                    // Multi-status filter allows OR logic: status=1 OR status=2
                    statusFilter = new List<ShipmentStatusEnum> 
                    { 
                        ShipmentStatusEnum.Created,   // Status 1
                        ShipmentStatusEnum.Updated    // Status 2
                    };
                }
                else if (User.IsInRole(RoleNames.Operation))
                {
                    // ✅ Operation role: Show only Approved shipments (ready for processing)
                    statusFilter = new List<ShipmentStatusEnum>
                    {
                        ShipmentStatusEnum.Approved   // Status 3
                    };
                }
                else if (User.IsInRole(RoleNames.OperationManager))
                {
                    // ✅ OperationManager role: Show only ReadyForShipping shipments
                    statusFilter = new List<ShipmentStatusEnum> 
                    {  
                        ShipmentStatusEnum.ReadyForShipping  // Status 4
                    };
                }
                else
                {
                    // ✅ Default fallback: Regular users only see Created shipments
                    statusFilter = new List<ShipmentStatusEnum> 
                    { 
                        ShipmentStatusEnum.Created  // Status 1
                    };
                }

                // ✅ Admin view shows ALL users' shipments (not filtered by current user)
                bool isUserData = false;  // false = show all users' shipments

                // ✅ Query shipments with all filters: role-based status, search term, payment status, and paging
                var paged = await _shipmentQuery.GetShipments(
                    pageNumber: page,          // Current page (1-based)
                    pageSize: pageSize,        // Items per page
                    isUserData: isUserData,    // false = all users (admin view)
                    statuses: statusFilter,    // Role-based status filter (can be multiple statuses)
                    searchTerm: q,             // Search by sender/receiver/tracking
                    isPaid: isPaid             // Payment status filter
                );

                // ✅ Generate pagination info (page numbers, prev/next links, etc.)
                // windowSize=7 means show 7 page numbers at a time (e.g., "1 2 3 4 5 6 7")
                var pager = paged.ToPaginationInfo(windowSize: 7);

                // ✅ Build view model with all data needed for the view
                var vm = new ShipmentsIndexViewModel
                {
                    Paged = paged,             // Paged results with Items and metadata
                    Pager = pager,             // Pagination UI info (page links)
                    SortBy = sortBy,           // Current sort column (for maintaining state)
                    SortDir = sortDir,         // Current sort direction (for toggle)
                    Search = q,                // Current search term (for maintaining state)
                    StatusFilter = status,     // Current status filter (for dropdown)
                    IsPaidFilter = isPaid      // Current payment filter (for checkbox)
                };

                return View(vm);  // Render the List view with the view model
            }
            catch (Exception ex)
            {
                // ✅ ERROR HANDLING: Log error and return empty result instead of crashing
                _logger.LogError(ex, "Failed to load paged shipments (List)");

                // Create empty paged result with safe defaults
                var empty = new PagedResult<ShippmentDto>
                {
                    Items = new List<ShippmentDto>(),  // Empty list
                    Page = Math.Max(1, page),           // Ensure page >= 1
                    PageSize = Math.Max(1, pageSize),   // Ensure pageSize >= 1
                    TotalCount = 0                      // No results
                };

                // Generate pagination for empty result
                var pager = empty.ToPaginationInfo(windowSize: 7);

                // Return view with empty data (user sees "No results" instead of error page)
                var vm = new ShipmentsIndexViewModel 
                { 
                    Paged = empty, 
                    Pager = pager, 
                    SortBy = sortBy, 
                    SortDir = sortDir, 
                    Search = q 
                };
                return View(vm);
            }
        }
        
        public ActionResult Details(int id)
        {
            return View();
        }

       

        // POST: ShipmentsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ShipmentsController/Edit/{id}
        //[Authorize(Roles = "Admin,Reviewer")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer}")]
        public async Task<IActionResult> Approve(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for Edit {Id}", id);
                return StatusCode(500);
            }
        }

        //// GET: ShipmentsController/MakeShipmentReadyForShipp/{id}
        ////[Authorize(Roles = "Admin,Manager")]
        //[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Operation}")]
        //public async Task<IActionResult> MakeShipmentReadyForShipp(Guid id)
        //{
        //    if (id == Guid.Empty) return BadRequest();
        //    try
        //    {
        //        var dto = await _shipmentQuery.GetByIdAsync(id);
        //        if (dto == null) return NotFound();
        //        // Render a lightweight view that allows Admin/Manager to select Carrier and mark ReadyForShipping
        //        return View(dto);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to load shipment for MakeShipmentReadyForShipp {Id}", id);
        //        return StatusCode(500);
        //    }
        //}

        public async Task<IActionResult> MakeShipmentReadyForShipp(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();

            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();

                // ✅ Load current user context
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(AccountController.Login), "Account");
                }

                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user != null)
                {
                    
                    ViewData["DefaultCarrierId"] = user.DefaultCarrierId?.ToString() ?? "";

                    _logger.LogInformation("🎯 ReadyForShipping - loaded user defaults: Carrier={Carrier}", user.DefaultCarrierId);
                }

                // Render a lightweight view that allows Admin/Manager to select Carrier and mark ReadyForShipping
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to load shipment for MakeShipmentReadyForShipp {Id}", id);
                return StatusCode(500);
            }
        }


        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.OperationManager}")]
        public async Task<IActionResult> Shipped(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                // Render a lightweight view that allows Admin/Manager to select Carrier and mark ReadyForShipping
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for MakeShipmentReadyForShipp {Id}", id);
                return StatusCode(500);
            }
        }

        
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for SoftDelete {Id}", id);
                return StatusCode(500);
            }

        }

        // GET: ShipmentsController/Cancel/{id}
        [HttpGet]
        [Route("Cancel/{id}")]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<IActionResult> CancelShipment(Guid id)
        {

            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for cancel {Id}", id);
                return StatusCode(500);
            }
        }

        // GET: ShipmentsController/Deliver/{id}
        [HttpGet]
        [Route("Deliver/{id}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.OperationManager}")]
        public async Task<IActionResult> Deliver(Guid id)
        {

            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for Deliver {Id}", id);
                return StatusCode(500);
            }
        }

        // GET: ShipmentsController/Return/{id}
        [HttpGet]
        [Route("Return/{id}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.OperationManager}")]
        public async Task<IActionResult> Return(Guid id)
        {

            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for Return {Id}", id);
                return StatusCode(500);
            }
        }

        // GET: ShipmentsController/SoftDelete/{id}
        [HttpGet]
        [Route("SoftDelete/{id}")]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for SoftDelete {Id}", id);
                return StatusCode(500);
            }
        }



        /// <summary>
        /// Shows detailed information about a specific shipment
        /// </summary>
        /// <param name="id">The shipment ID</param>
        /// <returns>Show view with shipment details</returns>
        [HttpGet]
        [Route("Show/{id}")]
        public async Task<IActionResult> Show(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["MessageType"] = MessageType.Error;
                TempData["Message"] = "Invalid shipment ID";
                return RedirectToAction(nameof(List));
            }

            try
            {
                var shipment = await _shipmentQuery.GetByIdAsync(id);

                if (shipment == null)
                {
                    TempData["MessageType"] = MessageType.NotFound;
                    TempData["Message"] = "Shipment not found";
                    return RedirectToAction(nameof(List));
                }

                // Optional: Check if user has permission to view this shipment
                // For admin area, usually all authorized users can view all shipments
                return View(shipment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shipment {Id} for display", id);
                TempData["MessageType"] = MessageType.Error;
                TempData["Message"] = "An error occurred while loading the shipment details";
                return RedirectToAction(nameof(List));
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{RoleNames.Admin}")]
        public async Task<IActionResult> Refund(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();

            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load shipment for refund {Id}", id);
                return StatusCode(500);
            }
        }
    }
}
