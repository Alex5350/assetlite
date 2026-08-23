using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Assets;

/// <summary>Unit tests for <see cref="GetAssetByTagHandler"/>.</summary>
public sealed class GetAssetByTagHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();

    private GetAssetByTagHandler CreateHandler() =>
        new(_assetRepository, _officeRepository, _categoryRepository);

    [Fact]
    public async Task HandleAsync_WithKnownTag_ReturnsDetailWithNamesAndNewestAssignmentFirst()
    {
        var office = Office.Create("New York Site", "ASTNYC", null).GetValueOrThrow();
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var asset = AssetFactory.NewAsset(tag: TestHarness.Tag(1), categoryId: category.Id, officeId: office.Id);
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", AssetFactory.BaseTime);
        asset.AssignTo("Marcus Webb", "marcus.webb@assetlite.example", AssetFactory.BaseTime.AddDays(10));
        _assetRepository
            .GetByTagAsync(asset.Tag, Arg.Any<CancellationToken>())
            .Returns(asset);
        _officeRepository.GetByIdAsync(office.Id, Arg.Any<CancellationToken>()).Returns(office);
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetAssetByTagQuery("AST-000001"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var detail = result.Value;
        Assert.Equal("AST-000001", detail.Tag);
        Assert.Equal("Dell Latitude 5540", detail.Name);
        Assert.Equal(AssetStatus.Assigned, detail.Status);
        Assert.Equal("New York Site", detail.OfficeName);
        Assert.Equal("Laptops", detail.CategoryName);
        Assert.Equal("Marcus Webb", detail.CurrentAssigneeName);
        Assert.Equal("marcus.webb@assetlite.example", detail.CurrentAssigneeEmail);
        Assert.Equal(2, detail.Assignments.Count);
        Assert.Equal("Marcus Webb", detail.Assignments[0].AssigneeName); // newest first
        Assert.Equal("Sarah Chen", detail.Assignments[1].AssigneeName);
        Assert.Null(detail.Assignments[0].ReturnedAtUtc); // current assignment still open
        Assert.NotNull(detail.Assignments[1].ReturnedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownOfficeOrCategory_LeavesNamesNull()
    {
        var asset = AssetFactory.NewAsset(tag: TestHarness.Tag(7));
        _assetRepository
            .GetByTagAsync(asset.Tag, Arg.Any<CancellationToken>())
            .Returns(asset);
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((AssetCategory?)null);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetAssetByTagQuery(asset.Tag.Value),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Null(result.Value.OfficeName);
        Assert.Null(result.Value.CategoryName);
    }

    [Fact]
    public async Task HandleAsync_WithMalformedTag_ReturnsInvalidAssetTag()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetAssetByTagQuery("not-a-tag"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("AssetTag.Invalid", result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _assetRepository.DidNotReceive().GetByTagAsync(Arg.Any<Domain.ValueObjects.AssetTag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownTag_ReturnsAssetNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GetAssetByTagQuery("AST-999999"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("Asset.NotFound", result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
