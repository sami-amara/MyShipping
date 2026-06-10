using System;
using System.Collections.Generic;

namespace Domains;

/// <summary>
/// Payment transaction entity representing a single payment attempt for a shipment.
/// Stores complete audit trail including provider details, amounts, status, and reconciliation data.
/// </summary>
/// <remarks>
/// This entity serves multiple purposes:
/// - **Transaction logging**: Records all payment attempts with success/failure details
/// - **Idempotency tracking**: Prevents duplicate charges via unique IdempotencyKey
/// - **Webhook reconciliation**: Links provider webhook events to update transaction status
/// - **Financial reporting**: Provides data for payment analytics and commission calculations
/// - **Audit compliance**: Maintains immutable record of payment processing history
/// 
/// Key relationships:
/// - One transaction per shipment (one-to-one with TbShippment)
/// - References payment method for commission calculation (many-to-one with TbPaymentMethod)
/// - May be linked to webhook events via ProviderEventId for status reconciliation
/// 
/// Status flow examples:
/// - Immediate capture: Pending → Completed
/// - Failed payment: Pending → Failed (with ErrorMessage)
/// - Webhook update: Pending → Completed (reconciled from provider event)
/// - Refund: Completed → Refunded (with refund notes)
/// </remarks>
public partial class TbPaymentTransaction : BaseTable
{
    /// <summary>
    /// Idempotency key to prevent duplicate payment processing for the same shipment and payment method.
    /// Format: "ship:{ShipmentId}:pm:{PaymentMethodId}"
    /// </summary>
    /// <remarks>
    /// Used by PaymentTransactionService to detect retry attempts.
    /// If a transaction with this key already exists, the existing transaction is returned
    /// instead of creating a duplicate charge. Critical for preventing double-billing
    /// when users refresh payment pages or retry failed shipments.
    /// </remarks>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Payment provider name that processed this transaction (e.g., "Stripe", "PayPal").
    /// Populated from IPaymentGateway.GetProviderName() during payment processing.
    /// </summary>
    /// <remarks>
    /// Used for:
    /// - Identifying which gateway handled the transaction
    /// - Routing webhook events to the correct gateway for validation
    /// - Analytics and reporting by payment provider
    /// - Debugging provider-specific issues
    /// </remarks>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Foreign key to the shipment that this payment is for.
    /// </summary>
    public Guid ShipmentId { get; set; }

    /// <summary>
    /// Foreign key to the payment method used for this transaction (Visa, MasterCard, PayPal, etc.).
    /// Determines commission percentage and payment gateway routing.
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Base shipping cost before payment method commission is added.
    /// Calculated from shipment weight, distance, carrier rates, etc.
    /// </summary>
    public decimal ShippingRate { get; set; }

    /// <summary>
    /// Commission percentage charged by the payment method (e.g., 2.5 for 2.5%).
    /// Retrieved from TbPaymentMethod.Commission at transaction time.
    /// </summary>
    /// <remarks>
    /// Stored in transaction to preserve the commission rate at the time of payment,
    /// even if payment method commission changes later.
    /// </remarks>
    public double CommissionPercentage { get; set; }

    /// <summary>
    /// Calculated commission amount in dollars: ShippingRate * (CommissionPercentage / 100).
    /// Represents the fee charged for using this payment method.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Total amount charged to the customer: ShippingRate + CommissionAmount.
    /// This is the final amount sent to the payment gateway.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Current status of the payment transaction.
    /// Maps to PaymentTransactionStatus enum: 0=Pending, 1=Completed, 2=Failed, 3=Refunded.
    /// </summary>
    /// <remarks>
    /// Status transitions:
    /// - Set to Pending initially when payment is submitted
    /// - Updated to Completed when payment gateway confirms success
    /// - Updated to Failed if payment gateway rejects the payment
    /// - Updated to Refunded when a refund is processed
    /// - May be updated asynchronously via webhook reconciliation
    /// </remarks>
    public int TransactionStatus { get; set; }

    /// <summary>
    /// Payment provider's unique event ID for webhook events that updated this transaction.
    /// Used to link transaction updates to specific webhook deliveries for audit trails.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - Stripe: evt_1ABC2DEF3GHI4JKL (from webhook event ID)
    /// - PayPal: WH-12345-67890-ABCDE (from webhook event ID)
    /// 
    /// Null if transaction hasn't been reconciled from a webhook yet.
    /// </remarks>
    public string? ProviderEventId { get; set; }

    /// <summary>
    /// Unique transaction identifier from the payment provider (e.g., Stripe charge ID, PayPal order ID).
    /// Used for refunds, disputes, customer support, and webhook reconciliation.
    /// </summary>
    /// <remarks>
    /// Format varies by provider:
    /// - Stripe: ch_1ABC2DEF3GHI4JKL or pi_1ABC2DEF3GHI4JKL
    /// - PayPal: 5O190127TN364715T
    /// 
    /// This is the authoritative reference for the transaction in the provider's system.
    /// Required for processing refunds and looking up transaction details in provider dashboard.
    /// </remarks>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// UTC timestamp when the payment was processed by the payment gateway.
    /// Null if transaction is still pending or hasn't been submitted yet.
    /// </summary>
    public DateTime? ProcessedDate { get; set; }

    /// <summary>
    /// Error message from the payment gateway if the transaction failed.
    /// Contains provider-specific error details for debugging and user display.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - "Your card was declined."
    /// - "Insufficient funds"
    /// - "Invalid CVV"
    /// - "PayPal API error: INSTRUMENT_DECLINED"
    /// 
    /// Null for successful transactions.
    /// </remarks>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes about the transaction for internal use.
    /// May contain refund reasons, manual status changes, or administrative actions.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Additional information from the payment gateway.
    /// For PayPal: Contains approval URL when payment requires user action.
    /// For Stripe: May contain 3D Secure URLs or payment method details.
    /// </summary>
    /// <remarks>
    /// Used to pass gateway-specific data to the frontend:
    /// - PayPal approval URLs for redirect flow
    /// - Stripe payment intent client secrets
    /// - Additional provider metadata
    /// </remarks>
    public string? AdditionalInfo { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the shipment this payment is for.
    /// </summary>
    public virtual TbShippment? Shipment { get; set; }

    /// <summary>
    /// Navigation property to the payment method used for this transaction.
    /// Provides access to method name, commission details, and gateway routing info.
    /// </summary>
    public virtual TbPaymentMethod? PaymentMethod { get; set; }
}

/// <summary>
/// Enumeration of possible payment transaction statuses.
/// Used to track the lifecycle of a payment from initiation to completion or failure.
/// </summary>
public enum PaymentTransactionStatus
{
    /// <summary>
    /// Payment has been initiated but not yet confirmed by the payment gateway.
    /// Waiting for provider response or user action (approval, authentication, etc.).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment has been successfully processed and funds have been captured.
    /// Transaction is complete and shipment can proceed.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Payment processing failed due to card decline, insufficient funds, or other error.
    /// Shipment creation is rolled back and user must retry with different payment method.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Previously completed payment has been refunded to the customer.
    /// May be full or partial refund depending on refund request.
    /// </summary>
    Refunded = 3
}
