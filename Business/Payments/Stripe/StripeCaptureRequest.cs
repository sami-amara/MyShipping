using System;

namespace Business.Payments.Stripe
{
    /// <summary>
    /// Request model for capturing a Stripe payment.
    /// </summary>
    public class StripeCaptureRequest
    {
        /// <summary>
        /// The shipment ID being paid for
        /// </summary>
        public Guid ShipmentId { get; set; }

        /// <summary>
        /// The Stripe PaymentIntent ID to capture
        /// </summary>
        public string PaymentIntentId { get; set; } = string.Empty;

        /// <summary>
        /// Optional - backend will lookup Stripe payment method if not provided
        /// </summary>
        public Guid? PaymentMethodId { get; set; }
    }
}
