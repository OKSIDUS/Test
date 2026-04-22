namespace ProzorroAnalytics.Domain.Models;

public class Identifier
{
    public string? Scheme { get; init; }
    public string Id { get; init; } = null!;
    public string? LegalName { get; init; }
    public string? LegalNameEn { get; init; }
}
