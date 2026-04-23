using ProzorroAnalytics.Infrastructure.Jobs;

namespace ProzorroAnalytics.Tests.Jobs;

public class ImportJobQueueTests
{
    [Fact]
    public void TryEnqueue_ReturnsTrueWhenQueueIsEmpty()
    {
        var queue = new ImportJobQueue();

        var result = queue.TryEnqueue();

        Assert.True(result);
    }


    [Fact]
    public async Task WaitForJobAsync_CompletesAfterEnqueue()
    {
        var queue = new ImportJobQueue();
        queue.TryEnqueue();

        await queue.WaitForJobAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TryEnqueue_AllowsReEnqueueAfterJobConsumed()
    {
        var queue = new ImportJobQueue();
        queue.TryEnqueue();
        await queue.WaitForJobAsync(CancellationToken.None); 

        var result = queue.TryEnqueue();

        Assert.True(result);
    }

    [Fact]
    public async Task WaitForJobAsync_ThrowsOperationCanceledWhenTokenCancelled()
    {
        var queue = new ImportJobQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queue.WaitForJobAsync(cts.Token).AsTask());
    }
}
