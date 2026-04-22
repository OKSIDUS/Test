using Microsoft.Extensions.Logging;
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Options;
using ProzorroAnalytics.Domain.Models;

namespace ProzorroAnalytics.Application.Services;

public class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IProzorroApiClient _apiClient;
    private readonly IImportRepository _importRepository;
    private readonly FilterOptions _filterOptions;

    public ImportService(
        IProzorroApiClient apiClient,
        IImportRepository importRepository,
        ILogger<ImportService> logger,
        FilterOptions filterOptions)
    {
        _apiClient = apiClient;
        _importRepository = importRepository;
        _logger = logger;
        _filterOptions = filterOptions;
    }

    public async Task ImportTendersAsync(CancellationToken ct = default)
    {
        var lastSyncDate = await _importRepository.GetLastSyncDateAsync(ct);
        var cutoff = lastSyncDate ?? DateTime.UtcNow.AddMonths(-1);

        _logger.LogInformation("Starting import. Cutoff: {Cutoff}", cutoff);

        string? offset = null;
        int totalPersisted = 0;
        DateTimeOffset? newestDate = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var page = await _apiClient.GetTenderListAsync(offset, ct);

            if (page is null || page.Items.Count == 0)
                break;

            var idsToFetch = page.Items
                .Where(x => x.DateModified.ToUniversalTime() >= cutoff.UtcDateTime)
                .Select(x => x.Id)
                .ToList();

            if (idsToFetch.Count > 0)
            {
                var tenders = (await _apiClient.GetTendersAsync(idsToFetch, ct))
                    .Where(t => t is not null)
                    .Select(t => t!)
                    .ToList();

                var filtered = tenders
                    .Where(t => t.Status == _filterOptions.TargetStatus)
                    .Where(t => t.Items.Any(i => i.Classification?.Id == _filterOptions.TargetCpv))
                    .ToList();

                if (filtered.Count > 0)
                {
                    await _importRepository.UpsertTendersAsync(filtered, ct);
                    totalPersisted += filtered.Count;

                    var pageNewest = filtered.Max(t => t.DateModified);
                    if (newestDate is null || pageNewest > newestDate)
                        newestDate = pageNewest;
                }

                _logger.LogInformation(
                    "Page offset={Offset}: fetched {Total}, matched {Matched}",
                    offset ?? "initial", tenders.Count, filtered.Count);
            }

            if (page.Items.All(x => x.DateModified.ToUniversalTime() < cutoff.UtcDateTime))
            {
                _logger.LogInformation("Reached cutoff date. Stopping pagination.");
                break;
            }

            if (string.IsNullOrEmpty(page.Offset))
                break;

            offset = page.Offset;
        }

        if (newestDate is null)
        {
            _logger.LogInformation("No tenders matched filters. Nothing to persist.");
            return;
        }

        await _importRepository.UpdateSyncStateAsync(newestDate.Value, ct);

        _logger.LogInformation("Import complete. Persisted {Count} tenders.", totalPersisted);
    }
}
