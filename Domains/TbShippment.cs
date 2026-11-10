using System;
using System.Collections.Generic;
using Domains;


namespace  Domains;

public partial class TbShippment : BaseTable
{
    
    public DateTime ShippingDate { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public Guid ShippingTypeId { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Weight { get; set; }

    public double Length { get; set; }

    public decimal PackageValue { get; set; }

    public decimal ShippingRate { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public Guid? UserSubscriptionId { get; set; }

    public double? TrackingNumber { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid? ShipingPackgingId { get; set; }

    public DateTime? DelivryDate { get; set; }

    public Guid? CarrierId { get; set; }

    public bool IsPaid { get; set; } = false;

    public virtual TbCarrier? Carrier { get; set; }

    public virtual TbPaymentMethod? PaymentMethod { get; set; }

    public virtual TbUserReceiver Receiver { get; set; } = null!;

    public virtual TbUserSender Sender { get; set; } = null!;

    public virtual TbShipingPackging? ShipingPackging { get; set; }

    public virtual TbShippingType ShippingType { get; set; } = null!;

    public virtual ICollection<TbShippmentStatus> TbShippmentStatuses { get; set; } = new List<TbShippmentStatus>();


    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
   
    public Guid? ReadyBy { get; set; }
    public DateTime? ReadyAt { get; set; }
   
    public Guid? ShippedBy { get; set; }
    public DateTime? ShippedAt { get; set; }

    public Guid? DeliveredBy { get; set; }
    public DateTime? DeliveredAt { get; set; }


    // Cancellation Audit
    public Guid? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; } // Optional but highly recommended

    // Return Audit
    public Guid? ReturnedBy { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ReturnReason { get; set; } // Optional but highly recommended


    // Refund Audit
    public Guid? RefundedBy { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundReason { get; set; }



}
