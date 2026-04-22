namespace ProzorroAnalytics.Domain.Models;

public class BidItem
{
    public string Id { get; init; } = null!;
    public string? Description { get; init; }
    public decimal? Quantity { get; init; }
    public string? Product { get; init; }
    public TenderUnit? Unit { get; init; }
}
