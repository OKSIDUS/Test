namespace ProzorroAnalytics.Infrastructure.Persistence.Models;

public class TenderContractRow
{
    public long Id { get; init; }
    public string TenderId { get; init; } = null!;
    public string ProzorroId { get; init; } = null!;
    public decimal? ContractAmount { get; init; }
}
