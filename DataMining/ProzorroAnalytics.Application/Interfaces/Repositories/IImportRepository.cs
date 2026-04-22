using ProzorroAnalytics.Domain.Models;

namespace ProzorroAnalytics.Application.Interfaces.Repositories;

public interface IImportRepository
{
    Task<DateTimeOffset?> GetLastSyncDateAsync(CancellationToken ct = default);
    Task UpsertTendersAsync(IEnumerable<Tender> tenders, CancellationToken ct = default);
    Task UpdateSyncStateAsync(DateTimeOffset lastTenderDateModified, CancellationToken ct = default);
}
