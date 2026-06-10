using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    /// <summary>
    /// Payment Transactions Controller - Educational Example
    /// Handles simulated payment processing and transaction history
    /// THIS IS FOR LEARNING PURPOSES - NOT A REAL PAYMENT SYSTEM
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentTransactionsController : ControllerBase
    {
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly ILogger<PaymentTransactionsController> _logger;

        public PaymentTransactionsController(
            IPaymentTransactionService paymentTransactionService,
            ILogger<PaymentTransactionsController> logger)
        {
            _paymentTransactionService = paymentTransactionService;
            _logger = logger;
        }

        /// <summary>
        /// Process a simulated payment - Educational Endpoint
        /// POST /api/PaymentTransactions/process
        /// </summary>
        [HttpPost("process")]
        public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                // Validate request
                if (request.ShipmentId == Guid.Empty)
                {
                    var error = new Error("E001", "Shipment ID is required");
                    return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse("Invalid shipment ID", new List<Error> { error }));
                }

                if (request.PaymentMethodId == Guid.Empty)
                {
                    var error = new Error("E002", "Payment method ID is required");
                    return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse("Invalid payment method ID", new List<Error> { error }));
                }

                if (request.ShippingRate <= 0)
                {
                    var error = new Error("E003", "Shipping rate must be greater than zero");
                    return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse("Invalid shipping rate", new List<Error> { error }));
                }

                // Process simulated payment
                var transaction = await _paymentTransactionService.ProcessPayment(
                    request.ShipmentId,
                    request.PaymentMethodId,
                    request.ShippingRate,
                    request.PaymentMethodToken);

                // Check if payment failed
                if (transaction.TransactionStatus == 2) // Failed status
                {
                    _logger.LogWarning("Simulated payment failed for shipment {ShipmentId}: {Error}",
                        request.ShipmentId, transaction.ErrorMessage);

                    var error = new Error("E004", transaction.ErrorMessage ?? "Payment processing failed");

                    // Return failure response without data parameter
                    return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse(
                        "Payment failed",
                        new List<Error> { error }));
                }

                _logger.LogInformation("Simulated payment processed successfully for shipment {ShipmentId}. Reference: {Reference}",
                    request.ShipmentId, transaction.TransactionReference);

                return Ok(ApiResponse<PaymentTransactionDto>.SuccessResponse(transaction));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid payment request");
                var error = new Error("E005", ex.Message);
                return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse(ex.Message, new List<Error> { error }));
            }
            catch (DataAccessExceptions ex)
            {
                _logger.LogError(ex, "Database error processing payment");
                var error = new Error("E006", "Database error occurred");
                return StatusCode(500, ApiResponse<PaymentTransactionDto>.FailureResponse("Database error", new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                var error = new Error("E007", "An unexpected error occurred");
                return StatusCode(500, ApiResponse<PaymentTransactionDto>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }

        /// <summary>
        /// Get payment transaction by shipment ID
        /// GET /api/PaymentTransactions/shipment/{shipmentId}
        /// </summary>
        [HttpGet("shipment/{shipmentId}")]
        public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> GetByShipmentId(Guid shipmentId)
        {
            try
            {
                var transaction = await _paymentTransactionService.GetByShipmentId(shipmentId);

                if (transaction == null)
                {
                    var error = new Error("E008", "Payment transaction not found for this shipment");
                    return NotFound(ApiResponse<PaymentTransactionDto>.FailureResponse("Transaction not found", new List<Error> { error }));
                }

                return Ok(ApiResponse<PaymentTransactionDto>.SuccessResponse(transaction));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment transaction for shipment {ShipmentId}", shipmentId);
                var error = new Error("E009", "An error occurred");
                return StatusCode(500, ApiResponse<PaymentTransactionDto>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }

        /// <summary>
        /// Get payment history for current user - Educational Example
        /// GET /api/PaymentTransactions/my-history?page=1&pageSize=10
        /// </summary>
        [HttpGet("my-history")]
        public async Task<ActionResult<ApiResponse<DataAccessLayer.Model.PagedResult<PaymentTransactionDto>>>> GetMyHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // In a real system, you'd get user ID from claims/authentication
                // For educational purposes, this would need proper user context
                // The UI controller will handle current user identity
                var userId = Guid.Empty; // Placeholder - real implementation needs auth context

                var pagedTransactions = await _paymentTransactionService.GetUserPaymentHistory(page, pageSize, userId);

                return Ok(ApiResponse<DataAccessLayer.Model.PagedResult<PaymentTransactionDto>>.SuccessResponse(pagedTransactions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history");
                var error = new Error("E010", "An error occurred");
                return StatusCode(500, ApiResponse<DataAccessLayer.Model.PagedResult<PaymentTransactionDto>>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }

        /// <summary>
        /// Processes a full refund for a payment transaction.
        /// POST /api/PaymentTransactions/{transactionId}/refund
        /// </summary>
        [HttpPost("{transactionId}/refund")]
        public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> SimulateRefund(
            Guid transactionId,
            [FromBody] RefundRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    var error = new Error("E011", "Refund reason is required");
                    return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse("Invalid request", new List<Error> { error }));
                }

                var originalTransaction = await _paymentTransactionService.GetByIdAsync(transactionId);
                if (originalTransaction == null)
                {
                    var error = new Error("E012", "Payment transaction not found");
                    return NotFound(ApiResponse<PaymentTransactionDto>.FailureResponse("Payment transaction not found", new List<Error> { error }));
                }

                var transaction = await _paymentTransactionService.RefundPayment(originalTransaction.ShipmentId, request.Reason);

                _logger.LogInformation("Refund processed for transaction {TransactionId}", transactionId);

                return Ok(ApiResponse<PaymentTransactionDto>.SuccessResponse(transaction));
            }
            catch (ArgumentException ex)
            {
                var error = new Error("E012", ex.Message);
                return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse(ex.Message, new List<Error> { error }));
            }
            catch (InvalidOperationException ex)
            {
                var error = new Error("E013", ex.Message);
                return BadRequest(ApiResponse<PaymentTransactionDto>.FailureResponse(ex.Message, new List<Error> { error }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
                var error = new Error("E014", "An error occurred");
                return StatusCode(500, ApiResponse<PaymentTransactionDto>.FailureResponse("An error occurred", new List<Error> { error }));
            }
        }
    }

    /// <summary>
    /// Request model for processing payment
    /// </summary>
    public class ProcessPaymentRequest
    {
        public Guid ShipmentId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public decimal ShippingRate { get; set; }
        public string? PaymentMethodToken { get; set; }
    }

    /// <summary>
    /// Request model for refund
    /// </summary>
    public class RefundRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
