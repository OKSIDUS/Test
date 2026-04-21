namespace ProzorroAnalytics.Domain.Models;

public class RequirementGroup
{
    public string Id { get; init; } = null!;
    public string? Description { get; init; }
    public List<Requirement> Requirements { get; init; } = [];
}
