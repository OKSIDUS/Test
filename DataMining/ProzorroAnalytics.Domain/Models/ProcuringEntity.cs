namespace ProzorroAnalytics.Domain.Models;

public class ProcuringEntity
{
    public string Name { get; init; } = null!;
    public string? Kind { get; init; }
    public Identifier? Identifier { get; init; }
    public Address? Address { get; init; }
    public ContactPoint? ContactPoint { get; init; }
}
