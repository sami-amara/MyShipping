using System;
using Business.Payments.Shared;

namespace Business.Payments.Gateway
{
    /// <summary>
    /// Payment result returned by payment gateways after processing a payment request.
    /// Provides standardized response regardless of underlying provider (Stripe, PayPal, etc.).
    /// </summary>
    /// <remarks>
    /// This model is returned by IPaymentGateway.ProcessPayment implementations.
    /// PaymentTransactionService uses this result to:
    /// - Determine if shipment creation should proceed or rollback
    /// - Populate TbPaymentTransaction database record
    /// - Return success/error messages to the frontend
    /// 
    /// Gateway implementations map provider-specific responses to this standard format.
    /// 
    /// Success criteria:
    /// - Success = true AND Status = Completed → Payment successful, shipment proceeds
    /// - Success = false OR Status = Failed → Payment failed, shipment rolled back
    /// - Status = RequiresAction → Payment needs user action (3D Secure, approval)
    /// - Status = Pending → Payment initiated but not yet confirmed
    /// </remarks>
    public class PaymentResult
    {
        /// <summary>
        /// Indicates whether the payment was successfully processed.
        /// True if payment completed or is pending approval; false if rejected or errored.
        /// </summary>
        /// <remarks>
        /// Success determination:
        /// - true: Payment completed, pending, or requires action (shipment can proceed or wait)
        /// - false: Payment declined, failed, or encountered an error (shipment fails)
        /// 
        /// Always check both Success AND Status for complete understanding.
        /// Success alone doesn't guarantee funds are captured (could be Pending).
        /// </remarks>
        public bool Success { get; set; }

        /// <summary>
        /// Unique transaction identifier from the payment provider.
        /// Used for refunds, customer support, and webhook reconciliation.
        /// </summary>
        /// <remarks>
        /// Format varies by provider:
        /// - Stripe charge: ch_1ABC2DEF3GHI4JKL
        /// - Stripe payment intent: pi_1ABC2DEF3GHI4JKL
        /// - PayPal order: 5O190127TN364715T
        /// 
        /// Stored in TbPaymentTransaction.TransactionReference.
        /// Required for processing refunds via IPaymentGateway.ProcessRefund.
        /// Empty string if payment failed before transaction was created.
        /// </remarks>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable error message if payment failed.
        /// Null for successful payments. May contain sensitive details for logging only.
        /// </summary>
        /// <remarks>
        /// Error message sources:
        /// - Card declined by issuing bank
        /// - Insufficient funds
        /// - Invalid card details (expired, wrong CVV)
        /// - Provider API errors (network, authentication)
        /// 
        /// Usage:
        /// - Log for debugging (may contain sensitive info)
        /// - Store in TbPaymentTransaction.ErrorMessage
        /// - User-facing messages should be sanitized (don't expose full error)
        /// 
        /// Example messages:
        /// - "Your card was declined."
        /// - "Insufficient funds."
        /// - "PayPal API error: INSTRUMENT_DECLINED"
        /// </remarks>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Provider-specific error code for categorizing failures.
        /// Null for successful payments. Useful for analytics and specific error handling.
        /// </summary>
        /// <remarks>
        /// Error code examples by provider:
        /// - Stripe: "card_declined", "insufficient_funds", "expired_card"
        /// - PayPal: "INSTRUMENT_DECLINED", "PAYER_ACTION_REQUIRED"
        /// 
        /// Used for:
        /// - Analytics on failure reasons
        /// - Conditional retry logic (don't retry expired_card)
        /// - Specific user messaging (show different message for insufficient_funds)
        /// </remarks>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Current status of the payment after processing.
        /// Maps provider-specific statuses to standardized PaymentStatus enum.
        /// </summary>
        /// <remarks>
        /// Status meanings:
        /// - Pending: Payment initiated but awaiting confirmation
        /// - Completed: Funds captured successfully
        /// - Failed: Payment rejected or errored
        /// - RequiresAction: User must complete 3D Secure or approve in PayPal
        /// - Canceled: Payment voided before capture
        /// - Refunded: Previously completed payment was refunded
        /// 
        /// Stored in TbPaymentTransaction.TransactionStatus (as integer).
        /// May be updated later via webhook reconciliation.
        /// </remarks>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// UTC timestamp when the payment was processed by the gateway.
        /// Defaults to current UTC time. Used for audit trails and transaction history.
        /// </summary>
        /// <remarks>
        /// Stored in TbPaymentTransaction.ProcessedDate.
        /// Always in UTC for consistency across time zones.
        /// Used for:
        /// - Transaction history sorting
        /// - Audit compliance
        /// - Correlating with provider logs
        /// </remarks>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Actual amount that was charged to the customer.
        /// Should match PaymentRequest.Amount for successful payments.
        /// </summary>
        /// <remarks>
        /// Stored in TbPaymentTransaction.TotalAmount.
        /// Used for:
        /// - Verification that correct amount was charged
        /// - Displaying in transaction history
        /// - Financial reconciliation reports
        /// 
        /// In base currency units (dollars, not cents).
        /// </remarks>
        public decimal AmountCharged { get; set; }

        /// <summary>
        /// ISO 4217 currency code of the charged amount (e.g., "USD", "EUR").
        /// Should match PaymentRequest.Currency for successful payments.
        /// </summary>
        /// <remarks>
        /// Stored in TbPaymentTransaction for multi-currency support.
        /// Used to display correct currency symbol in UI ($ vs € vs £).
        /// </remarks>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Additional information from the payment gateway for debugging or display.
        /// May include provider order details, approval URLs, or processing notes.
        /// </summary>
        /// <remarks>
        /// Example content:
        /// - "PayPal Order: 5O190127TN364715T, Status: COMPLETED"
        /// - "Stripe PaymentIntent: pi_1ABC2DEF, Approval URL: https://..."
        /// - "3D Secure authentication required"
        /// 
        /// Used for:
        /// - Debugging payment issues
        /// - Providing user with next steps (approval links)
        /// - Storing provider-specific metadata
        /// </remarks>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Success or status message from the gateway.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Transaction reference number from the payment provider.
        /// May be the same as TransactionId or a different reference.
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// HTTP status code for API response (used for error handling).
        /// Default 200 for success, 400/404/500 for errors.
        /// </summary>
        public int StatusCode { get; set; } = 200;
    }
}
