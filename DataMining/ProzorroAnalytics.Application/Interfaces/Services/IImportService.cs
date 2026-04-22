namespace ProzorroAnalytics.Application.Interfaces.Services;

public interface IImportService
{
    Task ImportTendersAsync(CancellationToken ct = default);
}
