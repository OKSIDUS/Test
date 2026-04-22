namespace ProzorroAnalytics.Domain.Models;

public class Award
{
    public string Id { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTimeOffset Date { get; init; }
    public MoneyValue? Value { get; init; }
    public List<Supplier> Suppliers { get; init; } = [];
}
