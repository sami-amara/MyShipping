namespace Business.Configuration
{
    /// <summary>
    /// Root configuration options for payment gateway providers.
    /// Binds to "PaymentGateways" section in appsettings.json.
    /// </summary>
    /// <remarks>
    /// Configuration structure in appsettings.json:
    /// <code>
    /// "PaymentGateways": {
    ///   "DefaultGateway": "Stripe",
    ///   "Currency": "USD",
    ///   "CaptureMethod": "automatic",
    ///   "Stripe": { ... },
    ///   "PayPal": { ... }
    /// }
    /// </code>
    /// 
    /// Loaded in Program.cs via:
    /// services.Configure&lt;PaymentGatewayOptions&gt;(configuration.GetSection("PaymentGateways"));
    /// 
    /// Injected into gateway services via IOptions&lt;PaymentGatewayOptions&gt;.
    /// </remarks>
    public class PaymentGatewayOptions
    {
        /// <summary>
        /// Stripe payment gateway configuration containing API keys and webhook secrets.
        /// </summary>
        public StripeOptions Stripe { get; set; } = new StripeOptions();

        /// <summary>
        /// PayPal payment gateway configuration containing client credentials and webhook settings.
        /// </summary>
        public PayPalOptions PayPal { get; set; } = new PayPalOptions();

        /// <summary>
        /// Default payment gateway to use when payment method routing is ambiguous.
        /// Valid values: "Stripe", "PayPal". Defaults to "Stripe".
        /// </summary>
        /// <remarks>
        /// Used as fallback when PaymentGatewayFactory cannot determine gateway from method name.
        /// </remarks>
        public string DefaultGateway { get; set; } = "Stripe";

        /// <summary>
        /// Default currency code for payment transactions (ISO 4217 format).
        /// Defaults to "USD". Common values: "USD", "EUR", "GBP".
        /// </summary>
        /// <remarks>
        /// Used when payment request doesn't specify currency.
        /// Must be supported by the selected payment gateway.
        /// </remarks>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Payment capture method: "automatic" or "manual".
        /// - automatic: Funds captured immediately upon payment authorization
        /// - manual: Funds authorized but not captured until explicit capture call
        /// </summary>
        /// <remarks>
        /// Automatic capture is standard for most e-commerce scenarios.
        /// Manual capture useful for pre-orders or when shipment may be cancelled.
        /// </remarks>
        public string CaptureMethod { get; set; } = "automatic";
    }

   
}
