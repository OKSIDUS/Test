namespace ProzorroAnalytics.Domain.Models;

public class TenderItem
{
    public string Id { get; init; } = null!;
    public string? Description { get; init; }
    public decimal? Quantity { get; init; }
    public string? Profile { get; init; }
    public string? Category { get; init; }
    public string? Product { get; init; }
    public Classification? Classification { get; init; }
    public TenderUnit? Unit { get; init; }
    public DateTimeOffset? DeliveryEndDate { get; init; }
    public Address? DeliveryAddress { get; init; }
}
