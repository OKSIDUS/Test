namespace ProzorroAnalytics.Domain.Models;

public class Tender
{
    public string Id { get; init; } = null!;
    public string TenderId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public string? Owner { get; init; }
    public string? MainProcurementCategory { get; init; }
    public string? ProcurementMethod { get; init; }
    public string? ProcurementMethodType { get; init; }
    public string? AwardCriteria { get; init; }
    public DateTimeOffset Date { get; init; }
    public DateTimeOffset DateCreated { get; init; }
    public DateTimeOffset DateModified { get; init; }
    public DateTimeOffset? NoticePublicationDate { get; init; }

    public MoneyValue? Value { get; init; }
    public TenderPeriod? TenderPeriod { get; init; }
    public ProcuringEntity? ProcuringEntity { get; init; }
    public TenderAgreement? Agreement { get; init; }

    public Dictionary<string, ContractChangeRationaleType> ContractChangeRationaleTypes { get; init; } = [];
    public List<TenderPlan> Plans { get; init; } = [];
    public List<TenderItem> Items { get; init; } = [];
    public List<Document> Documents { get; init; } = [];
    public List<Milestone> Milestones { get; init; } = [];
    public List<Criterion> Criteria { get; init; } = [];
    public List<Bid> Bids { get; init; } = [];
    public List<Award> Awards { get; init; } = [];
    public List<Contract> Contracts { get; init; } = [];
}
