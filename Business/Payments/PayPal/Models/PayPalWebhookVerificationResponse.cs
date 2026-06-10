namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Response from PayPal webhook verification API.
    /// Indicates whether the webhook signature is valid.
    /// </summary>
    internal class PayPalWebhookVerificationResponse
    {
        /// <summary>
        /// Verification result: "SUCCESS" indicates valid webhook, "FAILURE" indicates invalid
        /// </summary>
        public string VerificationStatus { get; set; } = string.Empty;
    }
}
