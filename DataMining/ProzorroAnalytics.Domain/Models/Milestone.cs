namespace ProzorroAnalytics.Domain.Models;

public class Milestone
{
    public string Id { get; init; } = null!;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Type { get; init; }
    public string? Code { get; init; }
    public decimal? Percentage { get; init; }
    public int? SequenceNumber { get; init; }
    public MilestoneDuration? Duration { get; init; }
}
