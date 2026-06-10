using Business.DTOS;

namespace Business.Contracts;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardDataAsync();
}
