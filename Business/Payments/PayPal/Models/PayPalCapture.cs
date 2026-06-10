namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Represents a single payment capture transaction.
    /// </summary>
    internal class PayPalCapture
    {
        /// <summary>Unique capture transaction ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Capture status (COMPLETED, PENDING, etc.)</summary>
        public string Status { get; set; } = string.Empty;
    }
}
