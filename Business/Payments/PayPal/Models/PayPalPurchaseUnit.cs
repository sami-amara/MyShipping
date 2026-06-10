namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Represents a purchase unit in PayPal order/capture response.
    /// Contains payment details for the transaction.
    /// </summary>
    internal class PayPalPurchaseUnit
    {
        /// <summary>Payment details including captures</summary>
        public PayPalPayments? Payments { get; set; }
    }
}
