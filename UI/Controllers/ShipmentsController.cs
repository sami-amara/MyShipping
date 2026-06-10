using AutoMapper;
using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Helpers;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Exceptions;
using DataAccessLayer.Model;
using Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using UI.Constants;
using UI.Helpers;
using UI.Models;
using AppResource;

namespace UI.Controllers
{
    [Authorize]
    public class ShipmentsController : Controller
    {
        private readonly ILogger<ShipmentsController> _logger;
        private readonly IShipmentCommand _shipmentCommand;
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IUserService _userService;

        public ShipmentsController(
            ILogger<ShipmentsController> logger,
            IShipmentQuery shipmentQuery,
            IShipmentCommand shipmentCommand,
            IUserService userService)
        {

            _logger = logger;
            _shipmentCommand = shipmentCommand;
            _shipmentQuery = shipmentQuery;
            _userService = userService;
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(AccountController.Login), "Account");
                }

                // 📦 Load user preferences from Phase 5 (Shipping Preferences)
                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user != null)
                {
                    // ✅ Pass Phase 5 defaults to view via ViewData (Country, City, Package and Type only - no Carrier in Create flow)
                    ViewData["DefaultCountryId"] = user.DefaultCountryId?.ToString() ?? "";
                    ViewData["DefaultCityId"] = user.DefaultCityId?.ToString() ?? "";
                    ViewData["DefaultShippingPackageId"] = user.DefaultShippingPackageId?.ToString() ?? "";
                    ViewData["DefaultShippingTypeId"] = user.DefaultShippingTypeId?.ToString() ?? "";

                    _logger.LogInformation("🎯 Create shipment - loaded user defaults: Package={Package}, Type={Type}",
                        user.DefaultShippingPackageId, user.DefaultShippingTypeId);
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Create() GET: {Message}", ex.Message);
                return View();
            }
        }

        /// <summary>
        /// Displays a paged, filterable list of shipments for the current logged-in user.
        /// Unlike the admin version, this ONLY shows the user's own shipments (isUserData=true).
        /// Supports searching, status filtering, and payment status filtering with pagination.
        /// </summary>
        /// <param name="q">Search term to filter by sender/receiver name or tracking number (optional)</param>
        /// <param name="status">Shipment status filter (int enum value - optional, user can select from dropdown)</param>
        /// <param name="isPaid">Payment status filter: true=paid, false=unpaid, null=all (optional)</param>
        /// <param name="page">Current page number (1-based, default=1)</param>
        /// <param name="pageSize">Number of items per page (default=10)</param>
        /// <param name="sortBy">Column to sort by (default="CreatedDate", currently not used in query)</param>
        /// <param name="sortDir">Sort direction: "asc" or "desc" (default="desc", currently not used in query)</param>
        /// <returns>View with ShipmentsIndexViewModel containing the user's paged shipments</returns>
        public async Task<IActionResult> List(string q = null, int? status = null, bool? isPaid = null, int page = 1,
            int pageSize = 10, string sortBy = "CreatedDate", string sortDir = "desc")
        {
            PagedResult<ShippmentDto> paged;
            try
            {
                // ✅ Convert status parameter from nullable int to List<ShipmentStatusEnum>
                // If user selected a status from dropdown, create a single-item list
                List<ShipmentStatusEnum>? statusFilter = null;
                if (status.HasValue && Enum.IsDefined(typeof(ShipmentStatusEnum), status.Value))
                {
                    // User selected a specific status (e.g., Created, Approved, Shipped, etc.)
                    statusFilter = new List<ShipmentStatusEnum> { (ShipmentStatusEnum)status.Value };
                }
                // ✅ If status is null, statusFilter stays null = show all statuses

                // ✅ Compute sort direction (currently not passed to service - service always sorts by CreatedDate desc)
                // TODO: Future enhancement - pass sortBy and isDescending to service for dynamic sorting
                bool isDescending = SortingHelper.IsSortDescending(sortDir);

                // ✅ Query shipments for CURRENT USER ONLY (isUserData=true)
                // Key difference from admin controller: this only shows the logged-in user's shipments
                paged = await _shipmentQuery.GetShipments(
                    pageNumber: page,          // Current page (1-based)
                    pageSize: pageSize,        // Items per page
                    isUserData: true,          // ✅ TRUE = only current user's shipments (not admin view)
                    statuses: statusFilter,    // Status filter (null = all statuses)
                    searchTerm: q,             // Search by sender/receiver name or tracking number
                    isPaid: isPaid             // Payment status filter (null = all)
                );
            }
            catch (Exception ex)
            {
                // ✅ ERROR HANDLING: Log error and return empty result gracefully
                _logger.LogError(ex, "Failed to load paged shipments (List)");

                // Create safe empty result to show user "No shipments found" instead of error page
                paged = new PagedResult<ShippmentDto>
                {
                    Items = new List<ShippmentDto>(),  // Empty list
                    Page = Math.Max(1, page),           // Ensure page >= 1 (prevent negative/zero)
                    PageSize = Math.Max(1, pageSize),   // Ensure pageSize >= 1
                    TotalCount = 0                      // No results
                };
            }

            // ✅ Generate pagination UI info (page numbers, prev/next buttons, etc.)
            // windowSize=7 means show up to 7 page number links at once
            var pager = paged.ToPaginationInfo(windowSize: 7);

            // ✅ Build view model with all data needed for rendering the shipments list page
            var vm = new ShipmentsIndexViewModel
            {
                Paged = paged,             // Paged results (Items + pagination metadata)
                Pager = pager,             // UI pagination info (for rendering page links)
                SortBy = sortBy,           // Current sort column (maintain state in view)
                SortDir = sortDir,         // Current sort direction (for toggle links)
                Search = q,                // Current search term (maintain in search box)
                StatusFilter = status,     // Current status filter (selected dropdown value)
                IsPaidFilter = isPaid      // Current payment filter (checkbox state)
            };

            return View(vm);  // Render the List view with the populated view model
        }

       

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(Guid id)
        {
            TempData["MessageType"] = null;

            try
            {
                // ✅ Get shipment to check its current state
                var shipment = await _shipmentQuery.GetByIdAsync(id);

                if (shipment == null)
                {
                    TempData["MessageType"] = MessageType.NotFound;
                    TempData["Message"] = Labels.ShipmentNotFound;
                    return RedirectToAction(nameof(List));
                }

                // ✅ Business Rule: Only allow deletion of Created shipments
                if (shipment.CurrentState != (int)ShipmentStatusEnum.Created)
                {
                    var currentStatus = Enum.IsDefined(typeof(ShipmentStatusEnum), shipment.CurrentState)
                        ? ((ShipmentStatusEnum)shipment.CurrentState).ToString()
                        : "Unknown";

                    TempData["MessageType"] = MessageType.Warning;
                    TempData["Message"] = string.Format(Labels.CannotDeleteShipmentStatus, currentStatus);
                    return RedirectToAction(nameof(List));
                }

                // ✅ Safe to delete (soft delete by changing status to Deleted)
                //await _shipments.ChangeStatus(id, (int)ShipmentStatusEnum.Deleted);
                await _shipmentCommand.ChangeStatusAsync(id, (int)ShipmentStatusEnum.Deleted);
                TempData["MessageType"] = MessageType.DeleteSuccess;
                TempData["Message"] = Labels.ShipmentDeletedSuccessfully;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shipment {Id}", id);
                TempData["MessageType"] = MessageType.DeleteFailed;
                TempData["Message"] = Labels.FailedToDeleteShipment;
            }

            return RedirectToAction(nameof(List));
        }
        

        public async Task<IActionResult> Show(Guid id)
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
                _logger.LogError(ex, "Failed to load shipment details for {Id}", id);
                return StatusCode(500);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShippmentDto model, string command = "Save")
        {
            if (model == null) return BadRequest();

            // handle simple wizard navigation if you use it (Previous/Next)... here we assume full save
            if (!TryValidateModel(model))
            {
                return View(model);
            }

            try
            {
                // Call the strongly-typed service method directly. Edit returns Task (void) so
                // if it completes without throwing we treat the update as successful.
                await _shipmentCommand.Edit(model).ConfigureAwait(false);

                TempData["MessageType"] = MessageType.UpdateSuccess;
                return RedirectToAction(nameof(Show), new { id = model.Id });
            }
            catch (DataAccessExceptions dex)
            {
                _logger.LogError(dex, "Update database error for shipment {Id}", model?.Id);
                ModelState.AddModelError(string.Empty, Labels.DatabaseError);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit failed for shipment {Id}", model?.Id);
                ModelState.AddModelError(string.Empty, Labels.ErrorOccurredUpdating);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return View(model);
            }
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

    }
}




















