using Microsoft.AspNetCore.Mvc;
using Moq;
using ProzorroAnalytics.API.Controllers;
using ProzorroAnalytics.Application.DTOs.Analytics;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.Tests.Controllers;

public class ProzorroControllerTests
{
    private readonly Mock<IImportJobQueue> _queueMock = new();
    private readonly Mock<IAnalyticsService> _analyticsServiceMock = new();
    private readonly ProzorroController _sut;

    public ProzorroControllerTests()
    {
        _sut = new ProzorroController(_queueMock.Object, _analyticsServiceMock.Object);
    }

    [Fact]
    public void ImportTenders_ReturnsAcceptedWhenJobEnqueued()
    {
        _queueMock.Setup(q => q.TryEnqueue()).Returns(true);

        var result = _sut.ImportTenders();

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public void ImportTenders_ReturnsConflictWhenJobAlreadyRunning()
    {
        _queueMock.Setup(q => q.TryEnqueue()).Returns(false);

        var result = _sut.ImportTenders();

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboard_ReturnsOkWithDashboardData()
    {
        var dashboard = new DashboardDto(
            TotalSavings: 12345.67m,
            TopBuyers: [new TopEntryDto("Ministry", 5000m)],
            TopSuppliers: [new TopEntryDto("Supplier Co", 3000m)]);

        _analyticsServiceMock.Setup(s => s.GetDashboardAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(dashboard);

        var result = await _sut.GetDashboard(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dashboard, ok.Value);
    }

    [Fact]
    public async Task GetDashboard_PassesCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _analyticsServiceMock.Setup(s => s.GetDashboardAsync(token))
                              .ReturnsAsync(new DashboardDto(0m, [], []));

        await _sut.GetDashboard(token);

        _analyticsServiceMock.Verify(s => s.GetDashboardAsync(token), Times.Once);
    }
}
