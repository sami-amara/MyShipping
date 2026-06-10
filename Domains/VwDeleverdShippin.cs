using System;
using System.Collections.Generic;

namespace Domains;

public partial class VwDeleverdShippin
{
    public Guid Id { get; set; }

    public DateTime ShippingDate { get; set; }

    public int CurrentState { get; set; }

    public string? Notes { get; set; }

    public string? ShippingTypeEname { get; set; }

    public string CarrierName { get; set; } = null!;
}
