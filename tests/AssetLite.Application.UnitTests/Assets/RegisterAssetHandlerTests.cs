using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using AssetLite.Domain.ValueObjects;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Assets;

/// <summary>Factory helpers for asset handler tests.</summary>
internal static class AssetFactory
{
    public static readonly DateTimeOffset BaseTime = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

    public static Asset NewAsset(
        AssetTag? tag = null,
        CategoryId? categoryId = null,
        OfficeId? officeId = null,
        string name = "Dell Latitude 5540")
    {
        var asset = Asset.Create(
            tag ?? TestHarness.Tag(1),
            categoryId ?? CategoryId.New(),
            officeId ?? OfficeId.New(),
            name,
            AssetCondition.Good,
            BaseTime).GetValueOrThrow();
        asset.PullEvents();
        return asset;
    }

    public static Asset RetiredAsset() => InStatus(AssetStatus.Retired);

    public static Asset AssignedAsset() => InStatus(AssetStatus.Assigned);

    public static Asset InStatus(AssetStatus status)
    {
        var asset = NewAsset();
        switch (status)
        {
            case AssetStatus.InStock:
                break;
            case AssetStatus.Assigned:
                asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", BaseTime.AddDays(1));
                break;
            case AssetStatus.Maintenance:
                asset.StartMaintenance(BaseTime.AddDays(1));
                break;
            case AssetStatus.Retired:
                asset.Retire(BaseTime.AddDays(1));
                break;
            case AssetStatus.Disposed:
                asset.Retire(BaseTime.AddDays(1));
                asset.Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        asset.PullEvents();
        return asset;
    }
}

/// <summary>Unit tests for <see cref="RegisterAssetHandler"/>.</summary>
public sealed class RegisterAssetHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IAssetTagAllocator _tagAllocator = Substitute.For<IAssetTagAllocator>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = TestHarness.FrozenClock();

    private RegisterAssetHandler CreateHandler() =>
        new(_assetRepository, _tagAllocator, _categoryRepository, _officeRepository, _unitOfWork, _clock);

    private (AssetCategory Category, Office Office) ArrangeExistingCategoryAndOffice()
    {
        var category = AssetCategory.Create("Laptops", "Portable computers.", 36).GetValueOrThrow();
        var office = Office.Create("New York Site", "ASTNYC", OfficeId.New()).GetValueOrThrow();
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _officeRepository.GetByIdAsync(office.Id, Arg.Any<CancellationToken>()).Returns(office);
        return (category, office);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_AllocatesTagAndReturnsDetailDto()
    {
        var (category, office) = ArrangeExistingCategoryAndOffice();
        var allocatedTag = TestHarness.Tag(46);
        _tagAllocator.AllocateAsync(Arg.Any<CancellationToken>()).Returns(allocatedTag);
        Asset? staged = null;
        _ = _assetRepository
            .AddAsync(Arg.Do<Asset>(asset => staged = asset), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(
                category.Id,
                office.Id,
                "  Dell Latitude 5540  ",
                AssetCondition.Good,
                Manufacturer: "Dell",
                Model: "Latitude 5540",
                SerialNumber: "5CG1430ZQ2",
                PurchaseCost: 1149.999m,
                Currency: "USD"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("AST-000046", result.Value.Tag);
        Assert.Equal("Dell Latitude 5540", result.Value.Name);
        Assert.Equal(AssetStatus.InStock, result.Value.Status);
        Assert.Equal("New York Site", result.Value.OfficeName);
        Assert.Equal("Laptops", result.Value.CategoryName);
        Assert.Equal(1150.00m, result.Value.PurchaseCostAmount); // 2-dp banker's rounding
        Assert.Equal("USD", result.Value.PurchaseCostCurrency);
        Assert.Equal(TestHarness.FixedNow, result.Value.CreatedAtUtc);

        Assert.NotNull(staged);
        Assert.Equal(allocatedTag, staged!.Tag);
        Assert.Equal(AssetStatus.InStock, staged.Status);
        await _tagAllocator.Received(1).AllocateAsync(Arg.Any<CancellationToken>());
        await _assetRepository.Received(1).AddAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownCategory_ReturnsCategoryNotFound()
    {
        var office = Office.Create("New York Site", "ASTNYC", OfficeId.New()).GetValueOrThrow();
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((AssetCategory?)null);
        _officeRepository.GetByIdAsync(office.Id, Arg.Any<CancellationToken>()).Returns(office);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(CategoryId.New(), office.Id, "Dell Latitude 5540", AssetCondition.Good),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _tagAllocator.DidNotReceive().AllocateAsync(Arg.Any<CancellationToken>());
        await _assetRepository.DidNotReceive().AddAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownOffice_ReturnsOfficeNotFound()
    {
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(category.Id, OfficeId.New(), "Dell Latitude 5540", AssetCondition.Good),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _tagAllocator.DidNotReceive().AllocateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNegativePurchaseCost_ReturnsNegativeAmount()
    {
        var (category, office) = ArrangeExistingCategoryAndOffice();
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(
                category.Id,
                office.Id,
                "Dell Latitude 5540",
                AssetCondition.Good,
                PurchaseCost: -1m),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("Money.NegativeAmount", result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _tagAllocator.DidNotReceive().AllocateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidCurrency_ReturnsInvalidCurrency()
    {
        var (category, office) = ArrangeExistingCategoryAndOffice();
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(
                category.Id,
                office.Id,
                "Dell Latitude 5540",
                AssetCondition.Good,
                PurchaseCost: 10m,
                Currency: "DOLLAR"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("Money.InvalidCurrency", result.FirstError.Code);
    }

    [Fact]
    public async Task HandleAsync_WithValidationFailureFromAggregate_ReturnsDomainError()
    {
        var (category, office) = ArrangeExistingCategoryAndOffice();
        _tagAllocator.AllocateAsync(Arg.Any<CancellationToken>()).Returns(TestHarness.Tag(46));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RegisterAssetCommand(category.Id, office.Id, "   ", AssetCondition.Good),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.InvalidName.Code, result.FirstError.Code);
        await _assetRepository.DidNotReceive().AddAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
