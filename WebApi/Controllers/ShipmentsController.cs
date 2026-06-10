using Business.Contracts;
using Business.Contracts.Shipment;
using Business.DTOS;
using Business.Services;
using Business.Services.Shipment;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Exceptions;
using DataAccessLayer.Model;
using Domains;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Models;


namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class ShipmentsController : ControllerBase
    {


        private readonly ILogger<ShipmentsController> _logger;
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IShipmentCommand _shipmentCommand;
        private readonly IShipmentStateHandlerFactory _shipmentStateHandlerFactory;
       

        public ShipmentsController( 
            ILogger<ShipmentsController> logger,
            IShipmentQuery shipmentQuery,
            IShipmentCommand shipmentCommand,
            IShipmentStateHandlerFactory shipmentStateHandlerFactory)
            
        {
            _shipmentQuery = shipmentQuery;
            _logger = logger;
            _shipmentCommand = shipmentCommand;
            _shipmentStateHandlerFactory = shipmentStateHandlerFactory;
          
        }


        [HttpPost("{id}/ready")]
        [EnableRateLimiting("form-submit")]
        public async Task<IActionResult> MarkReady(Guid id, [FromQuery] Guid? carrierId = null)
        {
            if (id == Guid.Empty)
                return BadRequest(new { success = false, message = "Invalid shipment ID" });

            if (!Enum.IsDefined(typeof(ShipmentStatusEnum), (int)ShipmentStatusEnum.ReadyForShipping))
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid status value for ReadyForShipping"));

            try
            {
                // Use the factory handler for ReadyForShipping state (ShipmentStatusEnum.ReadyForShipping = 4)
                // CarrierId is passed via the DTO so ReadyForShippingShipment.HandleState can assign it
                //await _shipmentCommand.ReadyForShip(id, carrierId ?? Guid.Empty);
                var dto = new ShippmentDto
                {
                    Id = id,
                    CurrentState = (int)ShipmentStatusEnum.ReadyForShipping,
                    CarrierId = carrierId ?? Guid.Empty
                };

                var handler = _shipmentStateHandlerFactory.GetHandler(ShipmentStatusEnum.ReadyForShipping);
                await handler.HandleState(dto);

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Shipment Ready For Shipping"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark shipment {Id} as ready", id);
                return StatusCode(500, ApiResponse<object>.FailureResponse("Failed To Mark Shipment Ready For Shipping"));
            }
        }








        //[HttpPost("{id}/shipped")]
        //[EnableRateLimiting("form-submit")]
        //public async Task<IActionResult> MarkShipped(Guid id)
        //{
        //    if (id == Guid.Empty)
        //        return BadRequest(new { success = false, message = "Invalid shipment ID" });

        //    try
        //    {

        //        await _shipmentCommand.Shipped(id, DateTime.Now);

        //        var updated = await _shipmentQuery.GetByIdAsync(id);
        //        return Ok(ApiResponse<bool>.SuccessResponse(true, "Shipment Ready Fro Shipped"));

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to mark shipment {Id} as ready", id);
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("Failed To Mark Shipment Ready For Shipment"));
        //    }
        //}



        //[HttpPost("{id}/approved")]
        //[EnableRateLimiting("form-submit")]
        //public async Task<IActionResult> Approved(Guid id, [FromBody] ShippmentDto dto)
        //{
        //    if (id == Guid.Empty || dto == null || id != dto.Id)
        //        return BadRequest(new { success = false, message = "Invalid shipment ID or payload" });

        //    try
        //    {
        //        await _shipmentCommand.Approved(dto);
        //        var updated = await _shipmentQuery.GetByIdAsync(id);
        //        return Ok(ApiResponse<bool>.SuccessResponse(true, "Shipment approved"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to approve shipment {Id}", id);
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("Failed to approve shipment"));
        //    }
        //}



        //[HttpPut("{id}")]
        //[EnableRateLimiting("form-submit")]
        //public async Task<ActionResult<ApiResponse<bool>>> EditPut(Guid id, [FromBody] ShippmentDto dto)
        //{
        //    if (id == Guid.Empty || dto == null || id != dto.Id)
        //        return BadRequest(ApiResponse<bool>.FailureResponse("Invalid request payload."));

        //    if (!ModelState.IsValid)
        //    {
        //        var msErrors = ModelState
        //            .Where(kvp => kvp.Value.Errors.Any())
        //            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList());
        //        return BadRequest(new { errors = msErrors });
        //    }

        //    try
        //    {
        //        // Fix: Edit returns Task not Task<bool>, so just await it, don't assign
        //        await _shipmentCommand.Edit(dto).ConfigureAwait(false);

        //        // If no exception, assume update succeeded
        //        return Ok(ApiResponse<bool>.SuccessResponse(true, "Updated"));
        //    }
        //    catch (DataAccessExceptions dex)
        //    {
        //        _logger.LogError(dex, "Update database error for {Id}", id);
        //        var error = new Error("E002", "Database error");
        //        return StatusCode(500, ApiResponse<bool>.FailureResponse("Database error", new List<Error> { error }));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "API Edit failed for shipment {Id}", id);
        //        return StatusCode(500, ApiResponse<bool>.FailureResponse("An error occurred while updating the shipment."));
        //    }
        //}

        //[HttpPost("{id}")]
        //[EnableRateLimiting("form-submit")]
        //public Task<ActionResult<ApiResponse<bool>>> EditPost(Guid id, [FromBody] ShippmentDto dto)
        //{
        //    // Compatibility wrapper: delegate to PUT handler to keep a single update code path.
        //    return EditPut(id, dto);
        //}


        [HttpGet("paged")]
        public async Task<ActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "CreatedDate",
            [FromQuery] string sortDir = "desc")
        {
            try
            {
                var paged = await _shipmentQuery.GetAllShipments(page, pageSize);
                //var paged = await _shipment.;
                paged ??= new PagedResult<ShippmentDto>
                {
                    Items = new List<ShippmentDto>(),
                    TotalCount = 0,
                    Page = Math.Max(1, page),
                    PageSize = Math.Max(1, pageSize)
                };

                var result = new
                {
                    Items = paged.Items ?? new List<ShippmentDto>(),
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount,
                    TotalPages = paged.TotalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPaged failed");
                return StatusCode(500, "Failed to get paged shipments");
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ShippmentDto>>>> Get()
        {
            try
            {
                //var shipments = await _shipment.GetShippments();
                var shipments = await _shipmentQuery.GetShipments();
                if (shipments == null || shipments.Count == 0)
                {
                    var error = new Error("E003", "Shipments not found");
                    return ApiResponse<List<ShippmentDto>>.FailureResponse("Shipments not found", new List<Error> { error });
                }

                return Ok(ApiResponse<List<ShippmentDto>>.SuccessResponse(shipments));
            }
            catch (DataAccessExceptions ex)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<ShippmentDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipments");
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<List<ShippmentDto>>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }



        [HttpPost("Create")]
        [EnableRateLimiting("form-submit")]
        public async Task<IActionResult> Create([FromBody] ShippmentDto shipment)
        {
            if (shipment == null)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Shipment data is null"));
            }

            if (!ModelState.IsValid)
            {
                var msErrors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    );

                return BadRequest(new { errors = msErrors });
            }

            try
            {
                // Use the factory handler for Created state (ShipmentStatusEnum.Created = 1)
                var handler = _shipmentStateHandlerFactory.GetHandler(ShipmentStatusEnum.Created);
                await handler.HandleState(shipment);
                return Ok(ApiResponse<bool>.SuccessResponse(true, "Shipment Created Successfully"));
              

            }
            catch (DataAccessExceptions daex)
            {
                _logger.LogError(daex, "Create database error");
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<string>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An error occurred while creating shipment");
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<List<ShippmentDto>>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }



        ////[HttpDelete("{id}")] // This specifies a DELETE request to /api/Shipments/{id}

        //////[HttpDelete("{id}/Delete")]
        ///// <summary>
        ///// Deletes a shipment by id.
        ///// DEPRECATED: Use POST /api/Shipments/{id}/UpdateStatus?newState=0 instead for consistency with state pattern.
        ///// This endpoint is maintained for backward compatibility and delegates to the DeletedShipment state handler.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        ///// // add the extra route attribute so both /{id} and /{id}/Delete map to this action
        //[HttpDelete("{id}")]
        //[HttpDelete("{id}/Delete")]
        //[EnableRateLimiting("form-submit")]
        //[Obsolete("Use POST /api/Shipments/{id}/UpdateStatus?newState=0 instead for consistency with state pattern")]
        //public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
        //{
        //    if (id == Guid.Empty)
        //        return BadRequest(ApiResponse<object>.FailureResponse("Invalid id"));

        //    try
        //    {
        //        // ✅ Delegate to state handler for consistency with Approved, Shipped, Delivered, etc.
        //        // This ensures status history is tracked and soft-delete infrastructure is used
        //        var data = new ShippmentDto { Id = id, CurrentState = (int)ShipmentStatusEnum.Deleted };
        //        var handler = _shipmentStateHandlerFactory.GetHandler(ShipmentStatusEnum.Deleted);
        //        await handler.HandleState(data);

        //        return Ok(ApiResponse<object>.SuccessResponse(null, "Shipment deleted"));
        //    }
        //    catch (DataAccessExceptions ex)
        //    {
        //        _logger.LogError(ex, "Delete database error for {Id}", id);
        //        var error = new Error("E002", "Database error");
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("Database error", new List<Error> { error }));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting shipment {Id}", id);
        //        var error = new Error("E001", "An error occurred");
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred", new List<Error> { error }));
        //    }
        //}



        [HttpPost("{id}/ChangeStatus")]
        [EnableRateLimiting("form-submit")]
        // NOTE: this endpoint handles change status to make Edit Shipment and to Approve shipment
       
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ShippmentDto data)
        {
            if (data == null)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid request payload"));
            }

            try
            {
                ShipmentStatusEnum targetStatus = (ShipmentStatusEnum)data.CurrentState;

                // REMOVED: Payment validation moved to state handlers
                // Each state handler (ApproveShipment, EditShipment) 
              

                var result = _shipmentStateHandlerFactory.GetHandler(targetStatus);
                await result.HandleState(data);

                return Ok(ApiResponse<object>.SuccessResponse(result, "Shipment status changed successfully."));
            }
            catch (InvalidOperationException invOpEx)
            {
                // State handler business rule violation (e.g., unpaid shipment cannot be approved)
                _logger.LogWarning(invOpEx, "Business rule violation for shipment {Id}", id);
                return BadRequest(ApiResponse<object>.FailureResponse(invOpEx.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing status for shipment {Id}", id);
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }

        [HttpPost("{id}/UpdateStatus")]
        [EnableRateLimiting("form-submit")]
        // MINIMAL STATUS UPDATE ENDPOINT - Should NOT require full DTO validation
        // Used by: Shipped, Delivered, Cancelled, Returned, Deleted terminal states
        // THIS ENDPOINT WAS CAUSING 400 VALIDATION ERRORS because it passes minimal DTO to state handlers
        
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] int newState)
        {
            if (id == Guid.Empty)
                return BadRequest(ApiResponse<object>.FailureResponse("Invalid shipment ID"));

            if (!Enum.IsDefined(typeof(ShipmentStatusEnum), newState))
                return BadRequest(ApiResponse<object>.FailureResponse($"Invalid status value: {newState}"));

            try
            {
                ShipmentStatusEnum targetStatus = (ShipmentStatusEnum)newState;

                // PROBLEM: This minimal DTO only has Id and CurrentState
                // But state handlers might require full shipment data for validation
                var data = new ShippmentDto { Id = id, CurrentState = newState };

                // ISSUE: GetHandler returns handlers that may validate full DTO fields
                // causing errors like: Width, Height, Length, Weight, UserSender, UserReceiver required
                var result = _shipmentStateHandlerFactory.GetHandler(targetStatus);
                await result.HandleState(data);

                return Ok(ApiResponse<object>.SuccessResponse(null, "Status updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status for shipment {Id} to {State}", id, newState);
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }



        //[HttpPost("{id}/approved")]
        //[EnableRateLimiting("form-submit")]
        //public async Task<IActionResult> Approved(Guid id, [FromBody] ShippmentDto dto)
        //{
        //    if (id == Guid.Empty || dto == null || id != dto.Id)
        //        return BadRequest(new { success = false, message = "Invalid shipment ID or payload" });

        //    try
        //    {
        //        await _shipmentCommand.Approved(dto);
        //        var updated = await _shipmentQuery.GetByIdAsync(id);
        //        return Ok(ApiResponse<bool>.SuccessResponse(true, "Shipment approved"));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to approve shipment {Id}", id);
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("Failed to approve shipment"));
        //    }
        //}

        //[HttpPost("{id}/changestatus")]
        //[EnableRateLimiting("form-submit")]
        //public async Task<ActionResult<ApiResponse<object>>> ChangeStatus(Guid id, [FromQuery] int newState = 0)
        //{
        //    if (id == Guid.Empty) return BadRequest(ApiResponse<object>.FailureResponse("Invalid id"));

        //    try
        //    {
        //        // Call service ChangeStatusAsync directly (no reflection/invoke)
        //        var ok = await _shipmentCommand.ChangeStatusAsync(id, newState).ConfigureAwait(false);
        //        if (!ok)
        //        {
        //            return StatusCode(500, ApiResponse<object>.FailureResponse("Failed to change status"));
        //        }

        //        return Ok(ApiResponse<object>.SuccessResponse(null, "Status changed"));
        //    }
        //    catch (DataAccessExceptions ex)
        //    {
        //        _logger.LogError(ex, "ChangeStatus database error for {Id}", id);
        //        var error = new Error("E002", "Database error");
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("Database error", new List<Error> { error }));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error changing status for {Id}", id);
        //        var error = new Error("E001", "An error occurred");
        //        return StatusCode(500, ApiResponse<object>.FailureResponse("An error occurred", new List<Error> { error }));
        //    }
        //}



        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ShippmentDto>>> GetById(Guid id)
        {
            try
            {
                var dto = await _shipmentQuery.GetByIdAsync(id);
                if (dto == null)
                {
                    return NotFound(ApiResponse<ShippmentDto>.FailureResponse("Shipment not found", new List<Error> { new Error("E003", "Shipment not found") }));
                }
                return Ok(ApiResponse<ShippmentDto>.SuccessResponse(dto));
            }
            catch (DataAccessExceptions ex)
            {
                _logger.LogError(ex, "Database error getting shipment with ID {id}", id);
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<ShippmentDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipment with ID {id}", id);
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<ShippmentDto>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }


    }
}































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































