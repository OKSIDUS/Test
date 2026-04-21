namespace ProzorroAnalytics.Domain.Models;

public class Address
{
    public string? StreetAddress { get; init; }
    public string? Locality { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryName { get; init; }
}
