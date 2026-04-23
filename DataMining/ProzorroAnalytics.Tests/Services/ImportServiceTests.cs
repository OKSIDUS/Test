using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProzorroAnalytics.Application.DTOs.Prozorro;
using ProzorroAnalytics.Application.Interfaces.Http;
using ProzorroAnalytics.Application.Interfaces.Repositories;
using ProzorroAnalytics.Application.Options;
using ProzorroAnalytics.Application.Services;
using ProzorroAnalytics.Domain.Models;
using DomainTenderItem = ProzorroAnalytics.Domain.Models.TenderItem;
using AppTenderItem = ProzorroAnalytics.Application.DTOs.Prozorro.TenderItem;

namespace ProzorroAnalytics.Tests.Services;

public class ImportServiceTests
{
    private const string TargetCpv = "09310000-5";
    private const string TargetStatus = "complete";

    private readonly Mock<IProzorroApiClient> _apiMock = new();
    private readonly Mock<IImportRepository> _importRepoMock = new();
    private readonly Mock<IAnalyticsRepository> _analyticsRepoMock = new();
    private readonly FilterOptions _filterOptions = new() { TargetCpv = TargetCpv, TargetStatus = TargetStatus };
    private readonly ImportService _sut;

    public ImportServiceTests()
    {
        _sut = new ImportService(
            _apiMock.Object,
            _importRepoMock.Object,
            _analyticsRepoMock.Object,
            NullLogger<ImportService>.Instance,
            _filterOptions);
    }

    private static Tender MakeMatchingTender(string id, DateTimeOffset dateModified) =>
        new()
        {
            Id = id,
            TenderId = id,
            Title = "Test Tender",
            Status = TargetStatus,
            DateModified = dateModified,
            Value = new MoneyValue { Amount = 1000m },
            ProcuringEntity = new ProcuringEntity { Name = "Buyer1" },
            Items = [new DomainTenderItem { Classification = new Classification { Id = TargetCpv } }],
            Awards = [new Award { Id = "a1", Status = "active", Date = dateModified, Suppliers = [new Supplier { Name = "Supplier1" }] }],
            Contracts = [new Contract { Id = "c1", Status = "active", Value = new MoneyValue { Amount = 900m } }]
        };

    [Fact]
    public async Task ImportTendersAsync_StopsOnNullPage()
    {
        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync((DateTimeOffset?)null);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TenderListPage?)null);

        await _sut.ImportTendersAsync();

        _apiMock.Verify(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                        Times.Never);
        _importRepoMock.Verify(r => r.UpsertTendersAsync(It.IsAny<IEnumerable<Tender>>(), It.IsAny<CancellationToken>()),
                                Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_StopsOnEmptyPage()
    {
        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync((DateTimeOffset?)null);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage { Items = [], Offset = null });

        await _sut.ImportTendersAsync();

        _importRepoMock.Verify(r => r.UpdateSyncStateAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                                Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_DoesNotFetchDetailsForItemsOlderThanCutoff()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var oldDate = cutoff.AddDays(-1).UtcDateTime;

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "old", DateModified = oldDate }],
                    Offset = null
                });

        await _sut.ImportTendersAsync();

        _apiMock.Verify(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
                        Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_FetchesOnlyItemsNewerThanCutoff()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var newDate = cutoff.AddDays(1).UtcDateTime;
        var oldDate = cutoff.AddDays(-1).UtcDateTime;

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items =
                    [
                        new AppTenderItem { Id = "new", DateModified = newDate },
                        new AppTenderItem { Id = "old", DateModified = oldDate }
                    ],
                    Offset = null
                });

        var tender = MakeMatchingTender("new", cutoff.AddDays(1));
        _apiMock.Setup(a => a.GetTendersAsync(
                    It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "new" })),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([tender]);

        await _sut.ImportTendersAsync();

        _apiMock.Verify(
            a => a.GetTendersAsync(
                It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "new" })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportTendersAsync_FiltersOutTendersWithWrongStatus()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var date = cutoff.AddDays(1);

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t1", DateModified = date.UtcDateTime }],
                    Offset = null
                });

        var wrongStatusTender = new Tender
        {
            Id = "t1", TenderId = "t1", Title = "T",
            Status = "active",
            DateModified = date,
            Items = [new DomainTenderItem { Classification = new Classification { Id = TargetCpv } }]
        };
        _apiMock.Setup(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([wrongStatusTender]);

        await _sut.ImportTendersAsync();

        _importRepoMock.Verify(r => r.UpsertTendersAsync(It.IsAny<IEnumerable<Tender>>(), It.IsAny<CancellationToken>()),
                                Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_FiltersOutTendersWithWrongCpv()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var date = cutoff.AddDays(1);

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t1", DateModified = date.UtcDateTime }],
                    Offset = null
                });

        var wrongCpvTender = new Tender
        {
            Id = "t1", TenderId = "t1", Title = "T",
            Status = TargetStatus,
            DateModified = date,
            Items = [new DomainTenderItem { Classification = new Classification { Id = "99999999-9" } }]
        };
        _apiMock.Setup(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([wrongCpvTender]);

        await _sut.ImportTendersAsync();

        _importRepoMock.Verify(r => r.UpsertTendersAsync(It.IsAny<IEnumerable<Tender>>(), It.IsAny<CancellationToken>()),
                                Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_UpsertsMatchingTendersAndUpdatesSyncState()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var tenderDate = cutoff.AddDays(1);

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t1", DateModified = tenderDate.UtcDateTime }],
                    Offset = null
                });

        var tender = MakeMatchingTender("t1", tenderDate);
        _apiMock.Setup(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([tender]);

        await _sut.ImportTendersAsync();

        _importRepoMock.Verify(
            r => r.UpsertTendersAsync(
                It.Is<IEnumerable<Tender>>(tenders => tenders.Any(t => t.Id == "t1")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _importRepoMock.Verify(
            r => r.UpdateSyncStateAsync(tenderDate, It.IsAny<CancellationToken>()),
            Times.Once);
        _analyticsRepoMock.Verify(r => r.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportTendersAsync_SkipsSyncStateAndRefreshWhenNoTendersMatch()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var date = cutoff.AddDays(1);

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t1", DateModified = date.UtcDateTime }],
                    Offset = null
                });
        _apiMock.Setup(a => a.GetTendersAsync(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        await _sut.ImportTendersAsync();

        _importRepoMock.Verify(r => r.UpdateSyncStateAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                                Times.Never);
        _analyticsRepoMock.Verify(r => r.RefreshAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_StopsWhenAllItemsOlderThanCutoff()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var oldDate = cutoff.AddDays(-1).UtcDateTime;

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);
        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "old", DateModified = oldDate }],
                    Offset = "next_page"
                });

        await _sut.ImportTendersAsync();

        _apiMock.Verify(a => a.GetTenderListAsync("next_page", It.IsAny<CancellationToken>()),
                        Times.Never);
    }

    [Fact]
    public async Task ImportTendersAsync_PaginatesAcrossMultiplePages()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-5);
        var date1 = cutoff.AddDays(2);
        var date2 = cutoff.AddDays(1);

        _importRepoMock.Setup(r => r.GetLastSyncDateAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(cutoff);

        _apiMock.Setup(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t1", DateModified = date1.UtcDateTime }],
                    Offset = "page2"
                });
        _apiMock.Setup(a => a.GetTenderListAsync("page2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TenderListPage
                {
                    Items = [new AppTenderItem { Id = "t2", DateModified = date2.UtcDateTime }],
                    Offset = null
                });

        _apiMock.Setup(a => a.GetTendersAsync(
                    It.Is<List<string>>(ids => ids.Contains("t1")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([MakeMatchingTender("t1", date1)]);
        _apiMock.Setup(a => a.GetTendersAsync(
                    It.Is<List<string>>(ids => ids.Contains("t2")),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([MakeMatchingTender("t2", date2)]);

        await _sut.ImportTendersAsync();

        _apiMock.Verify(a => a.GetTenderListAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        _apiMock.Verify(a => a.GetTenderListAsync("page2", It.IsAny<CancellationToken>()), Times.Once);
        _importRepoMock.Verify(
            r => r.UpsertTendersAsync(It.IsAny<IEnumerable<Tender>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _analyticsRepoMock.Verify(r => r.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
