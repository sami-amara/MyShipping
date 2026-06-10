using System;
using System.Collections.Generic;

namespace  Domains;

public partial class Testing
{
    public string? CarrierName { get; set; }

    public int? CurrentState { get; set; }

    public Guid? Id { get; set; }

    public DateTime? ShippingDate { get; set; }

    public Guid? SenderId { get; set; }

    public Guid? ShippingTypeId { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedDate { get; set; }
}
