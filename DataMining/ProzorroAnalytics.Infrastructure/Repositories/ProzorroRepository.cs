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
        var list = tenders.ToList();
        if (list.Count == 0) return;

        ct.ThrowIfCancellationRequested();

        var ids        = list.Select(t => t.Id).ToArray();
        var cpvCodes   = list.Select(t => t.Items.FirstOrDefault()?.Classification?.Id ?? "").ToArray();
        var statuses   = list.Select(t => t.Status).ToArray();
        var budgets    = list.Select(t => t.Value?.Amount).ToArray();
        var buyerNames = list.Select(t => t.ProcuringEntity?.Name ?? "").ToArray();
        var dateMods   = list.Select(t => t.DateModified.ToUniversalTime().DateTime).ToArray();
        var extraData  = list.Select(SerializeExtraData).ToArray();

        var contracts = list
            .SelectMany(t => t.Contracts.Select(c => (
                TenderId:      t.Id,
                ProzorroId:    c.Id,
                c.Value?.Amount,
                BuyerName:     t.ProcuringEntity?.Name ?? "",
                InitialBudget: t.Value?.Amount
            )))
            .ToList();

        var suppliers = list
            .SelectMany(t => t.Awards
                .SelectMany(a => a.Suppliers)
                .Select(s => s.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .Select(name => (TenderId: t.Id, Name: name)))
            .Distinct()
            .ToList();

        using var conn = connectionFactory.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            """
            INSERT INTO tenders (id, cpv_code, status, initial_budget, buyer_name, date_modified, extra_data)
            SELECT
                unnest(@Ids::text[]),
                unnest(@CpvCodes::text[]),
                unnest(@Statuses::text[]),
                unnest(@Budgets::numeric[]),
                unnest(@BuyerNames::text[]),
                unnest(@DateMods::timestamptz[]),
                unnest(@ExtraData::text[])::jsonb
            ON CONFLICT (id) DO UPDATE SET
                cpv_code       = EXCLUDED.cpv_code,
                status         = EXCLUDED.status,
                initial_budget = EXCLUDED.initial_budget,
                buyer_name     = EXCLUDED.buyer_name,
                date_modified  = EXCLUDED.date_modified,
                extra_data     = EXCLUDED.extra_data
            """,
            new { Ids = ids, CpvCodes = cpvCodes, Statuses = statuses, Budgets = budgets,
                  BuyerNames = buyerNames, DateMods = dateMods, ExtraData = extraData },
            tx);

        if (contracts.Count > 0)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO tender_contracts (tender_id, prozorro_id, contract_amount, buyer_name, initial_budget)
                SELECT
                    unnest(@TenderIds::text[]),
                    unnest(@ProzorroIds::text[]),
                    unnest(@Amounts::numeric[]),
                    unnest(@BuyerNames::text[]),
                    unnest(@InitialBudgets::numeric[])
                ON CONFLICT (tender_id, prozorro_id) DO NOTHING
                """,
                new
                {
                    TenderIds      = contracts.Select(c => c.TenderId).ToArray(),
                    ProzorroIds    = contracts.Select(c => c.ProzorroId).ToArray(),
                    Amounts        = contracts.Select(c => c.Amount).ToArray(),
                    BuyerNames     = contracts.Select(c => c.BuyerName).ToArray(),
                    InitialBudgets = contracts.Select(c => c.InitialBudget).ToArray()
                },
                tx);
        }

        if (suppliers.Count > 0)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO tender_suppliers (tender_id, supplier_name)
                SELECT unnest(@TenderIds::text[]), unnest(@Names::text[])
                ON CONFLICT (tender_id, supplier_name) DO NOTHING
                """,
                new
                {
                    TenderIds = suppliers.Select(s => s.TenderId).ToArray(),
                    Names     = suppliers.Select(s => s.Name).ToArray()
                },
                tx);
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
