using Microsoft.Extensions.Options;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Options;

namespace ProzorroAnalytics.API.BackgroundServices;

public sealed class NightlyImportService : BackgroundService
{
    private readonly IImportJobQueue _queue;
    private readonly IOptionsMonitor<NightlyImportOptions> _options;
    private readonly ILogger<NightlyImportService> _logger;

    public NightlyImportService(
        IImportJobQueue queue,
        IOptionsMonitor<NightlyImportOptions> options,
        ILogger<NightlyImportService> logger)
    {
        _queue = queue;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var targetHour = _options.CurrentValue.UtcHour;
            var delay = CalculateDelayUntilNextUtcHour(targetHour);

            _logger.LogInformation(
                "Nightly import scheduled in {Delay:hh\\:mm\\:ss} (next UTC {Hour:D2}:00).",
                delay, targetHour);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var enqueued = _queue.TryEnqueue();
            _logger.LogInformation(
                enqueued
                    ? "Nightly trigger: import job enqueued."
                    : "Nightly trigger: import already queued or running, skipped.");
        }
    }

    private static TimeSpan CalculateDelayUntilNextUtcHour(int targetHour)
    {
        var now = DateTime.UtcNow;
        var next = now.Date.AddHours(targetHour);
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }
}
