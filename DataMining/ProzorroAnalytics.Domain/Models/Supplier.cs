namespace ProzorroAnalytics.Domain.Models;

public class Supplier
{
    public string Name { get; init; } = null!;
    public string? NameEn { get; init; }
    public string? Scale { get; init; }
    public Identifier? Identifier { get; init; }
    public Address? Address { get; init; }
    public ContactPoint? ContactPoint { get; init; }
}
