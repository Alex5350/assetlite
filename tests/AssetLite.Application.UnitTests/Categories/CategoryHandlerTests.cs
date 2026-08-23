using AssetLite.Application.Abstractions;
using AssetLite.Application.Categories;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Categories;

/// <summary>Unit tests for the category command and query handlers.</summary>
public sealed class CategoryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateCategory_WithValidShape_ReturnsDtoAndPersists()
    {
        _categoryRepository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        AssetCategory? staged = null;
        _ = _categoryRepository
            .AddAsync(Arg.Do<AssetCategory>(category => staged = category), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = new CreateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new CreateCategoryCommand("  Tablets  ", "  iPads and Android tablets.  ", 36),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Tablets", result.Value.Name);
        Assert.Equal("iPads and Android tablets.", result.Value.Description);
        Assert.Equal(36, result.Value.ExpectedLifespanMonths);
        Assert.NotNull(staged);
        Assert.Equal("Tablets", staged!.Name);
        await _categoryRepository.Received(1).AddAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsDuplicateName()
    {
        _categoryRepository
            .NameExistsAsync("Laptops", Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new CreateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new CreateCategoryCommand("Laptops", null, 36),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.DuplicateName.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _categoryRepository.DidNotReceive().AddAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_WithInvalidLifespan_ReturnsInvalidLifespan()
    {
        _categoryRepository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new CreateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new CreateCategoryCommand("Laptops", null, 0),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidLifespan.Code, result.FirstError.Code);
        await _categoryRepository.DidNotReceive().AddAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WithExistingCategory_UpdatesAndReturnsDto()
    {
        var category = AssetCategory.Create("Laptops", "Old description.", 36).GetValueOrThrow();
        _categoryRepository
            .GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository
            .NameExistsAsync("Notebook Computers", category.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new UpdateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new UpdateCategoryCommand(category.Id, "  Notebook Computers  ", "New description.", 48),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Notebook Computers", result.Value.Name);
        Assert.Equal("New description.", result.Value.Description);
        Assert.Equal(48, result.Value.ExpectedLifespanMonths);
        Assert.Equal("Notebook Computers", category.Name);
        await _categoryRepository.Received(1).UpdateAsync(category, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WithUnknownCategory_ReturnsCategoryNotFound()
    {
        _categoryRepository
            .GetByIdAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns((AssetCategory?)null);
        var handler = new UpdateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new UpdateCategoryCommand(CategoryId.New(), "Laptops", null, 36),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _categoryRepository.DidNotReceive().UpdateAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WithDuplicateName_ReturnsDuplicateName()
    {
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        _categoryRepository
            .GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository
            .NameExistsAsync("Monitors", category.Id, Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new UpdateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new UpdateCategoryCommand(category.Id, "Monitors", null, 48),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.DuplicateName.Code, result.FirstError.Code);
        Assert.Equal("Laptops", category.Name); // unchanged
        await _categoryRepository.DidNotReceive().UpdateAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WithInvalidShape_ReturnsDomainErrorAndSkipsPersistence()
    {
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        _categoryRepository
            .GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);
        _categoryRepository
            .NameExistsAsync(Arg.Any<string>(), Arg.Any<CategoryId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new UpdateCategoryHandler(_categoryRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new UpdateCategoryCommand(category.Id, "", null, 36),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidName.Code, result.FirstError.Code);
        await _categoryRepository.DidNotReceive().UpdateAsync(Arg.Any<AssetCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCategories_ReturnsAllCategoriesOrderedByName()
    {
        var laptops = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();
        var tablets = AssetCategory.Create("Tablets", null, 36).GetValueOrThrow();
        var monitors = AssetCategory.Create("Monitors", null, 48).GetValueOrThrow();
        _categoryRepository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns([tablets, laptops, monitors]); // deliberately unsorted input
        var handler = new ListCategoriesHandler(_categoryRepository);

        var result = await handler.HandleAsync(new ListCategoriesQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["Laptops", "Monitors", "Tablets"], result.Value.Select(category => category.Name));
    }
}
