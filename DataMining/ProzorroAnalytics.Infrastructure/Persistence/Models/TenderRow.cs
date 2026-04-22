namespace ProzorroAnalytics.Infrastructure.Persistence.Models;

public class TenderRow
{
    public string Id { get; init; } = null!;
    public string CpvCode { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal? InitialBudget { get; init; }
    public string BuyerName { get; init; } = null!;
    public DateTimeOffset DateModified { get; init; }
    public string? ExtraData { get; init; }
}
