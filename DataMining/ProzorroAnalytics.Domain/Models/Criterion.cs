namespace ProzorroAnalytics.Domain.Models;

public class Criterion
{
    public string Id { get; init; } = null!;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Source { get; init; }
    public string? RelatesTo { get; init; }
    public string? RelatedItem { get; init; }
    public Classification? Classification { get; init; }
    public List<Legislation> Legislation { get; init; } = [];
    public List<RequirementGroup> RequirementGroups { get; init; } = [];
}
