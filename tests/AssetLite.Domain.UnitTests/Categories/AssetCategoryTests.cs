using AssetLite.Domain.Categories;
using AssetLite.Domain.Errors;
using Xunit;

namespace AssetLite.Domain.UnitTests.Categories;

/// <summary>Unit tests for the <see cref="AssetCategory"/> configuration entity.</summary>
public sealed class AssetCategoryTests
{
    [Fact]
    public void Create_WithValidShape_ReturnsTrimmedCategory()
    {
        var result = AssetCategory.Create("  Laptops  ", "  Portable computers.  ", 36);

        Assert.True(result.IsSuccess);
        var category = result.GetValueOrThrow();
        Assert.Equal("Laptops", category.Name);
        Assert.Equal("Portable computers.", category.Description);
        Assert.Equal(36, category.ExpectedLifespanMonths);
        Assert.False(category.Id.IsEmpty);
    }

    [Fact]
    public void Create_WithWhitespaceOnlyDescription_NormalizesToNull()
    {
        var result = AssetCategory.Create("Laptops", "   ", 36);

        Assert.True(result.IsSuccess);
        Assert.Null(result.GetValueOrThrow().Description);
    }

    [Fact]
    public void Create_WithNullDescription_KeepsDescriptionNull()
    {
        var result = AssetCategory.Create("Laptops", null, 36);

        Assert.True(result.IsSuccess);
        Assert.Null(result.GetValueOrThrow().Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsInvalidNameError(string? name)
    {
        var result = AssetCategory.Create(name!, "Description", 36);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidName, result.Error);
    }

    [Fact]
    public void Create_WithNameLongerThanMaxLength_ReturnsInvalidNameError()
    {
        var name = new string('L', AssetCategory.NameMaxLength + 1);

        var result = AssetCategory.Create(name, null, 36);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidName, result.Error);
    }

    [Fact]
    public void Create_WithDescriptionLongerThanMaxLength_ReturnsInvalidDescriptionError()
    {
        var description = new string('D', AssetCategory.DescriptionMaxLength + 1);

        var result = AssetCategory.Create("Laptops", description, 36);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidDescription, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-120)]
    public void Create_WithNonPositiveLifespan_ReturnsInvalidLifespanError(int lifespanMonths)
    {
        var result = AssetCategory.Create("Laptops", null, lifespanMonths);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidLifespan, result.Error);
        Assert.Equal("Category.InvalidLifespan", result.Error!.Code);
    }

    [Fact]
    public void Create_WithLifespanOfOneMonth_Succeeds()
    {
        var result = AssetCategory.Create("Laptops", null, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.GetValueOrThrow().ExpectedLifespanMonths);
    }

    [Fact]
    public void Update_WithValidShape_UpdatesFields()
    {
        var category = AssetCategory.Create("Laptops", "Old description", 36).GetValueOrThrow();

        var result = category.Update("  Notebook Computers  ", " New description ", 48);

        Assert.True(result.IsSuccess);
        Assert.Equal("Notebook Computers", category.Name);
        Assert.Equal("New description", category.Description);
        Assert.Equal(48, category.ExpectedLifespanMonths);
    }

    [Fact]
    public void Update_WithWhitespaceDescription_SetsDescriptionToNull()
    {
        var category = AssetCategory.Create("Laptops", "Old description", 36).GetValueOrThrow();

        var result = category.Update("Laptops", "   ", 36);

        Assert.True(result.IsSuccess);
        Assert.Null(category.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Update_WithNonPositiveLifespan_FailsAndKeepsOriginalValues(int lifespanMonths)
    {
        var category = AssetCategory.Create("Laptops", "Old description", 36).GetValueOrThrow();

        var result = category.Update("Notebooks", "New description", lifespanMonths);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidLifespan, result.Error);
        Assert.Equal("Laptops", category.Name);
        Assert.Equal("Old description", category.Description);
        Assert.Equal(36, category.ExpectedLifespanMonths);
    }

    [Fact]
    public void Update_WithEmptyName_FailsAndKeepsOriginalName()
    {
        var category = AssetCategory.Create("Laptops", null, 36).GetValueOrThrow();

        var result = category.Update("", null, 36);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.InvalidName, result.Error);
        Assert.Equal("Laptops", category.Name);
    }
}
