using System.Text.Json.Serialization;

namespace Business.Payments.PayPal.Models
{
    /// <summary>
    /// Response from PayPal OAuth 2.0 token endpoint.
    /// Contains access token and expiration information.
    /// </summary>
    internal class PayPalTokenResponse
    {
        /// <summary>OAuth 2.0 Bearer access token</summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>Token type, typically "Bearer"</summary>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        /// <summary>Token lifetime in seconds (typically 32400 = 9 hours)</summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
