using System;

namespace Business.Payments.Shared
{
    /// <summary>
    /// Enumeration of possible payment statuses across all payment providers.
    /// Provides standardized status codes for Stripe, PayPal, and future gateways.
    /// </summary>
    /// <remarks>
    /// Gateway mappings:
    /// 
    /// Stripe:
    /// - succeeded → Completed
    /// - processing → Pending
    /// - requires_payment_method/requires_action → RequiresAction
    /// - canceled → Canceled
    /// - failed → Failed
    /// 
    /// PayPal:
    /// - CREATED/SAVED → Pending
    /// - APPROVED → RequiresAction (needs capture)
    /// - COMPLETED → Completed
    /// - VOIDED → Canceled
    /// - Unknown → Failed
    /// 
    /// These statuses are stored in TbPaymentTransaction.TransactionStatus as integers.
    /// </remarks>
    public enum PaymentStatus
    {
        /// <summary>
        /// Payment is pending confirmation or processing.
        /// Funds have not yet been captured. Transaction may complete asynchronously.
        /// </summary>
        /// <remarks>
        /// Common scenarios:
        /// - Bank transfer initiated but not yet cleared
        /// - PayPal order created but not yet approved by customer
        /// - Asynchronous payment method processing
        /// 
        /// Webhooks may update to Completed or Failed later.
        /// </remarks>
        Pending = 0,

        /// <summary>
        /// Payment completed successfully and funds have been captured.
        /// Shipment can proceed with fulfillment.
        /// </summary>
        /// <remarks>
        /// Terminal success state.
        /// Funds are in your account (minus processing fees).
        /// Only status that allows shipment to complete.
        /// Can transition to Refunded if refund is processed later.
        /// </remarks>
        Completed = 1,

        /// <summary>
        /// Payment failed due to decline, insufficient funds, or processing error.
        /// Shipment creation is rolled back.
        /// </summary>
        /// <remarks>
        /// Terminal failure state.
        /// Common failure reasons:
        /// - Card declined by issuing bank
        /// - Insufficient funds in account
        /// - Invalid payment details (expired card, wrong CVV)
        /// - Provider API errors or network issues
        /// 
        /// User must retry with different payment method.
        /// </remarks>
        Failed = 2,

        /// <summary>
        /// Previously completed payment has been fully refunded to the customer.
        /// Funds returned to customer's original payment method.
        /// </summary>
        /// <remarks>
        /// Only applicable to Completed payments.
        /// Refund may take 5-10 business days to appear on customer's statement.
        /// Can transition to PartiallyRefunded if only part of amount is refunded.
        /// </remarks>
        Refunded = 3,

        /// <summary>
        /// Payment was partially refunded - only some of the original amount was returned.
        /// Remaining balance still captured by merchant.
        /// </summary>
        /// <remarks>
        /// Example: $100 charge, $30 refunded → PartiallyRefunded status
        /// Used for partial order cancellations or goodwill refunds.
        /// Can transition to Refunded if remaining balance is also refunded.
        /// </remarks>
        PartiallyRefunded = 4,

        /// <summary>
        /// Payment requires additional customer action to complete.
        /// Customer must authenticate via 3D Secure, approve in PayPal, or complete verification.
        /// </summary>
        /// <remarks>
        /// Common scenarios:
        /// - 3D Secure (SCA) authentication required for European cards
        /// - PayPal order approved but needs manual capture
        /// - Bank transfer awaiting customer confirmation
        /// 
        /// AdditionalInfo may contain approval URL for customer to complete action.
        /// May transition to Completed after customer completes action (via webhook).
        /// </remarks>
        RequiresAction = 5,

        /// <summary>
        /// Payment was canceled or voided before funds were captured.
        /// No charges applied to customer's payment method.
        /// </summary>
        /// <remarks>
        /// Different from Failed (which indicates decline/error).
        /// Canceled means intentional cancellation before capture.
        /// Common scenarios:
        /// - Shipment canceled before payment capture
        /// - Order voided by admin
        /// - Customer canceled during approval flow
        /// </remarks>
        Canceled = 6
    }

    /// <summary>
    /// Refund status enumeration
    /// </summary>
    public enum RefundStatus
    {
        /// <summary>
        /// Refund is pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Refund completed successfully
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Refund failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Refund was canceled
        /// </summary>
        Canceled = 3
    }

    /// <summary>
    /// Type of refund
    /// </summary>
    public enum RefundType
    {
        /// <summary>
        /// Full refund of the entire transaction
        /// </summary>
        Full,

        /// <summary>
        /// Partial refund of a specified amount
        /// </summary>
        Partial
    }
}
