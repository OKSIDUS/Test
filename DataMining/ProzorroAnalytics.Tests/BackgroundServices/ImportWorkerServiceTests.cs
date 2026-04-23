using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProzorroAnalytics.API.BackgroundServices;
using ProzorroAnalytics.Application.Interfaces.Services;
using ProzorroAnalytics.Infrastructure.Jobs;

namespace ProzorroAnalytics.Tests.BackgroundServices;

public class ImportWorkerServiceTests
{
    private static IServiceScopeFactory CreateScopeFactory(IImportService importService)
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IImportService)))
            .Returns(importService);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        return factoryMock.Object;
    }

    [Fact]
    public async Task ExecuteAsync_CallsImportServiceWhenJobSignaled()
    {
        var queue = new ImportJobQueue();
        var importCalled = new SemaphoreSlim(0, 1);
        var importServiceMock = new Mock<IImportService>();
        importServiceMock
            .Setup(s => s.ImportTendersAsync(It.IsAny<CancellationToken>()))
            .Callback(() => importCalled.Release())
            .Returns(Task.CompletedTask);

        using var worker = new ImportWorkerService(
            queue,
            CreateScopeFactory(importServiceMock.Object),
            NullLogger<ImportWorkerService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);

        queue.TryEnqueue();
        var reached = await importCalled.WaitAsync(TimeSpan.FromSeconds(3));

        await worker.StopAsync(CancellationToken.None);

        Assert.True(reached, "ImportService was not called within the expected time.");
        importServiceMock.Verify(s => s.ImportTendersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesProcessingAfterImportFailure()
    {
        var queue = new ImportJobQueue();
        var firstJobFailed = new TaskCompletionSource();
        var secondJobDone = new TaskCompletionSource();
        int callCount = 0;

        var importServiceMock = new Mock<IImportService>();
        importServiceMock
            .Setup(s => s.ImportTendersAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    firstJobFailed.TrySetResult();
                    throw new InvalidOperationException("Simulated import failure");
                }
                secondJobDone.TrySetResult();
                return Task.CompletedTask;
            });

        using var worker = new ImportWorkerService(
            queue,
            CreateScopeFactory(importServiceMock.Object),
            NullLogger<ImportWorkerService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);

        queue.TryEnqueue();
        await firstJobFailed.Task.WaitAsync(TimeSpan.FromSeconds(3));

        queue.TryEnqueue();
        await secondJobDone.Task.WaitAsync(TimeSpan.FromSeconds(3));

        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ExitsCleanlyOnCancellation()
    {
        var queue = new ImportJobQueue();
        var importServiceMock = new Mock<IImportService>();

        using var worker = new ImportWorkerService(
            queue,
            CreateScopeFactory(importServiceMock.Object),
            NullLogger<ImportWorkerService>.Instance);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        importServiceMock.Verify(
            s => s.ImportTendersAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
