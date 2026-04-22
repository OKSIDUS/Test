using ProzorroAnalytics.Application.DTOs.Analytics;

namespace ProzorroAnalytics.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
}
