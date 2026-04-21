namespace ProzorroAnalytics.Domain.Models;

public class Bid
{
    public string Id { get; init; } = null!;
    public DateTimeOffset Date { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? SubmissionDate { get; init; }
    public MoneyValue? Value { get; init; }
    public MoneyValue? InitialValue { get; init; }
    public List<Supplier> Tenderers { get; init; } = [];
    public List<Document> Documents { get; init; } = [];
    public List<BidItem> Items { get; init; } = [];
    public List<RequirementResponse> RequirementResponses { get; init; } = [];
}
