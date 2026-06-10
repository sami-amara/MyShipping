using System;

namespace Business.Payments.Stripe
{
    /// <summary>
    /// Request model for creating a Stripe PaymentIntent.
    /// </summary>
    public class StripePaymentIntentRequest
    {
        /// <summary>
        /// The shipment ID to create payment for
        /// </summary>
        public Guid ShipmentId { get; set; }

        /// <summary>
        /// Total payment amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}
