using System;
using System.Collections.Generic;
using Domains;

namespace Domains;

public partial class TbCountry : BaseTable
{
   

    public string? CountryAname { get; set; }

    public string? CountryEname { get; set; }

    public virtual ICollection<TbCity> TbCities { get; set; } = new List<TbCity>();
}
