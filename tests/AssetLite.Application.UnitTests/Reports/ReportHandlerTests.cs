using AssetLite.Application.Abstractions;
using AssetLite.Application.Reports;
using AssetLite.Application.UnitTests.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using AssetLite.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Reports;

/// <summary>Unit tests for <see cref="GetInventorySummaryHandler"/> and <see cref="GetAssetRegisterHandler"/>.</summary>
public sealed class ReportHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IDateTimeProvider _clock = TestHarness.FrozenClock();

    private static Asset NewAsset(
        AssetTag tag,
        OfficeId officeId,
        CategoryId categoryId,
        AssetStatus status,
        decimal? cost = null)
    {
        Money? money = cost is { } amount ? Money.Create(amount).GetValueOrThrow() : null;
        var asset = Asset.Create(
            tag,
            categoryId,
            officeId,
            $"Asset {tag.Number}",
            AssetCondition.Good,
            AssetFactory.BaseTime,
            purchaseCost: money).GetValueOrThrow();
        switch (status)
        {
            case AssetStatus.Assigned:
                asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", AssetFactory.BaseTime);
                break;
            case AssetStatus.Maintenance:
                asset.StartMaintenance(AssetFactory.BaseTime);
                break;
            case AssetStatus.Retired:
                asset.Retire(AssetFactory.BaseTime);
                break;
            case AssetStatus.Disposed:
                asset.Retire(AssetFactory.BaseTime);
                asset.Dispose();
                break;
        }

        asset.PullEvents();
        return asset;
    }

    private void ArrangeAssets(params IReadOnlyList<Asset> assets) =>
        _assetRepository
            .SearchAsync(Arg.Any<AssetSearchFilter>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (assets, assets.Count));

    [Fact]
    public async Task GetInventorySummary_ComposesPerOfficeAndPerCategoryTotalsIncludingZeroEntries()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", hq.Id).GetValueOrThrow(); // no assets at all
        var laptops = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var phones = AssetCategory.Create("Phones", null, 24).GetValueOrThrow();    // no assets at all

        var inStock = NewAsset(TestHarness.Tag(1), hq.Id, laptops.Id, AssetStatus.InStock, cost: 100m);
        var assigned = NewAsset(TestHarness.Tag(2), hq.Id, laptops.Id, AssetStatus.Assigned, cost: 250.50m);
        var retired = NewAsset(TestHarness.Tag(3), hq.Id, laptops.Id, AssetStatus.Retired, cost: 49.50m);

        ArrangeAssets(inStock, assigned, retired);
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([hq, east]);
        _categoryRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([laptops, phones]);
        var handler = new GetInventorySummaryHandler(_assetRepository, _officeRepository, _categoryRepository, _clock);

        var result = await handler.HandleAsync(new GetInventorySummaryQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var summary = result.Value;
        Assert.Equal(TestHarness.FixedNow, summary.GeneratedAtUtc);
        Assert.Equal(3, summary.TotalAssets);
        Assert.Equal(400.00m, summary.TotalPurchaseValue);

        Assert.Equal(2, summary.Offices.Count);
        // Offices are ordered by name, so the (asset-less) East Region comes first.
        Assert.Equal(["East Region", "Headquarters"], summary.Offices.Select(office => office.OfficeName));
        var eastSummary = summary.Offices[0];
        Assert.Equal(0, eastSummary.TotalAssets);
        Assert.Equal(0m, eastSummary.TotalPurchaseValue);
        Assert.Equal(0, eastSummary.AssignedCount);

        var hqSummary = summary.Offices[1];
        Assert.Equal("Headquarters", hqSummary.OfficeName);
        Assert.Equal("ASTHQ", hqSummary.OfficeCode);
        Assert.Equal(3, hqSummary.TotalAssets);
        Assert.Equal(1, hqSummary.InStockCount);
        Assert.Equal(1, hqSummary.AssignedCount);
        Assert.Equal(0, hqSummary.MaintenanceCount);
        Assert.Equal(1, hqSummary.RetiredCount);
        Assert.Equal(0, hqSummary.DisposedCount);
        Assert.Equal(400.00m, hqSummary.TotalPurchaseValue);

        Assert.Equal(2, summary.Categories.Count);
        var laptopSummary = summary.Categories[0];
        Assert.Equal("Laptops", laptopSummary.CategoryName);
        Assert.Equal(3, laptopSummary.TotalAssets);
        Assert.Equal(400.00m, laptopSummary.TotalPurchaseValue);

        // Categories without assets appear too.
        var phoneSummary = summary.Categories[1];
        Assert.Equal("Phones", phoneSummary.CategoryName);
        Assert.Equal(0, phoneSummary.TotalAssets);
    }

    [Fact]
    public async Task GetInventorySummary_WithEmptyInventory_ReturnsAllZeroTotals()
    {
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _categoryRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        ArrangeAssets([]);
        var handler = new GetInventorySummaryHandler(_assetRepository, _officeRepository, _categoryRepository, _clock);

        var result = await handler.HandleAsync(new GetInventorySummaryQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(0, result.Value.TotalAssets);
        Assert.Equal(0m, result.Value.TotalPurchaseValue);
        Assert.Empty(result.Value.Offices);
        Assert.Empty(result.Value.Categories);
    }

    [Fact]
    public async Task GetAssetRegister_OrdersRowsByTagNumber()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var laptops = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var tag10 = NewAsset(TestHarness.Tag(10), hq.Id, laptops.Id, AssetStatus.InStock);
        var tag2 = NewAsset(TestHarness.Tag(2), hq.Id, laptops.Id, AssetStatus.Assigned);
        var tag1 = NewAsset(TestHarness.Tag(1), hq.Id, laptops.Id, AssetStatus.InStock);
        ArrangeAssets(tag10, tag2, tag1); // deliberately unsorted input
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([hq]);
        _categoryRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([laptops]);
        var handler = new GetAssetRegisterHandler(_assetRepository, _officeRepository, _categoryRepository);

        var result = await handler.HandleAsync(new GetAssetRegisterQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["AST-000001", "AST-000002", "AST-000010"], result.Value.Select(row => row.Tag));
    }

    [Fact]
    public async Task GetAssetRegister_MapsNamesAndCurrentAssignee()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var laptops = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var assigned = NewAsset(TestHarness.Tag(2), hq.Id, laptops.Id, AssetStatus.Assigned);
        var unknownRefs = NewAsset(TestHarness.Tag(3), OfficeId.New(), CategoryId.New(), AssetStatus.InStock);
        ArrangeAssets(assigned, unknownRefs);
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([hq]);
        _categoryRepository.ListAsync(Arg.Any<CancellationToken>()).Returns([laptops]);
        var handler = new GetAssetRegisterHandler(_assetRepository, _officeRepository, _categoryRepository);

        var result = await handler.HandleAsync(new GetAssetRegisterQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var assignedRow = result.Value[0];
        Assert.Equal("Headquarters", assignedRow.OfficeName);
        Assert.Equal("Laptops", assignedRow.CategoryName);
        Assert.Equal("Sarah Chen", assignedRow.CurrentAssigneeName);
        Assert.Equal("sarah.chen@assetlite.example", assignedRow.CurrentAssigneeEmail);

        // Unknown office/category references degrade to empty names, not nulls.
        var unknownRow = result.Value[1];
        Assert.Equal(string.Empty, unknownRow.OfficeName);
        Assert.Equal(string.Empty, unknownRow.CategoryName);
        Assert.Null(unknownRow.CurrentAssigneeName);
    }
}
