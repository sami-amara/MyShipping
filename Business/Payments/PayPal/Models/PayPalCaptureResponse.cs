using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Response from PayPal order capture endpoint.
    /// Contains capture details and payment information.
    /// </summary>
    internal class PayPalCaptureResponse
    {
        /// <summary>Unique capture ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Capture status (COMPLETED, PENDING, etc.)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Purchase units containing payment capture details</summary>
        [JsonPropertyName("purchase_units")]
        public List<PayPalPurchaseUnit>? PurchaseUnits { get; set; }
    }
}
