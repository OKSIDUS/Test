namespace ProzorroAnalytics.Application.DTOs.Analytics;

public record DashboardDto(
    decimal TotalSavings,
    IReadOnlyList<TopEntryDto> TopBuyers,
    IReadOnlyList<TopEntryDto> TopSuppliers
);

public record TopEntryDto(string Name, decimal TotalAmount);
