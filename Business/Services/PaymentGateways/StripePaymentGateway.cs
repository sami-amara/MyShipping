using Business.Contracts;
using Business.Payments.Gateway;
using Business.Payments.Shared;
using Business.Configuration;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Payments.Stripe;

namespace Business.Services.PaymentGateways
{
    /// <summary>
    /// Stripe Payment Gateway Implementation
    /// Handles payment processing through Stripe API
    /// </summary>
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly StripeClient _client;
        private readonly StripeOptions _options;

        public StripePaymentGateway(IOptions<PaymentGatewayOptions> options)
        {
            _options = options.Value.Stripe;

            if (string.IsNullOrEmpty(_options.SecretKey))
                throw new InvalidOperationException("Stripe SecretKey is not configured");
            // Initialize Stripe client with secret key
            _client = new StripeClient(_options.SecretKey);
        }

        public string GetProviderName() => "Stripe";

        public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
        {
            try
            {
                // Step 1: Create a Payment Intent
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = request.Currency.ToLower(),
                    Description = request.Description,
                    Metadata = request.Metadata ?? new Dictionary<string, string>(),
                    CaptureMethod = request.CaptureImmediately ? "automatic" : "manual"
                };

                // Add customer information if provided
                if (!string.IsNullOrEmpty(request.Customer.Email))
                {
                    paymentIntentOptions.ReceiptEmail = request.Customer.Email;
                }

                // If payment method token is provided, attach it and confirm
                if (!string.IsNullOrEmpty(request.PaymentMethodToken))
                {
                    paymentIntentOptions.PaymentMethod = request.PaymentMethodToken;
                    paymentIntentOptions.Confirm = true;
                    // Don't use automatic payment methods when a specific payment method is provided
                    paymentIntentOptions.PaymentMethodTypes = new List<string> { "card" };
                }
                else
                {
                    // Enable automatic payment methods only when no specific payment method is provided
                    // Disable redirects to avoid needing a return_url for testing
                    paymentIntentOptions.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"  // Prevent redirect-based payment methods
                    };
                }

                var service = new PaymentIntentService(_client);
                var paymentIntent = await service.CreateAsync(paymentIntentOptions);
                // Map Stripe status to our PaymentStatus
                var status = MapStripeStatus(paymentIntent.Status);
                return new PaymentResult
                {
                    Success = paymentIntent.Status == "succeeded",
                    TransactionId = paymentIntent.Id,
                    Status = status,
                    ProcessedAt = DateTime.UtcNow,
                    AmountCharged = request.Amount,
                    Currency = request.Currency,
                    ErrorMessage = paymentIntent.Status == "requires_payment_method" 
                        ? "Payment method required" 
                        : null,
                    AdditionalInfo = $"Stripe Payment Intent: {paymentIntent.Id}, Status: {paymentIntent.Status}"
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = string.Empty,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.StripeError?.Code,
                    ProcessedAt = DateTime.UtcNow,
                    AmountCharged = 0,
                    Currency = request.Currency
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = string.Empty,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = $"Payment processing failed: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow,
                    AmountCharged = 0,
                    Currency = request.Currency
                };
            }
        }

        public async Task<RefundResult> ProcessRefund(RefundRequest request)
        {
            try
            {
                // Stripe refunds work with EITHER:
                // 1. Charge ID (ch_xxx) - preferred, always works
                // 2. PaymentIntent ID (pi_xxx) - works if there's only one charge
                //
                // Since we store PaymentIntent IDs, we need to:
                // - Get the PaymentIntent
                // - Extract the Charge ID from it
                // - Refund the Charge

                var service = new RefundService(_client);
                RefundCreateOptions refundOptions;

                // Check if this is a Charge ID or PaymentIntent ID
                if (request.TransactionId.StartsWith("ch_"))
                {
                    // Direct charge refund
                    refundOptions = new RefundCreateOptions
                    {
                        Charge = request.TransactionId,
                        Reason = MapRefundReason(request.Reason)
                    };
                }
                else if (request.TransactionId.StartsWith("pi_"))
                {
                    // PaymentIntent refund - Stripe will handle finding the charge
                    refundOptions = new RefundCreateOptions
                    {
                        PaymentIntent = request.TransactionId,
                        Reason = MapRefundReason(request.Reason)
                    };
                }
                else
                {
                    // Unknown ID format
                    return new RefundResult
                    {
                        Success = false,
                        RefundId = string.Empty,
                        TransactionId = request.TransactionId,
                        Status = RefundStatus.Failed,
                        ErrorMessage = $"Invalid Stripe transaction ID format: {request.TransactionId}. Expected 'pi_xxx' (PaymentIntent) or 'ch_xxx' (Charge).",
                        ProcessedAt = DateTime.UtcNow,
                        AmountRefunded = 0
                    };
                }

                // If partial refund, specify amount
                if (request.Type == RefundType.Partial && request.Amount.HasValue)
                {
                    refundOptions.Amount = (long)(request.Amount.Value * 100); // Convert to cents
                }

                var refund = await service.CreateAsync(refundOptions);
                return new RefundResult
                {
                    Success = refund.Status == "succeeded",
                    RefundId = refund.Id,
                    TransactionId = request.TransactionId,
                    Status = MapStripeRefundStatus(refund.Status),
                    ProcessedAt = DateTime.UtcNow,
                    AmountRefunded = (decimal)refund.Amount / 100, // Convert from cents
                    Currency = refund.Currency.ToUpper()
                };
            }
            catch (StripeException ex)
            {
                return new RefundResult
                {
                    Success = false,
                    RefundId = string.Empty,
                    TransactionId = request.TransactionId,
                    Status = RefundStatus.Failed,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.StripeError?.Code,
                    ProcessedAt = DateTime.UtcNow,
                    AmountRefunded = 0
                };
            }
            catch (Exception ex)
            {
                return new RefundResult
                {
                    Success = false,
                    RefundId = string.Empty,
                    TransactionId = request.TransactionId,
                    Status = RefundStatus.Failed,
                    ErrorMessage = $"Refund processing failed: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow,
                    AmountRefunded = 0
                };
            }
        }

        public Task<bool> ValidateWebhook(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.WebhookSecret))
                {
                    throw new InvalidOperationException("Stripe WebhookSecret is not configured");
                }

                // Stripe validates webhook signature
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    _options.WebhookSecret,
                    throwOnApiVersionMismatch: false
                );
                return Task.FromResult(stripeEvent != null);
            }
            catch (StripeException)
            {
                return Task.FromResult(false);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Map Stripe payment intent status to our PaymentStatus enum
        /// </summary>
        private PaymentStatus MapStripeStatus(string stripeStatus)
        {
            return stripeStatus switch
            {
                "succeeded" => PaymentStatus.Completed,
                "processing" => PaymentStatus.Pending,
                "requires_payment_method" => PaymentStatus.Failed,
                "requires_confirmation" => PaymentStatus.Pending,
                "requires_action" => PaymentStatus.RequiresAction,
                "requires_capture" => PaymentStatus.Pending,
                "canceled" => PaymentStatus.Canceled,
                _ => PaymentStatus.Failed
            };
        }

        /// <summary>
        /// Map Stripe refund status to our RefundStatus enum
        /// </summary>
        private RefundStatus MapStripeRefundStatus(string stripeStatus)
        {
            return stripeStatus switch
            {
                "succeeded" => RefundStatus.Completed,
                "pending" => RefundStatus.Pending,
                "failed" => RefundStatus.Failed,
                "canceled" => RefundStatus.Canceled,
                _ => RefundStatus.Failed
            };
        }

        /// <summary>
        /// Map refund reason to Stripe refund reason enum
        /// </summary>
        private string MapRefundReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return "requested_by_customer";

            reason = reason.ToLower();
            if (reason.Contains("duplicate"))
                return "duplicate";
            if (reason.Contains("fraud"))
                return "fraudulent";

            return "requested_by_customer";
        }

        #endregion

        #region Stripe-Specific Methods for Frontend Integration

        /// <summary>
        /// Creates a Stripe PaymentIntent for frontend card payment collection
        /// Returns client secret for Stripe.js integration
        /// </summary>
        public async Task<StripeIntentResult> CreatePaymentIntentAsync(Guid shipmentId, decimal amount)
        {
            try
            {
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = "usd",
                    Description = $"Shipment payment for {shipmentId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "shipmentId", shipmentId.ToString() }
                    },
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new PaymentIntentService(_client);
                var paymentIntent = await service.CreateAsync(paymentIntentOptions);
                return new StripeIntentResult
                {
                    Success = true,
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id
                };
            }
            catch (StripeException ex)
            {
                return new StripeIntentResult
                {
                    Success = false,
                    Error = ex.StripeError?.Message ?? "Stripe error occurred",
                    Details = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new StripeIntentResult
                {
                    Success = false,
                    Error = "An error occurred while creating payment intent",
                    Details = ex.Message
                };
            }
        }

        /// <summary>
        /// Captures/confirms a Stripe payment after frontend confirmation
        /// </summary>
        public async Task<PaymentResult> CapturePaymentAsync(string paymentIntentId, Guid shipmentId, Guid paymentMethodId)
        {
            try
            {
                var service = new PaymentIntentService(_client);
                var paymentIntent = await service.GetAsync(paymentIntentId);
                // Check if payment succeeded
                if (paymentIntent.Status != "succeeded")
                {
                    return new PaymentResult
                    {
                        Success = false,
                        TransactionId = paymentIntentId,
                        Status = MapStripeStatus(paymentIntent.Status),
                        ErrorMessage = $"Payment not completed. Status: {paymentIntent.Status}",
                        StatusCode = 400
                    };
                }

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = paymentIntent.Id,
                    TransactionReference = paymentIntent.Id,
                    Status = PaymentStatus.Completed,
                    ProcessedAt = DateTime.UtcNow,
                    AmountCharged = paymentIntent.Amount / 100m, // Convert from cents
                    Currency = paymentIntent.Currency.ToUpper(),
                    Message = "Payment captured successfully",
                    AdditionalInfo = $"Stripe PaymentIntent: {paymentIntent.Id}"
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = paymentIntentId,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = ex.StripeError?.Message ?? "Stripe error occurred",
                    AdditionalInfo = ex.Message,
                    StatusCode = 500
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = paymentIntentId,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = "An error occurred while capturing payment",
                    AdditionalInfo = ex.Message,
                    StatusCode = 500
                };
            }
        }

        #endregion
    }

   
}
