using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

public partial class TbShippmentStatus : BaseTable
{
   
    public Guid? ShippmentId { get; set; }

    public string? Notes { get; set; }

    public virtual TbShippment? Shippment { get; set; }
}
