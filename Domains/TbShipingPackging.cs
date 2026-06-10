using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

public partial class TbShipingPackging : BaseTable
{
    

    public string? TbShipingPackginAname { get; set; }
    public string? TbShipingPackginEname { get; set; }
    public virtual ICollection<TbShippment> TbShippments { get; set; } = new List<TbShippment>();

}
