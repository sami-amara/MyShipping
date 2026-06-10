using AppResource;

namespace UI.Helpers
{
    public static class PaymentTransactionLocalizationHelper
    {
        public static string GetStatusColor(string? statusName)
        {
            return (statusName ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "completed" => "success",
                "pending" => "warning",
                "failed" => "danger",
                "refunded" => "secondary",
                _ => "secondary"
            };
        }

        public static string GetLocalizedStatus(int status, string? statusName)
        {
            return status switch
            {
                0 => Labels.Pending ?? "Pending",
                1 => Labels.Completed ?? "Completed",
                2 => Labels.Failed ?? "Failed",
                3 => Labels.Refunded ?? "Refunded",
                4 => ResourceTextHelper.L("PartiallyRefunded", "Partially Refunded"),
                _ => ResourceTextHelper.L("Unknown", statusName ?? "Unknown")
            };
        }

        public static string GetLocalizedPaymentMethod(string? paymentMethodName)
        {
            var normalized = string.IsNullOrWhiteSpace(paymentMethodName)
                ? ResourceTextHelper.L("NotAvailable", "N/A")
                : paymentMethodName.Trim();

            return normalized.ToLowerInvariant() switch
            {
                "paypal" => ResourceTextHelper.L("PaymentMethodPayPal", "PayPal"),
                "stripe" => ResourceTextHelper.L("PaymentMethodStripe", "Stripe"),
                "cash" => ResourceTextHelper.L("PaymentMethodCash", "Cash"),
                "credit card" => ResourceTextHelper.L("PaymentMethodCreditCard", "Credit Card"),
                _ => normalized
            };
        }
    }
}
