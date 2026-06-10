using System;

namespace Business.Contracts
{
    /// <summary>
    /// Factory for resolving and creating payment gateway instances based on payment method.
    /// Enables dynamic gateway selection for processing different payment types.
    /// </summary>
    /// <remarks>
    /// The factory pattern allows the system to:
    /// - Route credit card payments (Visa, MasterCard, etc.) to Stripe
    /// - Route PayPal payments to PayPal gateway
    /// - Add new payment providers without modifying existing code
    /// - Centralize gateway selection logic
    /// 
    /// Gateway selection is based on payment method name matching rules:
    /// - Names containing "PayPal" → PayPal gateway
    /// - Names containing card brands (Visa, MasterCard, etc.) → Stripe gateway
    /// - Default fallback → Stripe gateway
    /// 
    /// The factory is registered as a singleton service in dependency injection.
    /// </remarks>
    public interface IPaymentGatewayFactory
    {
        /// <summary>
        /// Resolves the appropriate payment gateway based on payment method name.
        /// </summary>
        /// <param name="paymentMethodName">
        /// Payment method name from the database (e.g., "PayPal", "Visa", "MasterCard").
        /// Case-insensitive matching is used for gateway selection.
        /// </param>
        /// <returns>
        /// IPaymentGateway instance configured for the specified payment method.
        /// Returns Stripe gateway as default if payment method name is not recognized.
        /// </returns>
        /// <remarks>
        /// This method uses in-memory resolution and does not access the database.
        /// For database-driven resolution, use GetGatewayByIdAsync instead.
        /// 
        /// Matching rules:
        /// - Contains "paypal" (case-insensitive) → PayPal gateway
        /// - Contains "visa", "mastercard", "amex", "discover" → Stripe gateway
        /// - Unknown or null → Stripe gateway (default)
        /// </remarks>
        IPaymentGateway GetGateway(string paymentMethodName);

        /// <summary>
        /// Resolves the appropriate payment gateway by querying the database for payment method details.
        /// </summary>
        /// <param name="paymentMethodId">Unique identifier of the payment method from TbPaymentMethod table</param>
        /// <returns>
        /// Task returning IPaymentGateway instance configured for the specified payment method ID.
        /// Returns Stripe gateway as default if payment method is not found.
        /// </returns>
        /// <remarks>
        /// This method:
        /// 1. Queries TbPaymentMethod table to retrieve payment method name
        /// 2. Delegates to GetGateway(string) for gateway selection
        /// 3. Returns default Stripe gateway if payment method ID doesn't exist
        /// 
        /// Prefer this method when you have a payment method ID from a shipment or transaction.
        /// Use GetGateway(string) when you already have the payment method name loaded.
        /// </remarks>
        Task<IPaymentGateway> GetGatewayByIdAsync(Guid paymentMethodId);

        /// <summary>
        /// Resolves the appropriate payment gateway by provider name (e.g., "PayPal", "Stripe").
        /// </summary>
        /// <param name="providerName">
        /// Payment provider name (case-insensitive).
        /// Examples: "PayPal", "Stripe"
        /// </param>
        /// <returns>
        /// Task returning IPaymentGateway instance for the specified provider.
        /// Throws ArgumentException if provider name is not recognized.
        /// </returns>
        /// <remarks>
        /// Used for:
        /// - PayPal callback processing (need PayPal gateway to capture orders)
        /// - Webhook processing (need specific gateway to validate signatures)
        /// - Admin refund operations (when you know the original provider)
        /// 
        /// Unlike GetGateway(string paymentMethodName), this matches exact provider names.
        /// </remarks>
        Task<IPaymentGateway> GetGatewayByNameAsync(string providerName);
    }
}
