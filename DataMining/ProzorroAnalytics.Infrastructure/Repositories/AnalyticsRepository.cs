using Dapper;
using ProzorroAnalytics.Application.DTOs.Analytics;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Infrastructure.Persistence;

namespace ProzorroAnalytics.Infrastructure.Repositories;

public class AnalyticsRepository(DbConnectionFactory connectionFactory) : IAnalyticsRepository
{
    public async Task<decimal> GetBudgetSavingsAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        return await conn.ExecuteScalarAsync<decimal>(
            """
            SELECT COALESCE(SUM(initial_budget - contract_amount), 0)
            FROM tender_contracts
            """);
    }

    public async Task<IReadOnlyList<TopEntryDto>> GetTopBuyersAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        var rows = await conn.QueryAsync<TopEntryDto>(
            """
            SELECT buyer_name AS name, SUM(contract_amount) AS total_amount
            FROM tender_contracts
            GROUP BY buyer_name
            ORDER BY total_amount DESC
            LIMIT 5
            """);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<TopEntryDto>> GetTopSuppliersAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        var rows = await conn.QueryAsync<TopEntryDto>(
            """
            SELECT s.supplier_name AS name, SUM(c.contract_amount) AS total_amount
            FROM tender_suppliers s
            JOIN tender_contracts c ON c.tender_id = s.tender_id
            GROUP BY s.supplier_name
            ORDER BY total_amount DESC
            LIMIT 5
            """);
        return rows.ToList();
    }
}
