namespace ProzorroAnalytics.Domain.Models;

public class RequirementResponse
{
    public string Id { get; init; } = null!;
    public List<string>? Values { get; init; }
    public object? Value { get; init; }
    public RequirementReference? Requirement { get; init; }
    public Classification? Classification { get; init; }
}
