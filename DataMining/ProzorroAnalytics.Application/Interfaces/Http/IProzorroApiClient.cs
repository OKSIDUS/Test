using ProzorroAnalytics.Application.DTOs.Prozorro;
using ProzorroAnalytics.Domain.Models;

namespace ProzorroAnalytics.Application.Interfaces.Http;

public interface IProzorroApiClient
{
    Task<TenderListPage?> GetTenderListAsync(string? offset = null, CancellationToken ct = default);
    Task<IReadOnlyList<Tender?>> GetTendersAsync(List<string> ids, CancellationToken ct = default);
}