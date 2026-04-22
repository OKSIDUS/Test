using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.API.BackgroundServices;

public sealed class ImportWorkerService : BackgroundService
{
    private readonly IImportJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportWorkerService> _logger;

    public ImportWorkerService(
        IImportJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ImportWorkerService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _queue.WaitForJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

                _logger.LogInformation("Background import starting.");
                await importService.ImportTendersAsync(stoppingToken);
                _logger.LogInformation("Background import completed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Import cancelled due to host shutdown.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background import failed.");
            }
        }
    }
}
