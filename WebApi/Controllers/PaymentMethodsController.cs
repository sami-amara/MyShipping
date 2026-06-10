using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly IPaymentMethods _paymentMethods;
        private readonly ILogger<PaymentMethodsController> _logger;

        public PaymentMethodsController(IPaymentMethods paymentMethods, ILogger<PaymentMethodsController> logger)
        {
            _paymentMethods = paymentMethods;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PaymentMethodDto>>>> Get()
        {
            try
            {
                var paymentMethods = await _paymentMethods.GetActivePaymentMethods();
                if (paymentMethods == null || paymentMethods.Count == 0)
                {
                    var error = new Error("E003", "Active payment methods not found");
                    return ApiResponse<List<PaymentMethodDto>>.FailureResponse("Active payment methods not found", new List<Error> { error });
                }

                return Ok(ApiResponse<List<PaymentMethodDto>>.SuccessResponse(paymentMethods));
            }
            catch (DataAccessExceptions)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<List<PaymentMethodDto>>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment methods");
                var error = new Error("E001", "An error occurred");
                return ApiResponse<List<PaymentMethodDto>>.FailureResponse("An error occurred", new List<Error> { error });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PaymentMethodDto>>> GetById(Guid id)
        {
            try
            {
                var paymentMethod = await _paymentMethods.GetPaymentMethodById(id);
                if (paymentMethod == null)
                {
                    var error = new Error("E003", $"Payment method with ID {id} not found");
                    return NotFound(ApiResponse<PaymentMethodDto>.FailureResponse("Payment method not found", new List<Error> { error }));
                }

                return Ok(ApiResponse<PaymentMethodDto>.SuccessResponse(paymentMethod));
            }
            catch (DataAccessExceptions)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<PaymentMethodDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment method by ID");
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<PaymentMethodDto>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }

        [HttpPost("calculate-total")]
        public async Task<ActionResult<ApiResponse<decimal>>> CalculateTotal([FromBody] CalculateTotalRequest request)
        {
            try
            {
                if (request.PaymentMethodId == Guid.Empty)
                {
                    var error = new Error("E004", "Payment method ID is required");
                    return BadRequest(ApiResponse<decimal>.FailureResponse("Invalid payment method ID", new List<Error> { error }));
                }

                if (!await _paymentMethods.IsPaymentMethodActive(request.PaymentMethodId))
                {
                    var error = new Error("E006", "Payment method is inactive or not found");
                    return BadRequest(ApiResponse<decimal>.FailureResponse("Inactive payment method", new List<Error> { error }));
                }

                if (request.ShippingRate <= 0)
                {
                    var error = new Error("E005", "Shipping rate must be greater than zero");
                    return BadRequest(ApiResponse<decimal>.FailureResponse("Invalid shipping rate", new List<Error> { error }));
                }

                var totalAmount = await _paymentMethods.CalculateTotalWithCommission(request.PaymentMethodId, request.ShippingRate);
                return Ok(ApiResponse<decimal>.SuccessResponse(totalAmount));
            }
            catch (ArgumentException ex)
            {
                var error = new Error("E003", ex.Message);
                return NotFound(ApiResponse<decimal>.FailureResponse(ex.Message, new List<Error> { error }));
            }
            catch (DataAccessExceptions)
            {
                var error = new Error("E002", "Database error");
                return StatusCode(500, ApiResponse<decimal>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total with commission");
                var error = new Error("E001", "An error occurred");
                return StatusCode(500, ApiResponse<decimal>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }
    }

    public class CalculateTotalRequest
    {
        public Guid PaymentMethodId { get; set; }
        public decimal ShippingRate { get; set; }
    }
}
