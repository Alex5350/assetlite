using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using Xunit;

namespace AssetLite.Domain.UnitTests.Offices;

/// <summary>Unit tests for the <see cref="Office"/> aggregate's shape validation and re-parenting.</summary>
public sealed class OfficeTests
{
    private static readonly OfficeId ParentId = OfficeId.New();

    [Fact]
    public void Create_WithValidNameCodeAndParent_ReturnsTrimmedOffice()
    {
        var result = Office.Create("  East Region  ", "ASTEAST", ParentId);

        Assert.True(result.IsSuccess);
        var office = result.GetValueOrThrow();
        Assert.Equal("East Region", office.Name);
        Assert.Equal("ASTEAST", office.Code);
        Assert.Equal(ParentId, office.ParentOfficeId);
        Assert.False(office.Id.IsEmpty);
    }

    [Fact]
    public void Create_WithoutParent_ReturnsRootOffice()
    {
        var result = Office.Create("Headquarters", "ASTHQ", null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.GetValueOrThrow().ParentOfficeId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsInvalidNameError(string? name)
    {
        var result = Office.Create(name!, "ASTHQ", null);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidName, result.Error);
    }

    [Fact]
    public void Create_WithNameLongerThanMaxLength_ReturnsInvalidNameError()
    {
        var name = new string('A', Office.NameMaxLength + 1);

        var result = Office.Create(name, "ASTHQ", null);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidName, result.Error);
    }

    [Fact]
    public void Create_WithNameAtMaxLength_Succeeds()
    {
        var name = new string('A', Office.NameMaxLength);

        var result = Office.Create(name, "ASTHQ", null);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("AB")]      // too short (2 chars)
    [InlineData("abc123")]  // lowercase letters
    [InlineData("AST-HQ")]  // dash not allowed
    [InlineData("AST_HQ")]  // underscore not allowed
    [InlineData("AST HQ")]  // space not allowed
    [InlineData("ÄSTHQ")]   // non-ASCII
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ReturnsInvalidCodeError(string? code)
    {
        var result = Office.Create("East Region", code!, null);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidCode, result.Error);
        Assert.Equal("Office.InvalidCode", result.Error!.Code);
    }

    [Fact]
    public void Create_WithCodeLongerThanMaxLength_ReturnsInvalidCodeError()
    {
        var code = new string('A', Office.CodeMaxLength + 1);

        var result = Office.Create("East Region", code, null);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidCode, result.Error);
    }

    [Theory]
    [InlineData("AST")]           // minimum length
    [InlineData("ASTEAST")]       // mixed length
    [InlineData("ABCDEFGH")]      // maximum length
    [InlineData("12345678")]      // digits only
    [InlineData("AST1")]          // letter + digit mix
    public void Create_WithValidCodeCharset_Succeeds(string code)
    {
        var result = Office.Create("East Region", code, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.GetValueOrThrow().Code);
    }

    [Fact]
    public void Create_TrimsCodeBeforeValidating()
    {
        var result = Office.Create("East Region", "  ASTHQ  ", null);

        Assert.True(result.IsSuccess);
        Assert.Equal("ASTHQ", result.GetValueOrThrow().Code);
    }

    [Fact]
    public void Create_WithEmptyParentId_ReturnsInvalidParentError()
    {
        var result = Office.Create("East Region", "ASTEAST", new OfficeId(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidParent, result.Error);
    }

    [Fact]
    public void Reparent_WithNewParent_UpdatesParentOfficeId()
    {
        var office = Office.Create("East Region", "ASTEAST", null).GetValueOrThrow();
        var newParent = OfficeId.New();

        var result = office.Reparent(newParent);

        Assert.True(result.IsSuccess);
        Assert.Equal(newParent, office.ParentOfficeId);
    }

    [Fact]
    public void Reparent_WithNull_MakesOfficeTheRoot()
    {
        var office = Office.Create("East Region", "ASTEAST", ParentId).GetValueOrThrow();

        var result = office.Reparent(null);

        Assert.True(result.IsSuccess);
        Assert.Null(office.ParentOfficeId);
    }

    [Fact]
    public void Reparent_WithOwnId_ReturnsCannotBeOwnParentError()
    {
        var office = Office.Create("East Region", "ASTEAST", null).GetValueOrThrow();

        var result = office.Reparent(office.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.CannotBeOwnParent, result.Error);
        Assert.Null(office.ParentOfficeId);
    }

    [Fact]
    public void Reparent_WithEmptyId_ReturnsInvalidParentError()
    {
        var office = Office.Create("East Region", "ASTEAST", ParentId).GetValueOrThrow();
        var originalParent = office.ParentOfficeId;

        var result = office.Reparent(new OfficeId(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.InvalidParent, result.Error);
        Assert.Equal(originalParent, office.ParentOfficeId);
    }
}
