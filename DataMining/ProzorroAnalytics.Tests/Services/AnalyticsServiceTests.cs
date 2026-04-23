using Moq;
using ProzorroAnalytics.Application.DTOs.Analytics;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Services;

namespace ProzorroAnalytics.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsRepository> _repoMock = new();
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _sut = new AnalyticsService(_repoMock.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsDtoWithAllFieldsMapped()
    {
        var buyers = new List<TopEntryDto> { new("Buyer1", 500m), new("Buyer2", 300m) };
        var suppliers = new List<TopEntryDto> { new("Supplier1", 200m) };

        _repoMock.Setup(r => r.GetBudgetSavingsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1000m);
        _repoMock.Setup(r => r.GetTopBuyersAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(buyers);
        _repoMock.Setup(r => r.GetTopSuppliersAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(suppliers);

        var result = await _sut.GetDashboardAsync();

        Assert.Equal(1000m, result.TotalSavings);
        Assert.Same(buyers, result.TopBuyers);
        Assert.Same(suppliers, result.TopSuppliers);
    }

    [Fact]
    public async Task GetDashboardAsync_PassesCancellationTokenToAllRepositoryMethods()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repoMock.Setup(r => r.GetBudgetSavingsAsync(token)).ReturnsAsync(0m);
        _repoMock.Setup(r => r.GetTopBuyersAsync(token)).ReturnsAsync(new List<TopEntryDto>());
        _repoMock.Setup(r => r.GetTopSuppliersAsync(token)).ReturnsAsync(new List<TopEntryDto>());

        await _sut.GetDashboardAsync(token);

        _repoMock.Verify(r => r.GetBudgetSavingsAsync(token), Times.Once);
        _repoMock.Verify(r => r.GetTopBuyersAsync(token), Times.Once);
        _repoMock.Verify(r => r.GetTopSuppliersAsync(token), Times.Once);
    }

    [Fact]
    public async Task GetDashboardAsync_CallsAllThreeRepositoryMethods()
    {
        _repoMock.Setup(r => r.GetBudgetSavingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        _repoMock.Setup(r => r.GetTopBuyersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TopEntryDto>());
        _repoMock.Setup(r => r.GetTopSuppliersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TopEntryDto>());

        await _sut.GetDashboardAsync();

        _repoMock.Verify(r => r.GetBudgetSavingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.GetTopBuyersAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.GetTopSuppliersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
