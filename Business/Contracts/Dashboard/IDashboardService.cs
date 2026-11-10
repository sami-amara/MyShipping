using Business.DTOS;

namespace Business.Contracts.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardDataAsync();
}
