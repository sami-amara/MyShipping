using System.Collections.Generic;

namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Response from PayPal order creation endpoint.
    /// Contains the order ID, current status, and HATEOAS links.
    /// </summary>
    internal class PayPalOrderResponse
    {
        /// <summary>Unique PayPal order ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Order status (CREATED, APPROVED, etc.)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>HATEOAS links for approval, capture, etc.</summary>
        public List<PayPalLink>? Links { get; set; }
    }
}
