using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Domain.Models;
using ProzorroAnalytics.Infrastructure.Persistence;

namespace ProzorroAnalytics.Infrastructure.Repositories;

public class ProzorroRepository(DbConnectionFactory connectionFactory) : IImportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<DateTimeOffset?> GetLastSyncDateAsync(CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        var dt = await conn.ExecuteScalarAsync<DateTime?>(
            "SELECT last_tender_date_modified FROM sync_state WHERE id = 1");
        return dt.HasValue ? new DateTimeOffset(dt.Value, TimeSpan.Zero) : null;
    }

    public async Task UpsertTendersAsync(IEnumerable<Tender> tenders, CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var tender in tenders)
        {
            ct.ThrowIfCancellationRequested();

            await UpsertTenderRowAsync(conn, tx, tender);
            await UpsertContractsAsync(conn, tx, tender);
            await UpsertSuppliersAsync(conn, tx, tender);
        }

        tx.Commit();
    }

    public async Task UpdateSyncStateAsync(DateTimeOffset lastTenderDateModified, CancellationToken ct = default)
    {
        using var conn = connectionFactory.Create();
        await conn.ExecuteAsync(
            "UPDATE sync_state SET last_tender_date_modified = @LastDate, last_synced_at = NOW() WHERE id = 1",
            new { LastDate = lastTenderDateModified.ToUniversalTime() });
    }

    private static async Task UpsertTenderRowAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx, Tender tender)
    {
        var cpvCode = tender.Items.FirstOrDefault()?.Classification?.Id ?? string.Empty;
        var buyerName = tender.ProcuringEntity?.Name ?? string.Empty;
        var extraData = SerializeExtraData(tender);

        await conn.ExecuteAsync(
            """
            INSERT INTO tenders (id, cpv_code, status, initial_budget, buyer_name, date_modified, extra_data)
            VALUES (@Id, @CpvCode, @Status, @InitialBudget, @BuyerName, @DateModified, @ExtraData::jsonb)
            ON CONFLICT (id) DO UPDATE SET
                cpv_code       = EXCLUDED.cpv_code,
                status         = EXCLUDED.status,
                initial_budget = EXCLUDED.initial_budget,
                buyer_name     = EXCLUDED.buyer_name,
                date_modified  = EXCLUDED.date_modified,
                extra_data     = EXCLUDED.extra_data
            """,
            new
            {
                tender.Id,
                CpvCode = cpvCode,
                tender.Status,
                InitialBudget = tender.Value?.Amount,
                BuyerName = buyerName,
                DateModified = tender.DateModified.ToUniversalTime(),
                ExtraData = extraData
            },
            tx);
    }

    private static async Task UpsertContractsAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx, Tender tender)
    {
        foreach (var contract in tender.Contracts)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO tender_contracts (tender_id, prozorro_id, contract_amount)
                VALUES (@TenderId, @ProzorroId, @ContractAmount)
                ON CONFLICT (tender_id, prozorro_id) DO NOTHING
                """,
                new
                {
                    TenderId = tender.Id,
                    ProzorroId = contract.Id,
                    ContractAmount = contract.Value?.Amount
                },
                tx);
        }
    }

    private static async Task UpsertSuppliersAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx, Tender tender)
    {
        var supplierNames = tender.Awards
            .SelectMany(a => a.Suppliers)
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct();

        foreach (var name in supplierNames)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO tender_suppliers (tender_id, supplier_name)
                VALUES (@TenderId, @SupplierName)
                ON CONFLICT (tender_id, supplier_name) DO NOTHING
                """,
                new { TenderId = tender.Id, SupplierName = name },
                tx);
        }
    }

    private static string SerializeExtraData(Tender tender) =>
        JsonSerializer.Serialize(new
        {
            tender.TenderId,
            tender.Title,
            tender.Description,
            tender.Owner,
            tender.MainProcurementCategory,
            tender.ProcurementMethod,
            tender.ProcurementMethodType,
            tender.AwardCriteria,
            tender.Date,
            tender.DateCreated,
            tender.NoticePublicationDate,
            tender.TenderPeriod,
            tender.Agreement,
            tender.Plans,
            tender.Items,
            tender.Documents,
            tender.Milestones,
            tender.Criteria,
            tender.Bids,
            tender.Awards,
            tender.ContractChangeRationaleTypes
        }, JsonOptions);
}
