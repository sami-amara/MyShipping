using System;

namespace Business.DTOS
{
    /// <summary>
    /// Payment Transaction DTO - Used to transfer payment transaction data between layers
    /// </summary>
    public class PaymentTransactionDto : Business.DTOS.BaseDto
    {
        /// <summary>
        /// Reference to the shipment being paid for
        /// </summary>
        public Guid ShipmentId { get; set; }

        /// <summary>
        /// Payment method used for this transaction
        /// </summary>
        public Guid PaymentMethodId { get; set; }

        /// <summary>
        /// Base shipping cost before commission
        /// </summary>
        public decimal ShippingRate { get; set; }

        /// <summary>
        /// Commission percentage charged by payment method
        /// </summary>
        public double CommissionPercentage { get; set; }

        /// <summary>
        /// Calculated commission amount in dollars
        /// </summary>
        public decimal CommissionAmount { get; set; }

        /// <summary>
        /// Total amount charged (ShippingRate + CommissionAmount)
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Transaction status: 0=Pending, 1=Completed, 2=Failed, 3=Refunded
        /// </summary>
        public int TransactionStatus { get; set; }

        /// <summary>
        /// Human-readable status name for display
        /// </summary>
        public string? TransactionStatusName { get; set; }

        /// <summary>
        /// Transaction reference number from payment gateway
        /// Format: TXN-{timestamp}-{random}
        /// </summary>
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Date/time when payment was processed
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Date/time when transaction was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Error message if transaction failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional notes about the transaction
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Additional information from payment gateway (e.g., PayPal approval URL, Stripe PaymentIntent ID)
        /// </summary>
        public string? AdditionalInfo { get; set; }

        // For display purposes
        public string? PaymentMethodName { get; set; }
        public string? ShipmentTrackingNumber { get; set; }
    }
}
