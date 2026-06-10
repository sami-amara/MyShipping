using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

/// <summary>
/// Payment method entity representing available payment options (Visa, MasterCard, PayPal, etc.).
/// Defines commission rates and gateway routing information for each payment type.
/// </summary>
/// <remarks>
/// This entity serves as the master list of accepted payment methods.
/// 
/// **Data-Driven UI:**
/// - Seeded via PaymentMethodSeeder at application startup
/// - Loaded by PaymentMethodsController for dropdown population
/// - PaymentMethodToken used as HTML data attribute for form metadata
/// 
/// **Commission Calculation:**
/// - Commission percentage is applied when calculating total payment amount
/// - Example: 2.5% commission on $100 shipping = $2.50 commission, $102.50 total
/// - Stored in PaymentTransaction to preserve historical rates
/// 
/// **Gateway Routing:**
/// - PaymentGatewayFactory uses MethodEname to select Stripe vs PayPal
/// - Card brands (Visa, MasterCard, etc.) → Stripe gateway
/// - PayPal → PayPal gateway
/// 
/// **Active/Inactive Management:**
/// - Soft-delete via BaseTable.IsDeleted allows disabling payment methods
/// - Only active methods (IsDeleted = false) shown to users
/// - Historical transactions preserve reference to disabled methods
/// 
/// Seeded payment methods:
/// - PayPal (commission varies)
/// - Visa (typically 2.9% + $0.30)
/// - MasterCard (typically 2.9% + $0.30)
/// - American Express (typically 3.5%)
/// - Discover (typically 2.9% + $0.30)
/// </remarks>
public partial class TbPaymentMethod : BaseTable
{
    /// <summary>
    /// Payment method name in Arabic for bilingual UI support.
    /// </summary>
    /// <remarks>
    /// Used in localized UI when user's language preference is Arabic.
    /// Corresponds to MethodEname for English equivalent.
    /// </remarks>
    public string? MethdAname { get; set; }

    /// <summary>
    /// Payment method name in English (e.g., "Visa", "MasterCard", "PayPal").
    /// Primary identifier used for gateway routing and user display.
    /// </summary>
    /// <remarks>
    /// This value is used by PaymentGatewayFactory to determine which gateway to use:
    /// - Contains "PayPal" → PayPal gateway
    /// - Contains "Visa", "MasterCard", "Amex", "Discover" → Stripe gateway
    /// - Unknown → Stripe gateway (default)
    /// 
    /// Also displayed in UI dropdowns, transaction history, and admin reporting.
    /// </remarks>
    public string? MethodEname { get; set; }

    /// <summary>
    /// Payment method token prefix or metadata for frontend integration.
    /// Stored as HTML data attribute for JavaScript payment processing.
    /// </summary>
    /// <remarks>
    /// Usage in UI:
    /// - Populated in dropdown via ManagePageControlls.fillPaymentMethodDropdown
    /// - Stored as data-payment-token attribute on select options
    /// - Copied to hidden PaymentMethodToken input field on selection change
    /// - Sent to backend for payment gateway token validation
    /// 
    /// Examples:
    /// - Stripe: "tok_", "pm_", "card_" prefix for token validation
    /// - PayPal: "PAYID-" prefix or order reference format
    /// 
    /// This enables frontend validation and proper token formatting before submission.
    /// </remarks>
    public string? PaymentMethodToken { get; set; }

    /// <summary>
    /// Commission percentage charged for using this payment method (e.g., 2.9 for 2.9%).
    /// Applied to shipping rate when calculating total payment amount.
    /// </summary>
    /// <remarks>
    /// Commission calculation:
    /// - CommissionAmount = ShippingRate * (Commission / 100)
    /// - TotalAmount = ShippingRate + CommissionAmount
    /// 
    /// Examples:
    /// - Visa: 2.9% + flat fee (simplified to 2.9% here)
    /// - American Express: 3.5% (higher processing fee)
    /// - PayPal: Variable based on business account tier
    /// 
    /// Null commission treated as 0% (no additional charge).
    /// Commission snapshot is stored in TbPaymentTransaction to preserve historical rates.
    /// </remarks>
    public double? Commission { get; set; }

    /// <summary>
    /// Navigation property to shipments using this payment method.
    /// One payment method can be used by many shipments.
    /// </summary>
    /// <remarks>
    /// Used for:
    /// - Reporting shipments by payment method
    /// - Analyzing payment method popularity
    /// - Calculating total revenue per payment method
    /// - Preventing deletion of payment methods in use (foreign key constraint)
    /// </remarks>
    public virtual ICollection<TbShippment> TbShippments { get; set; } = new List<TbShippment>();
}
