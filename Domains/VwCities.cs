using System;
using System.Collections.Generic;

namespace Domains;

public partial class VwCities
{
    public Guid Id { get; set; }

    public string? CityAname { get; set; }

    public string? CityEname { get; set; }

    public Guid? UpdatedBy { get; set; }

    public int CurrentState { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid CountryId { get; set; }

    public string? CountryAname { get; set; }

    public string? CountryEname { get; set; }
}
