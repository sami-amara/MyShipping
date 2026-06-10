using System;

namespace Business.Payments.PayPal
{
    /// <summary>
    /// Request model for creating a PayPal payment order via the JavaScript SDK flow.
    /// </summary>
    public class PaymentOrderRequest
    {
        /// <summary>
        /// The shipment ID to create payment for
        /// </summary>
        public Guid ShipmentId { get; set; }

        /// <summary>
        /// The payment method ID from TbPaymentMethod table
        /// </summary>
        public Guid PaymentMethodId { get; set; }

        /// <summary>
        /// Total payment amount including shipping rate and commissions
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (defaults to USD)
        /// </summary>
        public string? Currency { get; set; }
    }
}
