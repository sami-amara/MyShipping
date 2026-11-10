using Business.DTOS;
using DataAccessLayer.Model;

namespace UI.Models
{
    public sealed class AuditTimelineIndexViewModel
    {
        public PagedResult<ShipmentTimelineEventDto> Paged { get; init; } = new();
        public PaginationInfo Pager { get; init; } = new();
        public string Search { get; init; } = string.Empty;
    }
}
