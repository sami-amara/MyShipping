using Business.Contracts;
using DataAccessLayer.Contracts;
using Domains;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Business.Services.PaymentGateways
{
    /// <summary>
    /// Payment Gateway Factory Implementation
    /// Returns the appropriate payment gateway based on payment method
    /// </summary>
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGenericRepository<TbPaymentMethod> _paymentMethodRepository;

        public PaymentGatewayFactory(
            IServiceProvider serviceProvider,
            IGenericRepository<TbPaymentMethod> paymentMethodRepository)
        {
            _serviceProvider = serviceProvider;
            _paymentMethodRepository = paymentMethodRepository;
        }

        public IPaymentGateway GetGateway(string paymentMethodName)
        {
            if (string.IsNullOrEmpty(paymentMethodName))
                throw new ArgumentException("Payment method name cannot be null or empty", nameof(paymentMethodName));
            // Normalize the payment method name
            var normalizedName = paymentMethodName.ToLower().Trim();
            // Map payment method names to gateway implementations
            return normalizedName switch
            {
                // Stripe handles credit/debit cards
                "stripe" or "credit card" or "debit card" or "card" or "visa" or "mastercard" or "amex" or "american express" or "discover" =>
                    _serviceProvider.GetRequiredService<StripePaymentGateway>(),

                // PayPal handles PayPal payments
                "paypal" or "pay pal" =>
                    _serviceProvider.GetRequiredService<PayPalPaymentGateway>(),

                // Default to Stripe for unknown methods
                _ => _serviceProvider.GetRequiredService<StripePaymentGateway>()
            };
        }

        public async Task<IPaymentGateway> GetGatewayByIdAsync(Guid paymentMethodId)
        {
            if (paymentMethodId == Guid.Empty)
                throw new ArgumentException("Payment method ID cannot be empty", nameof(paymentMethodId));
            // Get payment method from database
            var paymentMethod = await _paymentMethodRepository.GetById(paymentMethodId);
            if (paymentMethod == null)
                throw new ArgumentException($"Payment method with ID {paymentMethodId} not found", nameof(paymentMethodId));
            // Use the English method name to determine the gateway
            var methodName = paymentMethod.MethodEname ?? paymentMethod.MethdAname ?? string.Empty;

            return GetGateway(methodName);
        }

        public Task<IPaymentGateway> GetGatewayByNameAsync(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
            var normalizedName = providerName.ToLower().Trim();
            IPaymentGateway gateway = normalizedName switch
            {
                "paypal" => _serviceProvider.GetRequiredService<PayPalPaymentGateway>(),
                "stripe" => _serviceProvider.GetRequiredService<StripePaymentGateway>(),
                _ => throw new ArgumentException($"Unknown payment provider: {providerName}", nameof(providerName))
            };

            return Task.FromResult(gateway);
        }
    }
}
