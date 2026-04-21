using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ProzorroAnalytics.Application.DTOs.Prozorro;
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Infrastructure.Models.Http;

namespace ProzorroAnalytics.Infrastructure.ApiClients;

public class ProzorroApiClient(HttpClient httpClient, ILogger<ProzorroApiClient> logger) : IProzorroApiClient
{
    public async Task<TenderListPage?> GetTenderListAsync(string? offset = null, CancellationToken ct = default)
    {
        var url = "tenders?descending=1";
        if (!string.IsNullOrEmpty(offset))
            url += $"&offset={Uri.EscapeDataString(offset)}";

        try
        {
            var response = await httpClient.GetFromJsonAsync<TenderListResponse>(url, ct);

            return new TenderListPage
            {
                Items = response.Data.Select(t => new Application.DTOs.Prozorro.TenderItem
                {
                    Id = t.Id,
                    DateModified = t.DateModified
                }).ToList(),

                Offset = response.Next_Page?.Offset
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch tender list with offset {Offset}", offset);
            return null;
        }
    }
}
