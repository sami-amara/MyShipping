using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Business.Payments.Gateway;

namespace Business.Contracts
{
    /// <summary>
    /// Defines the contract for payment gateway providers (Stripe, PayPal, etc.).
    /// Provides abstraction for processing payments, refunds, and webhook validation.
    /// </summary>
    /// <remarks>
    /// This interface enables the factory pattern for payment gateway selection,
    /// allowing the system to support multiple payment providers transparently.
    /// Each implementation handles provider-specific API integration, authentication,
    /// error handling, and webhook signature verification.
    /// 
    /// Implementations should:
    /// - Handle provider API authentication (OAuth, API keys, etc.)
    /// - Map provider-specific statuses to our internal enumerations
    /// - Implement idempotency for payment operations
    /// - Validate webhook signatures cryptographically
    /// - Provide detailed error messages for debugging
    /// </remarks>
    public interface IPaymentGateway
    {
        /// <summary>
        /// Processes a payment request through the payment gateway provider.
        /// </summary>
        /// <param name="request">Payment request containing amount, currency, payment token, and metadata</param>
        /// <returns>
        /// PaymentResult containing:
        /// - Success flag indicating if payment was processed
        /// - TransactionId from the payment provider
        /// - PaymentStatus (Pending, Completed, Failed, RequiresAction, Canceled)
        /// - Error details if the payment failed
        /// - Processed timestamp for audit trails
        /// </returns>
        /// <remarks>
        /// This method should:
        /// 1. Authenticate with the payment provider
        /// 2. Create/process the payment using the provider's API
        /// 3. Handle immediate capture or authorization based on request settings
        /// 4. Return a standardized result regardless of provider
        /// 5. Never throw exceptions for payment failures (return error in PaymentResult)
        /// 
        /// The caller is responsible for persisting the transaction to the database.
        /// </remarks>
        Task<PaymentResult> ProcessPayment(PaymentRequest request);

        /// <summary>
        /// Processes a full or partial refund for a previously completed payment.
        /// </summary>
        /// <param name="request">Refund request containing original transaction ID, amount, and reason</param>
        /// <returns>
        /// RefundResult containing:
        /// - Success flag indicating if refund was processed
        /// - RefundId from the payment provider
        /// - RefundStatus (Pending, Completed, Failed, Canceled)
        /// - Error details if the refund failed
        /// - Processed timestamp for audit trails
        /// </returns>
        /// <remarks>
        /// Refund behavior:
        /// - If Amount is null or matches original amount, processes a full refund
        /// - If Amount is less than original, processes a partial refund
        /// - Refund availability depends on provider policies (time limits, fees, etc.)
        /// - Some providers process refunds asynchronously; status may be Pending initially
        /// 
        /// The caller should verify the original transaction exists and is refundable
        /// before calling this method.
        /// </remarks>
        Task<RefundResult> ProcessRefund(RefundRequest request);

        /// <summary>
        /// Validates that a webhook request originated from the payment provider
        /// by verifying its cryptographic signature.
        /// </summary>
        /// <param name="payload">Raw webhook payload body as received from the provider (JSON string)</param>
        /// <param name="signature">Signature header value from the webhook HTTP request</param>
        /// <returns>True if the webhook signature is valid and request is authentic; false otherwise</returns>
        /// <remarks>
        /// Security considerations:
        /// - Always validate webhooks before processing to prevent webhook forgery attacks
        /// - Signature validation methods vary by provider:
        ///   * Stripe: HMAC-SHA256 using webhook secret
        ///   * PayPal: RSA signature verification via PayPal API
        /// - Invalid signatures must return false, not throw exceptions
        /// - Log failed validation attempts for security monitoring
        /// 
        /// This method must be called before processing any webhook event data.
        /// Webhook events should be deduplicated using provider event IDs.
        /// </remarks>
        Task<bool> ValidateWebhook(string payload, string signature);

        /// <summary>
        /// Gets the unique identifier name for this payment gateway provider.
        /// </summary>
        /// <returns>
        /// Provider name string (e.g., "Stripe", "PayPal", "Square").
        /// Used for logging, transaction records, and provider selection.
        /// </returns>
        /// <remarks>
        /// This value is stored in TbPaymentTransaction.ProviderName for audit purposes
        /// and helps identify which gateway processed each transaction.
        /// </remarks>
        string GetProviderName();
    }
}
