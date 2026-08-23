using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Assets;

/// <summary>Unit tests for <see cref="SearchAssetsHandler"/>.</summary>
public sealed class SearchAssetsHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IOfficeHierarchy _officeHierarchy = Substitute.For<IOfficeHierarchy>();

    private SearchAssetsHandler CreateHandler() =>
        new(_assetRepository, _officeRepository, _categoryRepository, _officeHierarchy);

    private AssetSearchFilter? _capturedFilter;

    private void ArrangeSearchReturns(IReadOnlyList<Domain.Assets.Asset> items, int total) =>
        _assetRepository
            .SearchAsync(
                Arg.Do<AssetSearchFilter>(filter => _capturedFilter = filter),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => (items, total));

    private void ArrangeNames(IReadOnlyList<Office> offices, IReadOnlyList<AssetCategory> categories)
    {
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns(offices);
        _categoryRepository.ListAsync(Arg.Any<CancellationToken>()).Returns(categories);
    }

    [Fact]
    public async Task HandleAsync_WithoutIncludeDescendants_PassesExactOfficeFilter()
    {
        var office = Office.Create("New York Site", "ASTNYC", null).GetValueOrThrow();
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        ArrangeNames([office], [category]);
        var asset = AssetFactory.NewAsset(tag: TestHarness.Tag(1), categoryId: category.Id, officeId: office.Id);
        ArrangeSearchReturns([asset], 1);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new SearchAssetsQuery(
                OfficeId: office.Id,
                IncludeDescendantOffices: false,
                SearchText: "  latitude  ",
                Page: 2,
                PageSize: 10),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        await _officeHierarchy.DidNotReceive().CollectOfficeAndDescendantsAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>());
        Assert.NotNull(_capturedFilter);
        Assert.Equal(office.Id, _capturedFilter!.OfficeId);
        Assert.Null(_capturedFilter.OfficeIdsIncludingDescendants); // exact office search: no subtree list
        Assert.Equal("latitude", _capturedFilter.SearchText);        // trimmed before hitting persistence
        Assert.Equal(2, _capturedFilter.Page);
        Assert.Equal(10, _capturedFilter.PageSize);
    }

    [Fact]
    public async Task HandleAsync_WithIncludeDescendants_PassesSubtreeListWithPrecedence()
    {
        var root = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", root.Id).GetValueOrThrow();
        var west = Office.Create("West Region", "ASTWEST", root.Id).GetValueOrThrow();
        var subtree = new List<OfficeId> { root.Id, east.Id, west.Id };
        _officeHierarchy
            .CollectOfficeAndDescendantsAsync(root.Id, Arg.Any<CancellationToken>())
            .Returns(subtree);
        ArrangeNames([root, east, west], []);
        ArrangeSearchReturns([], 0);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new SearchAssetsQuery(OfficeId: root.Id, IncludeDescendantOffices: true),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        await _officeHierarchy.Received(1).CollectOfficeAndDescendantsAsync(root.Id, Arg.Any<CancellationToken>());
        Assert.NotNull(_capturedFilter);
        // The subtree list is present; repositories give it precedence over the exact office id.
        Assert.Equal(subtree, _capturedFilter!.OfficeIdsIncludingDescendants);
        Assert.Equal(root.Id, _capturedFilter.OfficeId);
    }

    [Fact]
    public async Task HandleAsync_WithoutOfficeFilter_DoesNotResolveTheHierarchy()
    {
        ArrangeNames([], []);
        ArrangeSearchReturns([], 0);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(new SearchAssetsQuery(IncludeDescendantOffices: true), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        await _officeHierarchy.DidNotReceive().CollectOfficeAndDescendantsAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MapsOfficeAndCategoryNamesOntoListItems()
    {
        var office = Office.Create("New York Site", "ASTNYC", null).GetValueOrThrow();
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var assignedAsset = AssetFactory.NewAsset(tag: TestHarness.Tag(2), categoryId: category.Id, officeId: office.Id);
        assignedAsset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", TestHarness.FixedNow);
        var plainAsset = AssetFactory.NewAsset(tag: TestHarness.Tag(3), categoryId: CategoryId.New(), officeId: OfficeId.New());
        ArrangeNames([office], [category]);
        ArrangeSearchReturns([assignedAsset, plainAsset], 2);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(new SearchAssetsQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Items.Count);
        var first = result.Value.Items[0];
        Assert.Equal("AST-000002", first.Tag);
        Assert.Equal("New York Site", first.OfficeName);
        Assert.Equal("Laptops", first.CategoryName);
        Assert.Equal("Sarah Chen", first.CurrentAssigneeName);
        var second = result.Value.Items[1];
        Assert.Null(second.OfficeName);   // office id not in the name lookup
        Assert.Null(second.CategoryName); // category id not in the name lookup
        Assert.Null(second.CurrentAssigneeName);
    }

    [Fact]
    public async Task HandleAsync_ComputesPaginationMetadataFromTheTotal()
    {
        ArrangeNames([], []);
        ArrangeSearchReturns([], 25);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new SearchAssetsQuery(Page: 2, PageSize: 10),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var page = result.Value;
        Assert.Equal(25, page.Total);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(3, page.TotalPages); // 25 items / 10 per page -> 3 pages
    }

    [Fact]
    public async Task HandleAsync_WithNoMatches_ReturnsEmptyPageWithZeroPages()
    {
        ArrangeNames([], []);
        ArrangeSearchReturns([], 0);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(new SearchAssetsQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.Total);
        Assert.Equal(0, result.Value.TotalPages);
    }
}
