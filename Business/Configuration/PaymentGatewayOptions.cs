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

    /// <summary>
    /// Stripe payment gateway configuration containing API credentials and settings.
    /// </summary>
    /// <remarks>
    /// Stripe requires different keys for test vs production environments:
    /// - Test keys: pk_test_..., sk_test_..., whsec_test_...
    /// - Live keys: pk_live_..., sk_live_..., whsec_live_...
    /// 
    /// Obtain keys from Stripe Dashboard (https://dashboard.stripe.com/apikeys).
    /// NEVER commit secret keys to source control - use environment variables or Azure Key Vault.
    /// </remarks>
    public class StripeOptions
    {
        /// <summary>
        /// Whether Stripe gateway is enabled for payment processing.
        /// Set to false to disable Stripe and prevent gateway initialization.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Stripe publishable key (starts with pk_test_ or pk_live_).
        /// Used in frontend JavaScript for Stripe.js initialization.
        /// Safe to expose publicly in HTML/JavaScript.
        /// </summary>
        /// <remarks>
        /// This key is used client-side to:
        /// - Tokenize credit card data securely without touching your server
        /// - Create payment methods through Stripe.js
        /// - Display Stripe UI components (card element, payment element)
        /// 
        /// Environment-specific:
        /// - Test: pk_test_51ABC...
        /// - Live: pk_live_51ABC...
        /// </remarks>
        public string PublishableKey { get; set; } = string.Empty;

        /// <summary>
        /// Stripe secret key (starts with sk_test_ or sk_live_).
        /// Used in backend API calls to create charges, process refunds, etc.
        /// MUST BE KEPT SECRET - never expose in frontend code!
        /// </summary>
        /// <remarks>
        /// This key grants full access to your Stripe account and must be protected:
        /// - Store in environment variables (not appsettings.json in production)
        /// - Use Azure Key Vault or similar secrets management
        /// - Never log or display this value
        /// - Rotate immediately if compromised
        /// 
        /// Environment-specific:
        /// - Test: sk_test_51ABC...
        /// - Live: sk_live_51ABC...
        /// </remarks>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Webhook signing secret for validating webhook signatures (starts with whsec_).
        /// Used to verify webhooks originated from Stripe and prevent forgery attacks.
        /// </summary>
        /// <remarks>
        /// Obtained from Stripe Dashboard → Webhooks → Add Endpoint.
        /// Each webhook endpoint has its own signing secret.
        /// 
        /// Validation flow:
        /// 1. Extract Stripe-Signature header from webhook request
        /// 2. Use this secret with Stripe SDK to verify HMAC signature
        /// 3. Reject webhook if signature invalid
        /// 
        /// Environment-specific:
        /// - Test: whsec_test_...
        /// - Live: whsec_live_...
        /// </remarks>
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>
        /// Environment identifier: "Test" or "Production".
        /// Used for logging and monitoring to distinguish test vs live transactions.
        /// </summary>
        /// <remarks>
        /// Defaults to "Test" to prevent accidental live charges during development.
        /// Must match the environment of your API keys (test keys for Test, live keys for Production).
        /// </remarks>
        public string Environment { get; set; } = "Test";

        /// <summary>
        /// Stripe API version to use (e.g., "2023-10-16").
        /// Optional - uses Stripe's latest API version if not specified.
        /// </summary>
        /// <remarks>
        /// Stripe periodically releases new API versions with breaking changes.
        /// Pinning a version ensures consistent behavior.
        /// See: https://stripe.com/docs/api/versioning
        /// </remarks>
        public string? ApiVersion { get; set; }
    }

    /// <summary>
    /// PayPal payment gateway configuration containing REST API credentials and settings.
    /// </summary>
    /// <remarks>
    /// PayPal uses OAuth 2.0 client credentials flow for authentication.
    /// Different credentials are required for Sandbox (testing) vs Live (production).
    /// 
    /// Obtain credentials from PayPal Developer Dashboard (https://developer.paypal.com/dashboard).
    /// NEVER commit client secrets to source control - use environment variables or Azure Key Vault.
    /// </remarks>
    public class PayPalOptions
    {
        /// <summary>
        /// Whether PayPal gateway is enabled for payment processing.
        /// Set to false to disable PayPal and prevent gateway initialization.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// PayPal REST API client ID (from PayPal Developer Dashboard).
        /// Used for OAuth 2.0 authentication to obtain access tokens.
        /// </summary>
        /// <remarks>
        /// Obtain from PayPal Developer Dashboard → My Apps & Credentials.
        /// Different IDs for Sandbox vs Live environments.
        /// 
        /// Environment-specific:
        /// - Sandbox: Starts with random alphanumeric string for testing
        /// - Live: Production client ID for real transactions
        /// 
        /// This is not secret but should still be protected from public exposure.
        /// </remarks>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// PayPal REST API client secret (from PayPal Developer Dashboard).
        /// Used with ClientId for OAuth 2.0 authentication.
        /// MUST BE KEPT SECRET - never expose in frontend code!
        /// </summary>
        /// <remarks>
        /// This secret grants access to process payments and refunds:
        /// - Store in environment variables (not appsettings.json in production)
        /// - Use Azure Key Vault or similar secrets management
        /// - Never log or display this value
        /// - Rotate immediately if compromised
        /// 
        /// Used to obtain OAuth 2.0 Bearer tokens via /v1/oauth2/token endpoint.
        /// Tokens expire after 9 hours (32400 seconds) and are auto-refreshed by gateway.
        /// </remarks>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// PayPal webhook ID for validating webhook signatures.
        /// Obtained after creating a webhook endpoint in PayPal Dashboard.
        /// </summary>
        /// <remarks>
        /// Setup:
        /// 1. Go to PayPal Developer Dashboard → Webhooks
        /// 2. Create webhook endpoint with your server URL (e.g., https://yourdomain.com/api/PaymentWebhooks/paypal)
        /// 3. Select event types to subscribe to (e.g., PAYMENT.CAPTURE.COMPLETED)
        /// 4. Copy the Webhook ID from the dashboard
        /// 
        /// Used by ValidateWebhook to call PayPal's verification API:
        /// POST /v1/notifications/verify-webhook-signature
        /// 
        /// If not configured, webhook validation falls back to simple signature presence check (less secure).
        /// </remarks>
        public string WebhookId { get; set; } = string.Empty;

        /// <summary>
        /// PayPal environment: "Sandbox" or "Live".
        /// Determines which PayPal API base URL is used.
        /// </summary>
        /// <remarks>
        /// Environment URLs:
        /// - Sandbox: https://api-m.sandbox.paypal.com (for testing)
        /// - Live: https://api-m.paypal.com (for production)
        /// 
        /// Defaults to "Sandbox" to prevent accidental live charges.
        /// Must match the environment of your ClientId and ClientSecret.
        /// </remarks>
        public string Environment { get; set; } = "Sandbox";

        /// <summary>
        /// PayPal REST API base URL (optional).
        /// Auto-computed from Environment if not explicitly set.
        /// </summary>
        /// <remarks>
        /// Normally left empty to auto-select based on Environment setting.
        /// Can be overridden for:
        /// - Testing against local PayPal mock server
        /// - Using region-specific endpoints if PayPal introduces them
        /// 
        /// Default URLs:
        /// - Sandbox: https://api-m.sandbox.paypal.com
        /// - Live: https://api-m.paypal.com
        /// </remarks>
        public string? BaseUrl { get; set; }
    }
}
