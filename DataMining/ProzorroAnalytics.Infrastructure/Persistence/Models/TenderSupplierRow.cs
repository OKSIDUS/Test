namespace ProzorroAnalytics.Infrastructure.Persistence.Models;

public class TenderSupplierRow
{
    public long Id { get; init; }
    public string TenderId { get; init; } = null!;
    public string SupplierName { get; init; } = null!;
}
