using ProzorroAnalytics.Application.DTOs.Analytics;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.Application.Services;

public class AnalyticsService(IAnalyticsRepository repository) : IAnalyticsService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var savingsTask = repository.GetBudgetSavingsAsync(ct);
        var buyersTask = repository.GetTopBuyersAsync(ct);
        var suppliersTask = repository.GetTopSuppliersAsync(ct);

        await Task.WhenAll(savingsTask, buyersTask, suppliersTask);

        return new DashboardDto(
            TotalSavings: savingsTask.Result,
            TopBuyers: buyersTask.Result,
            TopSuppliers: suppliersTask.Result
        );
    }
}
