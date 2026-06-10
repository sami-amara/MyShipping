using System;

namespace Business.Payments.PayPal
{
    /// <summary>
    /// Request model for capturing an approved PayPal order via the JavaScript SDK flow.
    /// </summary>
    public class PaymentCaptureRequest
    {
        /// <summary>
        /// The PayPal order ID returned from CreateOrder
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// The shipment ID being paid for
        /// </summary>
        public Guid ShipmentId { get; set; }

        /// <summary>
        /// The payment method ID used
        /// </summary>
        public Guid PaymentMethodId { get; set; }

        /// <summary>
        /// Total payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (defaults to USD)
        /// </summary>
        public string? Currency { get; set; }
    }
}
