using System;
using System.Collections.Generic;

namespace Business.Payments.Gateway
{
    /// <summary>
    /// Payment request model containing all information needed to process a payment through any gateway.
    /// Provides gateway-agnostic abstraction for Stripe, PayPal, and other payment providers.
    /// </summary>
    /// <remarks>
    /// This model is passed to IPaymentGateway.ProcessPayment by PaymentTransactionService.
    /// Gateway implementations map these fields to provider-specific API calls.
    /// 
    /// Usage flow:
    /// 1. PaymentTransactionService builds PaymentRequest from shipment/payment method data
    /// 2. PaymentGatewayFactory selects appropriate gateway (Stripe/PayPal)
    /// 3. Gateway processes payment using provider API
    /// 4. Gateway returns PaymentResult with transaction details
    /// 
    /// Gateway-specific mapping:
    /// - Stripe: Amount sent as integer cents (Amount * 100), PaymentMethodToken as pm_xxx
    /// - PayPal: Amount sent as decimal string, Metadata.OrderId as reference_id
    /// </remarks>
    public class PaymentRequest
    {
        /// <summary>
        /// Payment amount in the base currency unit (e.g., dollars, not cents).
        /// Must be positive and typically includes shipping rate + commission.
        /// </summary>
        /// <remarks>
        /// Gateway conversion:
        /// - Stripe: Converted to smallest currency unit (cents) by multiplying by 100
        /// - PayPal: Sent as decimal string with 2 decimal places (e.g., "102.50")
        /// 
        /// Example: Amount = 102.50 means $102.50 USD
        /// </remarks>
        public decimal Amount { get; set; }

        /// <summary>
        /// ISO 4217 currency code for the payment (e.g., "USD", "EUR", "GBP").
        /// Must be supported by the selected payment gateway.
        /// </summary>
        /// <remarks>
        /// Common currencies:
        /// - USD: United States Dollar
        /// - EUR: Euro
        /// - GBP: British Pound Sterling
        /// 
        /// Defaults to "USD" if not specified.
        /// Gateway will reject unsupported currencies (e.g., PayPal doesn't support all currencies Stripe does).
        /// </remarks>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Human-readable description of the payment that appears on the customer's credit card statement.
        /// Keep concise as statement descriptors have character limits (typically 22 characters).
        /// </summary>
        /// <remarks>
        /// Best practices:
        /// - Include business name or recognizable brand
        /// - Add shipment tracking number if space allows
        /// - Avoid special characters that may be rejected by card networks
        /// 
        /// Example: "MyShipping TRK-ABC123" (shows business and tracking number)
        /// 
        /// Provider handling:
        /// - Stripe: Used for statement_descriptor field
        /// - PayPal: Shown in order description and transaction details
        /// </remarks>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Customer information for the payment including email, name, and contact details.
        /// Used for receipt delivery, fraud detection, and customer support.
        /// </summary>
        /// <remarks>
        /// Gateways use customer info for:
        /// - Sending payment receipts via email
        /// - Fraud detection and risk scoring
        /// - Linking multiple payments to same customer
        /// - Displaying in provider dashboard for support
        /// 
        /// See CustomerInfo class for available fields.
        /// </remarks>
        public CustomerInfo Customer { get; set; } = new CustomerInfo();

        /// <summary>
        /// Additional metadata key-value pairs to attach to the payment for reference.
        /// Stored by provider but not shown to customer. Useful for linking to internal records.
        /// </summary>
        /// <remarks>
        /// Common metadata:
        /// - "ShipmentId": Link payment to shipment record
        /// - "UserId": Link payment to user account
        /// - "OrderId": Link to order/tracking number
        /// - "Environment": "Test" or "Production" for debugging
        /// 
        /// Metadata is:
        /// - Searchable in provider dashboards (Stripe, PayPal)
        /// - Included in webhook events for reconciliation
        /// - Not visible to end customers
        /// - Limited to provider-specific size limits (typically 50 keys, 500 chars per value)
        /// 
        /// Used by webhooks to match events to database records.
        /// </remarks>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Payment method token or identifier from the frontend payment UI.
        /// Gateway-specific format: Stripe uses pm_xxx/tok_xxx, PayPal uses order IDs.
        /// </summary>
        /// <remarks>
        /// Token sources by gateway:
        /// - **Stripe**: Payment method (pm_xxx) or token (tok_xxx) from Stripe.js
        /// - **PayPal**: Order ID or payment token from PayPal SDK
        /// 
        /// Security:
        /// - Tokens are single-use or time-limited to prevent replay attacks
        /// - Frontend obtains token by submitting card data directly to provider (not your server)
        /// - This prevents PCI compliance requirements from handling raw card data
        /// 
        /// Null for testing/simulation, but required for real payment processing.
        /// Gateway will reject payment if token is missing or invalid.
        /// </remarks>
        public string? PaymentMethodToken { get; set; }

        /// <summary>
        /// Whether to capture payment funds immediately (true) or only authorize them for later capture (false).
        /// Defaults to true for immediate capture in standard e-commerce flows.
        /// </summary>
        /// <remarks>
        /// **Immediate Capture (true - default):**
        /// - Funds are charged to customer's card immediately
        /// - Transaction completes in single API call
        /// - Standard for most shipment payment scenarios
        /// - Cannot be reversed (only refunded after capture)
        /// 
        /// **Authorize Only (false):**
        /// - Card is authorized but funds not yet captured
        /// - Allows canceling shipment without charging customer
        /// - Funds must be captured via separate API call within authorization window (typically 7 days)
        /// - Useful for:
        ///   * Pre-orders where shipment may be delayed
        ///   * Scenarios where shipment cost may change
        ///   * When fraud review is needed before charging
        /// 
        /// Gateway behavior:
        /// - Stripe: intent = "capture" vs "authorize"
        /// - PayPal: intent = "CAPTURE" vs "AUTHORIZE"
        /// </remarks>
        public bool CaptureImmediately { get; set; } = true;
    }
}
