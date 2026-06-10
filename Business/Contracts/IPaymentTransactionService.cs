using Business.DTOS;
using Business.Payments.PayPal;
using Business.Payments.Stripe;
using Business.Payments.Shared;
using DataAccessLayer.Model;
using Domains;


namespace Business.Contracts
{
    /// <summary>
    /// Payment transaction service for processing payments, managing transaction records,
    /// and handling webhook events with idempotency and reconciliation.
    /// </summary>
    /// <remarks>
    /// This service orchestrates payment operations by:
    /// - Coordinating with payment gateways (Stripe, PayPal) via IPaymentGatewayFactory
    /// - Persisting payment transactions to the database with idempotency keys
    /// - Recording webhook events for audit trails and replay protection
    /// - Reconciling transactions from asynchronous webhook notifications
    /// - Providing payment history and admin reporting capabilities
    /// 
    /// Key features:
    /// - **Idempotency**: Prevents duplicate charges using shipment+paymentMethod composite keys
    /// - **Webhook deduplication**: Tracks provider event IDs to skip already-processed webhooks
    /// - **Transaction reconciliation**: Updates transaction status from webhook data
    /// - **Audit trails**: Full event logging with payloads, timestamps, and processing notes
    /// 
    /// This is a production-grade service used in real shipment payment flows.
    /// </remarks>
    public interface IPaymentTransactionService : IBaseService<TbPaymentTransaction, PaymentTransactionDto>
    {
        /// <summary>
        /// Processes a payment for a shipment through the appropriate payment gateway.
        /// Implements idempotency to prevent duplicate charges for the same shipment+payment method.
        /// </summary>
        /// <param name="shipmentId">Unique identifier of the shipment being paid for</param>
        /// <param name="paymentMethodId">Payment method ID from TbPaymentMethod table</param>
        /// <param name="shippingRate">Base shipping rate before payment method commission</param>
        /// <param name="paymentMethodToken">
        /// Payment token from frontend (Stripe token, PayPal order ID, etc.).
        /// Optional for testing, but required for real payment processing.
        /// </param>
        /// <returns>
        /// PaymentTransactionDto containing transaction details, reference number, and status.
        /// Throws InvalidOperationException if payment fails.
        /// </returns>
        /// <remarks>
        /// Processing flow:
        /// 1. Check idempotency: return existing transaction if already processed for this shipment+payment method
        /// 2. Calculate total amount including payment method commission
        /// 3. Select appropriate payment gateway via IPaymentGatewayFactory
        /// 4. Call gateway's ProcessPayment with amount, currency, and token
        /// 5. Persist transaction to database with provider name, event ID, and idempotency key
        /// 6. Return transaction DTO with reference number for user display
        /// 
        /// Idempotency key format: "ship:{shipmentId}:pm:{paymentMethodId}"
        /// This prevents charging the same shipment twice if retry/refresh occurs.
        /// </remarks>
        Task<PaymentTransactionDto> ProcessPayment(Guid shipmentId, Guid paymentMethodId, decimal shippingRate, string? paymentMethodToken = null);

        /// <summary>
        /// Creates a PayPal order for a shipment payment.
        /// </summary>
        Task<PaymentOrchestrationResult> CreateOrderAsync(PaymentOrderRequest request);

        /// <summary>
        /// Captures a PayPal order and completes the payment.
        /// </summary>
        Task<PaymentOrchestrationResult> CaptureOrderAsync(PaymentCaptureRequest request);

        /// <summary>
        /// Creates a Stripe PaymentIntent for a shipment payment.
        /// </summary>
        Task<PaymentOrchestrationResult> CreateStripeIntentAsync(StripePaymentIntentRequest request);

        /// <summary>
        /// Captures a Stripe payment and marks shipment as paid.
        /// </summary>
        Task<PaymentOrchestrationResult> CaptureStripeAsync(StripeCaptureRequest request);

        /// <summary>
        /// Retrieves the payment transaction associated with a specific shipment.
        /// </summary>
        /// <param name="shipmentId">Unique identifier of the shipment</param>
        /// <returns>PaymentTransactionDto if a transaction exists for the shipment; null otherwise</returns>
        Task<PaymentTransactionDto?> GetByShipmentId(Guid shipmentId);

        /// <summary>
        /// Retrieves paginated payment history for the current or specified user's shipments.
        /// </summary>
        /// <param name="pageNumber">Page number (1-based indexing)</param>
        /// <param name="pageSize">Number of transactions per page</param>
        /// <param name="userId">Optional user ID; uses current authenticated user if null</param>
        /// <returns>Paged list of payment transactions ordered by date descending</returns>
        /// <remarks>
        /// Includes transaction status, payment method name, shipment tracking number,
        /// and transaction reference for display in user payment history.
        /// </remarks>
        Task<PagedResult<PaymentTransactionDto>> GetUserPaymentHistory(int pageNumber = 1, int pageSize = 10, Guid? userId = null);

        /// <summary>
        /// Retrieves a specific payment transaction by its unique identifier.
        /// </summary>
        /// <param name="id">Transaction ID (primary key)</param>
        /// <returns>PaymentTransactionDto if found; null otherwise</returns>
        Task<PaymentTransactionDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// Processes a full refund for a shipment's completed PayPal payment transaction.
        /// </summary>
        /// <param name="shipmentId">Shipment ID whose completed payment should be refunded</param>
        /// <param name="reason">Human-readable reason for the refund (stored in notes)</param>
        /// <returns>Updated PaymentTransactionDto with refunded status and refund metadata</returns>
        /// <remarks>
        /// Refund flow:
        /// 1. Retrieve original transaction for the shipment
        /// 2. Validate PayPal/completed/not-yet-refunded conditions
        /// 3. Call payment gateway's ProcessRefund method for a full refund
        /// 4. Update transaction status to Refunded and store audit details
        /// 5. Mark shipment IsPaid = false
        /// </remarks>
        Task<PaymentTransactionDto> RefundPayment(Guid shipmentId, string reason);

        /// <summary>
        /// Retrieves all payment transactions with filtering and pagination for admin reporting.
        /// </summary>
        /// <param name="pageNumber">Page number (1-based indexing)</param>
        /// <param name="pageSize">Number of transactions per page</param>
        /// <param name="status">Filter by TransactionStatus enum value (optional)</param>
        /// <param name="paymentMethodId">Filter by specific payment method (optional)</param>
        /// <param name="searchTerm">Search in tracking numbers or transaction references (optional)</param>
        /// <param name="startDate">Filter transactions on or after this date (optional)</param>
        /// <param name="endDate">Filter transactions on or before this date (optional)</param>
        /// <returns>Paged list of payment transactions matching filter criteria</returns>
        /// <remarks>
        /// Designed for admin dashboard and reporting.
        /// Supports multi-dimensional filtering for finding specific transactions.
        /// Results include joined data: payment method names, shipment tracking numbers, etc.
        /// </remarks>
        Task<PagedResult<PaymentTransactionDto>> GetAllPaymentTransactions(
            int pageNumber = 1, 
            int pageSize = 10,
            int? status = null,
            Guid? paymentMethodId = null,
            string? searchTerm = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Checks if a webhook event from a payment provider has already been processed.
        /// Prevents duplicate processing of the same webhook event.
        /// </summary>
        /// <param name="providerName">Payment provider name (e.g., "Stripe", "PayPal")</param>
        /// <param name="providerEventId">Unique event ID from the payment provider</param>
        /// <returns>True if the event has already been processed; false if it's new</returns>
        /// <remarks>
        /// Used for webhook deduplication. Payment providers may retry webhook deliveries,
        /// so checking for duplicates prevents double-processing transactions.
        /// Queries TbPaymentWebhookEvent table by provider name and event ID.
        /// </remarks>
        Task<bool> IsWebhookEventProcessed(string providerName, string providerEventId);

        /// <summary>
        /// Records a webhook event in the database for audit and replay protection.
        /// </summary>
        /// <param name="providerName">Payment provider name (e.g., "Stripe", "PayPal")</param>
        /// <param name="providerEventId">Unique event ID from the payment provider</param>
        /// <param name="eventType">Type of event (e.g., "payment_intent.succeeded", "PAYMENT.CAPTURE.COMPLETED")</param>
        /// <param name="transactionReference">Associated transaction ID or reference from provider</param>
        /// <param name="payload">Full JSON payload from the webhook for debugging</param>
        /// <returns>Task representing the async operation</returns>
        /// <remarks>
        /// Creates a TbPaymentWebhookEvent record with:
        /// - ReceivedAt timestamp for audit trails
        /// - IsProcessed flag (initially false)
        /// - Raw payload for debugging/investigation
        /// 
        /// Should be called immediately after webhook signature validation succeeds.
        /// Skipped if IsWebhookEventProcessed returns true (already recorded).
        /// </remarks>
        Task RecordWebhookEvent(string providerName, string providerEventId, string? eventType, string? transactionReference, string payload);

        /// <summary>
        /// Marks a webhook event as processed and optionally stores processing notes.
        /// </summary>
        /// <param name="providerName">Payment provider name (e.g., "Stripe", "PayPal")</param>
        /// <param name="providerEventId">Unique event ID from the payment provider</param>
        /// <param name="isProcessed">True if successfully processed; false if processing failed</param>
        /// <param name="processingNotes">Optional notes about processing result (errors, skipped reasons, etc.)</param>
        /// <returns>Task representing the async operation</returns>
        /// <remarks>
        /// Updates TbPaymentWebhookEvent record:
        /// - Sets IsProcessed flag
        /// - Records ProcessedAt timestamp
        /// - Stores processing notes for debugging
        /// 
        /// Should be called after ReconcileTransactionFromWebhook completes,
        /// regardless of success or failure, to mark the event as handled.
        /// </remarks>
        Task MarkWebhookEventProcessed(string providerName, string providerEventId, bool isProcessed, string? processingNotes = null);

        /// <summary>
        /// Reconciles a payment transaction by updating its status based on webhook event data.
        /// </summary>
        /// <param name="providerName">Payment provider name (e.g., "Stripe", "PayPal")</param>
        /// <param name="providerEventId">Unique event ID from the payment provider</param>
        /// <param name="eventType">Type of event indicating status change</param>
        /// <param name="transactionReference">Transaction ID or reference from the provider to match in database</param>
        /// <param name="payload">Full JSON payload for extracting status and metadata</param>
        /// <returns>True if reconciliation succeeded (transaction found and updated); false otherwise</returns>
        /// <remarks>
        /// Reconciliation flow:
        /// 1. Find TbPaymentTransaction by TransactionReference matching provider's transaction ID
        /// 2. Parse webhook payload to extract new payment status
        /// 3. Map provider status to internal TransactionStatus enum
        /// 4. Update transaction record if status changed
        /// 5. Store provider event ID for linking transaction to webhook
        /// 
        /// Returns false if:
        /// - No matching transaction found (may be an event for a different system)
        /// - Payload parsing fails
        /// - Status mapping fails
        /// 
        /// This enables asynchronous payment status updates (e.g., PayPal approval, Stripe disputes).
        /// </remarks>
        Task<bool> ReconcileTransactionFromWebhook(string providerName, string providerEventId, string? eventType, string? transactionReference, string payload);

        /// <summary>
        /// Reconciles a payment transaction after PayPal callback/approval.
        /// Updates transaction status based on capture result.
        /// </summary>
        /// <param name="paypalOrderId">PayPal order ID (stored as TransactionReference during order creation)</param>
        /// <param name="captureResult">Result from capturing the PayPal order</param>
        /// <returns>Task representing the async operation</returns>
        /// <remarks>
        /// Callback reconciliation flow:
        /// 1. Find transaction by TransactionReference matching PayPal order ID
        /// 2. Update status to Completed if capture succeeded
        /// 3. Update TransactionReference to capture ID (different from order ID)
        /// 4. Set ProcessedDate to capture timestamp
        /// 5. Store capture details in Notes
        /// 
        /// Called from PayPalCallbackController after user approves payment on PayPal.
        /// Unlike webhook reconciliation, this is synchronous (user waiting for redirect).
        /// </remarks>
        Task ReconcileTransactionFromCallback(string paypalOrderId, Business.Payments.Gateway.PaymentResult captureResult);

        /// <summary>
        /// Retrieves a payment transaction by its transaction reference (PayPal order ID or capture ID).
        /// </summary>
        /// <param name="transactionReference">Transaction reference from payment provider</param>
        /// <returns>PaymentTransactionDto if found; null otherwise</returns>
        Task<PaymentTransactionDto?> GetByTransactionReferenceAsync(string transactionReference);
    }
}
