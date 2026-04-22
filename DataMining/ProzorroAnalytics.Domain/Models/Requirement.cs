namespace ProzorroAnalytics.Domain.Models;

public class Requirement
{
    public string Id { get; init; } = null!;
    public string? Title { get; init; }
    public string? DataType { get; init; }
    public string? Status { get; init; }
    public List<string>? ExpectedValues { get; init; }
    public object? ExpectedValue { get; init; }
    public int? ExpectedMinItems { get; init; }
    public int? ExpectedMaxItems { get; init; }
    public DateTimeOffset? DatePublished { get; init; }
}
