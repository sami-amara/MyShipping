using Business.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class PaymentWebhooksController : ControllerBase
    {
        private readonly IPaymentGatewayFactory _paymentGatewayFactory;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly ILogger<PaymentWebhooksController> _logger;

        public PaymentWebhooksController(
            IPaymentGatewayFactory paymentGatewayFactory,
            IPaymentTransactionService paymentTransactionService,
            ILogger<PaymentWebhooksController> logger)
        {
            _paymentGatewayFactory = paymentGatewayFactory;
            _paymentTransactionService = paymentTransactionService;
            _logger = logger;
        }

        [HttpPost("stripe")]
        public async Task<ActionResult<ApiResponse<object>>> Stripe()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

            try
            {
                var stripeGateway = _paymentGatewayFactory.GetGateway("Stripe");
                var isValid = await stripeGateway.ValidateWebhook(payload, signature);

                if (!isValid)
                {
                    var error = new Error("E_WEBHOOK_STRIPE", "Invalid Stripe webhook signature");
                    return BadRequest(ApiResponse<object>.FailureResponse("Invalid Stripe webhook", new List<Error> { error }));
                }

                var eventId = ExtractJsonString(payload, "id");
                var eventType = ExtractJsonString(payload, "type");
                var transactionReference = ExtractStripeTransactionReference(payload);

                if (string.IsNullOrWhiteSpace(eventId))
                {
                    var error = new Error("E_WEBHOOK_STRIPE", "Stripe webhook event id is missing");
                    return BadRequest(ApiResponse<object>.FailureResponse("Invalid Stripe webhook payload", new List<Error> { error }));
                }

                if (await _paymentTransactionService.IsWebhookEventProcessed("Stripe", eventId))
                {
                    _logger.LogInformation("Stripe webhook already processed. EventId: {EventId}", eventId);
                    return Ok(ApiResponse<object>.SuccessResponse(null, "Stripe webhook already processed"));
                }

                await _paymentTransactionService.RecordWebhookEvent("Stripe", eventId, eventType, transactionReference, payload);

                var reconciled = await _paymentTransactionService.ReconcileTransactionFromWebhook(
                    "Stripe",
                    eventId,
                    eventType,
                    transactionReference,
                    payload);

                await _paymentTransactionService.MarkWebhookEventProcessed(
                    "Stripe",
                    eventId,
                    reconciled,
                    reconciled ? "Reconciled successfully" : "No matching transaction found for reconciliation");

                _logger.LogInformation("Stripe webhook processed. EventId: {EventId}, Reconciled: {Reconciled}", eventId, reconciled);
                return Ok(ApiResponse<object>.SuccessResponse(null, "Stripe webhook processed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                var error = new Error("E_WEBHOOK_STRIPE", ex.Message);
                return StatusCode(500, ApiResponse<object>.FailureResponse("Failed to process Stripe webhook", new List<Error> { error }));
            }
        }

        [HttpPost("paypal")]
        public async Task<ActionResult<ApiResponse<object>>> PayPal()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Paypal-Transmission-Sig"].FirstOrDefault() ?? string.Empty;

            try
            {
                var payPalGateway = _paymentGatewayFactory.GetGateway("PayPal");
                var isValid = await payPalGateway.ValidateWebhook(payload, signature);

                if (!isValid)
                {
                    var error = new Error("E_WEBHOOK_PAYPAL", "Invalid PayPal webhook signature");
                    return BadRequest(ApiResponse<object>.FailureResponse("Invalid PayPal webhook", new List<Error> { error }));
                }

                var eventId = ExtractJsonString(payload, "id");
                var eventType = ExtractJsonString(payload, "event_type");
                var transactionReference = ExtractPayPalTransactionReference(payload);

                if (string.IsNullOrWhiteSpace(eventId))
                {
                    var error = new Error("E_WEBHOOK_PAYPAL", "PayPal webhook event id is missing");
                    return BadRequest(ApiResponse<object>.FailureResponse("Invalid PayPal webhook payload", new List<Error> { error }));
                }

                if (await _paymentTransactionService.IsWebhookEventProcessed("PayPal", eventId))
                {
                    _logger.LogInformation("PayPal webhook already processed. EventId: {EventId}", eventId);
                    return Ok(ApiResponse<object>.SuccessResponse(null, "PayPal webhook already processed"));
                }

                await _paymentTransactionService.RecordWebhookEvent("PayPal", eventId, eventType, transactionReference, payload);

                var reconciled = await _paymentTransactionService.ReconcileTransactionFromWebhook(
                    "PayPal",
                    eventId,
                    eventType,
                    transactionReference,
                    payload);

                await _paymentTransactionService.MarkWebhookEventProcessed(
                    "PayPal",
                    eventId,
                    reconciled,
                    reconciled ? "Reconciled successfully" : "No matching transaction found for reconciliation");

                _logger.LogInformation("PayPal webhook processed. EventId: {EventId}, Reconciled: {Reconciled}", eventId, reconciled);
                return Ok(ApiResponse<object>.SuccessResponse(null, "PayPal webhook processed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal webhook");
                var error = new Error("E_WEBHOOK_PAYPAL", ex.Message);
                return StatusCode(500, ApiResponse<object>.FailureResponse("Failed to process PayPal webhook", new List<Error> { error }));
            }
        }

        private static string? ExtractJsonString(string payload, string propertyName)
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private static string? ExtractStripeTransactionReference(string payload)
        {
            using var doc = JsonDocument.Parse(payload);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("object", out var objectElement))
            {
                return null;
            }

            if (objectElement.TryGetProperty("id", out var objectId) && objectId.ValueKind == JsonValueKind.String)
            {
                return objectId.GetString();
            }

            if (objectElement.TryGetProperty("payment_intent", out var paymentIntent) && paymentIntent.ValueKind == JsonValueKind.String)
            {
                return paymentIntent.GetString();
            }

            return null;
        }

        private static string? ExtractPayPalTransactionReference(string payload)
        {
            using var doc = JsonDocument.Parse(payload);

            if (!doc.RootElement.TryGetProperty("resource", out var resourceElement))
                return null;

            if (resourceElement.TryGetProperty("id", out var resourceId) && resourceId.ValueKind == JsonValueKind.String)
            {
                return resourceId.GetString();
            }

            if (resourceElement.TryGetProperty("supplementary_data", out var supplementaryData) &&
                supplementaryData.TryGetProperty("related_ids", out var relatedIds) &&
                relatedIds.TryGetProperty("order_id", out var orderId) &&
                orderId.ValueKind == JsonValueKind.String)
            {
                return orderId.GetString();
            }

            return null;
        }
    }
}
