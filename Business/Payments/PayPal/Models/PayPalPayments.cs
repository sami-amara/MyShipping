using System.Collections.Generic;

namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Contains payment capture information within a purchase unit.
    /// </summary>
    internal class PayPalPayments
    {
        /// <summary>List of payment captures for this purchase unit</summary>
        public List<PayPalCapture>? Captures { get; set; }
    }
}
