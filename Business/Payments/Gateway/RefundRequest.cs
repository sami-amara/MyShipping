using System;
using Business.Payments.Shared;

namespace Business.Payments.Gateway
{
    /// <summary>
    /// Refund request model for processing refunds through any gateway
    /// </summary>
    public class RefundRequest
    {
        /// <summary>
        /// Original transaction/payment ID to refund
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Amount to refund (if null, refunds the full amount)
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Currency of the refund
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Reason for the refund
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a full or partial refund
        /// </summary>
        public RefundType Type { get; set; } = RefundType.Full;
    }
}
