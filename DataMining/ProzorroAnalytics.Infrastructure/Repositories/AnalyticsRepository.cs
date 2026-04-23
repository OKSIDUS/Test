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
            "SELECT total_savings FROM analytics_summary WHERE id = 1");
    }

    public async Task<IReadOnlyList<TopEntryDto>> GetTopBuyersAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        var rows = await conn.QueryAsync<TopEntryDto>(
            "SELECT buyer_name AS name, total_amount FROM analytics_buyers ORDER BY total_amount DESC LIMIT 5");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<TopEntryDto>> GetTopSuppliersAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        var rows = await conn.QueryAsync<TopEntryDto>(
            "SELECT supplier_name AS name, total_amount FROM analytics_suppliers ORDER BY total_amount DESC LIMIT 5");
        return rows.ToList();
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO analytics_summary (id, total_savings, last_updated_at)
            VALUES (1,
                COALESCE((SELECT SUM(initial_budget - contract_amount) FROM tender_contracts), 0),
                NOW())
            ON CONFLICT (id) DO UPDATE SET
                total_savings   = EXCLUDED.total_savings,
                last_updated_at = EXCLUDED.last_updated_at
            """, transaction: tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO analytics_buyers (buyer_name, total_amount)
            SELECT buyer_name, SUM(contract_amount)
            FROM tender_contracts
            GROUP BY buyer_name
            ON CONFLICT (buyer_name) DO UPDATE SET
                total_amount = EXCLUDED.total_amount;
            """, transaction: tx);

        await conn.ExecuteAsync(
            """
            INSERT INTO analytics_suppliers (supplier_name, total_amount)
            SELECT s.supplier_name, SUM(c.contract_amount)
            FROM tender_suppliers s
            JOIN tender_contracts c ON c.tender_id = s.tender_id
            GROUP BY s.supplier_name
            ON CONFLICT (supplier_name) DO UPDATE SET
                total_amount = EXCLUDED.total_amount;
            """, transaction: tx);

        tx.Commit();
    }
}
