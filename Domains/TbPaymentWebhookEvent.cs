using System;

namespace Domains;

/// <summary>
/// Webhook event entity for storing payment provider webhook deliveries.
/// Provides replay protection, audit trails, and debugging capabilities for asynchronous payment events.
/// </summary>
/// <remarks>
/// This entity serves critical purposes in the payment system:
/// 
/// **Replay Protection (Idempotency):**
/// - Prevents duplicate processing when providers retry webhook deliveries
/// - Composite unique index on (ProviderName + ProviderEventId) ensures each event is recorded once
/// - IsWebhookEventProcessed checks this table before processing webhook payloads
/// 
/// **Audit Trail:**
/// - Stores complete webhook history with timestamps for compliance and debugging
/// - Raw payload preservation enables investigation of payment discrepancies
/// - Processing notes capture success/failure reasons for operational monitoring
/// 
/// **Reconciliation:**
/// - Links webhook events to payment transactions via TransactionReference
/// - Enables asynchronous status updates (e.g., PayPal approval, Stripe dispute)
/// - Supports replaying events if reconciliation logic changes
/// 
/// **Debugging:**
/// - Full payload stored as JSON for reproducing issues
/// - ReceivedAt timestamp helps correlate with provider logs
/// - ProcessingNotes capture errors for troubleshooting
/// 
/// Webhook flow:
/// 1. PaymentWebhooksController receives webhook HTTP request
/// 2. Validates signature using IPaymentGateway.ValidateWebhook
/// 3. Checks IsWebhookEventProcessed (this table) for duplicates
/// 4. Records event using RecordWebhookEvent (creates this entity)
/// 5. Calls ReconcileTransactionFromWebhook to update payment status
/// 6. Marks event processed using MarkWebhookEventProcessed
/// </remarks>
public partial class TbPaymentWebhookEvent : BaseTable
{
    /// <summary>
    /// Payment provider that sent this webhook (e.g., "Stripe", "PayPal").
    /// Combined with ProviderEventId to form unique constraint for deduplication.
    /// </summary>
    /// <remarks>
    /// Used for:
    /// - Deduplication via composite index (ProviderName + ProviderEventId)
    /// - Routing webhook processing to correct gateway logic
    /// - Provider-specific analytics and reporting
    /// </remarks>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Unique event identifier from the payment provider.
    /// Combined with ProviderName to form unique constraint preventing duplicate event processing.
    /// </summary>
    /// <remarks>
    /// Examples by provider:
    /// - Stripe: "evt_1ABC2DEF3GHI4JKL"
    /// - PayPal: "WH-2WR32451HC0233532-67976317FL4543714"
    /// 
    /// This ID is assigned by the provider and guaranteed unique within that provider.
    /// The composite index (ProviderName + ProviderEventId) ensures global uniqueness.
    /// </remarks>
    public string ProviderEventId { get; set; } = string.Empty;

    /// <summary>
    /// Event type identifier from the provider's webhook payload.
    /// Indicates what kind of event occurred (payment succeeded, refund processed, etc.).
    /// </summary>
    /// <remarks>
    /// Examples by provider:
    /// - Stripe: "payment_intent.succeeded", "charge.refunded", "charge.dispute.created"
    /// - PayPal: "PAYMENT.CAPTURE.COMPLETED", "PAYMENT.CAPTURE.REFUNDED"
    /// 
    /// Used for:
    /// - Filtering reconciliation logic by event type
    /// - Analytics on webhook event patterns
    /// - Debugging specific event type processing issues
    /// 
    /// May be null if event type cannot be extracted from payload.
    /// </remarks>
    public string? EventType { get; set; }

    /// <summary>
    /// Payment transaction reference from the webhook payload, if available.
    /// Used to match webhook events to TbPaymentTransaction records for reconciliation.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// - Stripe charge ID: "ch_1ABC2DEF3GHI4JKL"
    /// - Stripe payment intent ID: "pi_1ABC2DEF3GHI4JKL"
    /// - PayPal order ID: "5O190127TN364715T"
    /// 
    /// This value is extracted from the webhook payload and matched against
    /// TbPaymentTransaction.TransactionReference during reconciliation.
    /// 
    /// Null if the webhook event is not related to a specific transaction
    /// (e.g., account-level events, subscription events).
    /// </remarks>
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Complete raw JSON payload received from the payment provider webhook.
    /// Stored for debugging, audit compliance, and potential replay scenarios.
    /// </summary>
    /// <remarks>
    /// This field contains the entire webhook body as-received before any processing.
    /// 
    /// Uses:
    /// - **Debugging**: Inspect exact payload when troubleshooting reconciliation issues
    /// - **Replay**: Reprocess events if reconciliation logic changes
    /// - **Compliance**: Maintain immutable record of provider communications
    /// - **Investigation**: Analyze payment discrepancies with full event details
    /// 
    /// Stored as string (JSON) to accommodate different provider payload structures.
    /// Consider column size limits if providers send very large payloads.
    /// </remarks>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating whether this webhook event has been successfully processed.
    /// Used for idempotency checks and operational monitoring.
    /// </summary>
    /// <remarks>
    /// Status meaning:
    /// - **false**: Event received but not yet processed (new/pending/failed)
    /// - **true**: Event successfully processed (reconciliation completed)
    /// 
    /// Set to false when RecordWebhookEvent creates the record.
    /// Updated to true when MarkWebhookEventProcessed is called after successful reconciliation.
    /// 
    /// Prevents duplicate reconciliation if provider retries webhook delivery.
    /// Enables monitoring of unprocessed events for alerting/investigation.
    /// </remarks>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Human-readable notes about the processing result or any errors encountered.
    /// Populated by MarkWebhookEventProcessed for operational visibility.
    /// </summary>
    /// <remarks>
    /// Example notes:
    /// - "Successfully reconciled transaction ch_1ABC2DEF to Completed status"
    /// - "No matching transaction found for reference pi_1ABC2DEF"
    /// - "Skipped: event type customer.subscription.updated not relevant"
    /// - "Error: payload parsing failed - invalid JSON"
    /// 
    /// Used for:
    /// - Debugging failed reconciliation attempts
    /// - Understanding why events were skipped
    /// - Operational dashboards showing webhook health
    /// - Customer support investigations
    /// 
    /// Null or empty if no special processing notes are needed.
    /// </remarks>
    public string? ProcessingNotes { get; set; }

    /// <summary>
    /// UTC timestamp when the webhook was received by our system.
    /// Used for audit trails, latency analysis, and correlating with provider logs.
    /// </summary>
    /// <remarks>
    /// Defaults to DateTime.UtcNow when the entity is created.
    /// 
    /// Uses:
    /// - Audit compliance (when did we receive this event?)
    /// - Latency analysis (compare to provider's sent timestamp if available)
    /// - Correlating webhook receipt with provider dashboard event times
    /// - Monitoring webhook delivery delays from providers
    /// 
    /// Always stored in UTC for consistency across time zones.
    /// </remarks>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
