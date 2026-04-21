namespace ProzorroAnalytics.Domain.Models;

public class Classification
{
    public string Id { get; init; } = null!;
    public string? Scheme { get; init; }
    public string? Description { get; init; }
}
