namespace ProzorroAnalytics.Domain.Models;

public class MoneyValue
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "UAH";
    public bool ValueAddedTaxIncluded { get; init; }
}
