using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using AppResource; // Assuming your resource class is here
using Business.Payments.Shared;


namespace Business.DTOS
{
    public partial class ShippmentDto : BaseDto
    {
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ShippingDateRequired", AllowEmptyStrings = false)]
        public DateTime ShippingDate { get; set; } = DateTime.UtcNow;

        //[Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "DeliveryDateRequired", AllowEmptyStrings = false)]
        public DateTime DelivryDate { get; set; }
        

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "SenderIdRequired", AllowEmptyStrings = false)]
        public Guid SenderId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ReceiverIdRequired", AllowEmptyStrings = false)]
        public Guid ReceiverId { get; set; }

        public Guid? CarrierId { get; set; }
                     

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ShippingTypeIdRequired", AllowEmptyStrings = false)]
        public Guid? ShippingTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PackagingIdRequired", AllowEmptyStrings = false)]
        public Guid? ShipingPackgingId { get; set; }

        [Range(0.1, 100, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "WidthInvalid")]
        public double Width { get; set; }

        [Range(0.1, 100, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "HeightInvalid")]
        public double Height { get; set; }

        [Range(0.1, 100, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "WeightInvalid")]
        public double Weight { get; set; }

        [Range(0.1, 100, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "LengthInvalid")]
        public double Length { get; set; }

        [Range(0.01, 10000, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PackageValueInvalid")]
        public decimal PackageValue { get; set; }
       

        [Range(0.01, double.MaxValue, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ShippingRateInvalid")]
        public decimal? ShippingRate { get; set; }

        public Guid? PaymentMethodId { get; set; }

        public string? PaymentMethodToken { get; set; }

        public Guid? UserSubscriptionId { get; set; }

        [Range(1, double.MaxValue, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "TrackingNumberInvalid")]
        public double? TrackingNumber { get; set; }

        public Guid? ReferenceId { get; set; }

        public UserSenderDto UserSender { get; set; }
        public UserReceiverDto UserReceiver { get; set; }

        // Payment transaction data (for PayPal redirect flow)
        public PaymentTransactionDto? PaymentTransaction { get; set; }

        public string? Status { get; set; }

        // Related entity names (for display purposes)
        public string? PackagingName { get; set; }
        public string? ShippingTypeName { get; set; }
        public string? CarrierName { get; set; }
        public string? PaymentMethodName { get; set; }

        public bool IsPaid { get; set; } = false;


        // Audit trail
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByUserName { get; set; }

        public DateTime? ReadyAt { get; set; }
        public Guid? ReadyBy { get; set; }
        public string? ReadyByUserName { get; set; }

        public DateTime? ShippedAt { get; set; }
        public Guid? ShippedBy { get; set; }
        public string? ShippedByUserName { get; set; }

        public DateTime CreatedDate { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? CreatedByUserName { get; set; }


        public Guid? DeliveredBy { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveredByUserName { get; set; }


        public Guid? CancelledBy { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelledByUserName { get; set; }
        public string? CancellationReason { get; set; }

        public Guid? ReturnedBy { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? ReturnedByUserName { get; set; }
        public string? ReturnReason { get; set; }


        public DateTime? DeletedDate { get; set; }
        public Guid? DeletedBy { get; set; }
        public string? DeletedByUserName { get; set; }


        public Guid? RefundedBy { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        public string? RefundedByUserName { get; set; } // 👈 For display only, no migration needed

    }
}
