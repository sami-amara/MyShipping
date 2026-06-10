using Business.Configuration;
using Business.Contracts;
using Business.Contracts.Shipment;
using Business.Payments.PayPal;
using Business.Payments.Stripe;
using Business.Payments.Shared;
using Business.Services.Shipment.ManageShipmentsState;
using DataAccessLayer.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using WebApi.Hubs;
using WebApi.Models;

namespace WebApi.Controllers
{
    /// <summary>
    /// Payment controller for handling PayPal JavaScript SDK integration.
    /// Provides endpoints for creating PayPal orders and capturing approved payments.
    /// </summary>
    /// <remarks>
    /// This controller implements the correct PayPal flow using the JavaScript SDK:
    /// 1. Frontend calls CreateOrder to initialize a PayPal order
    /// 2. PayPal JS SDK displays payment UI and handles user approval
    /// 3. Frontend calls CaptureOrder with the approved order ID
    /// 4. Payment is captured and persisted to database
    /// 
    /// This approach keeps shipment creation separate from payment processing.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IShipmentQuery _shipmentQuery;
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly PaymentGatewayOptions _paymentOptions;
        private readonly ILogger<PaymentController> _logger;
        private readonly IHubContext<YourHub> _hubContext;
        private readonly IShipmentCommand _shipmentCommand;

        public PaymentController(
            IShipmentQuery shipmentQuery,
            IPaymentGatewayFactory gatewayFactory,
            IPaymentTransactionService paymentTransactionService,
            IOptions<PaymentGatewayOptions> paymentOptions,
            ILogger<PaymentController> logger,
            IHubContext<YourHub> hubContext,
            IShipmentCommand shipmentCommand)
        {
            _shipmentQuery = shipmentQuery;
            _gatewayFactory = gatewayFactory;
            _paymentTransactionService = paymentTransactionService;
            _paymentOptions = paymentOptions.Value;
            _logger = logger;
            _hubContext = hubContext;
            _shipmentCommand = shipmentCommand;
        }

        /// <summary>
        /// Creates a PayPal order for a shipment payment.
        /// Called by the frontend PayPal JavaScript SDK to initialize payment.
        /// </summary>
        /// <param name="request">Order creation request containing shipment and payment details</param>
        /// <returns>PayPal order ID for frontend SDK to process</returns>
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] PaymentOrderRequest request)
        {
            try
            {
                var result = await _paymentTransactionService.CreateOrderAsync(request);

                if (!result.Success)
                {
                    if (result.StatusCode == 404)
                        return NotFound(new { error = result.Error });

                    if (result.StatusCode == 400)
                        return BadRequest(new { error = result.Error });

                    return StatusCode(result.StatusCode, new { error = result.Error, details = result.Details });
                }

                return Ok(new
                {
                    orderId = result.OrderId,
                    status = result.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for request {@Request}", request);
                return StatusCode(500, new
                {
                    error = "An error occurred while creating the payment order",
                    details = ex.Message
                });
            }
        }



        [HttpPost("CaptureOrder")]
        public async Task<IActionResult> CaptureOrder([FromBody] PaymentCaptureRequest request)
        {
            try
            {
                var result = await _paymentTransactionService.CaptureOrderAsync(request);

                if (!result.Success)
                {
                    if (result.StatusCode == 404)
                        return NotFound(new { error = result.Error });

                    if (result.StatusCode == 400)
                        return BadRequest(new { error = result.Error, status = result.Status });

                    return StatusCode(result.StatusCode, new { error = result.Error, details = result.Details });
                }

                return Ok(new
                {
                    success = true,
                    transactionId = result.TransactionId,
                    transactionReference = result.TransactionReference,
                    status = result.Status,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order for request {@Request}", request);
                return StatusCode(500, new
                {
                    error = "An error occurred while capturing the payment",
                    details = ex.Message
                });
            }
        }

        [HttpPost("Refund")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refund([FromBody] ShipmentRefundRequest request)
        {
            try
            {
                if (request == null || request.ShipmentId == Guid.Empty)
                    return BadRequest(new { error = "Shipment ID is required" });

                if (string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(new { error = "Refund reason is required" });

                var refundedTransaction = await _paymentTransactionService.RefundPayment(request.ShipmentId, request.Reason);

                await _hubContext.Clients.All.SendAsync("ShipmentStatusUpdated", new
                {
                    shipmentId = request.ShipmentId,
                    newState   = (int)Business.Services.Shipment.ManageShipmentsState.ShipmentStatusEnum.Refunded,
                    statusName = "Refunded",
                    isPaid     = false
                });

                return Ok(new
                {
                    success = true,
                    shipmentId = request.ShipmentId,
                    transactionId = refundedTransaction.Id,
                    status = refundedTransaction.TransactionStatusName,
                    message = "Payment refunded successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding shipment payment for request {@Request}", request);
                return StatusCode(500, new
                {
                    error = "An error occurred while refunding the payment",
                    details = ex.Message
                });
            }
        }

        [HttpGet("GetPayPalConfig")]
        [AllowAnonymous]
        public IActionResult GetPayPalConfig()
        {
            try
            {
                var paypalConfig = _paymentOptions.PayPal;

                if (paypalConfig == null || string.IsNullOrEmpty(paypalConfig.ClientId))
                {
                    return BadRequest(new { error = "PayPal is not configured" });
                }

                return Ok(new
                {
                    clientId = paypalConfig.ClientId,
                    environment = paypalConfig.Environment ?? "sandbox",
                    currency = "USD"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while retrieving PayPal configuration",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Get Stripe publishable key for frontend integration
        /// </summary>
        [HttpGet("GetStripePublishableKey")]
        [AllowAnonymous]
        public IActionResult GetStripePublishableKey()
        {
            try
            {
                var stripeConfig = _paymentOptions.Stripe;

                if (stripeConfig == null || string.IsNullOrEmpty(stripeConfig.PublishableKey))
                {
                    return BadRequest(new { error = "Stripe is not configured" });
                }

                return Ok(new
                {
                    publishableKey = stripeConfig.PublishableKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe publishable key");
                return StatusCode(500, new
                {
                    error = "An error occurred while retrieving Stripe configuration",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Creates a Stripe PaymentIntent for a shipment payment
        /// </summary>
        [HttpPost("CreateStripeIntent")]
        public async Task<IActionResult> CreateStripeIntent([FromBody] StripePaymentIntentRequest request)
        {
            try
            {
           

                var result = await _paymentTransactionService.CreateStripeIntentAsync(request);

                if (!result.Success)
                {
                    if (result.StatusCode == 404)
                        return NotFound(new { error = result.Error });

                    if (result.StatusCode == 400)
                        return BadRequest(new { error = result.Error, details = result.Details });

                    return StatusCode(result.StatusCode, new { error = result.Error, details = result.Details });
                }

                return Ok(new
                {
                    clientSecret = result.ClientSecret,
                    paymentIntentId = result.PaymentIntentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe PaymentIntent for shipment {ShipmentId}", request.ShipmentId);
                return StatusCode(500, new
                {
                    error = "An error occurred while creating the payment intent",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Captures a Stripe payment and marks shipment as paid
        /// </summary>
        [HttpPost("CaptureStripe")]
        public async Task<IActionResult> CaptureStripe([FromBody] StripeCaptureRequest request)
        {
            try
            {
                var result = await _paymentTransactionService.CaptureStripeAsync(request);

                if (!result.Success)
                {
                    if (result.StatusCode == 404)
                        return NotFound(new { error = result.Error });

                    if (result.StatusCode == 400)
                        return BadRequest(new { error = result.Error, details = result.Details });

                    return StatusCode(result.StatusCode, new { error = result.Error, details = result.Details });
                }

                // Notify via SignalR
                await _hubContext.Clients.All.SendAsync("ShipmentStatusUpdated", new
                {
                    shipmentId = request.ShipmentId,
                    isPaid = true
                });

                return Ok(new
                {
                    success = true,
                    transactionId = result.TransactionId,
                    transactionReference = result.TransactionReference,
                    status = result.Status,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing Stripe payment for shipment {ShipmentId}", request.ShipmentId);
                return StatusCode(500, new
                {
                    error = "An error occurred while capturing the payment",
                    details = ex.Message
                });
            }
        }
    }

   
}
