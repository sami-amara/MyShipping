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

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "DeliveryDateRequired", AllowEmptyStrings = false)]
        public DateTime DelivryDate { get; set; }
                        
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "SenderIdRequired", AllowEmptyStrings = false)]
        public Guid SenderId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "ReceiverIdRequired", AllowEmptyStrings = false)]
        public Guid ReceiverId { get; set; }

        //[Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CarrierIdRequired", AllowEmptyStrings = false)]
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

       // [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PaymentMethodRequired", AllowEmptyStrings = false)]
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

        // Human-friendly status string (Active/Inactive etc.). Added so server can return both numeric and text.
        public string? Status { get; set; }

        // Related entity names (for display purposes)
        public string? PackagingName { get; set; }
        public string? ShippingTypeName { get; set; }
        public string? CarrierName { get; set; }
        public string? PaymentMethodName { get; set; }

        public bool IsPaid { get; set; } = false;
    }
}
