using System.Text.Json.Serialization;

namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Represents a monetary amount in PayPal API responses.
    /// </summary>
    internal class PayPalAmount
    {
        /// <summary>ISO 4217 currency code (e.g., USD, EUR)</summary>
        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Amount value as string with up to 2 decimal places</summary>
        public string Value { get; set; } = string.Empty;
    }
}
