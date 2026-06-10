namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Response from PayPal refund endpoint.
    /// Contains refund transaction details.
    /// </summary>
    internal class PayPalRefundResponse
    {
        /// <summary>Unique refund transaction ID</summary>
        public string? Id { get; set; }

        /// <summary>Refund status (COMPLETED, PENDING, FAILED, etc.)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Refunded amount details</summary>
        public PayPalAmount? Amount { get; set; }
    }
}
