using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOS;
using DataAccessLayer.Model;


namespace UI.Models
{
    public sealed class ShipmentsIndexViewModel
    {
        public PagedResult<ShippmentDto> Paged { get; init; } = new();
        public PaginationInfo Pager { get; init; } = new();
        public string SortBy { get; init; } = "CreatedDate";
        public string SortDir { get; init; } = "desc";
        public string Search { get; init; } = string.Empty;
        public int? StatusFilter { get; init; } = null;
        public bool? IsPaidFilter { get; init; } = null;
    }
}
