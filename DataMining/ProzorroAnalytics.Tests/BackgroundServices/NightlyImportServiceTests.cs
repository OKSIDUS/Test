using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ProzorroAnalytics.API.BackgroundServices;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Application.Options;

namespace ProzorroAnalytics.Tests.BackgroundServices;

public class NightlyImportServiceTests
{
    private static IOptionsMonitor<NightlyImportOptions> CreateOptions(int utcHour)
    {
        var mock = new Mock<IOptionsMonitor<NightlyImportOptions>>();
        mock.Setup(o => o.CurrentValue).Returns(new NightlyImportOptions { UtcHour = utcHour });
        return mock.Object;
    }

    [Fact]
    public async Task ExecuteAsync_ExitsCleanlyOnCancellationWithoutEnqueueing()
    {
        var queueMock = new Mock<IImportJobQueue>();

        using var service = new NightlyImportService(
            queueMock.Object,
            CreateOptions(utcHour: 2),
            NullLogger<NightlyImportService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        queueMock.Verify(q => q.TryEnqueue(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotThrowDuringNormalStartStop()
    {
        var queueMock = new Mock<IImportJobQueue>();
        queueMock.Setup(q => q.TryEnqueue()).Returns(true);

        using var service = new NightlyImportService(
            queueMock.Object,
            CreateOptions(utcHour: 3),
            NullLogger<NightlyImportService>.Instance);

        using var cts = new CancellationTokenSource();
        var exception = await Record.ExceptionAsync(async () =>
        {
            await service.StartAsync(cts.Token);
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        });

        Assert.Null(exception);
    }
}
