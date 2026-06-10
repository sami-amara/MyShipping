using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

public partial class TbShippingType : BaseTable
{
   

    public string? ShippingTypeAname { get; set; }

    public string ShippingTypeEname { get; set; } = null!;

    public double ShippingFactor { get; set; }

   

    public virtual ICollection<TbShippment> TbShippments { get; set; } = new List<TbShippment>();
}
