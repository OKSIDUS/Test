using Microsoft.Extensions.Logging;
using ProzorroAnalytics.Application.DTOs.Prozorro;
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Domain.Models;
using ProzorroAnalytics.Infrastructure.Models.Http;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace ProzorroAnalytics.Infrastructure.ApiClients;

public class ProzorroApiClient : IProzorroApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProzorroApiClient> _logger;
    private const int MAX_CONCURRENT_REQUESTS = 10;

    public ProzorroApiClient(HttpClient httpClient, ILogger<ProzorroApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    public async Task<TenderListPage?> GetTenderListAsync(string? offset = null, CancellationToken ct = default)
    {
        var url = "tenders?descending=1";
        if (!string.IsNullOrEmpty(offset))
            url += $"&offset={Uri.EscapeDataString(offset)}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<TenderListResponse>(url, ct);

            return new TenderListPage
            {
                Items = response!.Data.Select(t => new Application.DTOs.Prozorro.TenderItem
                {
                    Id = t.Id,
                    DateModified = t.DateModified
                }).ToList(),

                Offset = response.Next_Page?.Offset
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch tender list with offset {Offset}", offset);
            return null;
        }
    }

    public async Task<IReadOnlyList<Tender?>> GetTendersAsync(List<string> ids, CancellationToken ct)
    {
        var results = new ConcurrentBag<Tender>();
        var semaphore = new SemaphoreSlim(MAX_CONCURRENT_REQUESTS); 

        var tasks = ids.Select(async id =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TenderDetailResponse>($"tenders/{id}", ct);

                if (response == null)
                    return;

                results.Add(response.Data);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to fetch tender {TenderId}", id);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results.ToList();
    }
}
