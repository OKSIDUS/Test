namespace ProzorroAnalytics.Domain.Models;

public class Contract
{
    public string Id { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTimeOffset? Date { get; init; }
    public string? AwardId { get; init; }
    public MoneyValue? Value { get; init; }
}
