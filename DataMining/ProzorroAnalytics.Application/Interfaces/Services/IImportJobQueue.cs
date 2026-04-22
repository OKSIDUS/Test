namespace ProzorroAnalytics.Application.Interfaces.Services;

public interface IImportJobQueue
{
    bool TryEnqueue();
    ValueTask WaitForJobAsync(CancellationToken ct);
}
