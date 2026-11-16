using Business.DTOS;
using DataAccessLayer.Model;

namespace UI.Models
{
    public sealed class AccountAuditLogsViewModel
    {
        public PagedResult<AccountAuditLogRowDto> Paged { get; init; } = new();
        public PaginationInfo Pager { get; init; } = new();
        public string? Email { get; init; }
        public string? ActionFilter { get; init; }
    }
}
