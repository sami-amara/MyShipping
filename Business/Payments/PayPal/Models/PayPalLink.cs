namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// HATEOAS link from PayPal API responses.
    /// </summary>
    internal class PayPalLink
    {
        /// <summary>Link URL</summary>
        public string Href { get; set; } = string.Empty;

        /// <summary>Link relationship type (e.g., "approve", "capture")</summary>
        public string Rel { get; set; } = string.Empty;

        /// <summary>HTTP method for the link</summary>
        public string Method { get; set; } = string.Empty;
    }
}
