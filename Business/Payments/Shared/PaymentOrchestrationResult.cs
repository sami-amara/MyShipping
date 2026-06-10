using System;

namespace Business.Payments.Shared
{
    /// <summary>
    /// Result model for payment orchestration operations (PayPal and Stripe).
    /// Contains common and provider-specific properties to handle both payment flows.
    /// </summary>
    public class PaymentOrchestrationResult
    {
        /// <summary>
        /// Indicates whether the orchestration operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code for the operation
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Additional details about the operation result
        /// </summary>
        public string? Details { get; set; }

        // PayPal-specific properties

        /// <summary>
        /// PayPal Order ID returned from order creation
        /// </summary>
        public string? OrderId { get; set; }

        // Stripe-specific properties

        /// <summary>
        /// Stripe client secret for confirming payment on the frontend
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Stripe PaymentIntent ID for tracking the payment
        /// </summary>
        public string? PaymentIntentId { get; set; }

        // Common properties

        /// <summary>
        /// Current status of the payment (provider-specific)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Database transaction ID (TbPaymentTransaction primary key)
        /// </summary>
        public Guid? TransactionId { get; set; }

        /// <summary>
        /// Provider transaction reference number
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// User-friendly message about the operation result
        /// </summary>
        public string? Message { get; set; }
    }
}
