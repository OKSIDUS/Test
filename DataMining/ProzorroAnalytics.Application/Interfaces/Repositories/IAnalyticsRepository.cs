using ProzorroAnalytics.Application.DTOs.Analytics;

namespace ProzorroAnalytics.Application.Interfaces.Repositories;

public interface IAnalyticsRepository
{
    Task<decimal> GetBudgetSavingsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TopEntryDto>> GetTopBuyersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TopEntryDto>> GetTopSuppliersAsync(CancellationToken ct = default);
}
