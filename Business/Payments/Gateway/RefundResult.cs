using System;
using Business.Payments.Shared;

namespace Business.Payments.Gateway
{
    /// <summary>
    /// Refund result returned by payment gateway after processing refund
    /// </summary>
    public class RefundResult
    {
        /// <summary>
        /// Whether the refund was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Refund ID from the gateway
        /// </summary>
        public string RefundId { get; set; } = string.Empty;

        /// <summary>
        /// Original transaction ID that was refunded
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Error message if refund failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code from the gateway (if available)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Refund status
        /// </summary>
        public RefundStatus Status { get; set; }

        /// <summary>
        /// When the refund was processed
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Amount that was refunded
        /// </summary>
        public decimal AmountRefunded { get; set; }

        /// <summary>
        /// Currency of the refund
        /// </summary>
        public string Currency { get; set; } = "USD";
    }
}
