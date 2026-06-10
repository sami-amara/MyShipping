using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Payments.Stripe
{
    /// <summary>
    /// Result model for Stripe PaymentIntent creation
    /// </summary>
    public class StripeIntentResult
    {
        public bool Success { get; set; }
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
