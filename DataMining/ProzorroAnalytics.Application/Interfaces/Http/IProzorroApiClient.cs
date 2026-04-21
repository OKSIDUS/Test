using ProzorroAnalytics.Application.DTOs.Prozorro;

namespace ProzorroAnalytics.Application.Interfaces.Http;

public interface IProzorroApiClient
{
    Task<TenderListPage?> GetTenderListAsync(string? offset = null, CancellationToken ct = default);
}